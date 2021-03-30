using Microsoft.Data.SqlClient;
using Npgsql;
using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace DaJet.Metadata
{
    /// <summary>
    /// Интерфейс для чтения файлов конфигурации 1С из таблиц IBVersion, Params и Config
    /// </summary>
    public interface IMetadataFileReader
    {
        ///<summary>Используемый провайдер баз данных</summary>
        DatabaseProviders DatabaseProvider { get; }

        ///<summary>Устанавливает провайдера базы данных СУБД</summary>
        ///<param name="provider">Значение перечисления <see cref="DatabaseProviders"/> (Microsoft SQL Server или PostgreSQL)</param>
        void UseDatabaseProvider(DatabaseProviders databaseProvider);

        ///<summary>Возвращает установленную ранее строку подключения к базе данных 1С</summary>
        string ConnectionString { get; }

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
        private const string DBSCHEMA_FILE_NAME = "DBSchema"; // DBSchema

        private const string MS_IBVERSION_QUERY_SCRIPT = "SELECT TOP 1 [PlatformVersionReq] FROM [IBVersion];";
        private const string MS_PARAMS_QUERY_SCRIPT = "SELECT [BinaryData] FROM [Params] WHERE [FileName] = @FileName;";
        private const string MS_CONFIG_QUERY_SCRIPT = "SELECT [BinaryData] FROM [Config] WHERE [FileName] = @FileName;"; // Version 8.3 ORDER BY [PartNo] ASC";
        private const string MS_DBSCHEMA_QUERY_SCRIPT = "SELECT TOP 1 [SerializedData] FROM [DBSchema];";

        private const string PG_IBVERSION_QUERY_SCRIPT = "SELECT platformversionreq FROM ibversion LIMIT 1;";
        private const string PG_PARAMS_QUERY_SCRIPT = "SELECT binarydata FROM params WHERE filename = '{filename}';";
        private const string PG_CONFIG_QUERY_SCRIPT = "SELECT binarydata FROM config WHERE filename = '{filename}';"; // Version 8.3 ORDER BY [PartNo] ASC";
        private const string PG_DBSCHEMA_QUERY_SCRIPT = "SELECT serializeddata FROM dbschema LIMIT 1;";

        public string ConnectionString { get; private set; } = string.Empty;
        public DatabaseProviders DatabaseProvider { get; private set; } = DatabaseProviders.SQLServer;
        private byte[] CombineArrays(byte[] a1, byte[] a2)
        {
            if (a1 == null) return a2;

            byte[] result = new byte[a1.Length + a2.Length];
            Buffer.BlockCopy(a1, 0, result, 0, a1.Length);
            Buffer.BlockCopy(a2, 0, result, a1.Length, a2.Length);
            return result;
        }
        private DbConnection CreateDbConnection()
        {
            if (DatabaseProvider == DatabaseProviders.SQLServer)
            {
                return new SqlConnection(ConnectionString);
            }
            return new NpgsqlConnection(ConnectionString);
        }
        private void ConfigureFileNameParameter(DbCommand command, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;

            if (DatabaseProvider == DatabaseProviders.SQLServer)
            {
                ((SqlCommand)command).Parameters.AddWithValue("FileName", fileName);
            }
            else
            {
                command.CommandText = command.CommandText.Replace("{filename}", fileName);
            }
        }
        private T ExecuteScalar<T>(string script, string fileName)
        {
            T result = default(T);
            using (DbConnection connection = CreateDbConnection())
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = script;
                command.CommandType = CommandType.Text;
                ConfigureFileNameParameter(command, fileName);
                connection.Open();
                result = (T)command.ExecuteScalar();
            }
            return result;
        }
        private byte[] ExecuteReader(string script, string fileName)
        {
            byte[] fileData = null;
            using (DbConnection connection = CreateDbConnection())
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = script;
                command.CommandType = CommandType.Text;
                ConfigureFileNameParameter(command, fileName);
                connection.Open();
                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        byte[] data = (byte[])reader[0];
                        fileData = CombineArrays(fileData, data);
                    }
                }
            }
            return fileData;
        }
        public void UseConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
        }
        public void UseDatabaseProvider(DatabaseProviders databaseProvider)
        {
            DatabaseProvider = databaseProvider;
        }
        public void ConfigureConnectionString(string server, string database, string userName, string password)
        {
            if (DatabaseProvider == DatabaseProviders.SQLServer)
            {
                ConfigureConnectionStringForSQLServer(server, database, userName, password);
            }
            else
            {
                ConfigureConnectionStringForPostgreSQL(server, database, userName, password);
            }
        }
        private void ConfigureConnectionStringForSQLServer(string server, string database, string userName, string password)
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
        private void ConfigureConnectionStringForPostgreSQL(string server, string database, string userName, string password)
        {
            // Default values for PostgreSQL
            int serverPort = 5432;
            string serverName = "127.0.0.1";

            string[] serverInfo = server.Split(':');
            if (serverInfo.Length == 1)
            {
                serverName = serverInfo[0];
            }
            else if (serverInfo.Length > 1)
            {
                serverName = serverInfo[0];
                if (!int.TryParse(serverInfo[1], out serverPort))
                {
                    serverPort = 5432;
                }
            }

            NpgsqlConnectionStringBuilder connectionString = new NpgsqlConnectionStringBuilder()
            {
                Host = serverName,
                Port = serverPort,
                Database = database
            };
            if (!string.IsNullOrWhiteSpace(userName))
            {
                connectionString.Username = userName;
                connectionString.Password = password;
            }
            ConnectionString = connectionString.ToString();
        }
        public int GetPlatformRequiredVersion()
        {
            if (DatabaseProvider == DatabaseProviders.SQLServer)
            {
                return ExecuteScalar<int>(MS_IBVERSION_QUERY_SCRIPT, null);
            }
            return ExecuteScalar<int>(PG_IBVERSION_QUERY_SCRIPT, null);
        }
        public byte[] ReadBytes(string fileName)
        {
            if (fileName == ROOT_FILE_NAME)
            {
                if (DatabaseProvider == DatabaseProviders.SQLServer)
                {
                    return ExecuteReader(MS_CONFIG_QUERY_SCRIPT, fileName);
                }
                return ExecuteReader(PG_CONFIG_QUERY_SCRIPT, fileName);
            }
            else if (fileName == DBNAMES_FILE_NAME)
            {
                if (DatabaseProvider == DatabaseProviders.SQLServer)
                {
                    return ExecuteReader(MS_PARAMS_QUERY_SCRIPT, fileName);
                }
                return ExecuteReader(PG_PARAMS_QUERY_SCRIPT, fileName);
            }
            else if (fileName == DBSCHEMA_FILE_NAME)
            {
                if (DatabaseProvider == DatabaseProviders.SQLServer)
                {
                    return ExecuteReader(MS_DBSCHEMA_QUERY_SCRIPT, fileName);
                }
                return ExecuteReader(PG_DBSCHEMA_QUERY_SCRIPT, fileName);
            }
            if (DatabaseProvider == DatabaseProviders.SQLServer)
            {
                return ExecuteReader(MS_CONFIG_QUERY_SCRIPT, fileName);
            }
            return ExecuteReader(PG_CONFIG_QUERY_SCRIPT, fileName);
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