namespace DaJet
{
    public sealed class TablePart : MetadataObject
    {
        public TablePart(Guid uuid) : base(uuid)
        {
            DbNames = new List<DbName>(2); // VT + LineNo
        }
    }
}