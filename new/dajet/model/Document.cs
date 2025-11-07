namespace DaJet
{
    internal sealed class Document : ChangeTrackingObject
    {
        internal static Document Create(Guid uuid, int code, string name)
        {
            return new Document(uuid, code, name);
        }
        internal Document(Guid uuid, int code, string name) : base(uuid, code, name) { }
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.DocumentChngR)
            {
                _ChngR = code;
            }
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.DocumentChngR, _ChngR);
        }
    }
}