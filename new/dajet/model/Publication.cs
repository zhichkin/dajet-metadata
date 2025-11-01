namespace DaJet
{
    public sealed class Publication : MetadataObject
    {
        public Publication(Guid uuid) : base(uuid)
        {
            DbNames = new List<DbName>(1); // Node
        }
    }
}