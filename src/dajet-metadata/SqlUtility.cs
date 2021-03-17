using DaJet.Metadata.Model;
using System.Linq;

namespace DaJet.Metadata
{
    public static class SqlUtility
    {
        public static string CreateTableFieldScript(DatabaseField field)
        {
            string SIZE = string.Empty;
            if (field.TypeName == "char"
                | field.TypeName == "nchar"
                | field.TypeName == "binary"
                | field.TypeName == "varchar"
                | field.TypeName == "nvarchar"
                | field.TypeName == "varbinary")
            {
                SIZE = (field.Length < 0) ? "(MAX)" : $"({field.Length})";
            }
            else if (field.Precision > 0 && field.TypeName != "bit")
            {
                SIZE = $"({field.Precision}, {field.Scale})";
            }
            string NULLABLE = field.IsNullable ? " NULL" : " NOT NULL";
            return $"[{field.Name}] [{field.TypeName}]{SIZE}{NULLABLE}";
        }
        public static FieldPurpose ParseFieldPurpose(string fieldName)
        {
            char L = char.Parse(MetadataTokens.L);
            char N = char.Parse(MetadataTokens.N);
            char T = char.Parse(MetadataTokens.T);
            char S = char.Parse(MetadataTokens.S);
            char B = char.Parse(MetadataTokens.B);

            char test = fieldName[fieldName.Count() - 1];

            if (char.IsDigit(test)) return FieldPurpose.Value;

            if (test == L)
            {
                return FieldPurpose.Boolean;
            }
            else if (test == N)
            {
                return FieldPurpose.Numeric;
            }
            else if (test == T)
            {
                return FieldPurpose.DateTime;
            }
            else if (test == S)
            {
                return FieldPurpose.String;
            }
            else if (test == B)
            {
                return FieldPurpose.Binary;
            }

            string TYPE = MetadataTokens.TYPE;
            string TRef = MetadataTokens.TRef;
            string RRef = MetadataTokens.RRef;

            string postfix = fieldName.Count() < 8 ? string.Empty : fieldName.Substring(fieldName.Count() - 4);

            if (postfix == TYPE)
            {
                return FieldPurpose.Discriminator;
            }
            else if (postfix == TRef)
            {
                return FieldPurpose.TypeCode;
            }
            else if (postfix == RRef)
            {
                return FieldPurpose.Object;
            }

            return FieldPurpose.Value;
        }
    }
}