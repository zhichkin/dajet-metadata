using Microsoft.Data.SqlClient;
using System.Data;

namespace DaJet
{
    internal sealed class MsMetadataLoader : MetadataLoader
    {
        private const string MS_PARAMS_SELECT_SCRIPT = "SELECT (CASE WHEN SUBSTRING(BinaryData, 1, 3) = 0xEFBBBF THEN 1 ELSE 0 END) AS UTF8, CAST(DataSize AS int) AS DataSize, FileName, BinaryData FROM Params WHERE FileName = @FileName;";
        private const string MS_CONFIG_SELECT_SCRIPT = "SELECT (CASE WHEN SUBSTRING(BinaryData, 1, 3) = 0xEFBBBF THEN 1 ELSE 0 END) AS UTF8, CAST(DataSize AS int) AS DataSize, FileName, BinaryData FROM Config WHERE FileName = @FileName;";
        private const string MS_CONFIG_STREAM_SCRIPT = "SELECT (CASE WHEN SUBSTRING(BinaryData, 1, 3) = 0xEFBBBF THEN 1 ELSE 0 END) AS UTF8, CAST(DataSize AS int) AS DataSize, Config.FileName AS FileName, BinaryData FROM Config INNER JOIN #ConfigFileNames AS T ON Config.FileName = T.FileName;";

        private readonly string _connectionString;
        internal MsMetadataLoader(in string connectionString)
        {
            _connectionString = connectionString;
        }
        internal override int GetYearOffset()
        {
            using (SqlConnection connection = new(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '_YearOffset';";

                    object value = command.ExecuteScalar();

                    if (value is null)
                    {
                        return -1;
                    }

                    command.CommandText = "SELECT TOP 1 [Offset] FROM [_YearOffset];";

                    value = command.ExecuteScalar();

                    if (value is not int offset)
                    {
                        return 0;
                    }

                    return offset;
                }
            }
        }
        internal override ConfigFileBuffer Load(in string fileName)
        {
            ConfigFileBuffer buffer = new();

            using (SqlConnection connection = new(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = MS_CONFIG_SELECT_SCRIPT;
                    command.CommandTimeout = 10; // seconds

                    command.Parameters.AddWithValue("FileName", fileName);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            buffer.Load(reader);
                        }
                        reader.Close();
                    }
                }
            }

            return buffer;
        }
        internal override IEnumerable<ConfigFileBuffer> Stream(Guid[] files)
        {
            if (files is null || files.Length == 0)
            {
                yield break;
            }

            using (SqlConnection connection = new(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "CREATE TABLE #ConfigFileNames (FileName nvarchar(128) NOT NULL);";
                    command.CommandType = CommandType.Text;
                    command.CommandTimeout = 10; // seconds
                    command.ExecuteNonQuery();

                    using (SqlBulkCopy insert = new(connection))
                    {
                        insert.DestinationTableName = "#ConfigFileNames";
                        DataTable table = CreateFileNamesTable(in files);
                        insert.WriteToServer(table);
                    }

                    command.CommandText = MS_CONFIG_STREAM_SCRIPT;
                    command.CommandType = CommandType.Text;
                    command.CommandTimeout = 60; // seconds
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            using (ConfigFileBuffer buffer = new(reader))
                            {
                                yield return buffer;
                            }
                        }
                        reader.Close();
                    }
                }
            }
        }
        private static DataTable CreateFileNamesTable(in Guid[] files)
        {
            DataTable table = new();

            DataColumn column = new()
            {
                ColumnName = "FileName",
                DataType = typeof(string),
                MaxLength = 128,
                AllowDBNull = false
            };

            table.Columns.Add(column);

            for (int i = 0; i < files.Length; i++)
            {
                DataRow row = table.NewRow();

                row[0] = files[i].ToString().ToLowerInvariant();

                table.Rows.Add(row);
            }

            return table;
        }

        internal override ConfigFileBuffer Load(in string tableName, in string fileName)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                return Load(in fileName); // [Config] table
            }

            ConfigFileBuffer buffer = new();

            using (SqlConnection connection = new(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandTimeout = 10; // seconds

                    if (tableName == ConfigTables.Params)
                    {
                        command.CommandText = MS_PARAMS_SELECT_SCRIPT;
                    }
                    else
                    {
                        command.CommandText = MS_CONFIG_SELECT_SCRIPT;
                    }
                    
                    command.Parameters.AddWithValue("FileName", fileName);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            buffer.Load(reader);
                        }
                        reader.Close();
                    }
                }
            }

            return buffer;
        }
    }
}