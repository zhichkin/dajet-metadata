namespace DaJet
{
    internal sealed class Constant : ChangeTrackingObject
    {
        internal static Constant Create(Guid uuid, int code, string name)
        {
            return new Constant(uuid, code, name);
        }
        internal Constant(Guid uuid, int code, string name) : base(uuid, code, name) { }
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.ConstChngR)
            {
                _ChngR = code;
            }
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.ConstChngR, _ChngR);
        }
    }
}