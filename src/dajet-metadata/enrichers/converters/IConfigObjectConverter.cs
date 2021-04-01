using DaJet.Metadata.Model;

namespace DaJet.Metadata.Converters
{
    public interface IConfigObjectConverter
    {
        object Convert(ConfigObject configObject);
    }
}