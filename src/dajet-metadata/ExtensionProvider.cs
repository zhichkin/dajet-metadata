namespace DaJet.Metadata
{
    internal sealed class ExtensionProvider
    {
        private readonly MetadataLoader _loader;
        internal ExtensionProvider(in MetadataLoader loader)
        {
            _loader = loader;
        }
        internal List<ExtensionInfo> GetExtensions()
        {
            if (_loader.IsExtensionsSupported())
            {
                return _loader.GetExtensions();
            }

            return null;
        }
    }
}