using System.Numerics;
using System.Text;

namespace DaJet
{
    [Flags] internal enum DataTypeFlag : ushort
    {
        Null     = 0x0000,
        Boolean  = 0x0001,
        Decimal  = 0x0002,
        Integer  = 0x0004,
        DateTime = 0x0008,
        String   = 0x0010,
        Binary   = 0x0020,
        Uuid     = 0x0040,
        Entity   = 0x0080,
        Object   = 0x0100,
        Array    = 0x0200,
        SimpleTypes = Boolean | Decimal | DateTime | String,
        UnionTypes = Boolean | Decimal | DateTime | String | Entity
    }
    [Flags] internal enum QualifierFlag : ushort
    {
        None     = 0x0000,
        Fixed    = 0x0001, // variable | fixed
        UnSigned = 0x0002, // signed | unsigned
        Date     = 0x0004, // date | time | datetime
        Time     = 0x0008,
        DateTime = Date | Time
    }
    public sealed class DataType
    {
        private DataTypeFlag _types;
        private QualifierFlag _qualifiers;
        public bool IsUndefined { get { return _types == DataTypeFlag.Null; } }
        public ushort Size { get; set; }
        public byte Scale { get; set; }
        public byte Precision { get; set; }
        public int TypeCode { get; set; }

        #region "Конструкторы"
        public static DataType Boolean()
        {
            return new DataType() { IsBoolean = true };
        }
        public static DataType Decimal(byte precision, byte scale, NumericKind qualifier = NumericKind.CanBeNegative)
        {
            return new DataType()
            {
                IsDecimal = true,
                Scale = scale,
                Precision = precision,
                NumericQualifier = qualifier
            };
        }
        public static DataType Integer(ushort size = 4, NumericKind qualifier = NumericKind.CanBeNegative)
        {
            return new DataType()
            {
                IsInteger = true, Size = size, NumericQualifier = qualifier
            };
        }
        public static DataType DateTime(DateTimePart qualifier = DateTimePart.DateTime)
        {
            return new DataType() { IsDateTime = true, DateTimeQualifier = qualifier };
        }
        public static DataType String(ushort size = 10, StringKind qualifier = StringKind.Variable)
        {
            return new DataType()
            {
                IsString = true,
                Size = size,
                StringQualifier = qualifier
            };
        }
        public static DataType Binary(ushort size = 0)
        {
            return new DataType()
            {
                IsBinary = true, Size = size
            };
        }
        public static DataType Uuid()
        {
            return new DataType() { IsUuid = true };
        }
        public static DataType Entity(int typeCode = 0)
        {
            return new DataType()
            {
                IsEntity = true, TypeCode = typeCode
            };
        }

        public static DataType Union(params DataType[] types)
        {
            DataType union = new();

            foreach (DataType type in types)
            {
                if (type.IsBoolean)
                {
                    union.IsBoolean = true;
                }
                else if (type.IsDecimal)
                {
                    union.IsDecimal = true;
                    union.Scale = type.Scale;
                    union.Precision = type.Precision;
                }
                else if (type.IsDateTime)
                {
                    union.IsDateTime = true;
                    union.DateTimeQualifier = type.DateTimeQualifier;
                }
                else if (type.IsString)
                {
                    union.IsString = true;
                    union.Size = type.Size;
                    union.StringQualifier = type.StringQualifier;
                }
                else if (type.IsEntity)
                {
                    union.IsEntity = true;
                    union.TypeCode = type.TypeCode;
                }
            }

            return union;
        }

        #endregion

        #region "Составной тип данных"

        ///<summary>Типом значения свойства может быть "Булево" (поддерживает составной тип данных)</summary>
        public bool IsBoolean
        {
            get { return (_types & DataTypeFlag.Boolean) == DataTypeFlag.Boolean; }
            set
            {
                if ((_types & DataTypeFlag.UnionTypes) > 0)
                {
                    // Включён хотя бы один тип, поддерживающий составной тип данных
                }

                if (IsUuid || IsBinary) // Добавить сюда другие исключающие составной тип данных проверки, например, IsObject
                {
                    if (value) { _types = DataTypeFlag.Boolean; } // false is ignored
                }
                else if (value)
                {
                    _types |= DataTypeFlag.Boolean;
                }
                else if (IsBoolean)
                {
                    _types ^= DataTypeFlag.Boolean; // _types &= ~DataTypeFlag.Boolean;
                }
            }
        }

        ///<summary>Типом значения свойства может быть "Число" (поддерживает составной тип данных)</summary>
        public bool IsDecimal
        {
            get { return (_types & DataTypeFlag.Decimal) == DataTypeFlag.Decimal; }
            set
            {
                if (IsUuid || IsBinary)
                {
                    if (value) { _types = DataTypeFlag.Decimal; } // false is ignored
                }
                else if (value)
                {
                    _types |= DataTypeFlag.Decimal;
                }
                else if (IsDecimal)
                {
                    _types ^= DataTypeFlag.Decimal; // _types &= ~DataTypeFlag.Decimal;
                }
            }
        }

        ///<summary>Типом значения свойства может быть "Дата" (поддерживает составной тип данных)</summary>
        public bool IsDateTime
        {
            get { return (_types & DataTypeFlag.DateTime) == DataTypeFlag.DateTime; }
            set
            {
                if (IsUuid || IsBinary)
                {
                    if (value) { _types = DataTypeFlag.DateTime; } // false is ignored
                }
                else if (value)
                {
                    _types |= DataTypeFlag.DateTime;
                }
                else if (IsDateTime)
                {
                    _types ^= DataTypeFlag.DateTime; // _types &= ~DataTypeFlag.DateTime;
                }
            }
        }

        ///<summary>Типом значения свойства может быть "Строка" (поддерживает составной тип данных)</summary>
        public bool IsString
        {
            get { return (_types & DataTypeFlag.String) == DataTypeFlag.String; }
            set
            {
                if (IsUuid || IsBinary)
                {
                    if (value) { _types = DataTypeFlag.String; } // false is ignored
                }
                else if (value)
                {
                    _types |= DataTypeFlag.String;
                }
                else if (IsString)
                {
                    _types ^= DataTypeFlag.String; // _types &= ~DataTypeFlag.String;
                }
            }
        }

        ///<summary>Типом значения свойства может быть "Ссылка" (поддерживает составной тип данных)</summary>
        public bool IsEntity
        {
            get { return (_types & DataTypeFlag.Entity) == DataTypeFlag.Entity; }
            set
            {
                if (IsUuid || IsBinary)
                {
                    if (value) { _types = DataTypeFlag.Entity; } // false is ignored
                }
                else if (value)
                {
                    _types |= DataTypeFlag.Entity;
                }
                else if (IsEntity)
                {
                    _types ^= DataTypeFlag.Entity; // _types &= ~DataTypeFlag.Entity;
                }
            }
        }

        #endregion

        #region "Квалификаторы типов данных"

        public StringKind StringQualifier
        {
            get
            {
                return (_qualifiers & QualifierFlag.Fixed) == QualifierFlag.Fixed ? StringKind.Fixed : StringKind.Variable;
            }
            set
            {
                if (value == StringKind.Fixed)
                {
                    _qualifiers |= QualifierFlag.Fixed;
                }
                else
                {
                    _qualifiers &= ~QualifierFlag.Fixed;
                }
            }
        }

        public NumericKind NumericQualifier
        {
            get
            {
                return (_qualifiers & QualifierFlag.UnSigned) == QualifierFlag.UnSigned ? NumericKind.AlwaysPositive : NumericKind.CanBeNegative;
            }
            set
            {
                if (value == NumericKind.AlwaysPositive)
                {
                    _qualifiers |= QualifierFlag.UnSigned;
                }
                else
                {
                    _qualifiers &= ~QualifierFlag.UnSigned;
                }
            }
        }

        public DateTimePart DateTimeQualifier
        {
            get
            {
                if ((_qualifiers & QualifierFlag.DateTime) == QualifierFlag.DateTime)
                {
                    return DateTimePart.DateTime;
                }

                if ((_qualifiers & QualifierFlag.Date) == QualifierFlag.Date)
                {
                    return DateTimePart.Date;
                }

                return DateTimePart.Time;
            }
            set
            {
                if (value == DateTimePart.DateTime)
                {
                    _qualifiers |= QualifierFlag.DateTime;
                }
                else if (value == DateTimePart.Date)
                {
                    _qualifiers |= QualifierFlag.Date;
                    _qualifiers &= ~QualifierFlag.Time;
                }
                else
                {
                    _qualifiers |= QualifierFlag.Time;
                    _qualifiers &= ~QualifierFlag.Date;
                }
            }
        }

        #endregion

        ///<summary>Тип значения свойства "УникальныйИдентификатор", binary(16). Не поддерживает составной тип данных.</summary>
        public bool IsUuid
        {
            get { return (_types & DataTypeFlag.Uuid) == DataTypeFlag.Uuid; }
            set
            {
                if (value)
                {
                    _types = DataTypeFlag.Uuid;
                }
                else if (IsUuid)
                {
                    _types = DataTypeFlag.Null; // _types &= ~DataTypeFlag.Uuid;
                }
            }
        }

        ///<summary>Тип значения свойства "ХранилищеЗначения", varbinary(max). Не поддерживает составной тип данных.</summary>
        public bool IsBinary
        {
            get { return (_types & DataTypeFlag.Binary) == DataTypeFlag.Binary; }
            set
            {
                if (value)
                {
                    _types = DataTypeFlag.Binary; TypeCode = 0;
                }
                else if (IsBinary)
                {
                    _types = DataTypeFlag.Null;
                }
            }
        }

        public bool IsInteger
        {
            get { return (_types & DataTypeFlag.Integer) == DataTypeFlag.Integer; }
            set
            {
                if (value)
                {
                    _types = DataTypeFlag.Integer;
                }
                else
                {
                    _types &= ~DataTypeFlag.Integer;
                }
            }
        }

        ///<summary>Метод проверяет является ли описание составным типом данных</summary>
        public bool IsUnion
        {
            get
            {
                uint union = (uint)(_types & DataTypeFlag.UnionTypes);

                int count = BitOperations.PopCount(union);

                if (count > 1)
                {
                    return true;
                }

                if (count == 1 && IsEntity && TypeCode == 0)
                {
                    return true;
                }

                return false;
            }
        }

        public override string ToString()
        {
            StringBuilder view = new();

            if (IsUnion)
            {
                view.Append("union(");

                if (IsBoolean)
                {
                    view.Append(" boolean");
                }

                if (IsDecimal)
                {
                    if (NumericQualifier == NumericKind.CanBeNegative)
                    {
                        view.Append($" decimal({Precision},{Scale})");
                    }
                    else
                    {
                        view.Append($" decimal({Precision},{Scale}, unsigned)");
                    }
                }

                if (IsDateTime)
                {
                    if (DateTimeQualifier == DateTimePart.DateTime)
                    {
                        view.Append(" datetime");
                    }
                    else if (DateTimeQualifier == DateTimePart.Date)
                    {
                        view.Append(" date");
                    }
                    else
                    {
                        view.Append(" time");
                    }
                }

                if (IsString)
                {
                    if (StringQualifier == StringKind.Fixed)
                    {
                        view.Append($" string({Size}, fixed)");
                    }
                    else if (Size > 0)
                    {
                        view.Append($" string({Size})");
                    }
                    else
                    {
                        view.Append(" string");
                    }
                }

                if (IsEntity)
                {
                    if (TypeCode > 0)
                    {
                        view.Append($" entity({TypeCode})");
                    }
                    else
                    {
                        view.Append(" entity");
                    }
                }

                view.Append(" )");
            }
            else if (IsBoolean)
            {
                view.Append("boolean");
            }
            else if (IsDecimal)
            {
                view.Append($"decimal({Precision},{Scale})");
            }
            else if (IsInteger)
            {
                if (Size > 0)
                {
                    view.Append($"integer({Size})");
                }
                else
                {
                    view.Append("integer");
                }
            }
            else if (IsDateTime)
            {
                if (DateTimeQualifier == DateTimePart.DateTime)
                {
                    view.Append("datetime");
                }
                else if (DateTimeQualifier == DateTimePart.Date)
                {
                    view.Append("date");
                }
                else
                {
                    view.Append("time");
                }
            }
            else if (IsString)
            {
                if (StringQualifier == StringKind.Fixed)
                {
                    view.Append($"string({Size}, fixed)");
                }
                else if (Size > 0)
                {
                    view.Append($"string({Size})");
                }
                else
                {
                    view.Append("string");
                }
            }
            else if (IsBinary)
            {
                if (Size > 0)
                {
                    view.Append($"binary({Size})");
                }
                else
                {
                    view.Append("binary");
                }
            }
            else if (IsUuid)
            {
                view.Append("uuid");
            }
            else if (IsEntity)
            {
                if (TypeCode > 0)
                {
                    view.Append($"entity({TypeCode})");
                }
                else
                {
                    view.Append("entity");
                }
            }
            
            return view.ToString();
        }
    }
}