using DaJet.Metadata.Model;
using System.Text;

namespace DaJet.CodeGenerator
{
    internal class CSharpGenerator
    {
        public string GenerateSourceCode(InfoBase infoBase, ApplicationObject metaObject)
        {
            StringBuilder code = new StringBuilder($"public sealed class {metaObject.Name}\n");
            code.AppendLine("{");

            code.AppendLine($"\t[JsonPropertyName(\"Ref\")] [Key] public Guid Ссылка {{ get; set; }}");
            code.AppendLine($"\t[JsonPropertyName(\"DeletionMark\")] public bool ПометкаУдаления {{ get; set; }}");
            code.AppendLine($"\t[JsonPropertyName(\"Code\")] public string Код {{ get; set; }}");
            code.AppendLine($"\t[JsonPropertyName(\"Description\")] public string Наименование {{ get; set; }}");

            foreach (MetadataProperty property in metaObject.Properties)
            {
                if (property.Purpose == PropertyPurpose.System)
                {
                    continue;
                }
                code.AppendLine($"\tpublic {GetPropertyTypeName(infoBase, property)} {property.Name} {{ get; set; }}");
            }

            foreach (TablePart tablePart in metaObject.TableParts)
            {
                GenerateTablePartCode(infoBase, tablePart, code);
            }

            code.Append("}");
            return code.ToString();
        }
        private bool IsEnumType(InfoBase infoBase, DataTypeInfo typeInfo)
        {
            if (infoBase.ReferenceTypeUuids.TryGetValue(typeInfo.ReferenceTypeUuid, out ApplicationObject metaObject))
            {
                return (metaObject is Enumeration);
            }
            return false;
        }
        private string GetPropertyTypeName(InfoBase infoBase, MetadataProperty property)
        {
            if (property.PropertyType.IsMultipleType)
            {
                return "object";
            }
            else if (property.PropertyType.IsUuid)
            {
                return "Guid";
            }
            else if (property.PropertyType.IsValueStorage)
            {
                return "byte[]";
            }
            else if (property.PropertyType.CanBeString)
            {
                return "string";
            }
            else if (property.PropertyType.CanBeBoolean)
            {
                return "bool";
            }
            else if (property.PropertyType.CanBeNumeric)
            {
                return "decimal";
            }
            else if (property.PropertyType.CanBeDateTime)
            {
                return "DateTime";
            }
            else if (property.PropertyType.CanBeReference)
            {
                if (IsEnumType(infoBase, property.PropertyType))
                {
                    return "string";
                }
                else
                {
                    return "Guid";
                }
            }
            return "object";
        }
        private void GenerateTablePartCode(InfoBase infoBase, TablePart tablePart, StringBuilder code)
        {
            code.AppendLine($"\tpublic sealed class {tablePart.Name}");
            code.AppendLine("\t{");

            foreach (MetadataProperty property in tablePart.Properties)
            {
                if (property.Purpose == PropertyPurpose.System || property.Name == "НомерСтроки")
                {
                    continue;
                }
                code.AppendLine($"\t\tpublic {GetPropertyTypeName(infoBase, property)} {property.Name} {{ get; set; }}");
            }

            code.AppendLine("\t}");
        }

        public string GenerateSelectScript(ApplicationObject metaObject)
        {
            StringBuilder script = new StringBuilder();

            script.AppendLine($"SELECT");

            foreach (MetadataProperty property in metaObject.Properties)
            {
                GeneratePropertySelectScript(property, script);
            }
            script.Remove(script.Length - 1, 1);

            script.AppendLine();

            script.Append($"FROM {metaObject.TableName};");

            return script.ToString();
        }
        public void GeneratePropertySelectScript(MetadataProperty property, StringBuilder script)
        {
            for (int i = 0; i < property.Fields.Count; i++)
            {
                if (property.Fields[i].Purpose == FieldPurpose.TypeCode ||
                    property.Fields[i].Purpose == FieldPurpose.Discriminator)
                {
                    script.Append("CAST(");
                }

                script.Append(property.Fields[i].Name);

                if (property.Fields[i].Purpose == FieldPurpose.TypeCode ||
                    property.Fields[i].Purpose == FieldPurpose.Discriminator)
                {
                    script.Append(" AS int)");
                }

                script.Append(",");
            }
        }

        public string GenerateDataMapperCode(ApplicationObject metaObject)
        {
            StringBuilder code = new StringBuilder();

            code.AppendLine($"while (reader.Read())");
            code.AppendLine("{");

            foreach (MetadataProperty property in metaObject.Properties)
            {
                GeneratePropertyMapperCode(property, code);
            }

            code.AppendLine("}");
            return code.ToString();
        }
        public void GeneratePropertyMapperCode(MetadataProperty property, StringBuilder code)
        {
            for (int i = 0; i < property.Fields.Count; i++)
            {
                code.AppendLine($"\tentity.{property.Name} = reader[\"{property.Fields[i].Name}\"];");
            }
        }
    }
}
