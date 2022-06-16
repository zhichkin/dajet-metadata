using DaJet.Metadata.Model;

namespace DaJet.CodeGenerator
{
    internal class JdtoUtility
    {
        private const string CONST_TYPE_ENUM = "jcfg:EnumRef";
        private const string CONST_TYPE_CATALOG = "jcfg:CatalogRef";
        private const string CONST_TYPE_DOCUMENT = "jcfg:DocumentRef";
        private const string CONST_TYPE_EXCHANGE_PLAN = "jcfg:ExchangePlanRef";
        private const string CONST_TYPE_CHARACTERISTIC = "jcfg:ChartOfCharacteristicTypesRef";
        private const string CONST_REF = "Ref";
        private const string CONST_TYPE = "#type";
        private const string CONST_VALUE = "#value";
        private const string CONST_TYPE_STRING = "jxs:string";
        private const string CONST_TYPE_DECIMAL = "jxs:decimal";
        private const string CONST_TYPE_BOOLEAN = "jxs:boolean";
        private const string CONST_TYPE_DATETIME = "jxs:dateTime";
        private const string CONST_TYPE_CATALOG_REF = "jcfg:CatalogRef";
        private const string CONST_TYPE_CATALOG_OBJ = "jcfg:CatalogObject";
        private const string CONST_TYPE_DOCUMENT_REF = "jcfg:DocumentRef";
        private const string CONST_TYPE_DOCUMENT_OBJ = "jcfg:DocumentObject";
        private const string CONST_TYPE_OBJECT_DELETION = "jent:ObjectDeletion";
        private const string CONST_TYPE_INFO_REGISTER_SET = "jcfg:InformationRegisterRecordSet";
        private const string CONST_TYPE_ACCUM_REGISTER_SET = "jcfg:AccumulationRegisterRecordSet";

        private static readonly List<string> RegisterPropertyOrder = new List<string>()
        {
            "Регистратор", // Recorder   - uuid { #type + #value }
            "Период",      // Period     - DateTime
            "ВидДвижения", // RecordType - string { "Receipt", "Expense" }
            "Активность"   // Active     - bool
        };
        private readonly Dictionary<string, int> CatalogPropertyOrder = new Dictionary<string, int>()
        {
            { "ЭтоГруппа",        0 }, // IsFolder           - bool (invert)
            { "Ссылка",           1 }, // Ref                - uuid
            { "ПометкаУдаления",  2 }, // DeletionMark       - bool
            { "Владелец",         3 }, // Owner              - { #type + #value }
            { "Родитель",         4 }, // Parent             - uuid
            { "Код",              5 }, // Code               - string | number
            { "Наименование",     6 }, // Description        - string
            { "Предопределённый", 7 }  // PredefinedDataName - string
        };
        private readonly Dictionary<string, int> DocumentPropertyOrder = new Dictionary<string, int>()
        {
            { "Ссылка",           0 }, // Ref          - uuid
            { "ПометкаУдаления",  1 }, // DeletionMark - bool
            { "Дата",             2 }, // Date         - DateTime
            { "Номер",            3 }, // Number       - string | number
            { "Проведён",         4 }  // Posted       - bool
        };

        private int ValueOrdinal = -1;
        private int NumberOrdinal = -1;
        private int StringOrdinal = -1;
        private int ObjectOrdinal = -1;
        private int BooleanOrdinal = -1;
        private int DateTimeOrdinal = -1;
        private int TypeCodeOrdinal  = -1;
        private int DiscriminatorOrdinal = -1;
        internal void Initialize(InfoBase infoBase, MetadataProperty property, ref int ordinal)
        {
            if (infoBase.ReferenceTypeUuids.TryGetValue(property.PropertyType.ReferenceTypeUuid, out ApplicationObject metaObject))
            {
                Enumeration _enumeration = metaObject as Enumeration;
            }

            for (int i = 0; i < property.Fields.Count; i++)
            {
                ordinal++;

                FieldPurpose purpose = property.Fields[i].Purpose;

                if (purpose == FieldPurpose.Value)
                {
                    ValueOrdinal = ordinal;
                }
                else if (purpose == FieldPurpose.Discriminator)
                {
                    DiscriminatorOrdinal = ordinal; // binary(1) -> byte
                    // 0x01 - Неопределено -> null     -> null
                    // 0x02 - Булево       -> bool     -> jxs:boolean  + true | false
                    // 0x03 - Число        -> decimal  -> jxs:decimal  + numeric
                    // 0x04 - Дата         -> DateTime -> jxs:dateTime + string (ISO 8601)
                    // 0x05 - Строка       -> string   -> jxs:string   + string
                    // 0x08 - Ссылка       -> Guid     -> jcfg:EnumRef     + Name
                    //                                  | jcfg:CatalogRef  + UUID
                    // EntityRef { TypeCode, Identity } | jcfg:DocumentRef + UUID
                }
                else if (purpose == FieldPurpose.TypeCode)
                {
                    TypeCodeOrdinal = ordinal; // binary(4) -> int
                }
                else if (purpose == FieldPurpose.String)
                {
                    StringOrdinal = ordinal; // nvarchar | nchar -> string
                }
                else if (purpose == FieldPurpose.Boolean)
                {
                    BooleanOrdinal = ordinal; // binary(1) -> 0x00 | 0x01 -> bool
                }
                else if (purpose == FieldPurpose.Object)
                {
                    ObjectOrdinal = ordinal; // binary(16) -> Guid
                }
                else if (purpose == FieldPurpose.Numeric)
                {
                    NumberOrdinal = ordinal; // numeric -> decimal | int | long
                }
                else if (purpose == FieldPurpose.DateTime)
                {
                    DateTimeOrdinal = ordinal; // datetime2 -> DateTime
                }
                else
                {
                    // this should not happen =)
                }
            }
        }
    }
}