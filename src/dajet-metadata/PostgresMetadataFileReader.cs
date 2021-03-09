using Npgsql;
using System;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace DaJet.Metadata
{
    public sealed class PostgresMetadataFileReader : IMetadataFileReader
    {
        private const string ROOT_FILE_NAME = "root"; // Config
        private const string DBNAMES_FILE_NAME = "DBNames"; // Params

        private const string IBVERSION_QUERY_SCRIPT = "SELECT platformversionreq FROM ibversion LIMIT 1;";
        private const string PARAMS_QUERY_SCRIPT = "SELECT binarydata FROM params WHERE filename = '{filename}';";
        private const string CONFIG_QUERY_SCRIPT = "SELECT binarydata FROM config WHERE filename = '{filename}';"; // Version 8.3 ORDER BY [PartNo] ASC";

        public string ConnectionString { get; private set; }
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
                NpgsqlConnection connection = new NpgsqlConnection(ConnectionString);
                NpgsqlCommand command = connection.CreateCommand();
                command.CommandType = CommandType.Text;
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    command.CommandText = script;
                }
                else
                {
                    command.CommandText = script.Replace("{filename}", fileName);
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
                NpgsqlConnection connection = new NpgsqlConnection(ConnectionString);
                NpgsqlCommand command = connection.CreateCommand();
                NpgsqlDataReader reader = null;
                command.CommandType = CommandType.Text;
                command.CommandText = script.Replace("{filename}", fileName);
                try
                {
                    connection.Open();
                    reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        byte[] data = (byte[])reader[0];
                        fileData = CombineArrays(fileData, data);
                    }
                    reader.Close();
                }
                catch { throw; }
                finally { DisposeDatabaseResources(connection, command, reader); }
            }
            return fileData;
        }
        private void DisposeDatabaseResources(NpgsqlConnection connection, NpgsqlCommand command, NpgsqlDataReader reader)
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
            // Default values for PostgreSQL
            string serverName = "127.0.0.1";
            int serverPort = 5432;

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

    }
}