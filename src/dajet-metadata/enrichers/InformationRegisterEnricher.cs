using DaJet.Metadata.Model;
using DaJet.Metadata.Services;
using System;

namespace DaJet.Metadata.Enrichers
{
    public sealed class InformationRegisterEnricher : IContentEnricher
    {
        private Configurator Configurator { get; }
        public InformationRegisterEnricher(Configurator configurator)
        {
            Configurator = configurator;
        }
        public void Enrich(MetadataObject metadataObject)
        {
            if (!(metadataObject is InformationRegister register)) throw new ArgumentOutOfRangeException();

            ConfigObject configObject = Configurator.FileReader.ReadConfigObject(register.FileName.ToString());

            if (configObject == null) return; // TODO: log error

            register.Name = configObject.GetString(new int[] { 1, 15, 1, 2 });
            ConfigObject alias = configObject.GetObject(new int[] { 1, 15, 1, 3 });
            if (alias.Values.Count == 3)
            {
                register.Alias = configObject.GetString(new int[] { 1, 15, 1, 3, 2 });
            }
            register.UseRecorder = configObject.GetInt32(new int[] { 1, 19 }) != 0;
            register.Periodicity = (RegisterPeriodicity)configObject.GetInt32(new int[] { 1, 18 });

            if (register.Periodicity != RegisterPeriodicity.None)
            {
                Configurator.ConfigurePropertyПериод(register);
            }
            if (register.UseRecorder)
            {
                Configurator.ConfigurePropertyНомерЗаписи(register);
                Configurator.ConfigurePropertyАктивность(register);
            }

            // 4 - коллекция измерений
            ConfigObject properties = configObject.GetObject(new int[] { 4 });
            // 4.0 = 13134203-f60b-11d5-a3c7-0050bae0a776 - идентификатор коллекции измерений
            Guid propertiesUuid = configObject.GetUuid(new int[] { 4, 0 });
            if (propertiesUuid == new Guid("13134203-f60b-11d5-a3c7-0050bae0a776"))
            {
                Configurator.ConfigureProperties(register, properties, PropertyPurpose.Dimension);
            }

            // 3 - коллекция ресурсов
            properties = configObject.GetObject(new int[] { 3 });
            // 3.0 = 13134202-f60b-11d5-a3c7-0050bae0a776 - идентификатор коллекции ресурсов
            propertiesUuid = configObject.GetUuid(new int[] { 3, 0 });
            if (propertiesUuid == new Guid("13134202-f60b-11d5-a3c7-0050bae0a776"))
            {
                Configurator.ConfigureProperties(register, properties, PropertyPurpose.Measure);
            }

            // 7 - коллекция реквизитов
            properties = configObject.GetObject(new int[] { 7 });
            // 7.0 = a2207540-1400-11d6-a3c7-0050bae0a776 - идентификатор коллекции реквизитов
            propertiesUuid = configObject.GetUuid(new int[] { 7, 0 });
            if (propertiesUuid == new Guid("a2207540-1400-11d6-a3c7-0050bae0a776"))
            {
                Configurator.ConfigureProperties(register, properties, PropertyPurpose.Property);
            }

            Configurator.ConfigureSharedProperties(register);
        }
    }
}