namespace DaJet
{
    internal sealed class BusinessProcess : ChangeTrackingObject
    {
        internal static BusinessProcess Create(Guid uuid, int code, string name)
        {
            return new BusinessProcess(uuid, code, name);
        }
        internal BusinessProcess(Guid uuid, int code, string name) : base(uuid, code, name) { }

        private int _BPrPoints;
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.BPrPoints)
            {
                _BPrPoints = code;
            }
            else if (name == MetadataToken.BPrChngR)
            {
                _ChngR = code;
            }
        }
        internal string GetTableNameТочкиМаршрута()
        {
            return string.Format("_{0}{1}", MetadataToken.BPrPoints, _BPrPoints);
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.BPrChngR, _ChngR);
        }
    }
}