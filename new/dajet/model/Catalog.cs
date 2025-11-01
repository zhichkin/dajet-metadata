namespace DaJet
{
    public sealed class Catalog : MetadataObject
    {
        public Catalog(Guid uuid) : base(uuid)
        {
            DbNames = new List<DbName>(2); // Reference + ChngR
        }
    }
}