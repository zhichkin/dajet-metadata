namespace DaJet
{
    internal sealed class AccumulationRegister : ChangeTrackingObject //RegisterObject
    {
        internal static AccumulationRegister Create(Guid uuid, int code, string name)
        {
            return new AccumulationRegister(uuid, code, name);
        }
        internal AccumulationRegister(Guid uuid, int code, string name) : base(uuid, code, name) { }

        private int _AccumRgT;
        private int _AccumRgTn;
        private int _AccumRgOpt;
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.AccumRgT)
            {
                _AccumRgT = code;
            }
            else if (name == MetadataToken.AccumRgTn)
            {
                _AccumRgTn = code;
            }
            else if (name == MetadataToken.AccumRgOpt)
            {
                _AccumRgOpt = code;
            }
            else if (name == MetadataToken.AccumRgChngR)
            {
                _ChngR = code;
            }
        }
        internal string GetTableNameНастройки()
        {
            return string.Format("_{0}{1}", MetadataToken.AccumRgOpt, _AccumRgOpt);
        }
        internal string GetTableNameИтоги()
        {
            if (_AccumRgT > 0)
            {
                return string.Format("_{0}{1}", MetadataToken.AccumRgT, _AccumRgT);
            }
            else
            {
                return string.Format("_{0}{1}", MetadataToken.AccumRgTn, _AccumRgTn);
            }
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.AccumRgChngR, _ChngR);
        }
    }
}