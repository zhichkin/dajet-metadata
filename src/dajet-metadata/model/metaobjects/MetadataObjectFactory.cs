namespace DaJet.Metadata.Model
{
    public interface IMetadataObjectFactory : IMetadataPropertyFactory
    {
        MetadataObject CreateObject();
    }
    public sealed class MetadataObjectFactory<T> : IMetadataObjectFactory where T : MetadataObject, new()
    {
        private readonly IMetadataPropertyFactory PropertyFactory;
        public MetadataObjectFactory(IMetadataPropertyFactory factory)
        {
            PropertyFactory = factory;
        }
        public MetadataObject CreateObject()
        {
            return new T();
        }
        public MetadataProperty CreateProperty(MetadataObject owner, SqlFieldInfo field)
        {
            if (PropertyFactory == null) return null;
            return PropertyFactory.CreateProperty(owner, field);
        }
    }
}