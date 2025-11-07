namespace DaJet
{
    internal sealed class InformationRegister : ChangeTrackingObject //RegisterObject
    {
        internal static InformationRegister Create(Guid uuid, int code, string name)
        {
            return new InformationRegister(uuid, code, name);
        }
        internal InformationRegister(Guid uuid, int code, string name) : base(uuid, code, name) { }

        private int _InfoRgSF;
        private int _InfoRgSL;
        private int _InfoRgOpt;
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.InfoRgOpt)
            {
                _InfoRgOpt = code;
            }
            else if (name == MetadataToken.InfoRgSF)
            {
                _InfoRgSF = code;
            }
            else if (name == MetadataToken.InfoRgSL)
            {
                _InfoRgSL = code;
            }
            else if (name == MetadataToken.InfoRgChngR)
            {
                _ChngR = code;
            }
        }
        internal string GetTableNameНастройки()
        {
            return string.Format("_{0}{1}", MetadataToken.InfoRgOpt, _InfoRgOpt);
        }
        internal string GetTableNameСрезПервых()
        {
            return string.Format("_{0}{1}", MetadataToken.InfoRgSF, _InfoRgSF);
        }
        internal string GetTableNameСрезПоследних()
        {
            return string.Format("_{0}{1}", MetadataToken.InfoRgSL, _InfoRgSL);
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.InfoRgChngR, _ChngR);
        }
    }
}