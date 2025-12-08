namespace DaJet.Metadata
{
    internal abstract class DatabaseObject : MetadataObject
    {
        protected DatabaseObject(Guid uuid, int code, string name) : base(uuid)
        {
            DbName = name;
            TypeCode = code;
        }
        internal string DbName { get; private set; }
        internal int TypeCode { get; set; }
        internal string GetMainDbName()
        {
            return string.Format("_{0}{1}", DbName, TypeCode);
        }
    }
}