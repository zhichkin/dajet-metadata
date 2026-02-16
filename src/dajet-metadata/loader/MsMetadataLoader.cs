using DaJet.Data;
using DaJet.TypeSystem;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace DaJet.Metadata
{
    internal sealed class MsMetadataLoader : MetadataLoader
    {
        private const string MS_PARAMS_SELECT_SCRIPT = "SELECT (CASE WHEN SUBSTRING(BinaryData, 1, 3) = 0xEFBBBF THEN 1 ELSE 0 END) AS UTF8, CAST(DataSize AS int) AS DataSize, FileName, BinaryData FROM Params WHERE FileName = @FileName;";
        private const string MS_PARAMS_STREAM_SCRIPT = "SELECT (CASE WHEN SUBSTRING(BinaryData, 1, 3) = 0xEFBBBF THEN 1 ELSE 0 END) AS UTF8, CAST(DataSize AS int) AS DataSize, FileName, BinaryData FROM Params WHERE FileName LIKE @FileName;";
        private const string MS_CONFIG_SELECT_SCRIPT = "SELECT (CASE WHEN SUBSTRING(BinaryData, 1, 3) = 0xEFBBBF THEN 1 ELSE 0 END) AS UTF8, CAST(DataSize AS int) AS DataSize, FileName, BinaryData FROM Config WHERE FileName = @FileName;";
        private const string MS_CONFIG_STREAM_SCRIPT = "SELECT (CASE WHEN SUBSTRING(BinaryData, 1, 3) = 0xEFBBBF THEN 1 ELSE 0 END) AS UTF8, CAST(DataSize AS int) AS DataSize, Config.FileName AS FileName, BinaryData FROM Config INNER JOIN #ConfigFileNames AS T ON Config.FileName = T.FileName;";
        private const string MS_CONFIG_CAS_SCRIPT = "SELECT (CASE WHEN SUBSTRING(BinaryData, 1, 3) = 0xEFBBBF THEN 1 ELSE 0 END) AS UTF8, CAST(DataSize AS int) AS DataSize, FileName, BinaryData FROM ConfigCAS WHERE FileName = @FileName;";
        private const string MS_CONFIG_CAS_STREAM_SCRIPT = "SELECT (CASE WHEN SUBSTRING(BinaryData, 1, 3) = 0xEFBBBF THEN 1 ELSE 0 END) AS UTF8, CAST(DataSize AS int) AS DataSize, ConfigCAS.FileName AS FileName, BinaryData FROM ConfigCAS INNER JOIN #ConfigFileNames AS T ON ConfigCAS.FileName = T.FileName;";

        private readonly string _connectionString;
        internal MsMetadataLoader(in string connectionString)
        {
            _connectionString = connectionString;
        }

        internal override DbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
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
        
        internal override ConfigFileBuffer Load(in string tableName, in string fileName)
        {
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
                    else if (tableName == ConfigTables.ConfigCAS)
                    {
                        command.CommandText = MS_CONFIG_CAS_SCRIPT;
                    }
                    else // ConfigTables.Config
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
        internal override IEnumerable<ConfigFileBuffer> Stream(string tableName, string fileNamePattern)
        {
            using (SqlConnection connection = new(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandTimeout = 10; // seconds

                    if (tableName == ConfigTables.Params)
                    {
                        command.CommandText = MS_PARAMS_STREAM_SCRIPT;
                    }
                    //else if (tableName == ConfigTables.ConfigCAS)
                    //{
                    //    command.CommandText = MS_CONFIG_CAS_SCRIPT;
                    //}
                    //else // ConfigTables.Config
                    //{
                    //    command.CommandText = MS_CONFIG_SELECT_SCRIPT;
                    //}

                    command.Parameters.AddWithValue("FileName", fileNamePattern);

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

        private static DataTable CreateFileNamesTable(in string[] fileNames)
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

            for (int i = 0; i < fileNames.Length; i++)
            {
                DataRow row = table.NewRow();
                
                row[0] = fileNames[i];
                
                table.Rows.Add(row);
            }

            return table;
        }
        internal override IEnumerable<ConfigFileBuffer> Stream(string tableName, string[] fileNames)
        {
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
                        DataTable table = CreateFileNamesTable(in fileNames);
                        insert.WriteToServer(table);
                    }

                    command.CommandText = tableName == ConfigTables.Config
                        ? MS_CONFIG_STREAM_SCRIPT
                        : MS_CONFIG_CAS_STREAM_SCRIPT;

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

        internal override T ExecuteScalar<T>(in string script, int timeout)
        {
            T result = default;

            using (SqlConnection connection = new(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = script;
                    command.CommandTimeout = timeout; // seconds

                    object value = command.ExecuteScalar();

                    if (value is not null)
                    {
                        result = (T)value;
                    }
                }
            }

            return result;
        }
        private IEnumerable<SqlDataReader> ExecuteReader(string script, int timeout)
        {
            using (SqlConnection connection = new(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = script;
                    command.CommandTimeout = timeout;

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return reader;
                        }

                        reader.Close();
                    }
                }
            }
        }

        private const string SELECT_TABLE_SCHEMA_SCRIPT = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @tableName;";
        internal override EntityDefinition GetDbTableSchema(in string tableName)
        {
            EntityDefinition table = null;

            using (SqlConnection connection = new(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = SELECT_TABLE_SCHEMA_SCRIPT;
                    command.CommandTimeout = 10; // seconds

                    command.Parameters.AddWithValue("tableName", tableName);
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            table = new EntityDefinition()
                            {
                                Name = tableName,
                                DbName = tableName
                            };

                            while (reader.Read())
                            {
                                PropertyDefinition property = new()
                                {
                                    Name = reader.GetString(0)
                                };

                                //TODO: configure property DataType

                                table.Properties.Add(property);
                            }
                        }
                        
                        reader.Close();
                    }
                }
            }

            return table;
        }

        private const string IS_NEW_AGE_EXTENSIONS_SUPPORTED = "SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '_ExtensionsInfo' AND COLUMN_NAME = '_ExtensionZippedInfo';";
        private const string SELECT_EXTENSIONS_SCRIPT =
            "SELECT _IDRRef, _ExtensionOrder, _ExtName, _UpdateTime, " +
            "_ExtensionUsePurpose, _ExtensionScope, _ExtensionZippedInfo, " +
            "_MasterNode, _UsedInDistributedInfoBase, _Version " +
            "FROM _ExtensionsInfo ORDER BY " +
            "CASE WHEN SUBSTRING(_MasterNode, CAST(1.0 AS INT), CAST(34.0 AS INT)) = N'0:00000000000000000000000000000000' " +
            "THEN 0x01 ELSE 0x00 END, _ExtensionUsePurpose, _ExtensionScope, _ExtensionOrder;";
        private bool IsExtensionsSupported()
        {
            return (ExecuteScalar<int>(IS_NEW_AGE_EXTENSIONS_SUPPORTED, 10) == 1);
        }
        internal override List<ExtensionInfo> GetExtensions()
        {
            List<ExtensionInfo> list = new();

            if (!IsExtensionsSupported())
            {
                return list;
            }

            int YearOffset = GetYearOffset();
            
            foreach (SqlDataReader reader in ExecuteReader(SELECT_EXTENSIONS_SCRIPT, 10))
            {
                byte[] zippedInfo = (byte[])reader.GetValue(6);

                Guid uuid = new(DbUtilities.Get1CUuid((byte[])reader.GetValue(0)));

                ExtensionInfo extension = new()
                {
                    Identity = uuid, // Поле _IDRRef используется для поиска файла DbNames расширения
                    Order = (int)reader.GetDecimal(1),
                    Name = reader.GetString(2),
                    Updated = reader.GetDateTime(3).AddYears(-YearOffset),
                    Purpose = (ExtensionPurpose)reader.GetDecimal(4),
                    Scope = (ExtensionScope)reader.GetDecimal(5),
                    MasterNode = reader.GetString(7),
                    IsDistributed = (((byte[])reader.GetValue(8))[0] == 1)
                };

                DecodeZippedInfo(in zippedInfo, in extension);

                list.Add(extension);
            }

            return list;
        }
    }
}

//SELECT CASE WHEN dbs.COLUMN_NAME IS NULL THEN N'delete' ELSE N'insert' END AS RESULT
//      , CASE WHEN dbs.COLUMN_NAME IS NULL THEN entity.COLUMN_NAME ELSE dbs.COLUMN_NAME END AS COLUMN_NAME
//   FROM
//(SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, NUMERIC_PRECISION, NUMERIC_SCALE, CHARACTER_MAXIMUM_LENGTH
//   FROM INFORMATION_SCHEMA.COLUMNS
//  WHERE TABLE_CATALOG = 'erp_uh'
//    AND TABLE_SCHEMA  = 'dbo'
//    AND TABLE_NAME    = '_Reference577') AS dbs
//   FULL JOIN (VALUES ('column_name'), ('_IDRRef')) AS entity(COLUMN_NAME)
//     ON dbs.COLUMN_NAME = entity.COLUMN_NAME
//  WHERE NOT (dbs.COLUMN_NAME IS NOT NULL AND entity.COLUMN_NAME IS NOT NULL)