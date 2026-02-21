using System.Numerics;
using System.Text;

namespace DaJet.TypeSystem
{
    [Flags] public enum DataTypeFlags : ushort
    {
        Undefined = 0x0000,
        Boolean   = 0x0001,
        Decimal   = 0x0002,
        Integer   = 0x0004,
        DateTime  = 0x0008,
        String    = 0x0010,
        Binary    = 0x0020,
        Uuid      = 0x0040,
        Entity    = 0x0080,
        Object    = 0x0100,
        Array     = 0x0200,
        UnionTypes = Boolean | Decimal | DateTime | String | Entity
    }
    [Flags] public enum QualifierFlags : ushort
    {
        None       = 0x0000,
        Fixed      = 0x0001, // variable | fixed
        UnSigned   = 0x0002, // signed | unsigned
        Date       = 0x0004, // date
        Time       = 0x0008, // time
        DateTime   = Date | Time, // datetime
        Sequential = 0x0010 // random UUID RFC 4122 версия 4 | sequential UUID RFC 4122 версия 1
    }
    public readonly struct DataType
    {
        private readonly DataTypeFlags _types;
        private readonly QualifierFlags _qualifiers;
        public DataType(
            DataTypeFlags types, QualifierFlags qualifiers = QualifierFlags.None,
            ushort size = 0, byte precision = 0, byte scale = 0, int typeCode = 0)
        {
            _types = types;
            _qualifiers = qualifiers;
            Size = size;
            Precision = precision;
            Scale = scale;
            TypeCode = typeCode;
        }
        public ushort Size { get; }
        public byte Precision { get; }
        public byte Scale { get; }
        public int TypeCode { get; }

        public static readonly DataType Undefined;
        public static readonly DataType Boolean = new(DataTypeFlags.Boolean);
        public static readonly DataType Date = new(DataTypeFlags.DateTime, QualifierFlags.Date);
        public static readonly DataType Time = new(DataTypeFlags.DateTime, QualifierFlags.Time);
        public static readonly DataType DateTime = new(DataTypeFlags.DateTime, QualifierFlags.DateTime);
        public static readonly DataType Object = new(DataTypeFlags.Object);
        public static readonly DataType Array = new(DataTypeFlags.Array);

        public static DataType Decimal(byte precision = 10, byte scale = 0, bool signed = true)
        {
            QualifierFlags qualifier = signed ? QualifierFlags.None : QualifierFlags.UnSigned;

            return new DataType(DataTypeFlags.Decimal, qualifier, 0, precision, scale, 0);
        }
        public static DataType Integer(ushort size = 4, bool signed = true)
        {
            QualifierFlags qualifier = signed ? QualifierFlags.None : QualifierFlags.UnSigned;

            return new DataType(DataTypeFlags.Integer, qualifier, size);
        }
        public static DataType String(ushort size = 0, bool variable = true)
        {
            QualifierFlags qualifier = variable ? QualifierFlags.None : QualifierFlags.Fixed;

            return new DataType(DataTypeFlags.String, qualifier, size);
        }
        public static DataType Binary(ushort size = 0, bool variable = true)
        {
            QualifierFlags qualifier = variable ? QualifierFlags.None : QualifierFlags.Fixed;

            return new DataType(DataTypeFlags.Binary, qualifier, size);
        }
        public static DataType Uuid(bool random = true)
        {
            QualifierFlags qualifier = random ? QualifierFlags.None : QualifierFlags.Sequential;

            return new DataType(DataTypeFlags.Uuid, qualifier);
        }
        public static DataType Entity(int typeCode = 0)
        {
            return new DataType(DataTypeFlags.Entity, typeCode: typeCode);
        }
        
        public readonly bool IsUndefined { get { return _types == DataTypeFlags.Undefined; } }
        public readonly bool IsBoolean { get { return (_types & DataTypeFlags.Boolean) == DataTypeFlags.Boolean; } }
        public readonly bool IsDecimal { get { return (_types & DataTypeFlags.Decimal) == DataTypeFlags.Decimal; } }
        public readonly bool IsInteger { get { return (_types & DataTypeFlags.Integer) == DataTypeFlags.Integer; } }
        public readonly bool IsDateTime { get { return (_types & DataTypeFlags.DateTime) == DataTypeFlags.DateTime; } }
        public readonly bool IsString { get { return (_types & DataTypeFlags.String) == DataTypeFlags.String; } }
        public readonly bool IsBinary { get { return (_types & DataTypeFlags.Binary) == DataTypeFlags.Binary; } }
        public readonly bool IsUuid { get { return (_types & DataTypeFlags.Uuid) == DataTypeFlags.Uuid; } }
        public readonly bool IsEntity { get { return (_types & DataTypeFlags.Entity) == DataTypeFlags.Entity; } }
        public readonly bool IsObject { get { return (_types & DataTypeFlags.Object) == DataTypeFlags.Object; } }
        public readonly bool IsArray { get { return (_types & DataTypeFlags.Array) == DataTypeFlags.Array; } }
        public readonly bool IsUnion
        {
            get
            {
                uint union = (uint)(_types & DataTypeFlags.UnionTypes);

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
        public readonly bool IsReferenceOnlyUnion
        {
            get
            {
                uint union = (uint)(_types & DataTypeFlags.UnionTypes);

                int count = BitOperations.PopCount(union);

                return (count == 1 && IsEntity && TypeCode == 0);
            }
        }
        public readonly bool IsFixed { get { return (_qualifiers & QualifierFlags.Fixed) == QualifierFlags.Fixed; } }
        public readonly bool IsSigned { get { return (_qualifiers & QualifierFlags.UnSigned) == 0; } }
        public readonly bool IsSequential { get { return (_qualifiers & QualifierFlags.Sequential) == QualifierFlags.Sequential; } }
        public readonly bool IsDateOnly
        {
            get
            {
                return (_qualifiers & QualifierFlags.Time) == 0
                    && (_qualifiers & QualifierFlags.Date) == QualifierFlags.Date;
            }
        }
        public readonly bool IsTimeOnly
        {
            get
            {
                return (_qualifiers & QualifierFlags.Date) == 0
                    && (_qualifiers & QualifierFlags.Time) == QualifierFlags.Time;
            }
        }

        public override string ToString()
        {
            StringBuilder view = new();

            if (IsUndefined)
            {
                view.Append("undefined");
            }
            else if (IsUnion)
            {
                if (IsReferenceOnlyUnion)
                {
                    view.Append("entity");
                }
                else
                {
                    view.Append("union(");
                    if (IsBoolean)
                    {
                        view.Append(" boolean");
                    }
                    if (IsDecimal)
                    {
                        if (IsSigned)
                        {
                            view.Append($" decimal({Precision},{Scale})");
                        }
                        else
                        {
                            view.Append($" decimal({Precision},{Scale},unsigned)");
                        }
                    }
                    if (IsDateTime)
                    {
                        if (IsDateOnly)
                        {
                            view.Append(" date");
                        }
                        else if (IsTimeOnly)
                        {
                            view.Append(" time");
                        }
                        else
                        {
                            view.Append(" datetime");
                        }
                    }
                    if (IsString)
                    {
                        if (Size == 0)
                        {
                            view.Append(" string");
                        }
                        else if (IsFixed)
                        {
                            view.Append($" string({Size}, fixed)");
                        }
                        else
                        {
                            view.Append($" string({Size})");
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
                if (IsDateOnly)
                {
                    view.Append("date");
                    
                }
                else if (IsTimeOnly)
                {
                    view.Append("time");
                }
                else
                {
                    view.Append("datetime");
                }
            }
            else if (IsString)
            {
                if (Size == 0)
                {
                    view.Append("string");
                }
                else if (IsFixed)
                {
                    view.Append($"string({Size},fixed)");
                }
                else
                {
                    view.Append($"string({Size})");
                }
            }
            else if (IsBinary)
            {
                if (Size == 0)
                {
                    view.Append("binary");
                }
                else if (IsFixed)
                {
                    view.Append($"binary({Size},fixed)");
                }
                else
                {
                    view.Append($"binary({Size})");
                }
            }
            else if (IsUuid)
            {
                if (IsSequential)
                {
                    view.Append("uuid(sequential)");
                }
                else
                {
                    view.Append("uuid");
                }
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
            else if (IsArray)
            {
                view.Append("array");
            }
            else if (IsObject)
            {
                view.Append("object");
            }

            return view.ToString();
        }
    }
}