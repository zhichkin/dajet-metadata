namespace DaJet
{
    internal sealed class Characteristic : ChangeTrackingObject
    {
        internal static Characteristic Create(Guid uuid, int code, string name)
        {
            return new Characteristic(uuid, code, name);
        }
        internal Characteristic(Guid uuid, int code, string name) : base(uuid, code, name) { }
        internal DataType Type { get; set; }
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.ChrcChngR)
            {
                _ChngR = code;
            }
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.ChrcChngR, _ChngR);
        }
    }
}