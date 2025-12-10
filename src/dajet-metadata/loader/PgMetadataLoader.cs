using DaJet.Data;
using DaJet.Data.PostgreSql;
using DaJet.TypeSystem;
using Npgsql;
using System.Data;
using System.Text;

namespace DaJet.Metadata
{
    internal sealed class PgMetadataLoader : MetadataLoader
    {
        private const string PG_PARAMS_SELECT_SCRIPT = "SELECT (CASE WHEN SUBSTRING(binarydata, 1, 3) = E'\\\\xEFBBBF' THEN 1 ELSE 0 END) AS UTF8, CAST(datasize AS int) AS DataSize, filename::text, binarydata FROM params WHERE filename = $1::mvarchar";
        private const string PG_CONFIG_SELECT_SCRIPT = "SELECT (CASE WHEN SUBSTRING(binarydata, 1, 3) = E'\\\\xEFBBBF' THEN 1 ELSE 0 END) AS UTF8, CAST(datasize AS int) AS DataSize, filename::text, binarydata FROM config WHERE filename = $1::mvarchar";
        private const string PG_CONFIG_STREAM_SCRIPT = "SELECT (CASE WHEN SUBSTRING(binarydata, 1, 3) = E'\\\\xEFBBBF' THEN 1 ELSE 0 END) AS UTF8, CAST(datasize AS int) AS DataSize, filename::text, binarydata FROM config WHERE filename IN (";

        private readonly NpgsqlDataSource _source;
        internal PgMetadataLoader(in string connectionString)
        {
            _source = PgDataSourceFactory.GetDataSource(in connectionString);
        }
        internal override int GetYearOffset()
        {
            using (NpgsqlConnection connection = _source.CreateConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '_yearoffset';";

                    object value = command.ExecuteScalar();

                    if (value is null)
                    {
                        return -1;
                    }

                    command.CommandText = "SELECT ofset FROM _yearoffset LIMIT 1;";

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

            using (NpgsqlConnection connection = _source.CreateConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = PG_CONFIG_SELECT_SCRIPT;
                    command.CommandTimeout = 10; // seconds

                    //command.Parameters.AddWithValue("filename", fileName);

                    command.Parameters.Add(new NpgsqlParameter<string>()
                    {
                        TypedValue = fileName,
                        NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar
                    });

                    using (NpgsqlDataReader reader = command.ExecuteReader())
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
        internal override ConfigFileBuffer Load(in string tableName, in string fileName)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                return Load(in fileName); // "config" table
            }

            ConfigFileBuffer buffer = new();

            using (NpgsqlConnection connection = _source.CreateConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandTimeout = 10; // seconds

                    if (tableName == ConfigTables.Params)
                    {
                        command.CommandText = PG_PARAMS_SELECT_SCRIPT;
                    }
                    else
                    {
                        command.CommandText = PG_CONFIG_SELECT_SCRIPT;
                    }

                    command.Parameters.Add(new NpgsqlParameter<string>()
                    {
                        TypedValue = fileName,
                        NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar
                    });

                    using (NpgsqlDataReader reader = command.ExecuteReader())
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

        private static string GenerateStreamCommand(in Guid[] files)
        {
            StringBuilder script = new(PG_CONFIG_STREAM_SCRIPT);

            for (int i = 0; i < files.Length; i++)
            {
                if (i > 0) { script.Append(','); }

                script.Append('$').Append(i + 1).Append("::mvarchar");
            }

            script.Append(')');

            return script.ToString();
        }
        private static void ConfigureStreamParameters(in NpgsqlCommand command, in Guid[] files)
        {
            for (int i = 0; i < files.Length; i++)
            {
                command.Parameters.Add(new NpgsqlParameter<string>()
                {
                    NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar,
                    TypedValue = files[i].ToString().ToLowerInvariant()
                });
            }
        }
        internal override IEnumerable<ConfigFileBuffer> Stream(Guid[] files)
        {
            //NpgsqlException: A statement cannot have more than 65535 parameters (PostgreSQL limit)

            if (files is null || files.Length == 0)
            {
                yield break;
            }

            using (NpgsqlConnection connection = _source.CreateConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandTimeout = 60; // seconds

                    command.CommandText = GenerateStreamCommand(in files);

                    ConfigureStreamParameters(in command, in files);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
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

            using (NpgsqlConnection connection = _source.CreateConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = connection.CreateCommand())
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
        private IEnumerable<NpgsqlDataReader> ExecuteReader(string script, int timeout)
        {
            using (NpgsqlConnection connection = _source.CreateConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = script;
                    command.CommandTimeout = timeout;

                    using (NpgsqlDataReader reader = command.ExecuteReader())
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

        private const string SELECT_TABLE_SCHEMA_SCRIPT = "SELECT column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name = LOWER($1);";
        internal override EntityDefinition GetDbTableSchema(in string tableName)
        {
            EntityDefinition table = new()
            {
                Name = tableName,
                DbName = tableName
            };

            using (NpgsqlConnection connection = _source.CreateConnection())
            {
                connection.Open();

                using (NpgsqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = SELECT_TABLE_SCHEMA_SCRIPT;
                    command.CommandTimeout = 10; // seconds

                    command.Parameters.Add(new NpgsqlParameter<string>()
                    {
                        TypedValue = tableName,
                        NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar
                    });

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            PropertyDefinition property = new()
                            {
                                Name = reader.GetString(0)
                            };

                            //TODO: configure property DataType

                            table.Properties.Add(property);
                        }
                        reader.Close();
                    }
                }
            }

            return table;
        }

        
        private const string IS_NEW_AGE_EXTENSIONS_SUPPORTED = "SELECT 1 FROM information_schema.columns WHERE table_name = '_extensionsinfo' AND column_name = '_extensionzippedinfo';";
        private const string SELECT_EXTENSIONS_SCRIPT =
            "SELECT _idrref, _extensionorder, CAST(_extname AS varchar), _updatetime, " +
            "_extensionusepurpose, _extensionscope, _extensionzippedinfo, " +
            "CAST(_masternode AS varchar), _usedindistributedinfobase, _version " +
            "FROM _extensionsinfo ORDER BY " +
            "CASE WHEN SUBSTRING(CAST(_masternode AS varchar), 1, 34) = '0:00000000000000000000000000000000' " +
            "THEN 1 ELSE 0 END, _extensionusepurpose, _extensionscope, _extensionorder;";
        internal override bool IsExtensionsSupported()
        {
            return (ExecuteScalar<int>(IS_NEW_AGE_EXTENSIONS_SUPPORTED, 10) == 1);
        }
        internal override List<ExtensionInfo> GetExtensions()
        {
            int YearOffset = GetYearOffset();

            List<ExtensionInfo> list = new();

            byte[] zippedInfo;

            foreach (NpgsqlDataReader reader in ExecuteReader(SELECT_EXTENSIONS_SCRIPT, 10))
            {
                zippedInfo = (byte[])reader.GetValue(6);

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
                    IsDistributed = reader.GetBoolean(8)
                };

                DecodeZippedInfo(in zippedInfo, in extension);

                list.Add(extension);
            }

            return list;
        }
    }
}

//SELECT column_name, data_type, udt_name, is_nullable
//     , numeric_precision, numeric_scale, character_maximum_length
//  FROM information_schema.columns
// WHERE table_catalog = 'erp_uh'
//   AND table_schema  = 'public'
//   AND table_name    = LOWER('_Document1588')
//SELECT dbs.column_name, entity.column_name
//  FROM
//(SELECT column_name, data_type, udt_name, is_nullable
//     , numeric_precision, numeric_scale, character_maximum_length
//  FROM information_schema.columns
// WHERE table_catalog = 'erp_uh'
//   AND table_schema  = 'public'
//   AND table_name    = LOWER('_Document1588')) AS dbs
//FULL JOIN (VALUES ('column_name'), ('_idrref')) AS entity(column_name)
//  ON dbs.column_name = entity.column_name