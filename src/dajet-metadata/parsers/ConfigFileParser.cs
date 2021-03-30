using DaJet.Metadata.Model;
using System;
using System.Collections.Generic;
using System.IO;

namespace DaJet.Metadata.Parsers
{
    public sealed class ConfigFileParser
    {
        private const string ROOT_FILE_NAME = "root";
        private IMetadataFileReader FileReader { get; set; }
        public ConfigFileParser(IMetadataFileReader fileReader)
        {
            FileReader = fileReader;
        }
        public void Parse(InfoBase infoBase)
        {
            MDObject mdo;
            string fileName = GetConfigurationFileName();
            byte[] fileData = FileReader.ReadBytes(fileName);
            using (StreamReader reader = FileReader.CreateReader(fileData))
            {
                mdo = MDObjectParser.Parse(reader);
            }
            ConfigureSharedProperties(mdo, infoBase);
        }
        private string GetConfigurationFileName()
        {
            string fileName = null;
            byte[] fileData = FileReader.ReadBytes(ROOT_FILE_NAME);
            using (StreamReader reader = FileReader.CreateReader(fileData))
            {
                MDObject mdo = MDObjectParser.Parse(reader);
                fileName = MDObjectParser.GetString(mdo, new int[] { 1 });
            }
            return fileName;
        }
        private void ConfigureSharedProperties(MDObject mdo, InfoBase infoBase)
        {
            // 3.1.8.0 = 15794563-ccec-41f6-a83c-ec5f7b9a5bc1 - идентификатор коллекции общих реквизитов
            Guid collectionUuid = new Guid(MDObjectParser.GetString(mdo, new int[] { 3, 1, 8, 0 }));
            if (collectionUuid == new Guid("15794563-ccec-41f6-a83c-ec5f7b9a5bc1"))
            {
                int count = MDObjectParser.GetInt32(mdo, new int[] { 3, 1, 8, 1 });
                if (count == 0) return;

                // 3.1.8 - коллекция общих реквизитов
                MDObject collection = MDObjectParser.GetObject(mdo, new int[] { 3, 1, 8 });

                int offset = 2;
                SharedProperty property;
                for (int i = 0; i < count; i++)
                {
                    property = new SharedProperty()
                    {
                        FileName = new Guid(MDObjectParser.GetString(collection, new int[] { i + offset }))
                    };
                    ConfigureSharedProperty(infoBase, property);
                    infoBase.SharedProperties.Add(property.FileName, property);
                }
            }
        }
        private void ConfigureSharedProperty(InfoBase infoBase, SharedProperty property)
        {
            MDObject mdo;
            byte[] fileData = FileReader.ReadBytes(property.FileName.ToString());
            using (StreamReader reader = FileReader.CreateReader(fileData))
            {
                mdo = MDObjectParser.Parse(reader);
            }

            if (infoBase.Properties.TryGetValue(property.FileName, out MetadataProperty propertyInfo))
            {
                property.DbName = propertyInfo.DbName;
            }
            property.Name = MDObjectParser.GetString(mdo, new int[] { 1, 1, 1, 1, 2 });
            MDObject aliasDescriptor = MDObjectParser.GetObject(mdo, new int[] { 1, 1, 1, 1, 3 });
            if (aliasDescriptor.Values.Count == 3)
            {
                property.Alias = MDObjectParser.GetString(mdo, new int[] { 1, 1, 1, 1, 3, 2 });
            }
            property.AutomaticUsage = (AutomaticUsage)MDObjectParser.GetInt32(mdo, new int[] { 1, 6 });

            // 1.1.1.2 - описание типов значений общего реквизита
            MDObject propertyTypes = MDObjectParser.GetObject(mdo, new int[] { 1, 1, 1, 2 });
            ConfigurePropertyType(propertyTypes, property);

            ConfigureDatabaseFields(property);

            // 1.2.1 - количество объектов метаданных, у которых значение использования общего реквизита не равно "Автоматически"
            int count = MDObjectParser.GetInt32(mdo, new int[] { 1, 2, 1 });
            if (count == 0) return;
            int step = 2;
            count *= step;
            int uuidIndex = 2;
            int usageOffset = 1;
            while (uuidIndex <= count)
            {
                Guid uuid = new Guid(MDObjectParser.GetString(mdo, new int[] { 1, 2, uuidIndex }));
                SharedPropertyUsage usage = (SharedPropertyUsage)MDObjectParser.GetInt32(mdo, new int[] { 1, 2, uuidIndex + usageOffset, 1 });
                property.UsageSettings.Add(uuid, usage);
                uuidIndex += step;
            }
        }
        private void ConfigurePropertyType(MDObject propertyTypes, MetadataProperty property)
        {
            // 0 = "Pattern"
            int typeOffset = 1;
            List<Guid> typeUuids = new List<Guid>();
            DataTypeInfo typeInfo = new DataTypeInfo();
            int count = propertyTypes.Values.Count - 1;

            for (int t = 0; t < count; t++)
            {
                // T - type descriptor
                MDObject descriptor = MDObjectParser.GetObject(propertyTypes, new int[] { t + typeOffset });

                // T.Q - property type qualifiers
                string[] qualifiers = new string[descriptor.Values.Count];
                for (int q = 0; q < descriptor.Values.Count; q++)
                {
                    qualifiers[q] = MDObjectParser.GetString(propertyTypes, new int[] { t + typeOffset, q });
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
                        typeInfo.CanBeReference = true;
                        typeUuids.Add(typeUuid);
                    }
                }
            }
            if (typeUuids.Count == 1) // single type value
            {
                typeInfo.ReferenceTypeUuid = typeUuids[0];
            }
            property.PropertyType = typeInfo;
        }
        private void ConfigureDatabaseFields(MetadataProperty property)
        {
            if (property.PropertyType.IsMultipleType)
            {
                ConfigureDatabaseFieldsForMultipleType(property);
            }
            else
            {
                ConfigureDatabaseFieldsForSingleType(property);
            }
        }
        private void ConfigureDatabaseFieldsForSingleType(MetadataProperty property)
        {
            if (property.PropertyType.IsUuid)
            {
                property.Fields.Add(new DatabaseField(property.DbName, "binary", 16));
            }
            else if (property.PropertyType.IsBinary)
            {
                // is used only for system properties of system types
                // TODO: log if it eventually happens
            }
            else if (property.PropertyType.IsValueStorage)
            {
                property.Fields.Add(new DatabaseField(property.DbName, "varbinary", -1));
            }
            else if (property.PropertyType.CanBeString)
            {
                // should be updated from database
                property.Fields.Add(new DatabaseField(property.DbName, "nvarchar", 10));
            }
            else if (property.PropertyType.CanBeNumeric)
            {
                // should be updated from database
                property.Fields.Add(new DatabaseField(property.DbName, "numeric", 9, 10, 0));
            }
            else if (property.PropertyType.CanBeBoolean)
            {
                property.Fields.Add(new DatabaseField(property.DbName, "binary", 1));
            }
            else if (property.PropertyType.CanBeDateTime)
            {
                // can be updated from database
                property.Fields.Add(new DatabaseField(property.DbName, "datetime2", 6, 19, 0));
            }
            else if (property.PropertyType.CanBeReference)
            {
                property.Fields.Add(new DatabaseField(property.DbName + MetadataTokens.RRef, "binary", 16));
            }
        }
        private void ConfigureDatabaseFieldsForMultipleType(MetadataProperty property)
        {
            property.Fields.Add(new DatabaseField(property.DbName + "_" + MetadataTokens.TYPE, "binary", 1));
            if (property.PropertyType.CanBeString)
            {
                // should be updated from database
                property.Fields.Add(new DatabaseField(property.DbName + "_" + MetadataTokens.S, "nvarchar", 10));
            }
            if (property.PropertyType.CanBeNumeric)
            {
                // should be updated from database
                property.Fields.Add(new DatabaseField(property.DbName + "_" + MetadataTokens.N, "numeric", 9, 10, 0));
            }
            if (property.PropertyType.CanBeBoolean)
            {
                property.Fields.Add(new DatabaseField(property.DbName + "_" + MetadataTokens.L, "binary", 1));
            }
            if (property.PropertyType.CanBeDateTime)
            {
                // can be updated from database
                property.Fields.Add(new DatabaseField(property.DbName + "_" + MetadataTokens.T, "datetime2", 6, 19, 0));
            }
            if (property.PropertyType.CanBeReference)
            {
                if (property.PropertyType.ReferenceTypeUuid == Guid.Empty) // miltiple refrence type
                {
                    property.Fields.Add(new DatabaseField(property.DbName + "_" + MetadataTokens.RTRef, "binary", 4));
                }
                property.Fields.Add(new DatabaseField(property.DbName + "_" + MetadataTokens.RRRef, "binary", 16));
            }
        }
    }
}