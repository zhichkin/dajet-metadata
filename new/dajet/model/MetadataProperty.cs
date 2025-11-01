namespace DaJet
{
    public sealed class MetadataProperty : MetadataObject
    {
        public MetadataProperty(Guid uuid) : base(uuid)
        {
            DbNames = new List<DbName>(1); // Fld
        }
    }
}