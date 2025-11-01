namespace DaJet
{
    public sealed class DefinedType : MetadataObject
    {
        public DefinedType(Guid uuid) : base(uuid)
        {
            DbNames = null;
        }
    }
}