using DaJet.Metadata.Model;
using DaJet.Metadata.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DaJet.Metadata.Converters
{
    public sealed class DataTypeInfoConverter : IConfigObjectConverter
    {
        private readonly Dictionary<Guid, Dictionary<Guid, ApplicationObject>> ReferenceBaseTypes = new Dictionary<Guid, Dictionary<Guid, ApplicationObject>>();
        private Configurator Configurator { get; }
        public DataTypeInfoConverter(Configurator configurator)
        {
            Configurator = configurator;
            ReferenceBaseTypes.Add(new Guid("280f5f0e-9c8a-49cc-bf6d-4d296cc17a63"), null); // ЛюбаяСсылка
            ReferenceBaseTypes.Add(new Guid("e61ef7b8-f3e1-4f4b-8ac7-676e90524997"), Configurator.InfoBase.Catalogs); // СправочникСсылка
            ReferenceBaseTypes.Add(new Guid("38bfd075-3e63-4aaa-a93e-94521380d579"), Configurator.InfoBase.Documents); // ДокументСсылка
            ReferenceBaseTypes.Add(new Guid("474c3bf6-08b5-4ddc-a2ad-989cedf11583"), Configurator.InfoBase.Enumerations); // ПеречислениеСсылка
            ReferenceBaseTypes.Add(new Guid("0a52f9de-73ea-4507-81e8-66217bead73a"), Configurator.InfoBase.Publications); // ПланОбменаСсылка
            ReferenceBaseTypes.Add(new Guid("99892482-ed55-4fb5-a7f7-20888820a758"), Configurator.InfoBase.Characteristics); // ПланВидовХарактеристикСсылка
            ReferenceBaseTypes.Add(new Guid("ac606d60-0209-4159-8e4c-794bc091ce38"), Configurator.InfoBase.Accounts); // ПланСчетовСсылка
        }
        public object Convert(ConfigObject configObject)
        {
            DataTypeInfo typeInfo = new DataTypeInfo();

            // 0 = "Pattern"
            int typeOffset = 1;
            List<Guid> typeUuids = new List<Guid>();
            int count = configObject.Values.Count - 1;

            for (int t = 0; t < count; t++)
            {
                // T - type descriptor
                ConfigObject descriptor = configObject.GetObject(new int[] { t + typeOffset });

                // T.Q - property type qualifiers
                string[] qualifiers = new string[descriptor.Values.Count];
                for (int q = 0; q < descriptor.Values.Count; q++)
                {
                    qualifiers[q] = configObject.GetString(new int[] { t + typeOffset, q });
                }
                if (qualifiers[0] == MetadataTokens.B) typeInfo.CanBeBoolean = true; // {"B"}
                else if (qualifiers[0] == MetadataTokens.S) typeInfo.CanBeString = true; // {"S"} | {"S",10,0} | {"S",10,1}
                else if (qualifiers[0] == MetadataTokens.N) typeInfo.CanBeNumeric = true; // {"N",10,2,0} | {"N",10,2,1}
                else if (qualifiers[0] == MetadataTokens.D) typeInfo.CanBeDateTime = true; // {"D"} | {"D","D"} | {"D","T"}
                else if (qualifiers[0] == MetadataTokens.R) // {"#",70497451-981e-43b8-af46-fae8d65d16f2}
                {
                    Guid typeUuid = new Guid(qualifiers[1]);
                    if (typeUuid == new Guid("e199ca70-93cf-46ce-a54b-6edc88c3a296")) // ХранилищеЗначения - varbinary(max)
                    {
                        typeInfo.IsValueStorage = true;
                    }
                    else if (typeUuid == new Guid("fc01b5df-97fe-449b-83d4-218a090e681e")) // УникальныйИдентификатор - binary(16)
                    {
                        typeInfo.IsUuid = true;
                    }
                    else if (ReferenceBaseTypes.TryGetValue(typeUuid, out Dictionary<Guid, ApplicationObject> collection))
                    {
                        if (collection == null) // Любая ссылка
                        {
                            typeInfo.CanBeReference = true;
                            typeUuids.Add(Guid.Empty);
                        }
                        else if (collection.Count == 1) // Единственный объект метаданных в коллекции
                        {
                            typeInfo.CanBeReference = true;
                            typeUuids.Add(collection.Values.First().Uuid);
                        }
                        else // Множественный ссылочный тип данных
                        {
                            typeInfo.CanBeReference = true;
                            typeUuids.Add(Guid.Empty);
                        }
                    }
                    else if (Configurator.InfoBase.CompoundTypes.TryGetValue(typeUuid, out CompoundType compound))
                    {
                        // since 8.3.3
                        ApplyCompoundType(typeInfo, compound);
                        typeUuids.Add(compound.TypeInfo.ReferenceTypeUuid);
                    }
                    else if (1 == 0)
                    {
                        // TODO: найти характеристику
                    }
                    else
                    {
                        // неизвестный тип данных
                        typeInfo.CanBeReference = true;
                        typeUuids.Add(typeUuid);
                    }
                }
            }
            if (typeUuids.Count == 1) // single type value
            {
                typeInfo.ReferenceTypeUuid = typeUuids[0];
            }

            return typeInfo;
        }
        private void ApplyCompoundType(DataTypeInfo typeInfo, CompoundType compound)
        {
            // TODO: add internal flags field to the DataTypeInfo class so as to use bitwise operations
            if (!typeInfo.CanBeString && compound.TypeInfo.CanBeString) typeInfo.CanBeString = true;
            if (!typeInfo.CanBeBoolean && compound.TypeInfo.CanBeBoolean) typeInfo.CanBeBoolean = true;
            if (!typeInfo.CanBeNumeric && compound.TypeInfo.CanBeNumeric) typeInfo.CanBeNumeric = true;
            if (!typeInfo.CanBeDateTime && compound.TypeInfo.CanBeDateTime) typeInfo.CanBeDateTime = true;
            if (!typeInfo.CanBeReference && compound.TypeInfo.CanBeReference) typeInfo.CanBeReference = true;
            if (!typeInfo.IsUuid && compound.TypeInfo.IsUuid) typeInfo.IsUuid = true;
            if (!typeInfo.IsValueStorage && compound.TypeInfo.IsValueStorage) typeInfo.IsValueStorage = true;
            if (!typeInfo.IsBinary && compound.TypeInfo.IsBinary) typeInfo.IsBinary = true;
        }
    }
}