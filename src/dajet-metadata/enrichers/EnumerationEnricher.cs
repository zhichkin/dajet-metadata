using DaJet.Metadata.Model;
using DaJet.Metadata.Services;
using System;

namespace DaJet.Metadata.Enrichers
{
    public sealed class EnumerationEnricher : IContentEnricher
    {
        private Configurator Configurator { get; }
        public EnumerationEnricher(Configurator configurator)
        {
            Configurator = configurator;
        }
        public void Enrich(MetadataObject metadataObject)
        {
            if (!(metadataObject is Enumeration enumeration)) throw new ArgumentOutOfRangeException();

            ConfigObject configObject = Configurator.FileReader.ReadConfigObject(enumeration.FileName.ToString());

            enumeration.Uuid = configObject.GetUuid(new int[] { 1, 1 });
            enumeration.Name = configObject.GetString(new int[] { 1, 5, 1, 2 });
            ConfigObject alias = configObject.GetObject(new int[] { 1, 5, 1, 3 });
            if (alias.Values.Count == 3)
            {
                enumeration.Alias = configObject.GetString(new int[] { 1, 5, 1, 3, 2 });
            }
            Configurator.ConfigurePropertyСсылка(enumeration);
            Configurator.ConfigurePropertyПорядок(enumeration);

            // 6 - коллекция значений
            ConfigObject values = configObject.GetObject(new int[] { 6 });
            // 6.0 = bee0a08c-07eb-40c0-8544-5c364c171465 - идентификатор коллекции значений
            Guid valuesUuid = configObject.GetUuid(new int[] { 6, 0 });
            if (valuesUuid == new Guid("bee0a08c-07eb-40c0-8544-5c364c171465"))
            {
                ConfigureValues(enumeration, values);
            }
        }
        private void ConfigureValues(Enumeration enumeration, ConfigObject values)
        {
            int valuesCount = values.GetInt32(new int[] { 1 }); // количество значений
            if (valuesCount == 0) return;

            int offset = 2;
            for (int v = 0; v < valuesCount; v++)
            {
                // V.0.1.1.2 - value uuid
                Guid uuid = values.GetUuid(new int[] { v + offset, 0, 1, 1, 2 });
                // V.0.1.2 - value name
                string name = values.GetString(new int[] { v + offset, 0, 1, 2 });
                // P.0.1.3 - value alias descriptor
                string alias = string.Empty;
                ConfigObject aliasDescriptor = values.GetObject(new int[] { v + offset, 0, 1, 3 });
                if (aliasDescriptor.Values.Count == 3)
                {
                    // P.0.1.3.2 - value alias
                    alias = values.GetString(new int[] { v + offset, 0, 1, 3, 2 });
                }
                enumeration.Values.Add(new EnumValue()
                {
                    Uuid = uuid,
                    Name = name,
                    Alias = alias
                });
            }
        }
    }
}