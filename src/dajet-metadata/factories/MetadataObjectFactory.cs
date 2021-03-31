namespace DaJet.Metadata.Model
{
    public interface IApplicationObjectFactory
    {
        ApplicationObject CreateObject();
        IMetadataPropertyFactory PropertyFactory { get; }
    }
    public sealed class ApplicationObjectFactory<T> : IApplicationObjectFactory where T : ApplicationObject, new()
    {
        public IMetadataPropertyFactory PropertyFactory { get; private set; }
        public ApplicationObjectFactory(IMetadataPropertyFactory factory)
        {
            PropertyFactory = factory;
        }
        public ApplicationObject CreateObject()
        {
            return new T();
        }
    }
}