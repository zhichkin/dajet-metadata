using DaJet.Metadata.Model;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DaJet.Metadata
{
    public interface IMetadataProvider
    {
        int ReadPlatformRequiredVersion();
        void SaveConfigToFile(string filePath);
        ConfigInfo ReadConfigurationProperties();

        Stream GetDBNamesFromDatabase();
        Dictionary<string, DBNameEntry> ParseDBNames(Stream stream);
        List<string[]> ParseDBNamesOptimized(Stream stream);

        void LoadDBNames(Dictionary<string, DBNameEntry> dbnames);
        InfoBase LoadInfoBase();
        void UseConnectionString(string connectionString);
        void UseConnectionParameters(string server, string database, string username, string password);
        MetaObject LoadMetaObject(string typeName, string objectName);
    }

    internal delegate InfoBase DoWork(out string errorMessage);
    public sealed class MetadataProvider : IMetadataProvider
    {
        private InfoBase InfoBase { get; set; }
        private Dictionary<string, DBNameEntry> DBNames = new Dictionary<string, DBNameEntry>();
        private Dictionary<string, DBNameEntry> DBFields = new Dictionary<string, DBNameEntry>();
        private ConcurrentDictionary<string, MetaObject> ReferenceTypes = new ConcurrentDictionary<string, MetaObject>();

        

        private string ConnectionString { get; set; }

        public MetadataProvider()
        {
            ConfigureSpecialParsers();
        }

        public void UseConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
        }
        public void UseConnectionParameters(string server, string database, string username, string password)
        {
            SqlConnectionStringBuilder connection = new SqlConnectionStringBuilder()
            {
                DataSource = server,
                InitialCatalog = database
            };
            if (!string.IsNullOrWhiteSpace(username))
            {
                connection.UserID = username;
                connection.Password = password;
            }
            connection.IntegratedSecurity = string.IsNullOrWhiteSpace(username);
            UseConnectionString(connection.ToString());
        }

        #region "Work with SqlConnection"

        private void DisposeDatabaseResources(SqlConnection connection, SqlCommand command, SqlDataReader reader)
        {
            if (reader != null)
            {
                if (!reader.IsClosed && reader.HasRows)
                {
                    if (command != null) command.Cancel();
                }
                reader.Dispose();
            }
            if (command != null) command.Dispose();
            if (connection != null) connection.Dispose();
        }

        #endregion

        #region "Read root file and configuration properties"

        public int ReadPlatformRequiredVersion()
        {
            int version = 0;
            {
                SqlConnection connection = new SqlConnection(ConnectionString);
                SqlCommand command = connection.CreateCommand();
                command.CommandType = CommandType.Text;
                command.CommandText = "SELECT TOP 1 [PlatformVersionReq] FROM [IBVersion];";
                try
                {
                    connection.Open();
                    version = (int)command.ExecuteScalar();
                }
                catch { throw; }
                finally { DisposeDatabaseResources(connection, command, null); }
            }
            return version;
        }

        private string ReadConfigFile(string fileName)
        {
            string content = string.Empty;

            SqlBytes binaryData = ReadConfigFromDatabase(fileName);
            if (binaryData == null)
            {
                return content;
            }
            
            using (DeflateStream stream = new DeflateStream(binaryData.Stream, CompressionMode.Decompress))
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                content = reader.ReadToEnd();
            }

            return content;
        }
        public void SaveConfigToFile(string filePath)
        {
            string fileName = GetConfigurationFileName();
            string metadata = ReadConfigFile(fileName);
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.Write(metadata);
            }
        }
        private string GetConfigurationFileName()
        {
            string rootContent = ReadConfigFile("root");
            string[] lines = rootContent.Split(',');
            string uuid = lines[1];
            return uuid;
        }
        
        #endregion

        public void LoadDBNames(Dictionary<string, DBNameEntry> dbnames)
        {
            SqlBytes binaryData = SelectDBNamesFromDatabase();

            if (binaryData == null) return;

            DeflateStream stream = new DeflateStream(binaryData.Stream, CompressionMode.Decompress);

            ParseDBNames(stream, dbnames);
        }
        public InfoBase LoadInfoBase()
        {
            InfoBase = new InfoBase();
            
            DBNames.Clear();
            DBFields.Clear();
            ReferenceTypes.Clear();

            LoadDBNames(DBNames);
            if (DBNames.Count == 0) { return InfoBase; }

            LoadReferenceTypes();

            InfoBase.Name = $"{ReferenceTypes.Count} reference types loaded.";

            LoadMetaObjects();

            InfoBase.Name += Environment.NewLine + $"{DBNames.Count} metaobjects loaded.";

            return InfoBase;
        }
        private void LoadMetaObjects()
        {
            Task[] tasks = new Task[DBNames.Count];
            for (int i = 0; i < tasks.Length; i++)
            {
                MetaObject metaObject = DBNames.Values.ElementAt(i).MetaObject;
                tasks[i] = Task.Factory.StartNew(LoadMetaObject, metaObject,
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);
            }

            try
            {
                Task.WaitAll(tasks);
            }
            catch (AggregateException ex)
            {
                foreach (Exception ie in ex.InnerExceptions)
                {
                    if (!string.IsNullOrEmpty(InfoBase.Alias))
                    {
                        InfoBase.Alias += Environment.NewLine + Environment.NewLine;
                    }
                    InfoBase.Alias += ie.Message;
                }
            }
        }
        private void LoadMetaObject(object parameters)
        {
            if (!(parameters is MetaObject metaObject)) return;

            ReadConfig(metaObject.UUID.ToString(), metaObject);
            AttachMetaObjectToInfoBase(metaObject);

        }
        private void AttachMetaObjectToInfoBase(MetaObject metaObject)
        {
            if (InfoBase == null) return;

            if (string.IsNullOrWhiteSpace(metaObject.Name))
            {
                return; // error in DBNames file - metaobject is not present in database
            }

            ConcurrentDictionary<string, MetaObject> objects = typeof(InfoBase)
                .GetProperty(metaObject.TypeName + "s")
                ?.GetValue(InfoBase) as ConcurrentDictionary<string, MetaObject>;

            if (objects == null) return;

            _ = objects.TryAdd(metaObject.Name, metaObject);
        }



        public MetaObject LoadMetaObject(string typeName, string objectName)
        {
            if (InfoBase == null)
            {
                LoadInfoBase();
            }

            ConcurrentDictionary<string, MetaObject> objects = typeof(InfoBase)
                .GetProperty(typeName + "s")
                ?.GetValue(InfoBase) as ConcurrentDictionary<string, MetaObject>;

            if (objects == null) return null;

            if (objects.TryGetValue(objectName, out MetaObject metaObject))
            {
                if (metaObject.Properties
                    .Where(p => p.Fields.Count == 0).Count() == metaObject.Properties.Count)
                {
                    (new SQLMetadataReader()).Load(ConnectionString, metaObject);
                }
            }

            return metaObject;
        }
        


        private void SaveConfigToFile(Stream stream, MetaObject metaObject)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string appCatalogPath = Path.GetDirectoryName(asm.Location);

            string filePath = Path.Combine(appCatalogPath, metaObject.TypeName + "_" + metaObject.TypeCode + ".txt");
            using (FileStream output = File.Create(filePath))
            {
                stream.CopyTo(output);
            }
        }

        private bool IsTokenLoadingAllowed(string token)
        {
            return token == MetadataTokens.Acc
                || token == MetadataTokens.Enum
                || token == MetadataTokens.Chrc
                || token == MetadataTokens.Node
                || token == MetadataTokens.Const
                || token == MetadataTokens.AccRg
                || token == MetadataTokens.InfoRg
                || token == MetadataTokens.AccumRg
                || token == MetadataTokens.Document
                || token == MetadataTokens.Reference
                || (token.EndsWith(MetadataTokens.ChngR) && !token.StartsWith(MetadataTokens.Config));
        }
        private string MapTokenToTypeName(string token)
        {
            if (token == MetadataTokens.Acc) return MetaObjectTypes.Account;
            else if (token == MetadataTokens.Enum) return MetaObjectTypes.Enumeration;
            else if (token == MetadataTokens.Node) return MetaObjectTypes.Publication;
            else if (token == MetadataTokens.Chrc) return MetaObjectTypes.Characteristic;
            else if (token == MetadataTokens.Const) return MetaObjectTypes.Constant;
            else if (token == MetadataTokens.AccRg) return MetaObjectTypes.AccountingRegister;
            else if (token == MetadataTokens.InfoRg) return MetaObjectTypes.InformationRegister;
            else if (token == MetadataTokens.AccumRg) return MetaObjectTypes.AccumulationRegister;
            else if (token == MetadataTokens.Document) return MetaObjectTypes.Document;
            else if (token == MetadataTokens.Reference) return MetaObjectTypes.Catalog;
            else return MetaObjectTypes.Unknown;
        }

        #region "DBNames"

        public Stream GetDBNamesFromDatabase()
        {
            SqlBytes binaryData = SelectDBNamesFromDatabase();

            if (binaryData == null) return null;

            return new DeflateStream(binaryData.Stream, CompressionMode.Decompress);
        }
        public Dictionary<string, DBNameEntry> ParseDBNames(Stream stream)
        {
            Dictionary<string, DBNameEntry> dbnames = new Dictionary<string, DBNameEntry>();

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                string line = reader.ReadLine();
                if (line != null)
                {
                    //int capacity = GetDBNamesCapacity(line);
                    while ((line = reader.ReadLine()) != null)
                    {
                        ParseDBNameLine(line, dbnames);
                    }
                }
            }

            return dbnames;
        }
        public List<string[]> ParseDBNamesOptimized(Stream stream)
        {
            if (stream == null || !stream.CanRead) return null;

            int readBytes = 0;
            int lineNumber = 0;
            int position, length, index, commaCount, quoteCount;
            //char[] buffer = new char[8192];
            List<string[]> result = new List<string[]>();

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, false))
            {
                #region "Version 1"

                //readBytes = reader.Read(buffer, 0, 512);

                //while (readBytes > 0)
                //{
                //    length = 0;
                //    position = 0;
                //    commaCount = 0;
                //    quoteCount = 0;
                //    string[] values = new string[3];

                //    for (index = 0; index < readBytes; index++)
                //    {
                //        if (buffer[index] == '{') // beginning of object
                //        {
                //            length = 0;
                //            position = index + 1;
                //        }
                //        else if (buffer[index] == '}') // end of object
                //        {
                //            if (length > 0)
                //            {
                //                values[commaCount] = new string(buffer, position, length);
                //                length = 0;
                //                position = index + 1;
                //            }
                //        }
                //        else if (buffer[index] == '"' && quoteCount == 0) // beginning of string value
                //        {
                //            quoteCount++;
                //            length = 0;
                //            position = index + 1;
                //        }
                //        else if (buffer[index] == '"' && quoteCount == 1) // end of string value
                //        {
                //            quoteCount = 0;
                //            if (length > 0)
                //            {
                //                values[commaCount] = new string(buffer, position, length);
                //                length = 0;
                //                position = index + 1;
                //            }
                //        }
                //        else if (buffer[index] == ',')
                //        {
                //            if (length > 0)
                //            {
                //                values[commaCount] = new string(buffer, position, length);
                //                length = 0;
                //            }
                //            if (commaCount == 2)
                //            {
                //                commaCount = 0;
                //            }
                //            else
                //            {
                //                commaCount++;
                //            }
                //            position = index + 1;
                //        }
                //        else if (buffer[index] == '\r')
                //        {
                //            // new line
                //        }
                //        else if (buffer[index] == '\n')
                //        {
                //            // new line
                //            lineNumber++;
                //            commaCount = 0;
                //            //result.Add(values);
                //            values = new string[3];
                //        }
                //        else
                //        {
                //            length++;
                //        }
                //    }

                //    readBytes = reader.Read(buffer, 0, 512);
                //}

                #endregion

                #region "Version 2"

                string buffer = reader.ReadToEnd();

                //length = 0;
                //position = 0;
                //commaCount = 0;
                //quoteCount = 0;
                //string[] values = new string[3];

                //for (index = 0; index < buffer.Length; index++)
                //{
                //    if (buffer[index] == '{') // beginning of object
                //    {
                //        length = 0;
                //        position = index + 1;
                //    }
                //    else if (buffer[index] == '}') // end of object
                //    {
                //        if (length > 0)
                //        {
                //            values[commaCount] = buffer.Substring(position, length);
                //            length = 0;
                //            position = index + 1;
                //        }
                //    }
                //    else if (buffer[index] == '"' && quoteCount == 0) // beginning of string value
                //    {
                //        quoteCount++;
                //        length = 0;
                //        position = index + 1;
                //    }
                //    else if (buffer[index] == '"' && quoteCount == 1) // end of string value
                //    {
                //        quoteCount = 0;
                //        if (length > 0)
                //        {
                //            values[commaCount] = buffer.Substring(position, length);
                //            length = 0;
                //            position = index + 1;
                //        }
                //    }
                //    else if (buffer[index] == ',')
                //    {
                //        if (length > 0)
                //        {
                //            values[commaCount] = buffer.Substring(position, length);
                //            length = 0;
                //        }
                //        if (commaCount == 2)
                //        {
                //            commaCount = 0;
                //        }
                //        else
                //        {
                //            commaCount++;
                //        }
                //        position = index + 1;
                //    }
                //    else if (buffer[index] == '\r')
                //    {
                //        // new line
                //    }
                //    else if (buffer[index] == '\n')
                //    {
                //        // new line
                //        lineNumber++;
                //        commaCount = 0;
                //        result.Add(values);
                //        values = new string[3];
                //    }
                //    else
                //    {
                //        length++;
                //    }
                //}

                #endregion
            }

            return result;
        }

        private SqlBytes SelectDBNamesFromDatabase()
        {
            SqlBytes binaryData = null;
            {
                SqlConnection connection = new SqlConnection(ConnectionString);
                SqlCommand command = connection.CreateCommand();
                SqlDataReader reader = null;
                command.CommandType = CommandType.Text;
                command.CommandText = "SELECT [BinaryData] FROM [Params] WHERE [FileName] = N'DBNames'";
                try
                {
                    connection.Open();
                    reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        binaryData = reader.GetSqlBytes(0);
                    }
                    reader.Close();
                }
                catch { throw; }
                finally { DisposeDatabaseResources(connection, command, reader); }
            }
            return binaryData;
        }
        private void ParseDBNames(Stream stream, Dictionary<string, DBNameEntry> dbnames)
        {
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                string line = reader.ReadLine();
                if (line != null)
                {
                    //int capacity = GetDBNamesCapacity(line);
                    while ((line = reader.ReadLine()) != null)
                    {
                        ParseDBNameLine(line, dbnames);
                    }
                }
            }
        }
        private int GetDBNamesCapacity(string line)
        {
            return int.Parse(line.Replace("{", string.Empty).Replace(",", string.Empty));
        }
        private void ParseDBNameLine(string line, Dictionary<string, DBNameEntry> dbnames)
        {
            string[] items = line.Split(',');
            if (items.Length < 3) return;

            string uuid = items[0].Substring(1); // .Replace("{", string.Empty);
            if (new Guid(uuid) == Guid.Empty) return; // system meta object - global settings etc.

            string token = items[1].Trim('\"');
            if (token == MetadataTokens.Fld)
            {
                if (DBFields.ContainsKey(uuid)) return;

                DBNameEntry Fld = new DBNameEntry();
                Fld.DBNames.Add(new DBName()
                {
                    Token = token,
                    TypeCode = int.Parse(items[2].TrimEnd('}')),
                    IsMainTable = false
                });
                DBFields.Add(uuid, Fld);
                
                return;
            }
            
            if (!IsTokenLoadingAllowed(token)) return;

            DBName dbname = new DBName()
            {
                Token = token,
                TypeCode = int.Parse(items[2].TrimEnd('}')) // .Replace("}", string.Empty))
            };
            dbname.IsMainTable = IsMainTable(dbname.Token);

            if (!dbnames.TryGetValue(uuid, out DBNameEntry entry))
            {
                entry = new DBNameEntry();
                dbnames.Add(uuid, entry);
            }
            entry.DBNames.Add(dbname);

            if (dbname.IsMainTable)
            {
                entry.MetaObject.UUID = new Guid(uuid);
                entry.MetaObject.TypeName = MapTokenToTypeName(dbname.Token);
                entry.MetaObject.TypeCode = dbname.TypeCode;
                entry.MetaObject.TableName = CreateTableName(entry.MetaObject, dbname);
            }
        }
        private bool IsMainTable(string token)
        {
            return token == MetadataTokens.Acc
                || token == MetadataTokens.Enum
                || token == MetadataTokens.Node
                || token == MetadataTokens.Chrc
                || token == MetadataTokens.Const
                || token == MetadataTokens.AccRg
                || token == MetadataTokens.InfoRg
                || token == MetadataTokens.AccumRg
                || token == MetadataTokens.Document
                || token == MetadataTokens.Reference;
        }
        private string CreateTableName(MetaObject metaObject, DBName dbname)
        {
            if (dbname.Token == MetadataTokens.VT)
            {
                if (metaObject.Owner == null)
                {
                    return string.Empty;
                }
                else
                {
                    return $"{metaObject.Owner.TableName}_{dbname.Token}{dbname.TypeCode}";
                }
            }
            else
            {
                return $"_{dbname.Token}{dbname.TypeCode}";
            }
        }
        private string CreateMetaFieldName(DBName dbname)
        {
            return $"{dbname.Token}{dbname.TypeCode}";
        }

        #endregion

        #region "Reference Types"

        private void LoadReferenceTypes()
        {
            IEnumerable<DBNameEntry> entries = DBNames.Values
                .Where(v => v.MetaObject.IsReferenceType);

            if (entries.Count() == 0) return;

            Task[] tasks = new Task[entries.Count()];
            for (int i = 0; i < tasks.Length; i++)
            {
                MetaObject metaObject = entries.ElementAt(i).MetaObject;
                tasks[i] = Task.Factory.StartNew(LoadReferenceType, metaObject,
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);
            }

            try
            {
                Task.WaitAll(tasks);
            }
            catch (AggregateException ex)
            {
                foreach (Exception ie in ex.InnerExceptions)
                {
                    if (ie is OperationCanceledException)
                    {
                        //Console.WriteLine("The word scrambling operation has been cancelled.");
                        //break;
                    }
                    else
                    {
                        //Console.WriteLine(ie.GetType().Name + ": " + ie.Message);
                    }
                }
            }
        }
        private void LoadReferenceType(object metaObject)
        {
            string uuid = GetReferenceTypeIdentifier((MetaObject)metaObject);
            _ = ReferenceTypes.TryAdd(uuid, (MetaObject)metaObject);
        }
        private string GetReferenceTypeIdentifier(MetaObject metaObject)
        {
            SqlBytes binaryData = ReadConfigFromDatabase(metaObject.UUID.ToString());
            if (binaryData == null) return string.Empty;

            DeflateStream stream = new DeflateStream(binaryData.Stream, CompressionMode.Decompress);

            string uuid = string.Empty;
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                _ = reader.ReadLine(); // 1. line
                string line = reader.ReadLine(); // 2. line
                uuid = ParseReferenceTypeIdentifier(line, metaObject);
            }
            return uuid;
        }

        #endregion

        #region " Read Config "

        // Структура ссылки на объект метаданных
        // {"#",157fa490-4ce9-11d4-9415-008048da11f9, - идентификатор класса объекта метаданных
        // {1,fd8fe814-97e6-42d3-a042-b1e429cfb067}   - внутренний идентификатор объекта метаданных
        // }

        internal delegate void SpecialParser(StreamReader reader, string line, MetaObject metaObject);

        private readonly Regex rxUUID = new Regex("[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}"); // Example: eb3dfdc7-58b8-4b1f-b079-368c262364c9
        private readonly Regex rxSpecialUUID = new Regex("^{[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12},\\d+(?:})?,$"); // Example: {3daea016-69b7-4ed4-9453-127911372fe6,0}, | {cf4abea7-37b2-11d4-940f-008048da11f9,5,
        private readonly Regex rxDbName = new Regex("^{\\d,\\d,[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}},\"\\w+\",$"); // Example: {0,0,3df19dbf-efe7-4e31-99ad-fafb59ec1329},"Размещение",
        private readonly Regex rxDbType = new Regex("^{\"[#BSDN]\""); // Example: {"#",1aaea747-a4ba-4fb2-9473-075b1ced620c}, | {"B"}, | {"S",10,0}, | {"D","T"}, | {"N",10,0,1}
        private readonly Regex rxNestedProperties = new Regex("^{888744e1-b616-11d4-9436-004095e12fc7,\\d+[},]$"); // look rxSpecialUUID
        private readonly Dictionary<string, SpecialParser> _SpecialParsers = new Dictionary<string, SpecialParser>();

        private void ConfigureSpecialParsers()
        {
            _SpecialParsers.Add("cf4abea7-37b2-11d4-940f-008048da11f9", ParseMetaObjectProperties); // Catalogs properties collection
            _SpecialParsers.Add("932159f9-95b2-4e76-a8dd-8849fe5c5ded", ParseNestedObjects); // Catalogs nested objects collection

            _SpecialParsers.Add("45e46cbc-3e24-4165-8b7b-cc98a6f80211", ParseMetaObjectProperties); // Documents properties collection
            _SpecialParsers.Add("21c53e09-8950-4b5e-a6a0-1054f1bbc274", ParseNestedObjects); // Documents nested objects collection

            _SpecialParsers.Add("31182525-9346-4595-81f8-6f91a72ebe06", ParseMetaObjectProperties); // Коллекция реквизитов плана видов характеристик
            _SpecialParsers.Add("54e36536-7863-42fd-bea3-c5edd3122fdc", ParseNestedObjects); // Коллекция табличных частей плана видов характеристик

            _SpecialParsers.Add("1a1b4fea-e093-470d-94ff-1d2f16cda2ab", ParseMetaObjectProperties); // Коллекция реквизитов плана обмена
            _SpecialParsers.Add("52293f4b-f98c-43ea-a80f-41047ae7ab58", ParseNestedObjects); // Коллекция табличных частей плана обмена

            _SpecialParsers.Add("13134203-f60b-11d5-a3c7-0050bae0a776", ParseMetaObjectDimensions); // Коллекция измерений регистра сведений
            _SpecialParsers.Add("13134202-f60b-11d5-a3c7-0050bae0a776", ParseMetaObjectMeasures); // Коллекция ресурсов регистра сведений
            _SpecialParsers.Add("a2207540-1400-11d6-a3c7-0050bae0a776", ParseMetaObjectProperties); // Коллекция реквизитов регистра сведений

            _SpecialParsers.Add("b64d9a43-1642-11d6-a3c7-0050bae0a776", ParseMetaObjectDimensions); // Коллекция измерений регистра накопления
            _SpecialParsers.Add("b64d9a41-1642-11d6-a3c7-0050bae0a776", ParseMetaObjectMeasures); // Коллекция ресурсов регистра накопления
            _SpecialParsers.Add("b64d9a42-1642-11d6-a3c7-0050bae0a776", ParseMetaObjectProperties); // Коллекция реквизитов регистра накопления

            _SpecialParsers.Add("6e65cbf5-daa8-4d8d-bef8-59723f4e5777", ParseMetaObjectProperties); // Коллекция реквизитов плана счетов
            _SpecialParsers.Add("78bd1243-c4df-46c3-8138-e147465cb9a4", ParseMetaObjectProperties); // Коллекция признаков учёта плана счетов
            // Коллекция признаков учёта субконто плана счетов - не имеет полей в таблице базы данных
            //_SpecialParsers.Add("c70ca527-5042-4cad-a315-dcb4007e32a3", ParseMetaObjectProperties);

            _SpecialParsers.Add("35b63b9d-0adf-4625-a047-10ae874c19a3", ParseMetaObjectDimensions); // Коллекция измерений регистра бухгалтерского учёта
            _SpecialParsers.Add("63405499-7491-4ce3-ac72-43433cbe4112", ParseMetaObjectMeasures); // Коллекция ресурсов регистра бухгалтерского учёта
            _SpecialParsers.Add("9d28ee33-9c7e-4a1b-8f13-50aa9b36607b", ParseMetaObjectProperties); // Коллекция реквизитов регистра бухгалтерского учёта
        }

        public void ReadConfig(string fileName, MetaObject metaObject)
        {
            if (new Guid(fileName) == Guid.Empty) return;

            SqlBytes binaryData = ReadConfigFromDatabase(fileName);
            if (binaryData == null) return;

            DeflateStream stream = new DeflateStream(binaryData.Stream, CompressionMode.Decompress);

            ParseMetadataObject(stream, fileName, metaObject);
        }
        private SqlBytes ReadConfigFromDatabase(string fileName)
        {
            SqlBytes binaryData = null;
            {
                SqlConnection connection = new SqlConnection(ConnectionString);
                SqlCommand command = connection.CreateCommand();
                SqlDataReader reader = null;
                command.CommandType = CommandType.Text;
                command.CommandText = "SELECT [BinaryData] FROM [Config] WHERE [FileName] = @FileName;"; // Version 8.3 ORDER BY [PartNo] ASC";
                command.Parameters.AddWithValue("FileName", fileName);
                try
                {
                    connection.Open();
                    reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        binaryData = reader.GetSqlBytes(0);
                    }
                    reader.Close();
                }
                catch { throw; }
                finally { DisposeDatabaseResources(connection, command, reader); }
            }
            return binaryData;
        }
        private void ParseMetadataObject(Stream stream, string fileName, MetaObject metaObject)
        {
            if (metaObject.TypeName == MetaObjectTypes.Constant)
            {
                ParseConstant(stream, metaObject);
                return;
            }
            if (metaObject.TypeName == MetaObjectTypes.AccumulationRegister) { ParseAccumulationRegister(fileName); }

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                string line = reader.ReadLine(); // 1. line

                line = reader.ReadLine(); // 2. line
                ParseReferenceTypeIdentifier(line, metaObject); // reference object identifier to resolve data type
                _ = reader.ReadLine(); // 3. line
                line = reader.ReadLine(); // 4. line
                if (metaObject.TypeName == MetaObjectTypes.Publication)
                {
                    ParseMetaObjectName(line, metaObject); // metaobject's UUID and Name
                }

                line = reader.ReadLine(); // 5. line
                if (metaObject.TypeName == MetaObjectTypes.Publication)
                {
                    ParseMetaObjectAlias(line, metaObject); // metaobject's alias
                }
                else
                {
                    ParseMetaObjectName(line, metaObject); // metaobject's UUID and Name
                }

                line = reader.ReadLine(); // 6. line
                if (metaObject.TypeName != MetaObjectTypes.Publication)
                {
                    ParseMetaObjectAlias(line, metaObject); // metaobject's alias
                }

                _ = reader.ReadLine(); // 7. line

                if (metaObject.TypeName == MetaObjectTypes.Catalog)
                {
                    // starts from 8. line
                    ParseReferenceOwner(reader, metaObject); // свойство справочника "Владелец"
                }
                else if (metaObject.TypeName == MetaObjectTypes.Document)
                {
                    // starts from 8. line
                    // TODO: Parse объекты метаданных, которые являются основанием для заполнения текущего
                    // starts after count (количество объектов оснований) * 3 (размер ссылки на объект метаданных) + 1 (тэг закрытия блока объектов оснований)
                    // TODO: Parse все регистры (информационные, накопления и бухгалтерские), по которым текущий документ выполняет движения.
                }

                int count = 0;
                string UUID = null;
                Match match = null;
                while ((line = reader.ReadLine()) != null)
                {
                    match = rxSpecialUUID.Match(line);
                    if (!match.Success) continue;

                    string[] lines = line.Split(',');
                    UUID = lines[0].Replace("{", string.Empty);
                    count = int.Parse(lines[1].Replace("}", string.Empty));
                    if (count == 0) continue;

                    if (_SpecialParsers.ContainsKey(UUID))
                    {
                        _SpecialParsers[UUID](reader, line, metaObject);
                    }
                }
            }
        }
        private string ParseReferenceTypeIdentifier(string line, MetaObject metaObject)
        {
            if (!metaObject.IsReferenceType) return string.Empty;

            string[] items = line.Split(',');

            return (metaObject.TypeName == MetaObjectTypes.Enumeration ? items[1] : items[3]);
        }
        private void ParseMetaObjectName(string line, MetaObject metaObject)
        {
            string[] lines = line.Split(',');
            string uuid = lines[2].Replace("}", string.Empty);
            metaObject.Name = lines[3].Replace("\"", string.Empty);
        }
        private void ParseMetaObjectAlias(string line, MetaObject metaObject)
        {
            string[] lines = line.Split(',');
            string alias = lines[2].Replace("}", string.Empty);
            metaObject.Alias = alias.Replace("\"", string.Empty);
        }
        
        private void ParseReferenceOwner(StreamReader reader, MetaObject metaObject)
        {
            int count = 0;
            string[] lines;

            string line = reader.ReadLine(); // 8. line
            if (line != null)
            {
                lines = line.Split(',');
                count = int.Parse(lines[1].Replace("}", string.Empty));
            }
            if (count == 0) return;

            Match match;
            List<int> owners = new List<int>();
            for (int i = 0; i < count; i++)
            {
                _ = reader.ReadLine();
                line = reader.ReadLine();
                if (line == null) return;

                match = rxUUID.Match(line);
                if (match.Success)
                {
                    if (DBNames.TryGetValue(match.Value, out DBNameEntry entry))
                    {
                        owners.Add(entry.MetaObject.TypeCode);
                    }
                }
                _ = reader.ReadLine();
            }

            if (owners.Count > 0)
            {
                MetaProperty property = new MetaProperty
                {
                    PropertyType = (owners.Count == 1) ? owners[0] : (int)DataTypes.Multiple,
                    Name = "Владелец",
                    Field = "OwnerID" // [_OwnerIDRRef] | [_OwnerID_TYPE] + [_OwnerID_RTRef] + [_OwnerID_RRRef]
                    // TODO: add DbField[s] at once ?
                };
                metaObject.Properties.Add(property);
            }
        }
        private void ParseMetaObjectProperties(StreamReader reader, string line, MetaObject metaObject)
        {
            ParseMetaProperties(reader, line, metaObject, PropertyPurpose.Property);
        }
        private void ParseMetaObjectDimensions(StreamReader reader, string line, MetaObject metaObject)
        {
            ParseMetaProperties(reader, line, metaObject, PropertyPurpose.Dimension);
        }
        private void ParseMetaObjectMeasures(StreamReader reader, string line, MetaObject metaObject)
        {
            ParseMetaProperties(reader, line, metaObject, PropertyPurpose.Measure);
        }
        private void ParseMetaProperties(StreamReader reader, string line, MetaObject metaObject, PropertyPurpose purpose)
        {
            string[] lines = line.Split(',');
            int count = int.Parse(lines[1].Replace("}", string.Empty));
            Match match;
            string nextLine;
            for (int i = 0; i < count; i++)
            {
                while ((nextLine = reader.ReadLine()) != null)
                {
                    match = rxDbName.Match(nextLine);
                    if (match.Success)
                    {
                        ParseMetaProperty(reader, nextLine, metaObject, purpose);
                        break;
                    }
                }
            }
        }
        private void ParseMetaProperty(StreamReader reader, string line, MetaObject metaObject, PropertyPurpose purpose)
        {
            string[] lines = line.Split(',');
            string fileName = lines[2].Replace("}", string.Empty);
            string objectName = lines[3].Replace("\"", string.Empty);

            MetaProperty property = new MetaProperty
            {
                Name = objectName,
                Purpose = purpose
            };
            metaObject.Properties.Add(property);

            if (DBFields.TryGetValue(fileName, out DBNameEntry entry))
            {
                if (entry.DBNames.Count == 1)
                {
                    property.Field = CreateMetaFieldName(entry.DBNames[0]);
                }
                else if (entry.DBNames.Count > 1) // ???
                {
                    foreach (var dbn in entry.DBNames.Where(dbn => dbn.Token == MetadataTokens.Fld))
                    {
                        property.Field = CreateMetaFieldName(dbn);
                    }
                }
            }
            ParseMetaPropertyTypes(reader, property);
        }
        private void ParseMetaPropertyTypes(StreamReader reader, MetaProperty property)
        {
            string line = reader.ReadLine();
            if (line == null) return;

            while (line != "{\"Pattern\",")
            {
                line = reader.ReadLine();
                if (line == null) return;
            }

            Match match;
            List<int> typeCodes = new List<int>();
            while ((line = reader.ReadLine()) != null)
            {
                match = rxDbType.Match(line);
                if (!match.Success) break;

                int typeCode = (int)DataTypes.NULL;
                string typeName = string.Empty;
                string token = match.Value.Replace("{", string.Empty).Replace("\"", string.Empty);
                switch (token)
                {
                    case MetadataTokens.S: { typeCode = (int)DataTypes.String; break; }
                    case MetadataTokens.B: { typeCode = (int)DataTypes.Boolean; break; }
                    case MetadataTokens.N: { typeCode = (int)DataTypes.Numeric; break; }
                    case MetadataTokens.D: { typeCode = (int)DataTypes.DateTime; break; }
                }
                if (typeCode != (int)DataTypes.NULL)
                {
                    typeCodes.Add(typeCode);
                }
                else
                {
                    string[] lines = line.Split(',');
                    string uuid = lines[1].Replace("}", string.Empty);

                    if (uuid == "e199ca70-93cf-46ce-a54b-6edc88c3a296")
                    {
                        // ХранилищеЗначения - varbinary(max)
                        typeCodes.Add((int)DataTypes.Binary);
                    }
                    else if (uuid == "fc01b5df-97fe-449b-83d4-218a090e681e")
                    {
                        // УникальныйИдентификатор - binary(16)
                        typeCodes.Add((int)DataTypes.UUID);
                    }
                    else if (ReferenceTypes.TryGetValue(uuid, out MetaObject type))
                    {
                        typeCodes.Add(type.TypeCode);
                    }
                    else // metaobject reference type file is not parsed yet
                    {
                        typeCodes.Add((int)DataTypes.Object);
                    }
                }

                if (typeCodes.Count > 1) break;
            }

            if (typeCodes.Count == 1) property.PropertyType = typeCodes[0];
            else if (typeCodes.Count > 1) property.PropertyType = (int)DataTypes.Multiple;
            else property.PropertyType = (int)DataTypes.NULL;
        }



        private void ParseNestedObjects(StreamReader reader, string line, MetaObject dbo)
        {
            string[] lines = line.Split(',');
            int count = int.Parse(lines[1]);
            Match match;
            string nextLine;
            for (int i = 0; i < count; i++)
            {
                while ((nextLine = reader.ReadLine()) != null)
                {
                    match = rxDbName.Match(nextLine);
                    if (match.Success)
                    {
                        ParseNestedObject(reader, nextLine, dbo);
                        break;
                    }
                }
            }
        }
        private void ParseNestedObject(StreamReader reader, string line, MetaObject dbo)
        {
            string[] lines = line.Split(',');
            string fileName = lines[2].Replace("}", string.Empty);
            string objectName = lines[3].Replace("\"", string.Empty);

            MetaObject nested = new MetaObject()
            {
                Owner = dbo,
                Name = objectName
            };
            dbo.MetaObjects.Add(nested);

            if (DBNames.TryGetValue(fileName, out DBNameEntry entry))
            {
                DBName dbname = entry.DBNames.Where(i => i.Token == MetadataTokens.VT).FirstOrDefault();
                if (dbname != null)
                {
                    nested.TypeName = dbname.Token;
                    nested.TypeCode = dbname.TypeCode;
                    nested.TableName = CreateTableName(nested, dbname);
                }
            }
            ParseNestedMetaProperties(reader, nested);
        }
        private void ParseNestedMetaProperties(StreamReader reader, MetaObject dbo)
        {
            string line;
            Match match;
            while ((line = reader.ReadLine()) != null)
            {
                match = rxNestedProperties.Match(line);
                if (match.Success)
                {
                    ParseMetaProperties(reader, line, dbo, PropertyPurpose.Property);
                    break;
                }
            }
        }



        private void ParseConstant(Stream stream, MetaObject metaObject)
        {
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                _ = reader.ReadLine(); // 1. line
                _ = reader.ReadLine(); // 2. line
                _ = reader.ReadLine(); // 3. line
                _ = reader.ReadLine(); // 4. line
                _ = reader.ReadLine(); // 5. line
                string line = reader.ReadLine(); // 6. line

                string[] lines = line.Split(',');
                string uuid = lines[2].TrimEnd('}');
                metaObject.Name = lines[3].Trim('"');

                MetaProperty property = new MetaProperty()
                {
                    Name = "Value"
                };
                if (DBFields.TryGetValue(uuid, out DBNameEntry entry))
                {
                    if (entry.DBNames.Count == 1)
                    {
                        property.Field = CreateMetaFieldName(entry.DBNames[0]);
                    }
                }
                metaObject.Properties.Add(property);

                ParseMetaPropertyTypes(reader, property);
            }
        }

        private void ParseAccumulationRegister(string fileName)
        {
            if (!DBNames.TryGetValue(fileName, out DBNameEntry entry)) return;

            foreach (DBName dbname in entry.DBNames)
            {
                string name = GetChildMetaObjectName(dbname.Token);
                if (string.IsNullOrEmpty(name)) { continue; }
                entry.MetaObject.MetaObjects.Add(
                    new MetaObject()
                    {
                        Name = name,
                        Owner = entry.MetaObject,
                        TypeName = dbname.Token,
                        TypeCode = dbname.TypeCode,
                        TableName = $"_{dbname.Token}{dbname.TypeCode}"
                    });
            }
        }
        private string GetChildMetaObjectName(string token)
        {
            if (token == MetadataTokens.AccumRgT) return "Итоги";
            else if (token == MetadataTokens.AccumRgOpt) return "Настройки";
            else if (token == MetadataTokens.AccumRgChngR) return "Изменения";
            return string.Empty;
        }

        #endregion

        #region "New implementation"

        public ConfigInfo ReadConfigurationProperties()
        {
            IMetadataFileReader reader = new MetadataFileReader();
            reader.UseConnectionString(ConnectionString);
            IConfigurationFileParser parser = new ConfigurationFileParser(reader);
            return parser.ReadConfigurationProperties();
        }

        

        #endregion
    }
}