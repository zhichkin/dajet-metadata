namespace DaJet
{
    public sealed class BusinessTask : MetadataObject
    {
        public BusinessTask(Guid uuid) : base(uuid)
        {
            DbNames = new List<DbName>(2); // Task + ChngR
        }
    }
}