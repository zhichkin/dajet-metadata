using DaJet.Metadata.Enrichers;
using DaJet.Metadata.Model;
using DaJet.Metadata.Parsers;
using System;
using System.Collections.Generic;
using System.IO;

namespace DaJet.Metadata.Services
{
    public sealed class Configurator
    {
        private const string ROOT_FILE_NAME = "root"; // Config
        private const string DBNAMES_FILE_NAME = "DBNames"; // Params

        internal InfoBase InfoBase;
        internal readonly IConfigFileReader FileReader;
        private readonly Dictionary<Type, IContentEnricher> Enrichers = new Dictionary<Type, IContentEnricher>();

        public Configurator(IConfigFileReader fileReader)
        {
            FileReader = fileReader ?? throw new ArgumentNullException();

            InitializeEnrichers();
        }
        private void InitializeEnrichers()
        {
            Enrichers.Add(typeof(InfoBase), new InfoBaseEnricher(this));
            Enrichers.Add(typeof(Catalog), new CatalogEnricher(this));
        }
        public InfoBase OpenInfoBase()
        {
            InfoBase = new InfoBase();

            DbNamesParser dbNames = new DbNamesParser();
            IMetadataManager manager = new MetadataManager();

            byte[] fileData = FileReader.ReadBytes(DBNAMES_FILE_NAME);
            using (StreamReader reader = FileReader.CreateReader(fileData))
            {
                dbNames.Parse(reader, InfoBase, manager);
            }

            ConfigObject root = FileReader.ReadConfigObject(ROOT_FILE_NAME);
            ConfigObject cfg = FileReader.ReadConfigObject(root.GetString(new int[] { 1 }));
            IContentEnricher enricher = GetEnricher<InfoBase>();
            enricher.Enrich(InfoBase, cfg);

            return InfoBase;
        }

        

        public IContentEnricher GetEnricher<T>() where T : MetadataObject
        {
            return GetEnricher(typeof(T));
        }
        public IContentEnricher GetEnricher(Type type)
        {
            if (Enrichers.TryGetValue(type, out IContentEnricher converter))
            {
                return converter;
            }
            return null;
        }
        
        public void ConfigureProperties(ApplicationObject metaObject, ConfigObject properties)
        {
            int propertiesCount = properties.GetInt32(new int[] { 1 }); // количество реквизитов
            if (propertiesCount == 0) return;

            int typeOffset = 1;
            int propertyOffset = 2;
            for (int p = 0; p < propertiesCount; p++)
            {
                // P.0.1.1.1.1.2 - property uuid
                Guid propertyUuid = properties.GetUuid(new int[] { p + propertyOffset, 0, 1, 1, 1, 1, 2 });
                // P.0.1.1.1.2 - property name
                string propertyName = properties.GetString(new int[] { p + propertyOffset, 0, 1, 1, 1, 2 });
                // P.0.1.1.1.3 - property alias descriptor
                string propertyAlias = string.Empty;
                ConfigObject aliasDescriptor = properties.GetObject(new int[] { p + propertyOffset, 0, 1, 1, 1, 3 });
                if (aliasDescriptor.Values.Count == 3)
                {
                    // P.0.1.1.1.3.2 - property alias
                    propertyAlias = properties.GetString(new int[] { p + propertyOffset, 0, 1, 1, 1, 3, 2 });
                }
                // P.0.1.1.2 - property types
                ConfigObject propertyTypes = properties.GetObject(new int[] { p + propertyOffset, 0, 1, 1, 2 });
                // P.0.1.1.2.0 = "Pattern"
                List<Guid> typeUuids = new List<Guid>();
                DataTypeInfo typeInfo = new DataTypeInfo();
                for (int t = 0; t < propertyTypes.Values.Count - 1; t++)
                {
                    // P.0.1.1.2.T - property type descriptor
                    ConfigObject propertyTypeInfo = properties.GetObject(new int[] { p + propertyOffset, 0, 1, 1, 2, t + typeOffset });

                    // P.0.1.1.2.T.Q - property type qualifiers
                    string[] qualifiers = new string[propertyTypeInfo.Values.Count];
                    for (int q = 0; q < propertyTypeInfo.Values.Count; q++)
                    {
                        qualifiers[q] = properties.GetString(new int[] { p + propertyOffset, 0, 1, 1, 2, t + typeOffset, q });
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

                ConfigureProperty(metaObject, propertyUuid, propertyName, propertyAlias, typeInfo);
            }
        }
        public void ConfigureProperty(ApplicationObject metaObject, Guid fileName, string name, string alias, DataTypeInfo type)
        {
            if (!InfoBase.Properties.TryGetValue(fileName, out MetadataProperty property)) return;

            property.Name = name;
            property.Alias = alias;
            property.PropertyType = type;
            metaObject.Properties.Add(property);

            ConfigureDatabaseFields(property);
        }
        public void ConfigurePropertyType(MetadataProperty property, ConfigObject propertyTypes)
        {
            // 0 = "Pattern"
            int typeOffset = 1;
            List<Guid> typeUuids = new List<Guid>();
            DataTypeInfo typeInfo = new DataTypeInfo();
            int count = propertyTypes.Values.Count - 1;

            for (int t = 0; t < count; t++)
            {
                // T - type descriptor
                ConfigObject descriptor = propertyTypes.GetObject(new int[] { t + typeOffset });

                // T.Q - property type qualifiers
                string[] qualifiers = new string[descriptor.Values.Count];
                for (int q = 0; q < descriptor.Values.Count; q++)
                {
                    qualifiers[q] = propertyTypes.GetString(new int[] { t + typeOffset, q });
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
        public void ConfigureSharedProperties(ApplicationObject metaObject)
        {
            foreach (SharedProperty property in InfoBase.SharedProperties.Values)
            {
                if (property.UsageSettings.TryGetValue(metaObject.FileName, out SharedPropertyUsage usage))
                {
                    if (usage == SharedPropertyUsage.Use)
                    {
                        metaObject.Properties.Add(property);
                    }
                }
                else // Auto
                {
                    if (property.AutomaticUsage == AutomaticUsage.Use)
                    {
                        metaObject.Properties.Add(property);
                    }
                }
            }
        }

        public void ConfigureTableParts(ApplicationObject owner, ConfigObject tableParts)
        {
            int tablePartsCount = tableParts.GetInt32(new int[] { 1 }); // количество табличных частей
            if (tablePartsCount == 0) return;

            int offset = 2;
            for (int t = 0; t < tablePartsCount; t++)
            {
                // T.0.1.5.1.1.2 - uuid табличной части
                Guid uuid = tableParts.GetUuid(new int[] { t + offset, 0, 1, 5, 1, 1, 2 });
                // T.0.1.5.1.2 - имя табличной части
                string name = tableParts.GetString(new int[] { t + offset, 0, 1, 5, 1, 2 });

                if (InfoBase.TableParts.TryGetValue(uuid, out ApplicationObject tablePart))
                {
                    if (tablePart is TablePart)
                    {
                        tablePart.Name = name;
                        ((TablePart)tablePart).Owner = owner;
                        tablePart.TableName = owner.TableName + tablePart.TableName;
                        owner.ApplicationObjects.Add(tablePart);

                        // T.2 - коллекция реквизитов табличной части (ConfigObject)
                        // T.2.0 = 888744e1-b616-11d4-9436-004095e12fc7 - идентификатор коллекции реквизитов табличной части
                        // T.2.1 - количество реквизитов табличной части
                        Guid collectionUuid = tableParts.GetUuid(new int[] { t + offset, 2, 0 });
                        if (collectionUuid == new Guid("888744e1-b616-11d4-9436-004095e12fc7"))
                        {
                            ConfigurePropertyСсылка(owner, tablePart);
                            ConfigurePropertyКлючСтроки(tablePart);
                            ConfigurePropertyНомерСтроки(tablePart);

                            ConfigObject properties = tableParts.GetObject(new int[] { t + offset, 2 });
                            ConfigureProperties(tablePart, properties);
                        }
                    }
                }
            }
        }
        private void ConfigurePropertyСсылка(ApplicationObject owner, ApplicationObject tablePart)
        {
            MetadataProperty property = new MetadataProperty()
            {
                Name = "Ссылка",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System,
                DbName = (FileReader.DatabaseProvider == DatabaseProvider.SQLServer ? owner.TableName + "_IDRRef" : owner.TableName + "_idrref")
            };
            property.PropertyType.IsUuid = true;
            property.Fields.Add(new DatabaseField()
            {
                Name = property.DbName,
                Length = 16,
                TypeName = "binary",
                KeyOrdinal = 1,
                IsPrimaryKey = true
            });
            tablePart.Properties.Add(property);
        }
        private void ConfigurePropertyКлючСтроки(ApplicationObject tablePart)
        {
            MetadataProperty property = new MetadataProperty()
            {
                Name = "КлючСтроки",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System,
                DbName = (FileReader.DatabaseProvider == DatabaseProvider.SQLServer ? "_KeyField" : "_keyfield")
            };
            property.PropertyType.IsBinary = true;
            property.Fields.Add(new DatabaseField()
            {
                Name = property.DbName,
                Length = 4,
                TypeName = "binary",
                KeyOrdinal = 2,
                IsPrimaryKey = true
            });
            tablePart.Properties.Add(property);
        }
        private void ConfigurePropertyНомерСтроки(ApplicationObject tablePart)
        {
            if (!InfoBase.Properties.TryGetValue(tablePart.FileName, out MetadataProperty property)) return;

            property.Name = "НомерСтроки";
            property.PropertyType.CanBeNumeric = true;
            property.Fields.Add(new DatabaseField()
            {
                Name = property.DbName,
                Length = 5,
                Precision = 5,
                TypeName = "numeric"
            });

            tablePart.Properties.Add(property);
        }

        public void ConfigureDatabaseFields(MetadataProperty property)
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

        #region "Reference type system properties"

        public void ConfigurePropertyСсылка(ApplicationObject metaObject)
        {
            MetadataProperty property = new MetadataProperty()
            {
                Name = "Ссылка",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System,
                DbName = (FileReader.DatabaseProvider == DatabaseProvider.SQLServer ? "_IDRRef" : "_idrref")
            };
            property.PropertyType.IsUuid = true;
            property.Fields.Add(new DatabaseField()
            {
                Name = property.DbName,
                Length = 16,
                TypeName = "binary",
                KeyOrdinal = 1,
                IsPrimaryKey = true
            });
            metaObject.Properties.Add(property);
        }
        public void ConfigurePropertyВерсияДанных(ApplicationObject metaObject)
        {
            MetadataProperty property = new MetadataProperty()
            {
                Name = "ВерсияДанных",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System,
                DbName = (FileReader.DatabaseProvider == DatabaseProvider.SQLServer ? "_Version" : "_version")
            };
            property.PropertyType.IsBinary = true;
            property.Fields.Add(new DatabaseField()
            {
                Name = property.DbName,
                Length = 8,
                TypeName = "timestamp"
            });
            metaObject.Properties.Add(property);
        }
        public void ConfigurePropertyПометкаУдаления(ApplicationObject metaObject)
        {
            MetadataProperty property = new MetadataProperty()
            {
                Name = "ПометкаУдаления",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System,
                DbName = (FileReader.DatabaseProvider == DatabaseProvider.SQLServer ? "_Marked" : "_marked")
            };
            property.PropertyType.CanBeBoolean = true;
            property.Fields.Add(new DatabaseField()
            {
                Name = property.DbName,
                Length = 1,
                TypeName = "binary"
            });
            metaObject.Properties.Add(property);
        }
        public void ConfigurePropertyПредопределённый(ApplicationObject metaObject)
        {
            MetadataProperty property = new MetadataProperty()
            {
                Name = "Предопределённый",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System,
                DbName = (FileReader.DatabaseProvider == DatabaseProvider.SQLServer ? "_PredefinedID" : "_predefinedid")
            };
            property.PropertyType.IsUuid = true;
            property.Fields.Add(new DatabaseField()
            {
                Name = property.DbName,
                Length = 16,
                TypeName = "binary"
            });
            metaObject.Properties.Add(property);
        }
        public void ConfigurePropertyКод(Catalog catalog)
        {
            MetadataProperty property = new MetadataProperty()
            {
                Name = "Код",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System,
                DbName = (FileReader.DatabaseProvider == DatabaseProvider.SQLServer ? "_Code" : "_code")
            };
            if (catalog.CodeType == CodeType.String)
            {
                property.PropertyType.CanBeString = true;
                property.Fields.Add(new DatabaseField()
                {
                    Name = property.DbName,
                    Length = catalog.CodeLength,
                    TypeName = "nvarchar"
                });
            }
            else
            {
                property.PropertyType.CanBeNumeric = true;
                property.Fields.Add(new DatabaseField()
                {
                    Name = property.DbName,
                    Precision = catalog.CodeLength,
                    TypeName = "numeric"
                });
            }
            catalog.Properties.Add(property);
        }
        public void ConfigurePropertyНаименование(Catalog catalog)
        {
            MetadataProperty property = new MetadataProperty()
            {
                Name = "Наименование",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System,
                DbName = (FileReader.DatabaseProvider == DatabaseProvider.SQLServer ? "_Description" : "_description")
            };
            property.PropertyType.CanBeString = true;
            property.Fields.Add(new DatabaseField()
            {
                Name = property.DbName,
                Length = catalog.DescriptionLength,
                TypeName = "nvarchar"
            });
            catalog.Properties.Add(property);
        }
        public void ConfigurePropertyРодитель(Catalog catalog)
        {
            MetadataProperty property = new MetadataProperty()
            {
                Name = "Родитель",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System,
                DbName = (FileReader.DatabaseProvider == DatabaseProvider.SQLServer ? "_ParentIDRRef" : "_parentidrref")
            };
            property.PropertyType.CanBeReference = true;
            property.PropertyType.ReferenceTypeUuid = catalog.Uuid;
            property.PropertyType.ReferenceTypeCode = catalog.TypeCode;
            property.Fields.Add(new DatabaseField()
            {
                Name = property.DbName,
                Length = 16,
                TypeName = "binary"
            });
            catalog.Properties.Add(property);
        }
        public void ConfigurePropertyЭтоГруппа(Catalog catalog)
        {
            MetadataProperty property = new MetadataProperty()
            {
                Name = "ЭтоГруппа",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System,
                DbName = (FileReader.DatabaseProvider == DatabaseProvider.SQLServer ? "_Folder" : "_folder")
            };
            property.PropertyType.CanBeBoolean = true;
            property.Fields.Add(new DatabaseField()
            {
                Name = property.DbName,
                Length = 1,
                TypeName = "binary"
            });
            catalog.Properties.Add(property);
        }
        public void ConfigurePropertyВладелец(Catalog catalog, Guid owner)
        {
            MetadataProperty property = new MetadataProperty
            {
                Name = "Владелец",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System,
                DbName = (FileReader.DatabaseProvider == DatabaseProvider.SQLServer ? "_OwnerID" : "_ownerid")
            };
            property.PropertyType.CanBeReference = true;

            if (catalog.Owners == 1) // Single type value
            {
                property.PropertyType.ReferenceTypeUuid = owner;
                property.Fields.Add(new DatabaseField()
                {
                    Name = (FileReader.DatabaseProvider == DatabaseProvider.SQLServer ? "_OwnerIDRRef" : "_owneridrref"),
                    Length = 16,
                    TypeName = "binary"
                });
            }
            else // Multiple type value
            {
                property.Fields.Add(new DatabaseField()
                {
                    Name = (FileReader.DatabaseProvider == DatabaseProvider.SQLServer ? "_OwnerID_TYPE" : "_ownerid_type"),
                    Length = 1,
                    TypeName = "binary",
                    Purpose = FieldPurpose.Discriminator
                });
                property.Fields.Add(new DatabaseField()
                {
                    Name = (FileReader.DatabaseProvider == DatabaseProvider.SQLServer ? "_OwnerID_RTRef" : "_ownerid_rtref"),
                    Length = 4,
                    TypeName = "binary",
                    Purpose = FieldPurpose.TypeCode
                });
                property.Fields.Add(new DatabaseField()
                {
                    Name = (FileReader.DatabaseProvider == DatabaseProvider.SQLServer ? "_OwnerID_RRRef" : "_ownerid_rrref"),
                    Length = 16,
                    TypeName = "binary",
                    Purpose = FieldPurpose.Object
                });
            }

            catalog.Properties.Add(property);
        }

        #endregion
    }
}