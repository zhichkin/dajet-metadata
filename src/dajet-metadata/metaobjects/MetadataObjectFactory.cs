namespace DaJet.Metadata.Model
{
    public interface IMetadataObjectFactory
    {
        MetadataObject Create();
    }
    public sealed class MetadataObjectFactory<T> : IMetadataObjectFactory where T : MetadataObject, new()
    {
        public MetadataObject Create()
        {
            return new T();
        }
    }
}