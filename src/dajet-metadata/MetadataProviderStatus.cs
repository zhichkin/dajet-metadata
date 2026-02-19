using DaJet.Data;

namespace DaJet.Metadata
{
    public sealed class MetadataProviderStatus
    {
        public string Name { get; set; }
        public DataSourceType DataSource { get; set; }
        public string ConnectionString { get; set; }
        public int LastUpdated { get; set; }
        public bool IsInitialized { get; set; }
    }
}