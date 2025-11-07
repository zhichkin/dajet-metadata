namespace DaJet
{
    internal sealed class Enumeration : DatabaseObject
    {
        internal static Enumeration Create(Guid uuid, int code, string name)
        {
            return new Enumeration(uuid, code, name);
        }
        internal Enumeration(Guid uuid, int code, string name) : base(uuid, code, name) { }
    }
}