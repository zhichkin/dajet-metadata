using DaJet.Metadata.Model;
using DaJet.Metadata.Services;
using System;
using System.Collections.Generic;

namespace DaJet.Metadata.Converters
{
    public sealed class DataTypeInfoConverter : IConfigObjectConverter
    {
        private Configurator Configurator { get; }
        public DataTypeInfoConverter(Configurator configurator)
        {
            Configurator = configurator;
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
                    else
                    {
                        // TODO:
                        // 1. check if it is DefinedType (since 8.3.3) определяемый тип
                        // 2. ПланОбменаСсылка, ЛюбаяСсылка, ДокументСсылка, ПеречислениеСсылка,
                        //    ПланВидовХарактеристикСсылка, ПланСчетовСсылка, СправочникСсылка
                        // 3. 
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
    }
}