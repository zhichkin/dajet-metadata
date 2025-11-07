namespace DaJet
{
    internal sealed class Property : DatabaseObject
    {
        internal static Property Create(Guid uuid, int code, string name)
        {
            return new Property(uuid, code, name);
        }
        internal Property(Guid uuid, int code, string name) : base(uuid, code, name) { }
    }
}