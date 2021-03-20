namespace DaJet.Metadata.Model
{
    public interface IMetadataObjectFactory
    {
        MetadataObject CreateObject();
        IMetadataPropertyFactory PropertyFactory { get; }
    }
    public sealed class MetadataObjectFactory<T> : IMetadataObjectFactory where T : MetadataObject, new()
    {
        public IMetadataPropertyFactory PropertyFactory { get; private set; }
        public MetadataObjectFactory(IMetadataPropertyFactory factory)
        {
            PropertyFactory = factory;
        }
        public MetadataObject CreateObject()
        {
            return new T();
        }
    }
}