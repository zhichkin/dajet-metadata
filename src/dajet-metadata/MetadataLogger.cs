using DaJet.Utilities;

namespace DaJet.Metadata
{
    internal static class MetadataLogger
    {
        private static readonly FileLogger _logger = new();
        static MetadataLogger()
        {
            _logger.UseCatalog(AppContext.BaseDirectory);
            _logger.UseLogFile("DaJet.Metadata.log");
        }
        internal static void Write(in string message)
        {
            _logger.Write(message);
        }
    }
}