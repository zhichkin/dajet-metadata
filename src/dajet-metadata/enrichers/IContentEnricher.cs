using DaJet.Metadata.Model;

namespace DaJet.Metadata.Enrichers
{
    public interface IContentEnricher
    {
        void Enrich(MetadataObject metadataObject, ConfigObject configObject);
    }
}