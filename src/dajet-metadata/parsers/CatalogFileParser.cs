using DaJet.Metadata.Model;
using DaJet.Metadata.Parsers;
using System;
using System.Collections.Generic;
using System.IO;

namespace DaJet.Metadata
{
    public sealed class CatalogFileParser
    {
        public CatalogFileParser() { }
        public void Parse(StreamReader stream, MetadataObject metaObject, InfoBase infoBase, DatabaseProviders provider)
        {
            if (!(metaObject is Catalog catalog)) throw new ArgumentOutOfRangeException("metaObject");

            MDObject mdo = MDObjectParser.Parse(stream);

            catalog.Uuid = new Guid(MDObjectParser.GetString(mdo, new int[] { 1, 3 }));
            catalog.Name = MDObjectParser.GetString(mdo, new int[] { 1, 9, 1, 2 });
            catalog.Alias = MDObjectParser.GetString(mdo, new int[] { 1, 9, 1, 3, 2 });
            catalog.CodeType = (CodeType)MDObjectParser.GetInt32(mdo, new int[] { 1, 18 });
            catalog.CodeLength = MDObjectParser.GetInt32(mdo, new int[] { 1, 17 });
            catalog.DescriptionLength = MDObjectParser.GetInt32(mdo, new int[] { 1, 19 });
            catalog.HierarchyType = (HierarchyType)MDObjectParser.GetInt32(mdo, new int[] { 1, 36 });
            catalog.IsHierarchical = MDObjectParser.GetInt32(mdo, new int[] { 1, 37 }) != 0;

            ConfigurePropertyСсылка(catalog, provider);
            ConfigurePropertyВерсияДанных(catalog, provider);
            ConfigurePropertyПометкаУдаления(catalog, provider);
            ConfigurePropertyПредопределённый(catalog, provider);

            // 1.12.1 - количество владельцев справочника
            // 1.12.N - описание владельцев
            // 1.12.N.2.1 - uuid'ы владельцев
            Guid ownerUuid = Guid.Empty;
            catalog.Owners = MDObjectParser.GetInt32(mdo, new int[] { 1, 12, 1 });
            if (catalog.Owners == 1)
            {
                ownerUuid = new Guid(MDObjectParser.GetString(mdo, new int[] { 1, 12, 2, 2, 1 }));
            }
            if (catalog.Owners > 0)
            {
                ConfigurePropertyВладелец(catalog, provider, ownerUuid);
            }

            if (catalog.CodeLength > 0)
            {
                ConfigurePropertyКод(catalog, provider);
            }
            if (catalog.DescriptionLength > 0)
            {
                ConfigurePropertyНаименование(catalog, provider);
            }
            if (catalog.IsHierarchical)
            {
                ConfigurePropertyРодитель(catalog, provider);
                if (catalog.HierarchyType == HierarchyType.Groups)
                {
                    ConfigurePropertyЭтоГруппа(catalog, provider);
                }
            }

            // 6 - коллекция реквизитов справочника
            MDObject properties = MDObjectParser.GetObject(mdo, new int[] { 6 });
            // 6.0 = cf4abea7-37b2-11d4-940f-008048da11f9 - идентификатор коллекции реквизитов справочника
            Guid propertiesUuid = new Guid(MDObjectParser.GetString(mdo, new int[] { 6, 0 }));
            if (propertiesUuid == new Guid("cf4abea7-37b2-11d4-940f-008048da11f9"))
            {
                ConfigureProperties(properties, catalog, infoBase);
            }

            ConfigureSharedProperties(catalog, infoBase);

            // 5 - коллекция табличных частей справочника
            MDObject tableParts = MDObjectParser.GetObject(mdo, new int[] { 5 });
            // 5.0 = 932159f9-95b2-4e76-a8dd-8849fe5c5ded - идентификатор коллекции табличных частей справочника
            Guid collectionUuid = new Guid(MDObjectParser.GetString(mdo, new int[] { 5, 0 }));
            if (collectionUuid == new Guid("932159f9-95b2-4e76-a8dd-8849fe5c5ded"))
            {
                ConfigureTableParts(tableParts, catalog, infoBase, provider);
            }
        }
        private void ConfigurePropertyСсылка(Catalog catalog, DatabaseProviders provider)
        {
            MetadataProperty property = new MetadataProperty()
            {
                Name = "Ссылка",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System,
                DbName = (provider == DatabaseProviders.SQLServer ? "_IDRRef" : "_idrref")
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
            catalog.Properties.Add(property);
        }
        private void ConfigurePropertyВерсияДанных(Catalog catalog, DatabaseProviders provider)
        {
            MetadataProperty property = new MetadataProperty()
            {
                Name = "ВерсияДанных",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System,
                DbName = (provider == DatabaseProviders.SQLServer ? "_Version" : "_version")
            };
            property.PropertyType.IsBinary = true;
            property.Fields.Add(new DatabaseField()
            {
                Name = property.DbName,
                Length = 8,
                TypeName = "timestamp"
            });
            catalog.Properties.Add(property);
        }
        private void ConfigurePropertyПометкаУдаления(Catalog catalog, DatabaseProviders provider)
        {
            MetadataProperty property = new MetadataProperty()
            {
                Name = "ПометкаУдаления",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System,
                DbName = (provider == DatabaseProviders.SQLServer ? "_Marked" : "_marked")
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
        private void ConfigurePropertyПредопределённый(Catalog catalog, DatabaseProviders provider)
        {
            MetadataProperty property = new MetadataProperty()
            {
                Name = "Предопределённый",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System,
                DbName = (provider == DatabaseProviders.SQLServer ? "_PredefinedID" : "_predefinedid")
            };
            property.PropertyType.IsUuid = true;
            property.Fields.Add(new DatabaseField()
            {
                Name = property.DbName,
                Length = 16,
                TypeName = "binary"
            });
            catalog.Properties.Add(property);
        }
        private void ConfigurePropertyКод(Catalog catalog, DatabaseProviders provider)
        {
            MetadataProperty property = new MetadataProperty()
            {
                Name = "Код",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System,
                DbName = (provider == DatabaseProviders.SQLServer ? "_Code" : "_code")
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
        private void ConfigurePropertyНаименование(Catalog catalog, DatabaseProviders provider)
        {
            MetadataProperty property = new MetadataProperty()
            {
                Name = "Наименование",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System,
                DbName = (provider == DatabaseProviders.SQLServer ? "_Description" : "_description")
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
        private void ConfigurePropertyРодитель(Catalog catalog, DatabaseProviders provider)
        {
            MetadataProperty property = new MetadataProperty()
            {
                Name = "Родитель",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System,
                DbName = (provider == DatabaseProviders.SQLServer ? "_ParentIDRRef" : "_parentidrref")
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
        private void ConfigurePropertyЭтоГруппа(Catalog catalog, DatabaseProviders provider)
        {
            MetadataProperty property = new MetadataProperty()
            {
                Name = "ЭтоГруппа",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System,
                DbName = (provider == DatabaseProviders.SQLServer ? "_Folder" : "_folder")
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
        private void ConfigurePropertyВладелец(Catalog catalog, DatabaseProviders provider, Guid owner)
        {
            MetadataProperty property = new MetadataProperty
            {
                Name = "Владелец",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System,
                DbName = (provider == DatabaseProviders.SQLServer ? "_OwnerID" : "_ownerid")
            };
            property.PropertyType.CanBeReference = true;

            if (catalog.Owners == 1) // Single type value
            {
                property.PropertyType.ReferenceTypeUuid = owner;
                property.Fields.Add(new DatabaseField()
                {
                    Name = (provider == DatabaseProviders.SQLServer ? "_OwnerIDRRef" : "_owneridrref"),
                    Length = 16,
                    TypeName = "binary"
                });
            }
            else // Multiple type value
            {
                property.Fields.Add(new DatabaseField()
                {
                    Name = (provider == DatabaseProviders.SQLServer ? "_OwnerID_TYPE" : "_ownerid_type"),
                    Length = 1,
                    TypeName = "binary",
                    Purpose = FieldPurpose.Discriminator
                });
                property.Fields.Add(new DatabaseField()
                {
                    Name = (provider == DatabaseProviders.SQLServer ? "_OwnerID_RTRef" : "_ownerid_rtref"),
                    Length = 4,
                    TypeName = "binary",
                    Purpose = FieldPurpose.TypeCode
                });
                property.Fields.Add(new DatabaseField()
                {
                    Name = (provider == DatabaseProviders.SQLServer ? "_OwnerID_RRRef" : "_ownerid_rrref"),
                    Length = 16,
                    TypeName = "binary",
                    Purpose = FieldPurpose.Object
                });
            }

            catalog.Properties.Add(property);
        }
        private void ConfigureProperty(InfoBase infoBase, MetadataObject metaObject, Guid uuid, string name, string alias, DataTypeInfo type)
        {
            if (!infoBase.Properties.TryGetValue(uuid, out MetadataProperty property)) return;

            property.Name = name;
            property.Alias = alias;
            property.PropertyType = type;
            metaObject.Properties.Add(property);

            ConfigureDatabaseFields(property);
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

        private void ConfigureSharedProperties(MetadataObject metaObject, InfoBase infoBase)
        {
            foreach (SharedProperty property in infoBase.SharedProperties.Values)
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

        private void ConfigureTableParts(MDObject tableParts, MetadataObject owner, InfoBase infoBase, DatabaseProviders provider)
        {
            int tablePartsCount = MDObjectParser.GetInt32(tableParts, new int[] { 1 }); // количество табличных частей
            if (tablePartsCount == 0) return;

            int offset = 2;
            for (int t = 0; t < tablePartsCount; t++)
            {
                // T.0.1.5.1.1.2 - uuid табличной части
                Guid uuid = new Guid(MDObjectParser.GetString(tableParts, new int[] { t + offset, 0, 1, 5, 1, 1, 2 }));
                // T.0.1.5.1.2 - имя табличной части
                string name = MDObjectParser.GetString(tableParts, new int[] { t + offset, 0, 1, 5, 1, 2 });

                if (infoBase.TableParts.TryGetValue(uuid, out MetadataObject tablePart))
                {
                    if (tablePart is TablePart)
                    {
                        tablePart.Name = name;
                        ((TablePart)tablePart).Owner = owner;
                        tablePart.TableName = owner.TableName + tablePart.TableName;
                        owner.MetadataObjects.Add(tablePart);

                        // T.2 - коллекция реквизитов табличной части (MDObject)
                        // T.2.0 = 888744e1-b616-11d4-9436-004095e12fc7 - идентификатор коллекции реквизитов табличной части
                        // T.2.1 - количество реквизитов табличной части
                        Guid collectionUuid = new Guid(MDObjectParser.GetString(tableParts, new int[] { t + offset, 2, 0 }));
                        if (collectionUuid == new Guid("888744e1-b616-11d4-9436-004095e12fc7"))
                        {
                            ConfigurePropertyСсылка(owner, tablePart, provider);
                            ConfigurePropertyКлючСтроки(tablePart, provider);
                            ConfigurePropertyНомерСтроки(tablePart, infoBase, provider);

                            MDObject properties = MDObjectParser.GetObject(tableParts, new int[] { t + offset, 2 });
                            ConfigureProperties(properties, tablePart, infoBase);
                        }
                    }
                }
            }
        }
        private void ConfigurePropertyСсылка(MetadataObject owner, MetadataObject tablePart, DatabaseProviders provider)
        {
            MetadataProperty property = new MetadataProperty()
            {
                Name = "Ссылка",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System,
                DbName = (provider == DatabaseProviders.SQLServer ? owner.TableName + "_IDRRef" : owner.TableName + "_idrref")
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
        private void ConfigurePropertyКлючСтроки(MetadataObject tablePart, DatabaseProviders provider)
        {
            MetadataProperty property = new MetadataProperty()
            {
                Name = "КлючСтроки",
                FileName = Guid.Empty,
                Purpose = PropertyPurpose.System,
                DbName = (provider == DatabaseProviders.SQLServer ? "_KeyField" : "_keyfield")
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
        private void ConfigurePropertyНомерСтроки(MetadataObject tablePart, InfoBase infoBase, DatabaseProviders provider)
        {
            if (!infoBase.Properties.TryGetValue(tablePart.FileName, out MetadataProperty property)) return;

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
        private void ConfigureProperties(MDObject properties, MetadataObject metaObject, InfoBase infoBase)
        {
            int propertiesCount = MDObjectParser.GetInt32(properties, new int[] { 1 }); // количество реквизитов
            if (propertiesCount == 0) return;

            int typeOffset = 1;
            int propertyOffset = 2;
            for (int p = 0; p < propertiesCount; p++)
            {
                // P.0.1.1.1.1.2 - property uuid
                Guid propertyUuid = new Guid(MDObjectParser.GetString(properties, new int[] { p + propertyOffset, 0, 1, 1, 1, 1, 2 }));
                // P.0.1.1.1.2 - property name
                string propertyName = MDObjectParser.GetString(properties, new int[] { p + propertyOffset, 0, 1, 1, 1, 2 });
                // P.0.1.1.1.3 - property alias descriptor
                string propertyAlias = string.Empty;
                MDObject aliasDescriptor = MDObjectParser.GetObject(properties, new int[] { p + propertyOffset, 0, 1, 1, 1, 3 });
                if (aliasDescriptor.Values.Count == 3)
                {
                    // P.0.1.1.1.3.2 - property alias
                    propertyAlias = MDObjectParser.GetString(properties, new int[] { p + propertyOffset, 0, 1, 1, 1, 3, 2 });
                }
                // P.0.1.1.2 - property types
                MDObject propertyTypes = MDObjectParser.GetObject(properties, new int[] { p + propertyOffset, 0, 1, 1, 2 });
                // P.0.1.1.2.0 = "Pattern"
                List<Guid> typeUuids = new List<Guid>();
                DataTypeInfo typeInfo = new DataTypeInfo();
                for (int t = 0; t < propertyTypes.Values.Count - 1; t++)
                {
                    // P.0.1.1.2.T - property type descriptor
                    MDObject propertyTypeInfo = MDObjectParser.GetObject(properties, new int[] { p + propertyOffset, 0, 1, 1, 2, t + typeOffset });

                    // P.0.1.1.2.T.Q - property type qualifiers
                    string[] qualifiers = new string[propertyTypeInfo.Values.Count];
                    for (int q = 0; q < propertyTypeInfo.Values.Count; q++)
                    {
                        qualifiers[q] = MDObjectParser.GetString(properties, new int[] { p + propertyOffset, 0, 1, 1, 2, t + typeOffset, q });
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

                ConfigureProperty(infoBase, metaObject, propertyUuid, propertyName, propertyAlias, typeInfo);
            }
        }
    }
}