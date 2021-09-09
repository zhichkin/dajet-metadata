using DaJet.Metadata.Model;
using DaJet.Metadata.Services;
using System;

namespace DaJet.Metadata.Enrichers
{
    public sealed class AccumulationRegisterEnricher : IContentEnricher
    {
        private Configurator Configurator { get; }
        public AccumulationRegisterEnricher(Configurator configurator)
        {
            Configurator = configurator;
        }
        public void Enrich(MetadataObject metadataObject)
        {
            if (!(metadataObject is AccumulationRegister register)) throw new ArgumentOutOfRangeException();

            ConfigObject configObject = Configurator.FileReader.ReadConfigObject(register.FileName.ToString());

            if (configObject == null) return; // TODO: log error

            register.Name = configObject.GetString(new int[] { 1, 13, 1, 2 });
            ConfigObject alias = configObject.GetObject(new int[] { 1, 13, 1, 3 });
            if (alias.Values.Count == 3)
            {
                register.Alias = configObject.GetString(new int[] { 1, 13, 1, 3, 2 });
            }
            register.UseSplitter = configObject.GetInt32(new int[] { 1, 20 }) == 1;
            register.RegisterKind = (RegisterKind)configObject.GetInt32(new int[] { 1, 15 });

            Configurator.ConfigurePropertyПериод(register);
            Configurator.ConfigurePropertyНомерЗаписи(register);
            Configurator.ConfigurePropertyАктивность(register);
            if (register.RegisterKind == RegisterKind.Balance)
            {
                Configurator.ConfigurePropertyВидДвижения(register);
            }

            // 7 - коллекция измерений
            ConfigObject properties = configObject.GetObject(new int[] { 7 });
            // 7.0 = b64d9a43-1642-11d6-a3c7-0050bae0a776 - идентификатор коллекции измерений
            Guid propertiesUuid = configObject.GetUuid(new int[] { 7, 0 });
            if (propertiesUuid == new Guid("b64d9a43-1642-11d6-a3c7-0050bae0a776"))
            {
                Configurator.ConfigureProperties(register, properties, PropertyPurpose.Dimension);
            }
            // TODO: ???
            // Configurator.ConfigurePropertyDimHash(register);
            // Справка 1С: Хеш-функция измерений.
            // Поле присутствует, если количество измерений не позволяет организовать уникальный индекс по измерениям.

            // 5 - коллекция ресурсов
            properties = configObject.GetObject(new int[] { 5 });
            // 5.0 = b64d9a41-1642-11d6-a3c7-0050bae0a776 - идентификатор коллекции ресурсов
            propertiesUuid = configObject.GetUuid(new int[] { 5, 0 });
            if (propertiesUuid == new Guid("b64d9a41-1642-11d6-a3c7-0050bae0a776"))
            {
                Configurator.ConfigureProperties(register, properties, PropertyPurpose.Measure);
            }

            // 6 - коллекция реквизитов
            properties = configObject.GetObject(new int[] { 6 });
            // 6.0 = b64d9a42-1642-11d6-a3c7-0050bae0a776 - идентификатор коллекции реквизитов
            propertiesUuid = configObject.GetUuid(new int[] { 6, 0 });
            if (propertiesUuid == new Guid("b64d9a42-1642-11d6-a3c7-0050bae0a776"))
            {
                Configurator.ConfigureProperties(register, properties, PropertyPurpose.Property);
            }

            Configurator.ConfigureSharedProperties(register);
        }
    }
}