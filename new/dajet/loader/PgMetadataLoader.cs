using DaJet.Data.PostgreSql;
using Npgsql;
using System.Data;
using System.Text;

namespace DaJet
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
    }
}