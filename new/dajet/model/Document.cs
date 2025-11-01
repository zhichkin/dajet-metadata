namespace DaJet
{
    public sealed class Document : MetadataObject
    {
        public Document(Guid uuid) : base(uuid)
        {
            DbNames = new List<DbName>(2); // Document + ChngR
        }
    }
}