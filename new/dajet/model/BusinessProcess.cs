namespace DaJet
{
    public sealed class BusinessProcess : MetadataObject
    {
        public BusinessProcess(Guid uuid) : base(uuid)
        {
            DbNames = new List<DbName>(3); // BPr + ChngR + BPrPoints
        }
    }
}