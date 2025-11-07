namespace DaJet
{
    internal sealed class Publication : DatabaseObject
    {
        internal static Publication Create(Guid uuid, int code, string name)
        {
            return new Publication(uuid, code, name);
        }
        internal Publication(Guid uuid, int code, string name) : base(uuid, code, name) { }
    }
}