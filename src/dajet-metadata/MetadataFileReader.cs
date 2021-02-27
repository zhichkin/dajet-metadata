using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace DaJet.Metadata
{
    /// <summary>
    /// Интерфейс для чтения файлов конфигурации 1С
    /// </summary>
    public interface IMetadataFileReader
    {
        ///<summary>Устанавливает строку подключения к базе данных 1С</summary>
        ///<param name="connectionString">Строка подключения к базе данных 1С</param>
        void UseConnectionString(string connectionString);

        ///<summary>Формирует строку подключения к базе данных 1С по параметрам</summary>
        ///<param name="server">Имя или сетевой адрес сервера SQL Server</param>
        ///<param name="database">Имя базы данных SQL Server</param>
        ///<param name="userName">Имя пользователя (если не указано, используется Windows аутентификация)</param>
        ///<param name="password">Пароль пользователя SQL Server (используется только в случае SQL Server аутентификации)</param>
        void ConfigureConnectionString(string server, string database, string userName, string password);

        ///<summary>Получает требуемую версию платформы 1С для работы с базой данных</summary>
        ///<returns>Требуемая версия платформы 1С</returns>
        int GetPlatformRequiredVersion();

        ///<summary>Получает файл метаданных в "сыром" (как есть) бинарном виде</summary>
        ///<param name="fileName">Имя файла метаданных: root, DBNames или значение UUID</param>
        ///<returns>Бинарные данные файла метаданных</returns>
        byte[] ReadBytes(string fileName);

        ///<summary>Распаковывает файл метаданных по алгоритму deflate и создаёт поток для чтения в формате UTF-8</summary>
        ///<param name="fileData">Бинарные данные файла метаданных</param>
        ///<returns>Поток для чтения файла метаданных в формате UTF-8</returns>
        StreamReader CreateReader(byte[] fileData);
    }
    /// <summary>
    /// Класс для чтения файлов конфигурации 1С из SQL Server
    /// </summary>
    public sealed class MetadataFileReader : IMetadataFileReader
    {
        private const string ROOT_FILE_NAME = "root"; // Config
        private const string DBNAMES_FILE_NAME = "DBNames"; // Params

        private const string IBVERSION_QUERY_SCRIPT = "SELECT TOP 1 [PlatformVersionReq] FROM [IBVersion];";
        private const string PARAMS_QUERY_SCRIPT = "SELECT [BinaryData] FROM [Params] WHERE [FileName] = @FileName;";
        private const string CONFIG_QUERY_SCRIPT = "SELECT [BinaryData] FROM [Config] WHERE [FileName] = @FileName;"; // Version 8.3 ORDER BY [PartNo] ASC";

        private string ConnectionString { get; set; }
        private byte[] CombineArrays(byte[] a1, byte[] a2)
        {
            if (a1 == null) return a2;

            byte[] result = new byte[a1.Length + a2.Length];
            Buffer.BlockCopy(a1, 0, result, 0, a1.Length);
            Buffer.BlockCopy(a2, 0, result, a1.Length, a2.Length);
            return result;
        }
        private T ExecuteScalar<T>(string script, string fileName)
        {
            T result = default(T);
            {
                SqlConnection connection = new SqlConnection(ConnectionString);
                SqlCommand command = connection.CreateCommand();
                command.CommandType = CommandType.Text;
                command.CommandText = script;
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    command.Parameters.AddWithValue("FileName", fileName);
                }
                try
                {
                    connection.Open();
                    result = (T)command.ExecuteScalar();
                }
                catch { throw; }
                finally { DisposeDatabaseResources(connection, command, null); }
            }
            return result;
        }
        private byte[] ExecuteReader(string script, string fileName)
        {
            byte[] fileData = null;
            {
                SqlConnection connection = new SqlConnection(ConnectionString);
                SqlCommand command = connection.CreateCommand();
                SqlDataReader reader = null;
                command.CommandType = CommandType.Text;
                command.CommandText = script;
                command.Parameters.AddWithValue("FileName", fileName);
                try
                {
                    connection.Open();
                    reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        byte[] data = reader.GetSqlBytes(0).Value;
                        fileData = CombineArrays(fileData, data);
                    }
                    reader.Close();
                }
                catch { throw; }
                finally { DisposeDatabaseResources(connection, command, reader); }
            }
            return fileData;
        }
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

        public void UseConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
        }
        public void ConfigureConnectionString(string server, string database, string userName, string password)
        {
            SqlConnectionStringBuilder connectionString = new SqlConnectionStringBuilder()
            {
                DataSource = server,
                InitialCatalog = database
            };
            if (!string.IsNullOrWhiteSpace(userName))
            {
                connectionString.UserID = userName;
                connectionString.Password = password;
            }
            connectionString.IntegratedSecurity = string.IsNullOrWhiteSpace(userName);
            ConnectionString = connectionString.ToString();
        }
        public int GetPlatformRequiredVersion()
        {
            return ExecuteScalar<int>(IBVERSION_QUERY_SCRIPT, null);
        }
        public byte[] ReadBytes(string fileName)
        {
            if (fileName == ROOT_FILE_NAME)
            {
                return ExecuteReader(CONFIG_QUERY_SCRIPT, fileName);
            }
            else if (fileName == DBNAMES_FILE_NAME)
            {
                return ExecuteReader(PARAMS_QUERY_SCRIPT, fileName);
            }
            else
            {
                return ExecuteReader(CONFIG_QUERY_SCRIPT, fileName);
            }
        }
        public StreamReader CreateReader(byte[] fileData)
        {
            MemoryStream memory = new MemoryStream(fileData);
            DeflateStream stream = new DeflateStream(memory, CompressionMode.Decompress);
            return new StreamReader(stream, Encoding.UTF8);
        }

        //TODO: чтение исходного кода 1С общих модулей конфигурации
        //private const string COMMON_MODULES_COLLECTION_UUID = "0fe48980-252d-11d6-a3c7-0050bae0a776";
        //public List<CommonModuleInfo> GetCommonModules()
        //{
        //    List<CommonModuleInfo> list = new List<CommonModuleInfo>();
        //    string fileName = GetRootConfigFileName();
        //    string metadata = ReadConfigFile(fileName);
        //    using (StringReader reader = new StringReader(metadata))
        //    {
        //        string line = reader.ReadLine();
        //        while (!string.IsNullOrEmpty(line))
        //        {
        //            if (line.Substring(1, 36) == COMMON_MODULES_COLLECTION_UUID)
        //            {
        //                list = ParseCommonModules(line); break;
        //            }
        //            line = reader.ReadLine();
        //        }
        //    }
        //    return list;
        //}
        //private List<CommonModuleInfo> ParseCommonModules(string line)
        //{
        //    List<CommonModuleInfo> list = new List<CommonModuleInfo>();
        //    string[] fileNames = line.TrimStart('{').TrimEnd('}').Split(',');
        //    if (int.TryParse(fileNames[1], out int count) && count == 0)
        //    {
        //        return list;
        //    }
        //    int offset = 2;
        //    for (int i = 0; i < count; i++)
        //    {
        //        CommonModuleInfo moduleInfo = ReadCommonModuleMetadata(fileNames[i + offset]);
        //        list.Add(moduleInfo);
        //    }
        //    return list;
        //}
        //private CommonModuleInfo ReadCommonModuleMetadata(string fileName)
        //{
        //    string metadata = ReadConfigFile(fileName);
        //    string uuid = string.Empty;
        //    string name = string.Empty;
        //    using (StringReader reader = new StringReader(metadata))
        //    {
        //        _ = reader.ReadLine(); // 1. line
        //        _ = reader.ReadLine(); // 2. line
        //        _ = reader.ReadLine(); // 3. line
        //        string line = reader.ReadLine(); // 4. line
        //        string[] lines = line.Split(',');
        //        uuid = lines[2].TrimEnd('}');
        //        name = lines[3].Trim('"');
        //    }
        //    return new CommonModuleInfo(uuid, name);
        //}
        //public string ReadCommonModuleSourceCode(CommonModuleInfo module)
        //{
        //    return ReadConfigFile(module.UUID + ".0");
        //}
    }
}