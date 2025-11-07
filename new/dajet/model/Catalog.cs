namespace DaJet
{
    internal sealed class Catalog : ChangeTrackingObject
    {
        internal static Catalog Create(Guid uuid, int code, string name)
        {
            return new Catalog(uuid, code, name);
        }
        internal Catalog(Guid uuid, int code, string name) : base(uuid, code, name) { }
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.ReferenceChngR)
            {
                _ChngR = code;
            }
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.ReferenceChngR, _ChngR);
        }
    }
}