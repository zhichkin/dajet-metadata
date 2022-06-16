namespace DaJet.CodeGenerator
{
    public sealed class EntityRef
    {
        public EntityRef(int typeCode, Guid identity)
        {
            IsEnum = false;
            TypeCode = typeCode;
            Identity = identity;
        }
        public EntityRef(int typeCode, Guid identity, string typeName) : this(typeCode, identity)
        {
            TypeName = typeName;
        }
        public EntityRef(int typeCode, Guid identity, string typeName, string enumValue) : this(typeCode, identity, typeName)
        {
            IsEnum = true;
            EnumValue = enumValue;
        }
        public bool IsEnum { get; }
        public int TypeCode { get; }
        public Guid Identity { get; }
        public string TypeName { get; }
        public string EnumValue { get; }
    }
}