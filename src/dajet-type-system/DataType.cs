using System.Collections.Frozen;
using System.Numerics;

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
    public readonly struct DataType : IEquatable<DataType>
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

        public static DataType Decimal(byte precision = 10, byte scale = 0)
        {
            return new DataType(DataTypeFlags.Decimal, QualifierFlags.None, 0, precision, scale, 0);
        }
        public static DataType Integer(ushort size = 4, bool signed = true)
        {
            if (!(size == 4 || size == 8 || size == 1 || size == 2))
            {
                throw new ArgumentOutOfRangeException(nameof(size), "Integer size must be 1, 2, 4 or 8.");
            }

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
        public static DataType Union()
        {
            DataTypeFlags types = DataTypeFlags.UnionTypes;
            QualifierFlags flags = QualifierFlags.DateTime;
            ushort size = 0;
            byte precision = 10;
            byte scale = 0;
            int typeCode = 0;

            return new DataType(types, flags, size, precision, scale, typeCode);
        }
        public static DataType Apply(DataType union, DataType type)
        {
            DataTypeFlags types = union._types;
            QualifierFlags flags = union._qualifiers;
            ushort size = union.Size;
            byte precision = union.Precision;
            byte scale = union.Scale;
            int typeCode = union.TypeCode;

            if (type.IsBoolean)
            {
                types |= DataTypeFlags.Boolean;
            }
            else if (type.IsDecimal)
            {
                types |= DataTypeFlags.Decimal;
                precision = type.Precision;
                scale = type.Scale;
            }
            else if (type.IsDateTime)
            {
                types |= DataTypeFlags.DateTime;
            }
            else if (type.IsString)
            {
                types |= DataTypeFlags.String;
                size = type.Size;
            }
            else if (type.IsEntity)
            {
                types |= DataTypeFlags.Entity;
                typeCode = type.TypeCode;
            }
            else
            {
                throw new InvalidOperationException();
            }

            return new DataType(types, flags, size, precision, scale, typeCode);
        }
        public static DataType Array()
        {
            return new DataType(DataTypeFlags.Array | DataTypeFlags.Object);
        }
        public static DataType Array(DataType item)
        {
            if (item.IsUndefined || item.IsArray)
            {
                throw new ArgumentOutOfRangeException(nameof(item), $"Invalid array item type [{item}].");
            }

            return new DataType(DataTypeFlags.Array | item._types,
                item._qualifiers, item.Size, item.Precision, item.Scale, item.TypeCode);
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
        public readonly bool IsEntityUnion
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

        public bool Equals(DataType target) { return _types == target._types; }
        public override int GetHashCode() { return _types.GetHashCode(); } // HashCode.Combine(_types);
        public override bool Equals(object target) { return target is DataType type && Equals(type); }
        public static bool operator ==(DataType left, DataType right) => left.Equals(right);
        public static bool operator !=(DataType left, DataType right) => !(left == right);

        public object DefaultValue()
        {
            if (IsArray)
            {
                if (IsObject)
                {
                    return new List<DataObject>();
                }
                else if (IsUnion)
                {
                    return IsEntityUnion ? new List<Entity>() : new List<Union>();
                }
                else if (IsBoolean) { return new List<bool>(); }
                else if (IsDecimal) { return new List<decimal>(); }
                else if (IsInteger)
                {
                    if (Size == 1) { return IsSigned ? new List<sbyte>() : new List<byte>(); }
                    else if (Size == 2) { return IsSigned ? new List<short>() : new List<ushort>(); }
                    else if (Size == 4) { return IsSigned ? new List<int>() : new List<uint>(); }
                    else if (Size == 8) { return IsSigned ? new List<long>() : new List<ulong>(); }
                }
                else if (IsDateTime) { return new List<DateTime>(); }
                else if (IsString) { return new List<string>(); }
                else if (IsBinary) { return new List<byte[]>(); }
                else if (IsUuid) { return new List<Guid>(); }
                else if (IsEntity) { return new List<Entity>(); }
            }
            else if (IsObject)
            {
                return new DataObject();
            }
            else if (IsUnion)
            {
                return IsEntityUnion ? TypeSystem.Entity.Undefined : TypeSystem.Union.Undefined;
            }
            else if (IsBoolean) { return false; }
            else if (IsDecimal) { return 0M; }
            else if (IsInteger)
            {
                if (Size == 1) { return IsSigned ? (sbyte)0 : (byte)0; }
                else if (Size == 2) { return IsSigned ? (short)0 : (ushort)0; }
                else if (Size == 4) { return IsSigned ? 0 : 0U; }
                else if (Size == 8) { return IsSigned ? 0L : 0UL; }
            }
            else if (IsDateTime) { return System.DateTime.MinValue; }
            else if (IsString) { return string.Empty; }
            else if (IsBinary) { return System.Array.Empty<byte>(); }
            else if (IsUuid) { return Guid.Empty; }
            else if (IsEntity) { return TypeSystem.Entity.Undefined; }

            return null;
        }
        public readonly Type ToType()
        {
            if (IsArray)
            {
                if (IsObject)
                {
                    return typeof(List<DataObject>);
                }
                else if (IsUnion)
                {
                    return IsEntityUnion ? typeof(List<Entity>) : typeof(List<Union>);
                }
                else if (IsBoolean) { return typeof(List<bool>); }
                else if (IsDecimal) { return typeof(List<decimal>); }
                else if (IsInteger)
                {
                    if (Size == 1) { return IsSigned ? typeof(List<sbyte>) : typeof(List<byte>); }
                    else if (Size == 2) { return IsSigned ? typeof(List<short>) : typeof(List<ushort>); }
                    else if (Size == 4) { return IsSigned ? typeof(List<int>) : typeof(List<uint>); }
                    else if (Size == 8) { return IsSigned ? typeof(List<long>) : typeof(List<ulong>); }
                }
                else if (IsDateTime) { return typeof(List<DateTime>); }
                else if (IsString) { return typeof(List<string>); }
                else if (IsBinary) { return typeof(List<byte[]>); }
                else if (IsUuid) { return typeof(List<Guid>); }
                else if (IsEntity) { return typeof(List<Entity>); }
            }
            else if (IsObject)
            {
                return typeof(DataObject);
            }
            else if (IsUnion)
            {
                return IsEntityUnion ? typeof(Entity) : typeof(Union);
            }
            else if (IsBoolean) { return typeof(bool); }
            else if (IsInteger)
            {
                if (Size == 1) { return IsSigned ? typeof(sbyte) : typeof(byte); }
                else if (Size == 2) { return IsSigned ? typeof(short) : typeof(ushort); }
                else if (Size == 4) { return IsSigned ? typeof(int) : typeof(uint); }
                else if (Size == 8) { return IsSigned ? typeof(long) : typeof(ulong); }
            }
            else if (IsDecimal) { return typeof(decimal); }
            else if (IsDateTime) { return typeof(DateTime); }
            else if (IsString) { return typeof(string); }
            else if (IsBinary) { return typeof(byte[]); }
            else if (IsUuid) { return typeof(Guid); }
            else if (IsEntity) { return typeof(Entity); }
            
            return null;
        }
        public override string ToString()
        {
            int position = 0;

            Span<char> buffer = stackalloc char[128];

            if (IsArray)
            {
                if (IsObject) { return "array"; } // default value
                else if (IsUnion)
                {
                    if (IsEntityUnion) { return "array(entity)"; }
                    else
                    {
                        "array".CopyTo(buffer[..5]);

                        position = 5; buffer[position] = '('; position++;

                        UnionToString(ref buffer, ref position);

                        buffer[position] = ')'; position++;

                        return buffer[0..position].ToString();
                    }
                }
                else if (IsBoolean) { return "array(boolean)"; }
                else if (IsInteger)
                {
                    "array".CopyTo(buffer[..5]);

                    position = 5; buffer[position] = '('; position++;

                    IntegerToString(ref buffer, ref position);

                    buffer[position] = ')'; position++;

                    return buffer[0..position].ToString();
                }
                else if (IsDecimal)
                {
                    "array".CopyTo(buffer[..5]);

                    position = 5; buffer[position] = '('; position++;

                    DecimalToString(ref buffer, ref position);

                    buffer[position] = ')'; position++;

                    return buffer[0..position].ToString();
                }
                else if (IsDateTime) { return "array(datetime)"; }
                else if (IsString)
                {
                    "array".CopyTo(buffer[..5]);

                    position = 5; buffer[position] = '('; position++;

                    StringToString(ref buffer, ref position);

                    buffer[position] = ')'; position++;

                    return buffer[0..position].ToString();
                }
                else if (IsBinary)
                {
                    "array".CopyTo(buffer[..5]);

                    position = 5; buffer[position] = '('; position++;

                    BinaryToString(ref buffer, ref position);

                    buffer[position] = ')'; position++;

                    return buffer[0..position].ToString();
                }
                else if (IsUuid) { return "array(uuid)"; }
                else if (IsEntity)
                {
                    "array".CopyTo(buffer[..5]);

                    position = 5; buffer[position] = '('; position++;

                    EntityToString(ref buffer, ref position);

                    buffer[position] = ')'; position++;

                    return buffer[0..position].ToString();
                }
            }
            else if (IsObject) { return "object"; }
            else if (IsUnion)
            {
                if (IsEntityUnion) { return "entity"; }
                else
                {
                    UnionToString(ref buffer, ref position);
                }
            }
            else if (IsBoolean) { return "boolean"; }
            else if (IsInteger) { IntegerToString(ref buffer, ref position); }
            else if (IsDecimal) { DecimalToString(ref buffer, ref position); }
            else if (IsDateTime) { return "datetime"; }
            else if (IsString) { StringToString(ref buffer, ref position); }
            else if (IsBinary) { BinaryToString(ref buffer, ref position); }
            else if (IsUuid) { return "uuid"; }
            else if (IsEntity) { EntityToString(ref buffer, ref position); }
            else if (IsUndefined) { return "undefined"; }

            return buffer[0..position].ToString();
        }
        public string ToString(in string schema)
        {
            if (!(IsArray || IsObject))
            {
                throw new InvalidOperationException();
            }

            if (IsArray)
            {
                return string.Format("array({0})", schema);
            }

            return string.Format("object({0})", schema);
        }
        private void IntegerToString(ref Span<char> buffer, ref int position)
        {
            "integer".CopyTo(buffer[position..(position + 7)]);

            position += 7;

            if (Size == 4 && IsSigned)
            {
                return; // default qualifiers
            }

            buffer[position] = '('; position++;

            if (!Size.TryFormat(buffer[position..], out int count, "d"))
            {
                throw new InvalidOperationException();
            }

            position += count;

            if (!IsSigned)
            {
                buffer[position] = ','; position++;

                "unsigned".CopyTo(buffer[position..]);

                position += 8;
            }

            buffer[position] = ')'; position++;
        }
        private void DecimalToString(ref Span<char> buffer, ref int position)
        {
            "decimal".CopyTo(buffer[position..(position + 7)]);

            position += 7;

            if (Precision == 10 && Scale == 0)
            {
                return; // default qualifiers
            }

            buffer[position] = '('; position++;
            
            if (!Precision.TryFormat(buffer[position..], out int count, "d"))
            {
                throw new InvalidOperationException();
            }

            position += count;

            buffer[position] = ','; position++;

            if (!Scale.TryFormat(buffer[position..], out count, "d"))
            {
                throw new InvalidOperationException();
            }

            position += count;

            buffer[position] = ')'; position++;
        }
        private void StringToString(ref Span<char> buffer, ref int position)
        {
            "string".CopyTo(buffer[position..(position + 6)]);

            position += 6;

            if (Size == 0 && !IsFixed)
            {
                return; // default qualifiers
            }

            buffer[position] = '('; position++;

            if (!Size.TryFormat(buffer[position..], out int count, "d"))
            {
                throw new InvalidOperationException();
            }

            position += count;

            if (IsFixed)
            {
                buffer[position] = ','; position++;

                "fixed".CopyTo(buffer[position..]);

                position += 5;
            }

            buffer[position] = ')'; position++;
        }
        private void BinaryToString(ref Span<char> buffer, ref int position)
        {
            "binary".CopyTo(buffer[position..(position + 6)]);

            position += 6;

            if (Size == 0 && !IsFixed)
            {
                return; // default qualifiers
            }

            buffer[position] = '('; position++;

            if (!Size.TryFormat(buffer[position..], out int count, "d"))
            {
                throw new InvalidOperationException();
            }

            position += count;

            if (IsFixed)
            {
                buffer[position] = ','; position++;

                "fixed".CopyTo(buffer[position..]);

                position += 5;
            }

            buffer[position] = ')'; position++;
        }
        private void EntityToString(ref Span<char> buffer, ref int position)
        {
            "entity".CopyTo(buffer[position..(position + 6)]);

            position += 6;

            if (TypeCode == 0)
            {
                return; // default qualifiers
            }

            buffer[position] = '('; position++;

            if (!TypeCode.TryFormat(buffer[position..], out int count, "d"))
            {
                throw new InvalidOperationException();
            }

            position += count;

            buffer[position] = ')'; position++;
        }
        private void UnionToString(ref Span<char> buffer, ref int position)
        {
            "union".CopyTo(buffer[position..(position + 5)]);

            position += 5;

            buffer[position] = '('; position++;

            bool useComma = false;

            if (IsBoolean)
            {
                "boolean".CopyTo(buffer[position..(position + 7)]);

                position += 7; useComma = true;
            }

            if (IsDecimal)
            {
                if (useComma) { buffer[position] = ','; position++; }

                DecimalToString(ref buffer, ref position);

                useComma = true;
            }

            if (IsDateTime)
            {
                if (useComma) { buffer[position] = ','; position++; }

                "datetime".CopyTo(buffer[position..(position + 8)]);

                position += 8; useComma = true;
            }

            if (IsString)
            {
                if (useComma) { buffer[position] = ','; position++; }

                StringToString(ref buffer, ref position);

                useComma = true;
            }

            if (IsEntity)
            {
                if (useComma) { buffer[position] = ','; position++; }

                EntityToString(ref buffer, ref position);
            }

            buffer[position] = ')'; position++;
        }

        private static readonly FrozenDictionary<Type, DataType> Types = CreateTypeLookup();
        private static readonly FrozenDictionary<string, DataType> Names = CreateNameLookup();
        private static FrozenDictionary<Type, DataType> CreateTypeLookup()
        {
            List<KeyValuePair<Type, DataType>> list =
            [
                new KeyValuePair<Type, DataType>(typeof(bool), Boolean),
                new KeyValuePair<Type, DataType>(typeof(sbyte), Integer(1)),
                new KeyValuePair<Type, DataType>(typeof(short), Integer(2)),
                new KeyValuePair<Type, DataType>(typeof(int), Integer(4)),
                new KeyValuePair<Type, DataType>(typeof(long), Integer(8)),
                new KeyValuePair<Type, DataType>(typeof(byte), Integer(1, false)),
                new KeyValuePair<Type, DataType>(typeof(ushort), Integer(2, false)),
                new KeyValuePair<Type, DataType>(typeof(uint), Integer(4, false)),
                new KeyValuePair<Type, DataType>(typeof(ulong), Integer(8, false)),
                new KeyValuePair<Type, DataType>(typeof(decimal), Decimal()),
                new KeyValuePair<Type, DataType>(typeof(DateTime), DateTime),
                new KeyValuePair<Type, DataType>(typeof(string), String()),
                new KeyValuePair<Type, DataType>(typeof(byte[]), Binary()),
                new KeyValuePair<Type, DataType>(typeof(Guid), Uuid()),
                new KeyValuePair<Type, DataType>(typeof(Entity), Entity()),
                new KeyValuePair<Type, DataType>(typeof(Union), Union()),
                new KeyValuePair<Type, DataType>(typeof(DataObject), Object),
                new KeyValuePair<Type, DataType>(typeof(List<bool>), Array(Boolean)),
                new KeyValuePair<Type, DataType>(typeof(List<sbyte>), Array(Integer(1))),
                new KeyValuePair<Type, DataType>(typeof(List<short>), Array(Integer(2))),
                new KeyValuePair<Type, DataType>(typeof(List<int>), Array(Integer(4))),
                new KeyValuePair<Type, DataType>(typeof(List<long>), Array(Integer(8))),
                new KeyValuePair<Type, DataType>(typeof(List<byte>), Array(Integer(1, false))),
                new KeyValuePair<Type, DataType>(typeof(List<ushort>), Array(Integer(2, false))),
                new KeyValuePair<Type, DataType>(typeof(List<uint>), Array(Integer(4, false))),
                new KeyValuePair<Type, DataType>(typeof(List<ulong>), Array(Integer(8, false))),
                new KeyValuePair<Type, DataType>(typeof(List<decimal>), Array(Decimal())),
                new KeyValuePair<Type, DataType>(typeof(List<DateTime>), Array(DateTime)),
                new KeyValuePair<Type, DataType>(typeof(List<string>), Array(String())),
                new KeyValuePair<Type, DataType>(typeof(List<byte[]>), Array(Binary())),
                new KeyValuePair<Type, DataType>(typeof(List<Guid>), Array(Uuid())),
                new KeyValuePair<Type, DataType>(typeof(List<Entity>), Array(Entity())),
                new KeyValuePair<Type, DataType>(typeof(List<Union>), Array(Union())),
                new KeyValuePair<Type, DataType>(typeof(List<DataObject>), Array())
            ];
            return FrozenDictionary.ToFrozenDictionary(list);
        }
        private static FrozenDictionary<string, DataType> CreateNameLookup()
        {
            List<KeyValuePair<string, DataType>> list =
            [
                new KeyValuePair<string, DataType>("boolean", Boolean),
                new KeyValuePair<string, DataType>("integer", Integer()),
                new KeyValuePair<string, DataType>("decimal", Decimal()),
                new KeyValuePair<string, DataType>("datetime", DateTime),
                new KeyValuePair<string, DataType>("string", String()),
                new KeyValuePair<string, DataType>("binary", Binary()),
                new KeyValuePair<string, DataType>("uuid", Uuid()),
                new KeyValuePair<string, DataType>("entity", Entity()),
                new KeyValuePair<string, DataType>("union", Union()),
                new KeyValuePair<string, DataType>("array", Array()),
                new KeyValuePair<string, DataType>("object", Object)
            ];
            return FrozenDictionary.ToFrozenDictionary(list, StringComparer.Ordinal);
        }
        
        private static readonly FrozenDictionary<string, DataType>.AlternateLookup<ReadOnlySpan<char>> NameLookup = Names.GetAlternateLookup<ReadOnlySpan<char>>();
        public static DataType FromType(Type type)
        {
            if (Types.TryGetValue(type, out DataType value))
            {
                return value;
            }

            return Undefined;
        }
        public static DataType FromName(in string name)
        {
            if (NameLookup.TryGetValue(name, out DataType type))
            {
                return type;
            }

            return Undefined;
        }

        public static bool TryParse(in string identifier, out DataType type, out string schema)
        {
            schema = string.Empty; // array or object qualifier: user-defined type identifier

            try
            {
                type = Parse(in identifier, out schema);
            }
            catch
            {
                type = Undefined;
            }

            return !type.IsUndefined;
        }
        public static DataType Parse(in string identifier, out string schema)
        {
            schema = string.Empty; // array or object qualifier: user-defined type identifier

            ReadOnlySpan<char> buffer = identifier.AsSpan();

            int position = 0;

            return Parse(ref buffer, ref position, out schema);
        }
        public static DataType Parse(ref ReadOnlySpan<char> buffer, ref int position, out string schema)
        {
            schema = string.Empty;

            if (!TryParseString(ref buffer, ref position, out int start))
            {
                throw new FormatException();
            }

            if (!NameLookup.TryGetValue(buffer[start..position], out DataType type))
            {
                throw new FormatException();
            }

            if (SkipSpaces(ref buffer, ref position))
            {
                return type; // end of the buffer
            }

            char current = buffer[position];

            if (current != '(')
            {
                throw new FormatException();
            }
            else
            {
                position++;
            }

            if (type.IsArray)
            {
                return ParseArray(ref buffer, position, out schema);
            }
            else if (type.IsObject)
            {
                return ParseObject(ref buffer, position, out schema);
            }
            if (type.IsUnion)
            {
                if (type.IsEntityUnion)
                {
                    return ParseEntity(ref buffer, ref position);
                }
                else
                {
                    return ParseUnion(ref buffer, ref position);
                }
            }
            else if (type.IsBoolean || type.IsDateTime || type.IsUuid)
            {
                return type;
            }
            else if (type.IsDecimal)
            {
                return ParseDecimal(ref buffer, ref position);
            }
            else if (type.IsInteger)
            {
                return ParseInteger(ref buffer, ref position);
            }
            else if (type.IsString)
            {
                return ParseString(ref buffer, ref position);
            }
            else if (type.IsBinary)
            {
                return ParseBinary(ref buffer, ref position);
            }
            else if (type.IsEntity)
            {
                return ParseEntity(ref buffer, ref position);
            }

            return Undefined;
        }
        private static bool SkipSpaces(ref ReadOnlySpan<char> buffer, ref int position)
        {
            char current;

            while (position < buffer.Length)
            {
                current = buffer[position];

                if (current == ' ')
                {
                    position++;
                }
                else
                {
                    break;
                }
            }

            return position == buffer.Length;
        }
        private static bool TryParseNumber(ref ReadOnlySpan<char> buffer, ref int position, out int start)
        {
            start = position;

            if (SkipSpaces(ref buffer, ref position))
            {
                return false; // end of the buffer
            }

            start = position; char current;
            
            while (position < buffer.Length)
            {
                current = buffer[position];

                if (current >= '0' && current <= '9')
                {
                    position++;
                }
                else
                {
                    break;
                }
            }

            if (start == position)
            {
                return false;
            }
            
            return true;
        }
        private static bool TryParseString(ref ReadOnlySpan<char> buffer, ref int position, out int start)
        {
            start = position;

            if (SkipSpaces(ref buffer, ref position))
            {
                return false; // end of the buffer
            }

            start = position; char current;

            while (position < buffer.Length)
            {
                current = buffer[position];

                if ((current == '.' || current == '_') ||
                    (current >= 'a' && current <= 'z') ||
                    (current >= 'A' && current <= 'Z') ||
                    (current >= 'а' && current <= 'я') ||
                    (current >= 'А' && current <= 'Я') ||
                    (current >= '0' && current <= '9'))
                {
                    position++;
                }
                else
                {
                    break;
                }
            }

            if (start == position)
            {
                return false;
            }

            return true;
        }
        private static DataType ParseArray(ref ReadOnlySpan<char> buffer, int position, out string schema)
        {
            schema = string.Empty;

            if (!TryParseString(ref buffer, ref position, out int start))
            {
                throw new FormatException();
            }

            char current;

            if (!NameLookup.TryGetValue(buffer[start..position], out DataType type))
            {
                if (SkipSpaces(ref buffer, ref position))
                {
                    throw new FormatException(); // end of the buffer
                }

                current = buffer[position];

                if (current != ')')
                {
                    throw new FormatException();
                }

                schema = buffer[start..position].ToString(); // database or user-defined object type
                
                return Array(); // array of user-defined objects
            }

            if (SkipSpaces(ref buffer, ref position))
            {
                throw new FormatException(); // end of the buffer
            }

            current = buffer[position];

            if (current == ')')
            {
                return Array(type);
            }
            else if (current == '(')
            {
                position++;
            }
            else
            {
                throw new FormatException();
            }

            if (type.IsUnion)
            {
                if (type.IsEntityUnion)
                {
                    type = ParseEntity(ref buffer, ref position);
                }
                else
                {
                    type = ParseUnion(ref buffer, ref position);
                }
            }
            else if (type.IsBoolean || type.IsDateTime || type.IsUuid)
            {
                return Array(type);
            }
            else if (type.IsDecimal)
            {
                type = ParseDecimal(ref buffer, ref position);
            }
            else if (type.IsInteger)
            {
                type = ParseInteger(ref buffer, ref position);
            }
            else if (type.IsString)
            {
                type = ParseString(ref buffer, ref position);
            }
            else if (type.IsBinary)
            {
                type = ParseBinary(ref buffer, ref position);
            }
            else if (type.IsEntity)
            {
                type = ParseEntity(ref buffer, ref position);
            }

            return Array(type);
        }
        private static DataType ParseObject(ref ReadOnlySpan<char> buffer, int position, out string schema)
        {
            schema = string.Empty;

            if (!TryParseString(ref buffer, ref position, out int start))
            {
                throw new FormatException();
            }
            
            if (NameLookup.TryGetValue(buffer[start..position], out _))
            {
                throw new FormatException(); // only user-defined data schema allowed
            }

            if (SkipSpaces(ref buffer, ref position))
            {
                throw new FormatException(); // end of the buffer
            }

            char current = buffer[position];

            if (current != ')')
            {
                throw new FormatException();
            }
            
            schema = buffer[start..position].ToString(); // user-defined data schema

            return Object;
        }
        private static DataType ParseDecimal(ref ReadOnlySpan<char> buffer, ref int position)
        {
            byte precision = 8, scale = 0;

            if (!TryParseNumber(ref buffer, ref position, out int start))
            {
                throw new FormatException();
            }

            if (!byte.TryParse(buffer[start..position], out precision))
            {
                throw new FormatException();
            }

            if (SkipSpaces(ref buffer, ref position))
            {
                throw new FormatException(); // end of the buffer
            }

            char current = buffer[position];

            if (current == ')')
            {
                return Decimal(precision);
            }

            if (current != ',')
            {
                throw new FormatException();
            }

            position++;
            
            if (!TryParseNumber(ref buffer, ref position, out start))
            {
                throw new FormatException();
            }

            if (!byte.TryParse(buffer[start..position], out scale))
            {
                throw new FormatException();
            }

            if (SkipSpaces(ref buffer, ref position))
            {
                throw new FormatException(); // end of the buffer
            }

            current = buffer[position];

            if (current != ')')
            {
                throw new FormatException();
            }

            if (scale > precision)
            {
                throw new FormatException($"Scale [{scale}] must be less or equal precision [{precision}].");
            }

            return Decimal(precision, scale);
        }
        private static DataType ParseInteger(ref ReadOnlySpan<char> buffer, ref int position)
        {
            ushort size = 4; bool signed = true;

            if (!TryParseNumber(ref buffer, ref position, out int start))
            {
                throw new FormatException();
            }

            if (!ushort.TryParse(buffer[start..position], out size))
            {
                throw new FormatException();
            }

            if (SkipSpaces(ref buffer, ref position))
            {
                throw new FormatException(); // end of the buffer
            }

            char current = buffer[position];

            if (current == ')')
            {
                return Integer(size);
            }

            if (current != ',')
            {
                throw new FormatException();
            }

            position++;

            if (!TryParseString(ref buffer, ref position, out start))
            {
                throw new FormatException();
            }

            if (buffer[start..position].SequenceEqual("unsigned"))
            {
                signed = false;
            }

            if (SkipSpaces(ref buffer, ref position))
            {
                throw new FormatException(); // end of the buffer
            }

            current = buffer[position];

            if (current != ')')
            {
                throw new FormatException();
            }
            
            if (!(size == 1 || size == 2 || size == 4 || size == 8))
            {
                throw new FormatException("Number literal of 1, 2, 4, or 8 expected.");
            }
            
            return Integer(size, signed);
        }
        private static DataType ParseString(ref ReadOnlySpan<char> buffer, ref int position)
        {
            ushort size = 0; bool variable = true;

            if (!TryParseNumber(ref buffer, ref position, out int start))
            {
                throw new FormatException();
            }

            if (!ushort.TryParse(buffer[start..position], out size))
            {
                throw new FormatException();
            }

            if (SkipSpaces(ref buffer, ref position))
            {
                throw new FormatException(); // end of the buffer
            }

            char current = buffer[position];

            if (current == ')')
            {
                return String(size);
            }

            if (current != ',')
            {
                throw new FormatException();
            }

            position++;

            if (!TryParseString(ref buffer, ref position, out start))
            {
                throw new FormatException();
            }

            if (buffer[start..position].SequenceEqual("fixed"))
            {
                variable = false;
            }

            if (SkipSpaces(ref buffer, ref position))
            {
                throw new FormatException(); // end of the buffer
            }

            current = buffer[position];

            if (current != ')')
            {
                throw new FormatException();
            }

            if (size > 1024)
            {
                throw new FormatException("Size of 1024 or less expected. Use 0 value for strings bigger in size.");
            }

            return String(size, variable);
        }
        private static DataType ParseBinary(ref ReadOnlySpan<char> buffer, ref int position)
        {
            ushort size = 0; bool variable = true;

            if (!TryParseNumber(ref buffer, ref position, out int start))
            {
                throw new FormatException();
            }

            if (!ushort.TryParse(buffer[start..position], out size))
            {
                throw new FormatException();
            }

            if (SkipSpaces(ref buffer, ref position))
            {
                throw new FormatException(); // end of the buffer
            }

            char current = buffer[position];

            if (current == ')')
            {
                return Binary(size);
            }

            if (current != ',')
            {
                throw new FormatException();
            }

            position++;

            if (!TryParseString(ref buffer, ref position, out start))
            {
                throw new FormatException();
            }

            if (buffer[start..position].SequenceEqual("fixed"))
            {
                variable = false;
            }

            if (SkipSpaces(ref buffer, ref position))
            {
                throw new FormatException(); // end of the buffer
            }

            current = buffer[position];

            if (current != ')')
            {
                throw new FormatException();
            }

            if (size > 1024)
            {
                throw new FormatException("Size of 1024 or less expected. Use 0 value for binaries bigger in size.");
            }

            return Binary(size, variable);
        }
        private static DataType ParseEntity(ref ReadOnlySpan<char> buffer, ref int position)
        {
            if (!TryParseNumber(ref buffer, ref position, out int start))
            {
                throw new FormatException();
            }

            if (!int.TryParse(buffer[start..position], out int typeCode))
            {
                throw new FormatException();
            }

            if (SkipSpaces(ref buffer, ref position))
            {
                throw new FormatException(); // end of the buffer
            }

            char current = buffer[position];

            if (current != ')')
            {
                throw new FormatException();
            }
            
            return Entity(typeCode);
        }
        private static DataType ParseUnion(ref ReadOnlySpan<char> buffer, ref int position)
        {
            // boolean, decimal, datetime, string, entity

            DataType union = Undefined;

            DataType type = ParseUnionType(ref buffer, ref position);
            
            union = Apply(union, type);

            char current = buffer[position];

            while (current == ',') // next type
            {
                position++;

                type = ParseUnionType(ref buffer, ref position);
                
                union = Apply(union, type);
                
                current = buffer[position];
            }

            if (SkipSpaces(ref buffer, ref position))
            {
                throw new FormatException(); // end of the buffer
            }

            if (current != ')') // end of union
            {
                throw new FormatException();
            }

            return union;
        }
        private static DataType ParseUnionType(ref ReadOnlySpan<char> buffer, ref int position)
        {
            if (!TryParseString(ref buffer, ref position, out int start))
            {
                throw new FormatException();
            }

            if (!NameLookup.TryGetValue(buffer[start..position], out DataType type))
            {
                throw new FormatException();
            }

            if (!(type.IsBoolean || type.IsDecimal || type.IsDateTime || type.IsString || type.IsEntity))
            {
                throw new FormatException();
            }

            if (SkipSpaces(ref buffer, ref position))
            {
                throw new FormatException(); // end of the buffer
            }

            char current = buffer[position];

            if (current == '(') // qualifiers: decimal, string, entity
            {
                position++;

                if (type.IsDecimal)
                {
                    type = ParseDecimal(ref buffer, ref position);
                }
                else if (type.IsString)
                {
                    type = ParseString(ref buffer, ref position);
                }
                else if (type.IsEntity)
                {
                    type = ParseEntity(ref buffer, ref position);
                }
                else
                {
                    throw new FormatException();
                }

                position++;

                if (SkipSpaces(ref buffer, ref position))
                {
                    throw new FormatException(); // end of the buffer
                }
            }

            return type;
        }
    }
}