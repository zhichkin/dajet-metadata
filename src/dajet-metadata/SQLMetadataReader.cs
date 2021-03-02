using DaJet.Metadata.Model;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DaJet.Metadata
{
    public sealed class SqlFieldInfo
    {
        public SqlFieldInfo() { }
        public int ORDINAL_POSITION;
        public string COLUMN_NAME;
        public string DATA_TYPE;
        public int CHARACTER_OCTET_LENGTH;
        public int CHARACTER_MAXIMUM_LENGTH;
        public byte NUMERIC_PRECISION;
        public byte NUMERIC_SCALE;
        public bool IS_NULLABLE;
        public override string ToString()
        {
            return COLUMN_NAME + " (" + DATA_TYPE + ")";
        }
    }
    public interface ISqlMetadataReader
    {
        void UseConnectionString(string connectionString);
        List<SqlFieldInfo> GetSqlFieldsOrderedByName(string tableName);
    }
    public sealed class SqlMetadataReader : ISqlMetadataReader
    {        
        private sealed class ClusteredIndexInfo
        {
            public ClusteredIndexInfo() { }
            public string NAME;
            public bool IS_UNIQUE;
            public bool IS_PRIMARY_KEY;
            public List<ClusteredIndexColumnInfo> COLUMNS = new List<ClusteredIndexColumnInfo>();
            public bool HasNullableColumns
            {
                get
                {
                    bool result = false;
                    foreach (ClusteredIndexColumnInfo item in COLUMNS)
                    {
                        if (item.IS_NULLABLE)
                        {
                            return true;
                        }
                    }
                    return result;
                }
            }
            public ClusteredIndexColumnInfo GetColumnByName(string name)
            {
                ClusteredIndexColumnInfo info = null;
                for (int i = 0; i < COLUMNS.Count; i++)
                {
                    if (COLUMNS[i].NAME == name) return COLUMNS[i];
                }
                return info;
            }
        }
        private sealed class ClusteredIndexColumnInfo
        {
            public ClusteredIndexColumnInfo() { }
            public byte KEY_ORDINAL;
            public string NAME;
            public bool IS_NULLABLE;
        }
        private string ConnectionString { get; set; }
        public void Load(string connectionString, MetaObject metaObject)
        {
            ConnectionString = connectionString;
            ReadSQLMetadata(metaObject);
            foreach (MetaObject nested in metaObject.MetaObjects)
            {
                ReadSQLMetadata(nested);
            }
        }
        private List<SqlFieldInfo> GetSqlFields(string tableName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"SELECT");
            sb.AppendLine(@"    ORDINAL_POSITION, COLUMN_NAME, DATA_TYPE,");
            sb.AppendLine(@"    ISNULL(CHARACTER_MAXIMUM_LENGTH, 0) AS CHARACTER_MAXIMUM_LENGTH,");
            sb.AppendLine(@"    ISNULL(NUMERIC_PRECISION, 0) AS NUMERIC_PRECISION,");
            sb.AppendLine(@"    ISNULL(NUMERIC_SCALE, 0) AS NUMERIC_SCALE,");
            sb.AppendLine(@"    CASE WHEN IS_NULLABLE = 'NO' THEN CAST(0x00 AS bit) ELSE CAST(0x01 AS bit) END AS IS_NULLABLE");
            sb.AppendLine(@"FROM");
            sb.AppendLine(@"    INFORMATION_SCHEMA.COLUMNS");
            sb.AppendLine(@"WHERE");
            sb.AppendLine(@"    TABLE_NAME = N'{0}'");
            sb.AppendLine(@"ORDER BY");
            sb.AppendLine(@"    ORDINAL_POSITION ASC;");

            string sql = string.Format(sb.ToString(), tableName);

            List<SqlFieldInfo> list = new List<SqlFieldInfo>();
            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SqlFieldInfo item = new SqlFieldInfo()
                            {
                                ORDINAL_POSITION = reader.GetInt32(0),
                                COLUMN_NAME = reader.GetString(1),
                                DATA_TYPE = reader.GetString(2),
                                CHARACTER_MAXIMUM_LENGTH = reader.GetInt32(3),
                                NUMERIC_PRECISION = reader.GetByte(4),
                                NUMERIC_SCALE = reader.GetByte(5),
                                IS_NULLABLE = reader.GetBoolean(6)
                            };
                            list.Add(item);
                        }
                    }
                }
            }
            return list;
        }
        private ClusteredIndexInfo GetClusteredIndexInfo(string tableName)
        {
            ClusteredIndexInfo info = null;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"SELECT");
            sb.AppendLine(@"    i.name,");
            sb.AppendLine(@"    i.is_unique,");
            sb.AppendLine(@"    i.is_primary_key,");
            sb.AppendLine(@"    c.key_ordinal,");
            sb.AppendLine(@"    f.name,");
            sb.AppendLine(@"    f.is_nullable");
            sb.AppendLine(@"FROM sys.indexes AS i");
            sb.AppendLine(@"INNER JOIN sys.tables AS t ON t.object_id = i.object_id");
            sb.AppendLine(@"INNER JOIN sys.index_columns AS c ON c.object_id = t.object_id AND c.index_id = i.index_id");
            sb.AppendLine(@"INNER JOIN sys.columns AS f ON f.object_id = t.object_id AND f.column_id = c.column_id");
            sb.AppendLine(@"WHERE");
            sb.AppendLine(@"    t.object_id = OBJECT_ID(@table) AND i.type = 1 -- CLUSTERED");
            sb.AppendLine(@"ORDER BY");
            sb.AppendLine(@"c.key_ordinal ASC;");
            string sql = sb.ToString();

            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                connection.Open();

                command.Parameters.AddWithValue("table", tableName);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        info = new ClusteredIndexInfo()
                        {
                            NAME = reader.GetString(0),
                            IS_UNIQUE = reader.GetBoolean(1),
                            IS_PRIMARY_KEY = reader.GetBoolean(2)
                        };
                        info.COLUMNS.Add(new ClusteredIndexColumnInfo()
                        {
                            KEY_ORDINAL = reader.GetByte(3),
                            NAME = reader.GetString(4),
                            IS_NULLABLE = reader.GetBoolean(5)
                        });
                        while (reader.Read())
                        {
                            info.COLUMNS.Add(new ClusteredIndexColumnInfo()
                            {
                                KEY_ORDINAL = reader.GetByte(3),
                                NAME = reader.GetString(4),
                                IS_NULLABLE = reader.GetBoolean(5)
                            });
                        }
                    }
                }
            }
            return info;
        }
        private void ReadSQLMetadata(MetaObject metaObject)
        {
            if (string.IsNullOrWhiteSpace(metaObject.TableName)) return;

            List<SqlFieldInfo> sql_fields = GetSqlFields(metaObject.TableName);

            ClusteredIndexInfo indexInfo = GetClusteredIndexInfo(metaObject.TableName);

            foreach (SqlFieldInfo info in sql_fields)
            {
                bool found = false; MetaField field = null;
                foreach (MetaProperty p in metaObject.Properties)
                {
                    if (string.IsNullOrEmpty(p.Field)) { continue; }

                    if (info.COLUMN_NAME.TrimStart('_') == MetadataTokens.Periodicity) { continue; }

                    if (info.COLUMN_NAME.TrimStart('_').StartsWith(p.Field))
                    {
                        field = new MetaField()
                        {
                            Name = info.COLUMN_NAME,
                            Purpose = SqlUtility.ParseFieldPurpose(info.COLUMN_NAME)
                        };
                        p.Fields.Add(field);
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    string propertyName = string.Empty;
                    DataTypeInfo propertyType = new DataTypeInfo();

                    if (metaObject.Owner != null // таблица итогов регистра накопления
                        && metaObject.TypeName == MetaObjectTypes.AccumulationRegister
                        && metaObject.Owner.TypeName == MetaObjectTypes.AccumulationRegister)
                    {
                        // find property name in main table object by field name
                        foreach (MetaProperty ownerProperty in metaObject.Owner.Properties)
                        {
                            if (ownerProperty.Fields.Where(f => f.Name == info.COLUMN_NAME).FirstOrDefault() != null)
                            {
                                propertyName = ownerProperty.Name;
                                if (ownerProperty.PropertyType.IsMultipleType)
                                {
                                    // ничего не делаем, так как DataTypeInfo является по умолчанию
                                    // многозначным типом (составным типом данных) для ссылочных типов (например, "Регистратор")
                                }
                                break;
                            }
                        }
                    }

                    MetaProperty property = metaObject.Properties.Where(p => p.Name == propertyName).FirstOrDefault();
                    if (property == null)
                    {
                        property = new MetaProperty()
                        {
                            Name = string.IsNullOrEmpty(propertyName) ? info.COLUMN_NAME : propertyName,
                            Purpose = PropertyPurpose.System
                        };
                        metaObject.Properties.Add(property);
                        
                        MatchFieldToProperty(info, metaObject, property);

                        //TODO: нужно провести рефакторинг этого кода
                        //if (property.PropertyType == (int)DataTypes.NULL)
                        //{
                        //    property.PropertyType = propertyType;
                        //}
                    }

                    field = new MetaField()
                    {
                        Name = info.COLUMN_NAME,
                        Purpose = SqlUtility.ParseFieldPurpose(info.COLUMN_NAME)
                    };
                    property.Fields.Add(field);
                }
                field.TypeName = info.DATA_TYPE;
                field.Length = info.CHARACTER_MAXIMUM_LENGTH;
                field.Precision = info.NUMERIC_PRECISION;
                field.Scale = info.NUMERIC_SCALE;
                field.IsNullable = info.IS_NULLABLE;

                if (indexInfo != null)
                {
                    ClusteredIndexColumnInfo columnInfo = indexInfo.GetColumnByName(info.COLUMN_NAME);
                    if (columnInfo != null)
                    {
                        field.IsPrimaryKey = true;
                        field.KeyOrdinal = columnInfo.KEY_ORDINAL;
                    }
                }
            }
        }
        private void MatchFieldToProperty(SqlFieldInfo field, MetaObject metaObject, MetaProperty property)
        {
            string columnName = field.COLUMN_NAME.TrimStart('_');
            if (columnName.StartsWith(MetadataTokens.IDRRef))
            {
                property.Name = "Ссылка";
                property.Field = MetadataTokens.IDRRef;
                property.PropertyType.CanBeReference = true;
                property.PropertyType.ReferenceTypeCode = metaObject.TypeCode;
            }
            else if (columnName.StartsWith(MetadataTokens.Version))
            {
                property.Name = "ВерсияДанных";
                property.Field = MetadataTokens.Version;
                property.PropertyType.IsBinary = true;
            }
            else if (columnName.StartsWith(MetadataTokens.Marked))
            {
                property.Name = "ПометкаУдаления";
                property.Field = MetadataTokens.Marked;
                property.PropertyType.CanBeBoolean = true;
            }
            else if (columnName.StartsWith(MetadataTokens.PredefinedID))
            {
                property.Name = "ИмяПредопределённыхДанных";
                property.Field = MetadataTokens.PredefinedID;
                property.PropertyType.IsUuid = true;
            }
            else if (columnName.StartsWith(MetadataTokens.Code))
            {
                property.Name = "Код";
                property.Field = MetadataTokens.Code;
                if (field.DATA_TYPE.Contains("char"))
                {
                    property.PropertyType.CanBeString = true;
                }
                else
                {
                    property.PropertyType.CanBeNumeric = true;
                }
            }
            else if (columnName.StartsWith(MetadataTokens.Description))
            {
                property.Name = "Наименование";
                property.Field = MetadataTokens.Description;
                property.PropertyType.CanBeString = true;
            }
            else if (columnName.StartsWith(MetadataTokens.Folder))
            {
                property.Name = "ЭтоГруппа";
                property.Field = MetadataTokens.Folder;
                property.PropertyType.CanBeBoolean = true;
            }
            else if (columnName.StartsWith(MetadataTokens.ParentIDRRef))
            {
                property.Name = "Родитель";
                property.Field = MetadataTokens.ParentIDRRef;
                property.PropertyType.CanBeReference = true;
                property.PropertyType.ReferenceTypeCode = metaObject.TypeCode;
            }
            else if (columnName.StartsWith(MetadataTokens.OwnerID))
            {
                property.Name = "Владелец";
                property.Field = MetadataTokens.OwnerID;
                property.PropertyType.CanBeReference = true;
                if (field.COLUMN_NAME.Contains(MetadataTokens.TRef)
                    || field.COLUMN_NAME.Contains(MetadataTokens.TYPE))
                {
                    property.PropertyType.ReferenceTypeCode = 0; // multiple type
                }
                else
                {
                    //TODO: рефакторинг логики этого кода
                }
            }
            else if (columnName.StartsWith(MetadataTokens.DateTime))
            {
                property.Name = "Дата";
                property.Field = MetadataTokens.DateTime;
                property.PropertyType.CanBeDateTime = true;
            }
            else if (columnName == MetadataTokens.Number)
            {
                property.Name = "Номер";
                property.Field = MetadataTokens.Number;
                // TODO: find out field data type: string or numeric
                //property.PropertyTypes.Add((int)DataTypes.String);
            }
            else if (columnName.StartsWith(MetadataTokens.Posted))
            {
                property.Name = "Проведён";
                property.Field = MetadataTokens.Posted;
                property.PropertyType.CanBeBoolean = true;
            }
            else if (columnName == MetadataTokens.NumberPrefix)
            {
                property.Name = "МоментВремени";
                property.Field = MetadataTokens.NumberPrefix;
            }
            else if (columnName.StartsWith(MetadataTokens.Periodicity))
            {
                property.Name = "Периодичность";
                property.Field = MetadataTokens.Periodicity;
            }
            else if (columnName.StartsWith(MetadataTokens.Period))
            {
                property.Name = "Период";
                property.Field = MetadataTokens.Period;
            }
            else if (columnName.StartsWith(MetadataTokens.ActualPeriod))
            {
                property.Name = "ПериодАктуальности";
                property.Field = MetadataTokens.ActualPeriod;
            }
            else if (columnName.StartsWith(MetadataTokens.Recorder))
            {
                property.Name = "Регистратор";
                property.Field = MetadataTokens.Recorder;
                // TODO: apply object or multiple data type
                property.PropertyType.CanBeReference = true;
            }
            else if (columnName.StartsWith(MetadataTokens.Active))
            {
                property.Name = "Активность";
                property.Field = MetadataTokens.Active;
                property.PropertyType.CanBeBoolean = true;
            }
            else if (columnName.StartsWith(MetadataTokens.LineNo))
            {
                property.Name = "НомерСтроки";
                property.Field = MetadataTokens.LineNo;
            }
            else if (columnName.StartsWith(MetadataTokens.RecordKind))
            {
                property.Name = "ВидДвижения";
                property.Field = MetadataTokens.RecordKind;
            }
            else if (columnName.StartsWith(MetadataTokens.KeyField))
            {
                property.Name = "КлючСтроки";
                property.Field = MetadataTokens.KeyField;
            }
            else if (columnName.EndsWith(MetadataTokens.IDRRef)) // табличные части
            {
                property.Name = "Ссылка";
                property.Field = MetadataTokens.IDRRef;
                property.PropertyType.CanBeReference = true;
                if (metaObject.Owner != null)
                {
                    property.PropertyType.ReferenceTypeCode = metaObject.Owner.TypeCode;
                }
            }
            else if (columnName == MetadataTokens.EnumOrder)
            {
                property.Name = "Порядок";
                property.Field = MetadataTokens.EnumOrder;
            }
            else if (columnName == MetadataTokens.Type) // ПланВидовХарактеристик
            {
                property.Name = "ТипЗначения"; // ОписаниеТипов - TypeConstraint
                property.Field = MetadataTokens.Type;
            }
            else if (columnName == MetadataTokens.Splitter)
            {
                property.Name = "РазделительИтогов";
                property.Field = MetadataTokens.Splitter;
            }
            else if (columnName == MetadataTokens.NodeTRef)
            {
                property.Name = "Узел";
                property.Field = MetadataTokens.Node;
                property.PropertyType.CanBeNumeric = true;
                // TODO: это код типа плана обмена
            }
            else if (columnName == MetadataTokens.NodeRRef)
            {
                property.Name = "Узел";
                property.Field = MetadataTokens.Node;
                property.PropertyType.CanBeReference = true;
            }
            else if (columnName == MetadataTokens.MessageNo)
            {
                property.Name = "НомерСообщения";
                property.Field = MetadataTokens.MessageNo;
            }
            else if (columnName == MetadataTokens.RepetitionFactor)
            {
                property.Name = "КоэффициентПериодичности";
                property.Field = MetadataTokens.RepetitionFactor;
            }
            else if (columnName == MetadataTokens.UseTotals)
            {
                property.Name = "ИспользоватьИтоги";
                property.Field = MetadataTokens.UseTotals;
            }
            else if (columnName == MetadataTokens.UseSplitter)
            {
                property.Name = "ИспользоватьРазделительИтогов";
                property.Field = MetadataTokens.UseSplitter;
            }
            else if (columnName == MetadataTokens.MinPeriod)
            {
                property.Name = "МинимальныйПериод";
                property.Field = MetadataTokens.MinPeriod;
            }
            else if (columnName == MetadataTokens.MinCalculatedPeriod)
            {
                property.Name = "МинимальныйПериодРассчитанныхИтогов";
                property.Field = MetadataTokens.MinCalculatedPeriod;
            }
            else if (columnName == MetadataTokens.Kind)
            {
                property.Name = "ТипСчёта";
                property.Field = MetadataTokens.Kind;
                property.PropertyType.CanBeNumeric = true;
            }
            else if (columnName == MetadataTokens.OrderField)
            {
                property.Name = "Порядок";
                property.Field = MetadataTokens.OrderField;
                property.PropertyType.CanBeString = true;
            }
            else if (columnName == MetadataTokens.OffBalance)
            {
                property.Name = "Забалансовый";
                property.Field = MetadataTokens.OffBalance;
                property.PropertyType.CanBeBoolean = true;
            }
            else if (columnName == MetadataTokens.AccountDtRRef)
            {
                property.Name = "СчётДебет";
                property.Field = MetadataTokens.AccountDtRRef;
                property.PropertyType.CanBeReference = true;
            }
            else if (columnName == MetadataTokens.AccountCtRRef)
            {
                property.Name = "СчётКредит";
                property.Field = MetadataTokens.AccountCtRRef;
                property.PropertyType.CanBeReference = true;
            }
            else if (columnName == MetadataTokens.EDHashDt)
            {
                property.Name = "ХешДебет";
                property.Field = MetadataTokens.EDHashDt;
                property.PropertyType.CanBeNumeric = true;
            }
            else if (columnName == MetadataTokens.EDHashCt)
            {
                property.Name = "ХэшКредит";
                property.Field = MetadataTokens.EDHashCt;
                property.PropertyType.CanBeNumeric = true;
            }
            else if (columnName == MetadataTokens.SentNo)
            {
                property.Name = "НомерОтправленного";
                property.Field = MetadataTokens.SentNo;
                property.PropertyType.CanBeNumeric = true;
            }
            else if (columnName == MetadataTokens.ReceivedNo)
            {
                property.Name = "НомерПринятого";
                property.Field = MetadataTokens.ReceivedNo;
                property.PropertyType.CanBeNumeric = true;
            }
        }


        public void UseConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
        }
        private string SelectSqlFieldsOrderedByNameScript()
        {
            StringBuilder script = new StringBuilder();
            script.AppendLine("SELECT");
            script.AppendLine("c.column_id AS ORDINAL_POSITION,");
            script.AppendLine("c.name AS COLUMN_NAME,");
            script.AppendLine("s.name AS DATA_TYPE,");
            script.AppendLine("c.max_length AS CHARACTER_OCTET_LENGTH,");
            script.AppendLine("c.max_length AS CHARACTER_MAXIMUM_LENGTH,"); // TODO: for nchar and nvarchar devide by 2
            script.AppendLine("c.precision AS NUMERIC_PRECISION,");
            script.AppendLine("c.scale AS NUMERIC_SCALE,");
            script.AppendLine("c.is_nullable AS IS_NULLABLE");
            script.AppendLine("FROM sys.tables AS t");
            script.AppendLine("INNER JOIN sys.columns AS c ON c.object_id = t.object_id");
            script.AppendLine("INNER JOIN sys.types AS s ON c.user_type_id = s.user_type_id");
            script.AppendLine("WHERE t.object_id = OBJECT_ID(@tableName)");
            script.AppendLine("ORDER BY c.name ASC;");
            return script.ToString();
        }
        public List<SqlFieldInfo> GetSqlFieldsOrderedByName(string tableName)
        {
            List<SqlFieldInfo> list = new List<SqlFieldInfo>();
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(SelectSqlFieldsOrderedByNameScript(), connection))
            {
                command.Parameters.AddWithValue("tableName", tableName);
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        SqlFieldInfo item = new SqlFieldInfo();
                        item.ORDINAL_POSITION = reader.GetInt32(0);
                        item.COLUMN_NAME = reader.GetString(1);
                        item.DATA_TYPE = reader.GetString(2);
                        item.CHARACTER_OCTET_LENGTH = reader.GetInt16(3);
                        item.CHARACTER_MAXIMUM_LENGTH = reader.GetInt16(4);
                        item.NUMERIC_PRECISION = reader.GetByte(5);
                        item.NUMERIC_SCALE = reader.GetByte(6);
                        item.IS_NULLABLE = reader.GetBoolean(7);
                        list.Add(item);
                    }
                }
            }
            return list;
        }
    }
}