using Npgsql;
using System.Collections.Generic;
using System.Text;

namespace DaJet.Metadata
{
    public sealed class PostgresMetadataReader : ISqlMetadataReader
    {
        private string ConnectionString { get; set; }

        public void UseConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
        }
        private string SelectSqlFieldsOrderedByNameScript()
        {
            StringBuilder script = new StringBuilder();
            script.AppendLine("SELECT");
            script.AppendLine("a.attname as \"COLUMN_NAME\",");
            script.AppendLine("pg_catalog.format_type(a.atttypid, a.atttypmod) as \"DATA_TYPE\"");
            script.AppendLine("FROM pg_catalog.pg_attribute a");
            script.AppendLine("WHERE");
            script.AppendLine("a.attnum > 0");
            script.AppendLine("AND NOT a.attisdropped");
            script.AppendLine("AND a.attrelid = (");
            script.AppendLine("SELECT c.oid");
            script.AppendLine("FROM pg_catalog.pg_class c");
            script.AppendLine("LEFT JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace");
            script.AppendLine("WHERE c.relname = '{tableName}'");
            script.AppendLine("AND pg_catalog.pg_table_is_visible(c.oid)");
            script.AppendLine(")");
            script.AppendLine("order by a.attname asc;");
            return script.ToString();
        }
        public List<SqlFieldInfo> GetSqlFieldsOrderedByName(string tableName)
        {
            List<SqlFieldInfo> list = new List<SqlFieldInfo>();
            string script = SelectSqlFieldsOrderedByNameScript();
            script = script.Replace("{tableName}", tableName.ToLowerInvariant());
            using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString))
            using (NpgsqlCommand command = new NpgsqlCommand(script, connection))
            {
                connection.Open();
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        SqlFieldInfo item = new SqlFieldInfo();
                        item.COLUMN_NAME = reader.GetString(0);
                        item.DATA_TYPE = reader.GetString(1);
                        list.Add(item);
                    }
                }
            }
            return list;
        }
    }
}