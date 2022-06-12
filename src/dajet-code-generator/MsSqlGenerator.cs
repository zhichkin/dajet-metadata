using DaJet.Metadata;
using DaJet.Metadata.Model;
using System.Data;
using System.Text;

namespace DaJet.CodeGenerator.SqlServer
{
    public sealed class SqlGenerator : ISqlGenerator
    {
        private readonly QueryExecutor _executor = new();
        private readonly SqlGeneratorOptions _options;
        
        private const string DROP_VIEW_SCRIPT =
            "IF OBJECT_ID(N'[{0}]', N'V') IS NOT NULL DROP VIEW [{0}];";

        private const string SELECT_VIEWS_SCRIPT =
            "SELECT s.name AS [Schema], v.name AS [View]" +
            "FROM sys.views AS v " +
            "INNER JOIN sys.schemas AS s " +
            "ON v.schema_id = s.schema_id AND is_ms_shipped = 0;";

        public SqlGenerator(SqlGeneratorOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _executor.ConnectionString = _options.ConnectionString;
        }

        private string GetNamespaceName(ApplicationObject metadata)
        {
            if (metadata is Catalog)
            {
                return $"Справочник";
            }
            else if (metadata is Document)
            {
                return $"Документ";
            }
            else if (metadata is InformationRegister)
            {
                return $"РегистрСведений";
            }
            else if (metadata is AccumulationRegister)
            {
                return $"РегистрНакопления";
            }
            else if (metadata is Enumeration)
            {
                return $"Перечисление";
            }
            else if (metadata is Constant)
            {
                return $"Константа";
            }
            else if (metadata is Characteristic)
            {
                return $"ПланВидовХарактеристик";
            }
            else if (metadata is Publication)
            {
                return $"ПланОбмена";
            }
            else if (metadata is Account)
            {
                return $"ПланСчетов";
            }
            else if (metadata is AccountingRegister)
            {
                return $"РегистрБухгалтерии";
            }

            return "Unknown";
        }
        private string CreateViewName(ApplicationObject metadata)
        {
            if (metadata is TablePart table)
            {
                return $"{GetNamespaceName(table.Owner)}.{table.Owner.Name}_{table.Name}";
            }

            return $"{GetNamespaceName(metadata)}.{metadata.Name}";
        }
        private string CreateFieldAlias(MetadataProperty property, DatabaseField field)
        {
            if (field.Purpose == FieldPurpose.Discriminator)
            {
                return property.Name + "_Тип";
            }
            else if (field.Purpose == FieldPurpose.TypeCode)
            {
                return property.Name + "_ТипСсылки";
            }
            else if (field.Purpose == FieldPurpose.Object)
            {
                return property.Name + "_Ссылка";
            }
            else if (field.Purpose == FieldPurpose.String)
            {
                return property.Name + "_Строка";
            }
            else if (field.Purpose == FieldPurpose.Boolean)
            {
                return property.Name + "_Булево";
            }
            else if (field.Purpose == FieldPurpose.Numeric)
            {
                return property.Name + "_Число";
            }
            else if (field.Purpose == FieldPurpose.DateTime)
            {
                return property.Name + "_Дата";
            }

            return property.Name;
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

            script.AppendLine($"CREATE VIEW [{CreateViewName(metadata)}] AS SELECT");

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
                    
                    fields.AppendLine($"{field.Name} AS [{CreateFieldAlias(property, field)}]");
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

            script.AppendLine($"CREATE VIEW [{CreateViewName(enumeration)}] AS");

            script.AppendLine("SELECT e._EnumOrder AS [Порядок], t.[Имя], t.[Синоним], t.[Значение]");
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
                fields.Append($"N'{value.Name}' AS [Имя], ");
                fields.Append($"N'{value.Alias}' AS [Синоним], ");
                fields.AppendLine($"0x{uuid} AS [Значение]");
            }

            script.Append(fields);
            script.Append(") AS t ON e._IDRRef = t.[Значение];");

            return script.ToString();
        }

        public int DropViews()
        {
            int result = 0;
            int VIEW_NAME = 1;

            foreach (IDataReader reader in _executor.ExecuteReader(SELECT_VIEWS_SCRIPT, 30))
            {
                if (reader.IsDBNull(VIEW_NAME))
                {
                    continue;
                }

                string name = reader.GetString(VIEW_NAME);

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
    }
}