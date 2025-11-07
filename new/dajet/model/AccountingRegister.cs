namespace DaJet
{
    internal sealed class AccountingRegister : ChangeTrackingObject //RegisterObject
    {
        internal static AccountingRegister Create(Guid uuid, int code, string name)
        {
            return new AccountingRegister(uuid, code, name);
        }
        internal AccountingRegister(Guid uuid, int code, string name) : base(uuid, code, name) { }
        
        private int _AccRgED;
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.AccRgED)
            {
                _AccRgED = code;
            }
            else if (name == MetadataToken.AccRgChngR)
            {
                _ChngR = code;
            }
        }
        internal string GetTableNameЗначенияСубконто()
        {
            return string.Format("_{0}{1}", MetadataToken.AccRgED, _AccRgED);
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.AccRgChngR, _ChngR);
        }
    }
}