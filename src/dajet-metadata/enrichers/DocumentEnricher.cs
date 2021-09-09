using DaJet.Metadata.Model;
using DaJet.Metadata.Services;
using System;

namespace DaJet.Metadata.Enrichers
{
    public sealed class DocumentEnricher : IContentEnricher
    {
        private Configurator Configurator { get; }
        public DocumentEnricher(Configurator configurator)
        {
            Configurator = configurator;
        }
        public void Enrich(MetadataObject metadataObject)
        {
            if (!(metadataObject is Document document)) throw new ArgumentOutOfRangeException();

            ConfigObject configObject = Configurator.FileReader.ReadConfigObject(document.FileName.ToString());

            if (configObject == null) return; // TODO: log error

            document.Uuid = configObject.GetUuid(new int[] { 1, 3 });
            document.Name = configObject.GetString(new int[] { 1, 9, 1, 2 });
            ConfigObject alias = configObject.GetObject(new int[] { 1, 9, 1, 3 });
            if (alias.Values.Count == 3)
            {
                document.Alias = configObject.GetString(new int[] { 1, 9, 1, 3, 2 });
            }
            
            document.NumberType = (NumberType)configObject.GetInt32(new int[] { 1, 11 });
            document.NumberLength = configObject.GetInt32(new int[] { 1, 12 });
            document.Periodicity = (Periodicity)configObject.GetInt32(new int[] { 1, 13 });

            Configurator.ConfigurePropertyСсылка(document);
            Configurator.ConfigurePropertyВерсияДанных(document);
            Configurator.ConfigurePropertyПометкаУдаления(document);
            Configurator.ConfigurePropertyДата(document);
            if (document.NumberLength > 0)
            {
                if (document.Periodicity != Periodicity.None)
                {
                    Configurator.ConfigurePropertyПериодичность(document);
                }
                Configurator.ConfigurePropertyНомер(document);
            }
            Configurator.ConfigurePropertyПроведён(document);

            ConfigObject registers = configObject.GetObject(new int[] { 1, 24 });
            Configurator.ConfigureRegistersToPost(document, registers);

            // 5 - коллекция реквизитов
            ConfigObject properties = configObject.GetObject(new int[] { 5 });
            // 5.0 = 45e46cbc-3e24-4165-8b7b-cc98a6f80211 - идентификатор коллекции реквизитов
            Guid propertiesUuid = configObject.GetUuid(new int[] { 5, 0 });
            if (propertiesUuid == new Guid("45e46cbc-3e24-4165-8b7b-cc98a6f80211"))
            {
                Configurator.ConfigureProperties(document, properties, PropertyPurpose.Property);
            }

            Configurator.ConfigureSharedProperties(document);

            // 3 - коллекция табличных частей справочника
            ConfigObject tableParts = configObject.GetObject(new int[] { 3 });
            // 3.0 = 21c53e09-8950-4b5e-a6a0-1054f1bbc274 - идентификатор коллекции табличных частей
            Guid collectionUuid = configObject.GetUuid(new int[] { 3, 0 });
            if (collectionUuid == new Guid("21c53e09-8950-4b5e-a6a0-1054f1bbc274"))
            {
                Configurator.ConfigureTableParts(document, tableParts);
            }
        }
    }
}