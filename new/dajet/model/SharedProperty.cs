namespace DaJet
{
    public sealed class SharedProperty : MetadataObject
    {
        public SharedProperty(Guid uuid) : base(uuid)
        {
            DbNames = new List<DbName>(1); // Fld
        }
    }
}