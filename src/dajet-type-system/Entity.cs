using System.Text.Json.Serialization;

namespace DaJet.TypeSystem
{
    public readonly struct Entity
    {
        public static readonly Entity Undefined;
        public static Entity Parse(string value)
        {
            // {int:uuid}

            ArgumentNullException.ThrowIfNullOrEmpty(value, nameof(value));

            if (value.Length < 40)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            ReadOnlySpan<char> buffer = value.AsSpan(1..(value.Length - 1));

            int colon = buffer.IndexOf(':');
            int typeCode = int.Parse(buffer[0..colon]);
            Guid identity = Guid.Parse(buffer[(colon + 1)..]);

            return new Entity(typeCode, identity);
        }
        public static bool TryParse(string value, out Entity entity)
        {
            entity = Undefined;

            try
            {
                entity = Parse(value);
            }
            catch
            {
                return false;
            }

            return true;
        }
        [JsonConstructor] public Entity(int typeCode, Guid identity)
        {
            TypeCode = typeCode;
            Identity = identity;
        }
        public int TypeCode { get; } = 0;
        public Guid Identity { get; } = Guid.Empty;
        public Entity Copy() { return new Entity(TypeCode, Identity); }
        [JsonIgnore] public bool IsEmpty { get { return TypeCode > 0 && Identity == Guid.Empty; } }
        [JsonIgnore] public bool IsUndefined { get { return this == Undefined; } }
        public override string ToString()
        {
            Span<char> buffer = stackalloc char[64];

            buffer[0] = '{'; int position = 1;

            if (!TypeCode.TryFormat(buffer[1..], out int count, "d"))
            {
                throw new FormatException();
            }

            position += count; buffer[position] = ':'; position++;

            if (!Identity.TryFormat(buffer[position..], out count, "D"))
            {
                throw new FormatException();
            }

            position += count; buffer[position] = '}'; position++;

            return buffer[0..position].ToString();
        }

        #region " Переопределение методов сравнения "

        public override int GetHashCode()
        {
            return Identity.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (obj == null) { return false; }

            if (obj is not Entity test)
            {
                return false;
            }

            return (this == test);
        }
        public static bool operator ==(Entity left, Entity right)
        {
            return left.TypeCode == right.TypeCode
                && left.Identity == right.Identity;
        }
        public static bool operator !=(Entity left, Entity right)
        {
            return !(left == right);
        }

        #endregion
    }
}