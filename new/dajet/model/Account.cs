namespace DaJet
{
    public sealed class Account : MetadataObject
    {
        public Account(Guid uuid) : base(uuid)
        {
            DbNames = new List<DbName>(2); // Acc + ChngR
        }
    }
}