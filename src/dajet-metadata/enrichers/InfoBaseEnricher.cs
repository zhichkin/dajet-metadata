using DaJet.Metadata.Model;
using DaJet.Metadata.Services;
using System;

namespace DaJet.Metadata.Enrichers
{
    public sealed class InfoBaseEnricher : IContentEnricher
    {
        private Configurator Configurator { get; }
        private IConfigFileReader FileReader { get; }
        public InfoBaseEnricher(Configurator configurator)
        {
            Configurator = configurator;
            FileReader = Configurator.FileReader;
        }
        public void Enrich(MetadataObject metadataObject, ConfigObject configObject)
        {
            if (!(metadataObject is InfoBase infoBase)) throw new ArgumentOutOfRangeException();
            
            // TODO: configure InfoBase properties, see ConfigInfo class
            ConfigureSharedProperties(configObject, infoBase);
        }

        private void ConfigureSharedProperties(ConfigObject cfo, InfoBase infoBase)
        {
            // 3.1.8.0 = 15794563-ccec-41f6-a83c-ec5f7b9a5bc1 - идентификатор коллекции общих реквизитов
            Guid collectionUuid = cfo.GetUuid(new int[] { 3, 1, 8, 0 });
            if (collectionUuid == new Guid("15794563-ccec-41f6-a83c-ec5f7b9a5bc1"))
            {
                int count = cfo.GetInt32(new int[] { 3, 1, 8, 1 });
                if (count == 0) return;

                // 3.1.8 - коллекция общих реквизитов
                ConfigObject collection = cfo.GetObject(new int[] { 3, 1, 8 });

                int offset = 2;
                SharedProperty property;
                for (int i = 0; i < count; i++)
                {
                    property = new SharedProperty()
                    {
                        FileName = collection.GetUuid(new int[] { i + offset })
                    };
                    ConfigureSharedProperty(property, infoBase);
                    infoBase.SharedProperties.Add(property.FileName, property);
                }
            }
        }
        private void ConfigureSharedProperty(SharedProperty property, InfoBase infoBase)
        {
            ConfigObject cfo = FileReader.ReadConfigObject(property.FileName.ToString());

            if (infoBase.Properties.TryGetValue(property.FileName, out MetadataProperty propertyInfo))
            {
                property.DbName = propertyInfo.DbName;
            }
            property.Name = cfo.GetString(new int[] { 1, 1, 1, 1, 2 });
            ConfigObject aliasDescriptor = cfo.GetObject(new int[] { 1, 1, 1, 1, 3 });
            if (aliasDescriptor.Values.Count == 3)
            {
                property.Alias = cfo.GetString(new int[] { 1, 1, 1, 1, 3, 2 });
            }
            property.AutomaticUsage = (AutomaticUsage)cfo.GetInt32(new int[] { 1, 6 });

            // 1.1.1.2 - описание типов значений общего реквизита
            ConfigObject propertyTypes = cfo.GetObject(new int[] { 1, 1, 1, 2 });
            Configurator.ConfigurePropertyType(property, propertyTypes);

            Configurator.ConfigureDatabaseFields(property);

            // 1.2.1 - количество объектов метаданных, у которых значение использования общего реквизита не равно "Автоматически"
            int count = cfo.GetInt32(new int[] { 1, 2, 1 });
            if (count == 0) return;
            int step = 2;
            count *= step;
            int uuidIndex = 2;
            int usageOffset = 1;
            while (uuidIndex <= count)
            {
                Guid uuid = cfo.GetUuid(new int[] { 1, 2, uuidIndex });
                SharedPropertyUsage usage = (SharedPropertyUsage)cfo.GetInt32(new int[] { 1, 2, uuidIndex + usageOffset, 1 });
                property.UsageSettings.Add(uuid, usage);
                uuidIndex += step;
            }
        }
    }
}