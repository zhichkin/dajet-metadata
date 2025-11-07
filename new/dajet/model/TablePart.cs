namespace DaJet
{
    internal sealed class TablePart : DatabaseObject
    {
        internal static TablePart Create(Guid uuid, int code, string name)
        {
            return new TablePart(uuid, code, name);
        }
        internal TablePart(Guid uuid, int code, string name) : base(uuid, code, name) { }

        private int _LineNo;
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.LineNo)
            {
                _LineNo = code;
            }
        }
        internal string GetColumnNameСсылка()
        {
            return string.Format("_{0}{1}_{2}", DbName, TypeCode, MetadataToken.IDRRef);
        }
        internal string GetColumnNameНомерСтроки()
        {
            return string.Format("_{0}{1}", MetadataToken.LineNo, _LineNo);
        }
    }
}