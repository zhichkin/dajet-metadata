using System.Reflection;
using System.Text;

namespace DaJet.CodeGenerator.CSharp
{
    public sealed class DatabaseAssemblyGenerator
    {
        //private string filePath = @"C:\Users\User\Desktop\GitHub\TestCode.cs";
        //private string assemblyPath = string.Empty;
        //public Assembly Generate(string serverName, string databaseName)
        //{
        //    MetadataReader reader = new MetadataReader();
        //    Database database = new Database()
        //    {
        //        Name = databaseName,
        //        Owner = new Server()
        //        {
        //            Address = serverName
        //        }
        //    };
        //    reader.ReadMetadata(database);

        //    string sourceCode = GenerateSourceCode(database);

        //    Assembly assembly = Assembly.GetExecutingAssembly();
        //    assemblyPath = Path.GetDirectoryName(assembly.Location);
        //    assemblyPath = Path.Combine(assemblyPath, "Databases");
        //    if (!Directory.Exists(assemblyPath))
        //    {
        //        Directory.CreateDirectory(assemblyPath);
        //    }
        //    assemblyPath = Path.Combine(assemblyPath, $"{databaseName}.dll");

        //    Compiler compiler = new Compiler();
        //    byte[] buffer = compiler.Compile(sourceCode, databaseName);
        //    File.WriteAllBytes(assemblyPath, buffer);
            
        //    return Assembly.Load(buffer);
        //}
        //private string GenerateSourceCode(Database database)
        //{
        //    StringBuilder code = new StringBuilder();
        //    code.AppendLine("using System;");
        //    code.AppendLine("using OneCSharp.DDL.Attributes;");
        //    code.AppendLine($"namespace {database.Name}");
        //    code.AppendLine("{"); // open database namespace
        //    foreach (var _namespace in database.Namespaces)
        //    {
        //        GenerateNamespaceCode(code, _namespace); // open namespace
        //        foreach (DataType dataType in _namespace.DataTypes)
        //        {
        //            if (dataType is MetaObject entity)
        //            {
        //                GenerateClassCode(code, entity);
        //            }
        //        }
        //        code.AppendLine("}"); // close namespace
        //    }
        //    code.Append("}"); // close database namespace and file

        //    //File.WriteAllText(filePath, code.ToString());
        //    return code.ToString();
        //}
        //private void GenerateNamespaceCode(StringBuilder code, Namespace _namespace)
        //{
        //    code.Insert(0, $"using {_namespace.Owner.Name}.{_namespace.Name};\n");
        //    code.AppendLine($"namespace {_namespace.Name}");
        //    code.AppendLine("{"); // open namespace
        //}
        //private void GenerateClassCode(StringBuilder code, MetaObject entity)
        //{
        //    code.Append($"[Entity(\"{entity.UUID.ToString()}\", TableName = \"{entity.Name}\", TypeCode = {entity.TypeCode.ToString()})] public sealed class {entity.Alias}");
        //    if (entity.Owner.Name == "Reference"
        //        || entity.Owner.Name == "Document")
        //    {
        //        code.Append(" : ReferenceObject");
        //    }
        //    code.AppendLine();
        //    code.AppendLine("{"); // open class
        //    foreach (Property property in entity.Properties.OrderBy(p => ((p.ValueType is ListType) ? 1 : 0)))
        //    {
        //        GeneratePropertyCode(code, entity, property);
        //    }
        //    code.AppendLine("}"); // close class
        //}
        //private void GeneratePropertyCode(StringBuilder code, MetaObject entity, Property property)
        //{
        //    if (property.ValueType is ListType listType) // nested meta-object = table part
        //    {
        //        MetaObject nested = (MetaObject)listType.Type;
        //        GenerateClassCode(code, nested);
        //        return;
        //    }

        //    foreach (Field field in property.Fields)
        //    {
        //        GenerateFieldCode(code, field);
        //    }

        //    string propertyType = "string";
        //    string propertyName = (entity.Alias == property.Name ? $"_{property.Name}" : property.Name);

        //    if (property.ValueType is MultipleType)
        //    {
        //        propertyType = "object";
        //    }
        //    else if (property.ValueType is MetaObject)
        //    {
        //        propertyType = "object";
        //    }
        //    else if (property.ValueType is SimpleType)
        //    {
        //        if (property.ValueType == SimpleType.Binary)
        //        {
        //            propertyType = "byte[]";
        //        }
        //        else if (property.ValueType == SimpleType.Boolean)
        //        {
        //            propertyType = "bool";
        //        }
        //        else if (property.ValueType == SimpleType.Numeric)
        //        {
        //            propertyType = "decimal";
        //        }
        //        else if (property.ValueType == SimpleType.String)
        //        {
        //            propertyType = "string";
        //        }
        //        else if (property.ValueType == SimpleType.DateTime)
        //        {
        //            propertyType = "DateTime";
        //        }
        //        else if (property.ValueType == SimpleType.UniqueIdentifier)
        //        {
        //            propertyType = "Guid";
        //        }
        //    }
        //    code.AppendLine($" public {propertyType} {propertyName} {{ get; set; }}");
        //}
        //private void GenerateFieldCode(StringBuilder code, Field field)
        //{
        //    //[Field(Name = "MyField", TypeName = "", IsPrimaryKey = true, KeyOrdinal = 0,
        //    //Length = 16, IsNullable = false, Precision = 0, Scale = 0,
        //    //IsAutoGenerated = false, Purpose = FieldPurpose.Numeric)]
        //    code.Append($"[Field(Name = \"{field.Name}\""); // open field attribute
        //    code.Append($", Purpose = FieldPurpose.{field.Purpose.ToString()}");
        //    code.Append($", TypeName = \"{field.TypeName}\"");
        //    if (field.Length > 0)
        //    {
        //        code.Append($", Length = {field.Length.ToString()}");
        //    }
        //    if (field.Scale > 0)
        //    {
        //        code.Append($", Scale = {field.Scale.ToString()}");
        //    }
        //    if (field.Precision > 0)
        //    {
        //        code.Append($", Precision = {field.Precision.ToString()}");
        //    }
        //    if (field.IsPrimaryKey)
        //    {
        //        code.Append($", IsPrimaryKey = true");
        //        code.Append($", KeyOrdinal = {field.KeyOrdinal.ToString()}");
        //    }
        //    if (field.IsNullable)
        //    {
        //        code.Append($", IsNullable = true");
        //    }
        //    if (field.IsReadOnly)
        //    {
        //        code.Append($", IsAutoGenerated = true");
        //    }
        //    code.Append(")]"); // close field
        //}
    }
}