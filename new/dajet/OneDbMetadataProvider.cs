namespace DaJet
{
    public sealed class OneDbMetadataProvider : IMetadataProvider
    {
        private MetadataRegistry _registry;
        private readonly bool UseExtensions;
        private readonly MetadataLoader _loader;
        public OneDbMetadataProvider(DataSourceType dataSource, in string connectionString, bool useExtensions = false)
        {
            UseExtensions = useExtensions;

            _loader = MetadataLoader.Create(dataSource, in connectionString);
        }
        public void Dump(in string tableName, in string fileName, in string outputPath)
        {
            _loader.Dump(in tableName, in fileName, in outputPath);
        }
        public int GetYearOffset()
        {
            return _loader.GetYearOffset();
        }
        public InfoBase GetInfoBase()
        {
            InfoBase infoBase = _loader.GetInfoBase();
            
            infoBase.YearOffset = GetYearOffset();

            return infoBase;
        }
        public TableDefinition GetMetadataObject(string metadataName)
        {
            //TODO: добавить параметр - bool LoadingMode.Full = false

            int dot = metadataName.IndexOf('.');

            if (dot < 0)
            {
                return null;
            }

            string type = metadataName[..dot];
            string name = metadataName[(dot + 1)..];

            if (!_registry.TryGetEntry(in type, in name, out MetadataObject metadata))
            {
                return null;
            }

            CatalogParser parser = new();

            using (ConfigFileBuffer file = _loader.Load(metadata.Uuid))
            {
                return parser.Load(metadata.Uuid, file.AsReadOnlySpan(), in _registry);
            }
        }
        public void Initialize()
        {
            _registry = _loader.GetMetadataRegistry(UseExtensions);
        }
    }
}