namespace DaJet
{
    public sealed class Enumeration : MetadataObject
    {
        public Enumeration(Guid uuid) : base(uuid)
        {
            DbNames = new List<DbName>(1); // Enum
        }
    }
}