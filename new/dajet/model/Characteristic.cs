namespace DaJet
{
    public sealed class Characteristic : MetadataObject
    {
        public Characteristic(Guid uuid) : base(uuid)
        {
            DbNames = new List<DbName>(2); // Chrc + ChngR
        }
    }
}