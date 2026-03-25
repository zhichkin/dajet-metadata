using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    public sealed class OneDbSchemaProvider : ISchemaProvider
    {
        public EntityDefinition GetSchema(in string domain, in string identifier)
        {
            ArgumentException.ThrowIfNullOrEmpty(domain, nameof(domain));
            ArgumentException.ThrowIfNullOrEmpty(identifier, nameof(identifier));

            MetadataProvider provider = MetadataProvider.Get(in domain);

            if (provider is null)
            {
                return null;
            }

            return provider.GetMetadataObject(in identifier);
        }
    }

    public static class OneDbSchemaProviderExtensions
    {
        public static Guid GetEnumerationValue(this ISchemaProvider _, in string domain, in string identifier)
        {
            ArgumentException.ThrowIfNullOrEmpty(domain, nameof(domain));
            ArgumentException.ThrowIfNullOrEmpty(identifier, nameof(identifier));

            MetadataProvider provider = MetadataProvider.Get(in domain);

            if (provider is null)
            {
                return Guid.Empty;
            }

            return provider.GetEnumerationValue(in identifier);
        }
    }
}