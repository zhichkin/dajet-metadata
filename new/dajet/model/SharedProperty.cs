namespace DaJet
{
    internal sealed class SharedProperty : DatabaseObject
    {
        internal SharedProperty(Guid uuid) : base(uuid, 0, MetadataToken.Fld) { }
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.Fld)
            {
                TypeCode = code;
            }
        }
    }
}