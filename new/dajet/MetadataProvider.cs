namespace DaJet
{
    public abstract class MetadataProvider
    {
        public abstract void Initialize();
        public abstract int GetYearOffset();
        public abstract InfoBase GetInfoBase();
        public abstract EntityDefinition GetMetadataObject(in string metadataName);
        public abstract IEnumerable<EntityDefinition> GetMetadataObjects(string typeName);
    }
}