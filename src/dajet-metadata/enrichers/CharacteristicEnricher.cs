using DaJet.Metadata.Converters;
using DaJet.Metadata.Model;
using DaJet.Metadata.Services;
using System;

namespace DaJet.Metadata.Enrichers
{
    public sealed class CharacteristicEnricher : IContentEnricher
    {
        private Configurator Configurator { get; }
        private IConfigObjectConverter TypeInfoConverter { get; }
        public CharacteristicEnricher(Configurator configurator)
        {
            Configurator = configurator;
            TypeInfoConverter = Configurator.GetConverter<DataTypeInfo>();
        }
        public void Enrich(MetadataObject metadataObject)
        {
            if (!(metadataObject is Characteristic model)) throw new ArgumentOutOfRangeException();

            ConfigObject configObject = Configurator.FileReader.ReadConfigObject(model.FileName.ToString());

            model.Uuid = configObject.GetUuid(new int[] { 1, 3 });
            model.TypeUuid = configObject.GetUuid(new int[] { 1, 9 });
            model.Name = configObject.GetString(new int[] { 1, 13, 1, 2 });
            ConfigObject alias = configObject.GetObject(new int[] { 1, 13, 1, 3 });
            if (alias.Values.Count == 3)
            {
                model.Alias = configObject.GetString(new int[] { 1, 13, 1, 3, 2 });
            }
            model.CodeLength = configObject.GetInt32(new int[] { 1, 21 });
            model.DescriptionLength = configObject.GetInt32(new int[] { 1, 23 });
            model.IsHierarchical = configObject.GetInt32(new int[] { 1, 19 }) != 0;

            Configurator.ConfigurePropertyСсылка(model);
            Configurator.ConfigurePropertyВерсияДанных(model);
            Configurator.ConfigurePropertyПометкаУдаления(model);
            Configurator.ConfigurePropertyПредопределённый(model);
            Configurator.ConfigurePropertyТипЗначения(model);

            if (model.CodeLength > 0)
            {
                Configurator.ConfigurePropertyКод(model);
            }
            if (model.DescriptionLength > 0)
            {
                Configurator.ConfigurePropertyНаименование(model);
            }
            if (model.IsHierarchical)
            {
                Configurator.ConfigurePropertyРодитель(model);
                if (model.HierarchyType == HierarchyType.Groups)
                {
                    Configurator.ConfigurePropertyЭтоГруппа(model);
                }
            }

            // 1.18 - описание типов значений характеристики
            ConfigObject propertyTypes = configObject.GetObject(new int[] { 1, 18 });
            model.TypeInfo = (DataTypeInfo)TypeInfoConverter.Convert(propertyTypes);

            // 3 - коллекция реквизитов
            ConfigObject properties = configObject.GetObject(new int[] { 3 });
            // 3.0 = 31182525-9346-4595-81f8-6f91a72ebe06 - идентификатор коллекции реквизитов
            Guid propertiesUuid = configObject.GetUuid(new int[] { 3, 0 });
            if (propertiesUuid == new Guid("31182525-9346-4595-81f8-6f91a72ebe06"))
            {
                Configurator.ConfigureProperties(model, properties);
            }

            Configurator.ConfigureSharedProperties(model);

            // 5 - коллекция табличных частей
            ConfigObject tableParts = configObject.GetObject(new int[] { 5 });
            // 5.0 = 54e36536-7863-42fd-bea3-c5edd3122fdc - идентификатор коллекции табличных частей
            Guid collectionUuid = configObject.GetUuid(new int[] { 5, 0 });
            if (collectionUuid == new Guid("54e36536-7863-42fd-bea3-c5edd3122fdc"))
            {
                Configurator.ConfigureTableParts(model, tableParts);
            }
        }
    }
}