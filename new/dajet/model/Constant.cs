namespace DaJet
{
    public sealed class Constant : MetadataObject
    {
        public Constant(Guid uuid) : base(uuid)
        {
            DbNames = new List<DbName>(2); // Const + ChngR
        }
    }
}