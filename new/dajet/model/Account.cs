namespace DaJet
{
    internal sealed class Account : ChangeTrackingObject
    {
        internal static Account Create(Guid uuid, int code, string name)
        {
            return new Account(uuid, code, name);
        }
        internal Account(Guid uuid, int code, string name) : base(uuid, code, name) { }
        
        private int _ExtDim;
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.ExtDim)
            {
                _ExtDim = code;
            }
            else if (name == MetadataToken.AccChngR)
            {
                _ChngR = code;
            }
        }
        internal string GetTableNameВидыСубконто()
        {
            return string.Format("_{0}{1}_{2}{3}", DbName, TypeCode, MetadataToken.ExtDim, _ExtDim);
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.AccChngR, _ChngR);
        }
    }
}