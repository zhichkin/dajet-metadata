using DaJet.Metadata;
using DaJet.Metadata.Model;
using System.Text;

namespace DaJet.CodeGenerator.SqlServer
{
    public sealed class SqlGenerator
    {
        private readonly QueryExecutor _executor = new QueryExecutor(DatabaseProvider.SQLServer);
        private const string DROP_VIEW_SCRIPT = "IF OBJECT_ID(N'[{0}]', N'V') IS NOT NULL DROP VIEW [{0}];";
        public void Configure(string connectionString)
        {
            _executor.UseConnectionString(connectionString);
        }
        public void GenerateViews(InfoBase infoBase)
        {
            foreach (Catalog catalog in infoBase.Catalogs.Values)
            {
                string script = GenerateViewScript(catalog);

                foreach (TablePart table in catalog.TableParts)
                {
                    script = GenerateViewScript(table);
                }
            }
        }
        private string GetNamespaceName(ApplicationObject metaObject)
        {
            if (metaObject is Catalog)
            {
                return $"Справочник";
            }
            else if (metaObject is Document)
            {
                return $"Документ";
            }
            else if (metaObject is InformationRegister)
            {
                return $"РегистрСведений";
            }
            else if (metaObject is AccumulationRegister)
            {
                return $"РегистрНакопления";
            }
            else if (metaObject is Enumeration)
            {
                return $"Перечисление";
            }
            else if (metaObject is Constant)
            {
                return $"Константа";
            }
            else if (metaObject is Characteristic)
            {
                return $"ПланВидовХарактеристик";
            }
            else if (metaObject is Publication)
            {
                return $"ПланОбмена";
            }
            else if (metaObject is Account)
            {
                return $"ПланСчетов";
            }
            else if (metaObject is AccountingRegister)
            {
                return $"РегистрБухгалтерии";
            }

            return "Unknown";
        }
        private string CreateViewName(ApplicationObject metaObject)
        {
            if (metaObject is TablePart table)
            {
                return $"{GetNamespaceName(table.Owner)}.{table.Owner.Name}_{table.Name}";
            }

            return $"{GetNamespaceName(metaObject)}.{metaObject.Name}";
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
        public bool TryCreateView(ApplicationObject metaObject, out string error)
        {
            error = string.Empty;

            string name = CreateViewName(metaObject);

            try
            {
                List<string> scripts = new List<string>()
                {
                    string.Format(DROP_VIEW_SCRIPT, name)
                };

                if (metaObject is Enumeration enumeration)
                {
                    scripts.Add(GenerateEnumViewScript(enumeration));
                }
                else
                {
                    scripts.Add(GenerateViewScript(metaObject));
                }

                _executor.TxExecuteNonQuery(scripts, 10);
            }
            catch (Exception exception)
            {
                error = ExceptionHelper.GetErrorText(exception);
            }

            return string.IsNullOrEmpty(error);
        }
        public string GenerateViewScript(ApplicationObject metaObject)
        {
            StringBuilder script = new();
            StringBuilder fields = new();

            script.AppendLine($"CREATE VIEW [{CreateViewName(metaObject)}] AS SELECT");

            foreach (MetadataProperty property in metaObject.Properties)
            {
                foreach (DatabaseField field in property.Fields)
                {

                    if (fields.Length > 0) { fields.Append(','); }
                    
                    fields.AppendLine($"{field.Name} AS [{CreateFieldAlias(property, field)}]");
                }
            }

            script.Append(fields);

            script.AppendLine($"FROM {metaObject.TableName};");

            return script.ToString();
        }
        public string GenerateEnumViewScript(Enumeration enumeration)
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
    }
}