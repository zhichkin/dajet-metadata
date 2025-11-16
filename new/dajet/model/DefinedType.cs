namespace DaJet
{
    internal sealed class DefinedType : MetadataObject
    {
        internal DefinedType(Guid uuid) : base(uuid) { }
        internal DataType Type { get; set; }
    }
}