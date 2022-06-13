using DaJet.Metadata;
using DaJet.Metadata.Model;
using System.Data;
using System.Text;

namespace DaJet.CodeGenerator.PostgreSql
{
    public sealed class SqlGenerator : ISqlGenerator
    {
        private readonly SqlGeneratorOptions _options;
        private readonly QueryExecutor _executor = new();

        private const string SCHEMA_PUBLIC = "public"; // default PostgreSQL schema

        private const string DROP_VIEW_SCRIPT =
            "DROP VIEW IF EXISTS {0};";

        private const string SELECT_VIEWS_SCRIPT =
            "SELECT table_schema, table_name " +
            "FROM information_schema.views " +
            "WHERE table_schema = '{0}';";

        private const string SCHEMA_EXISTS_SCRIPT =
            "SELECT 1 FROM information_schema.schemata WHERE schema_name = '{0}';";

        private const string CREATE_SCHEMA_SCRIPT = "CREATE SCHEMA {0};";

        private const string DROP_SCHEMA_SCRIPT = "DROP SCHEMA {0};";

        public SqlGenerator(SqlGeneratorOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _executor.ConnectionString = _options.ConnectionString;
        }

        private string GetNamespaceName(ApplicationObject metadata)
        {
            if (metadata is Catalog)
            {
                return $"СПР";
            }
            else if (metadata is Document)
            {
                return $"ДОК";
            }
            else if (metadata is InformationRegister)
            {
                return $"РС";
            }
            else if (metadata is AccumulationRegister)
            {
                return $"РН";
            }
            else if (metadata is Enumeration)
            {
                return $"ПРЧ";
            }
            else if (metadata is Constant)
            {
                return $"КСТ";
            }
            else if (metadata is Characteristic)
            {
                return $"ПВХ";
            }
            else if (metadata is Publication)
            {
                return $"ПО";
            }
            else if (metadata is Account)
            {
                return $"ПС";
            }
            else if (metadata is AccountingRegister)
            {
                return $"РБ";
            }

            return "Unknown";
        }
        private string CreateViewName(string viewName)
        {
            return $"{_options.Schema}.\"{viewName}\"";
        }
        private string CreateViewName(ApplicationObject metadata)
        {
            if (metadata is TablePart table)
            {
                return $"{_options.Schema}.\"{GetNamespaceName(table.Owner)}.{table.Owner.Name}_{table.Name}\"";
            }

            return $"{_options.Schema}.\"{GetNamespaceName(metadata)}.{metadata.Name}\"";
        }
        private string CreateFieldAlias(MetadataProperty property, DatabaseField field)
        {
            if (field.Purpose == FieldPurpose.Discriminator)
            {
                return "\"" + property.Name + "_Тип" + "\"";
            }
            else if (field.Purpose == FieldPurpose.TypeCode)
            {
                return "\"" + property.Name + "_ТипСсылки" + "\"";
            }
            else if (field.Purpose == FieldPurpose.Object)
            {
                return "\"" + property.Name + "_Ссылка" + "\"";
            }
            else if (field.Purpose == FieldPurpose.String)
            {
                return "\"" + property.Name + "_Строка" + "\"";
            }
            else if (field.Purpose == FieldPurpose.Boolean)
            {
                return "\"" + property.Name + "_Булево" + "\"";
            }
            else if (field.Purpose == FieldPurpose.Numeric)
            {
                return "\"" + property.Name + "_Число" + "\"";
            }
            else if (field.Purpose == FieldPurpose.DateTime)
            {
                return "\"" + property.Name + "_Дата" + "\"";
            }

            return "\"" + property.Name + "\"";
        }

        public bool SchemaExists(string name)
        {
            string script = string.Format(SCHEMA_EXISTS_SCRIPT, name);

            return (_executor.ExecuteScalar<int>(in script, 10) == 1);
        }
        public void CreateSchema(string name)
        {
            string script = string.Format(CREATE_SCHEMA_SCRIPT, name);

            _executor.ExecuteNonQuery(in script, 10);
        }
        public void DropSchema(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            string script = string.Format(DROP_SCHEMA_SCRIPT, name);

            _executor.ExecuteNonQuery(in script, 10);
        }
        private bool TryCreateSchemaIfNotExists(out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(_options.Schema))
            {
                _options.Schema = SCHEMA_PUBLIC;
            }

            if (_options.Schema == SCHEMA_PUBLIC)
            {
                return true;
            }

            try
            {
                if (!SchemaExists(_options.Schema))
                {
                    CreateSchema(_options.Schema);
                }
            }
            catch (Exception exception)
            {
                error = $"Failed to create schema [{_options.Schema}]: {ExceptionHelper.GetErrorText(exception)}";
            }

            return string.IsNullOrEmpty(error);
        }

        public bool TryCreateView(in ApplicationObject metadata, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(metadata.Name))
            {
                return true; //TODO: fix it in DaJet.Metadata
            }

            string name = CreateViewName(metadata);

            try
            {
                List<string> scripts = new()
                {
                    string.Format(DROP_VIEW_SCRIPT, name)
                };

                if (metadata is Enumeration enumeration)
                {
                    scripts.Add(GenerateEnumViewScript(enumeration));
                }
                else
                {
                    scripts.Add(GenerateViewScript(metadata));
                }

                _executor.TxExecuteNonQuery(scripts, 10);
            }
            catch (Exception exception)
            {
                error = $"[{name}] [{metadata.TableName}] {ExceptionHelper.GetErrorText(exception)}";
            }

            return string.IsNullOrEmpty(error);
        }
        public bool TryCreateViews(in InfoBase infoBase, out int result, out List<string> errors)
        {
            if (infoBase == null)
            {
                throw new ArgumentNullException(nameof(infoBase));
            }

            result = 0;
            errors = new();

            if (!TryCreateSchemaIfNotExists(out string error))
            {
                errors.Add(error);
                return false;
            }
            
            foreach (var ns in _options.Namespaces)
            {
                object? items = typeof(InfoBase).GetProperty(ns)?.GetValue(infoBase, null);

                if (items is not Dictionary<Guid, ApplicationObject> list)
                {
                    continue;
                }

                foreach (ApplicationObject metadata in list.Values)
                {
                    if (TryCreateView(metadata, out string error1))
                    {
                        result++;
                    }
                    else
                    {
                        errors.Add(error1);
                    }

                    if (metadata.TableParts.Count > 0)
                    {
                        foreach (TablePart table in metadata.TableParts)
                        {
                            if (TryCreateView(table, out string error2))
                            {
                                result++;
                            }
                            else
                            {
                                errors.Add(error2);
                            }
                        }
                    }
                }
            }

            return (errors.Count == 0);
        }
        
        public string GenerateViewScript(in ApplicationObject metadata)
        {
            bool isTablePart = (metadata is TablePart);

            StringBuilder script = new();
            StringBuilder fields = new();

            script.AppendLine($"CREATE VIEW {CreateViewName(metadata)} AS SELECT");

            foreach (MetadataProperty property in metadata.Properties)
            {
                // Костыль: табличные части документов "Бухгалтерия предприятия"
                // дублируют имя системного свойства _KeyField - "КлючСтроки"
                if (isTablePart && property.Name == "КлючСтроки" && property.Purpose != PropertyPurpose.System)
                {
                    property.Name = "Ключ_Строки";
                }

                foreach (DatabaseField field in property.Fields)
                {

                    if (fields.Length > 0) { fields.Append(','); }
                    
                    fields.AppendLine($"{field.Name} AS {CreateFieldAlias(property, field)}");
                }
            }

            script.Append(fields);

            script.AppendLine($"FROM {metadata.TableName};");

            return script.ToString();
        }
        public string GenerateEnumViewScript(in Enumeration enumeration)
        {
            StringBuilder script = new();
            StringBuilder fields = new();

            script.AppendLine($"CREATE VIEW {CreateViewName(enumeration)} AS");

            script.AppendLine("SELECT e._EnumOrder AS \"Порядок\", t.\"Имя\", t.\"Синоним\", t.\"Значение\"");
            script.AppendLine($"FROM {enumeration.TableName} AS e INNER JOIN");
            script.AppendLine("(");

            foreach (EnumValue value in enumeration.Values)
            {
                if (fields.Length > 0)
                {
                    fields.AppendLine("UNION ALL");
                }

                string uuid = value.Uuid.ToString("N");

                uuid =
                    uuid.Substring(16, 16) +
                    uuid.Substring(12, 4) +
                    uuid.Substring(8, 4) +
                    uuid.Substring(0, 8);

                fields.Append("SELECT ");
                fields.Append($"CAST('{value.Name}' AS mvarchar) AS \"Имя\", ");
                fields.Append($"CAST('{value.Alias}' AS mvarchar) AS \"Синоним\", ");
                fields.AppendLine($"CAST(E'\\\\x{uuid}' AS bytea) AS \"Значение\"");
            }

            script.Append(fields);
            script.Append(") AS t ON e._IDRRef = t.\"Значение\";");

            return script.ToString();
        }

        public int DropViews()
        {
            int result = 0;
            int VIEW_NAME = 1;

            string select = string.Format(SELECT_VIEWS_SCRIPT, _options.Schema);

            foreach (IDataReader reader in _executor.ExecuteReader(select, 30))
            {
                if (reader.IsDBNull(VIEW_NAME))
                {
                    continue;
                }

                string name = CreateViewName(reader.GetString(VIEW_NAME));

                string script = string.Format(DROP_VIEW_SCRIPT, name);

                _executor.ExecuteNonQuery(in script, 10);

                result++;
            }

            return result;
        }
        public void DropView(in ApplicationObject metadata)
        {
            if (string.IsNullOrWhiteSpace(metadata.Name))
            {
                return; //TODO: fix it in DaJet.Metadata
            }

            string name = CreateViewName(metadata);

            string script = string.Format(DROP_VIEW_SCRIPT, name);
            
            _executor.ExecuteNonQuery(script, 10);
        }

        public bool TryScriptViews(in InfoBase infoBase, out int result, out List<string> errors)
        {
            if (infoBase == null)
            {
                throw new ArgumentNullException(nameof(infoBase));
            }

            result = 0;
            errors = new();

            using (StreamWriter writer = new(_options.OutputFile, false, Encoding.UTF8))
            {
                foreach (var ns in _options.Namespaces)
                {
                    object? items = typeof(InfoBase).GetProperty(ns)?.GetValue(infoBase, null);

                    if (items is not Dictionary<Guid, ApplicationObject> list)
                    {
                        continue;
                    }

                    foreach (ApplicationObject metadata in list.Values)
                    {
                        if (TryScriptView(in writer, metadata, out string error1))
                        {
                            result++;
                        }
                        else
                        {
                            errors.Add(error1);
                        }

                        if (metadata.TableParts.Count > 0)
                        {
                            foreach (TablePart table in metadata.TableParts)
                            {
                                if (TryScriptView(in writer, table, out string error2))
                                {
                                    result++;
                                }
                                else
                                {
                                    errors.Add(error2);
                                }
                            }
                        }
                    }
                }
            }

            return (errors.Count == 0);
        }
        public bool TryScriptView(in StreamWriter writer, in ApplicationObject metadata, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(metadata.Name))
            {
                return true; //TODO: fix it in DaJet.Metadata
            }

            string name = CreateViewName(metadata);

            try
            {
                writer.WriteLine(string.Format(DROP_VIEW_SCRIPT, name));

                if (metadata is Enumeration enumeration)
                {
                    writer.WriteLine(GenerateEnumViewScript(enumeration));
                }
                else
                {
                    writer.WriteLine(GenerateViewScript(metadata));
                }
            }
            catch (Exception exception)
            {
                error = $"[{name}] [{metadata.TableName}] {ExceptionHelper.GetErrorText(exception)}";
            }

            return string.IsNullOrEmpty(error);
        }
    }
}