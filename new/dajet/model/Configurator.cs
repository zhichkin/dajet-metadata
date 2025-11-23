namespace DaJet
{
    internal static class Configurator
    {
        #region "Общие реквизиты"
        internal static void ConfigureSharedProperties(in MetadataRegistry registry, in DatabaseObject entry, in EntityDefinition target)
        {
            if (entry is Enumeration)
            {
                return;
            }

            //if (metadata is not ApplicationObject target)
            //{
            //    return; ???
            //}

            // Актуально для версии 8.3.27 и ниже:
            // 1. Общие реквизиты могут быть добавлены только в основной конфигурации.
            // 2. Расширения могут ТОЛЬКО заимствовать общие реквизиты из основной конфигурации.
            // 3. Использование общих реквизитов для собственных объектов расширений должно быть указано ЯВНО, так как
            //    собственные объекты расширения не имеют настройки "Авто", ТОЛЬКО "Использовать" или "Не использовать".

            //OneDbMetadataProvider provider = cache;

            //if (provider.Extension is null) // Основная конфигурация
            //{
            //    if (provider.TryGetExtendedInfo(target.Uuid, out MetadataItemEx extent))
            //    {
            //        if (extent.IsExtensionOwnObject) // Cобственный объект расширения
            //        {
            //            // Необходимо использовать провайдера метаданных соответствующего расширения
            //            if (provider.Extensions.TryGetValue(extent.Extension, out OneDbMetadataProvider extension))
            //            {
            //                provider = extension;
            //            }
            //            else
            //            {
            //                return; // Расширение основной конфигурации не загружено
            //            }
            //        }
            //    }
            //}
            //else // Раширение конфигурации
            //{
            //    // Конфигурируемый ApplicationObject должен быть из этого же расширения.
            //}

            bool IsMainConfig = true;
            //bool IsMainConfig = (provider.Extension is null); // Это основная конфигурация ?

            foreach (SharedProperty property in registry.GetMetadataObjects<SharedProperty>())
            {
                if (property.UsageSettings.TryGetValue(entry.Uuid, out SharedPropertyUsage usage))
                {
                    if (usage == SharedPropertyUsage.Use)
                    {
                        if (IsMainConfig) // Основная конфигурация
                        {
                            //PropertyDefinition definition = new()
                            //{
                            //    Name = property.Name,
                            //    Type = property.Type,
                            //    Purpose = PropertyPurpose.Property
                            //};

                            //ConfigureDatabaseColumns(in property, in definition);

                            target.Properties.Add(property.Definition);

                            ConfigureSharedPropertyForTableParts(in property, in entry, in target);
                        }
                        //else // Расширение конфигурации
                        //{
                        //    OneDbMetadataProvider main = provider.Extension.Host;

                        //    MetadataObject parent = null;

                        //    if (property.Parent != Guid.Empty) // Найти общий реквизит основной конфигурации по uuid
                        //    {
                        //        parent = main.GetMetadataObject(MetadataType.SharedProperty, property.Parent);
                        //    }

                        //    if (parent is null) // Найти общий реквизит основной конфигурации по имени
                        //    {
                        //        string metadataType = MetadataType.ResolveNameRu(MetadataType.SharedProperty);
                        //        parent = main.GetMetadataObject($"{metadataType}.{property.Name}");
                        //    }

                        //    if (parent is SharedProperty shared)
                        //    {
                        //        target.Properties.Add(shared);
                        //        ConfigureSharedPropertiesForTableParts(target, shared);
                        //    }
                        //}
                    }
                }
                else // SharedPropertyUsage.Auto
                {
                    if (IsMainConfig) // Основная конфигурация
                    {
                        if (property.AutomaticUsage == AutomaticUsage.Use)
                        {
                            //PropertyDefinition definition = new()
                            //{
                            //    Name = property.Name,
                            //    Type = property.Type,
                            //    Purpose = PropertyPurpose.Property
                            //};

                            //ConfigureDatabaseColumns(in property, in definition);

                            target.Properties.Add(property.Definition);

                            ConfigureSharedPropertyForTableParts(in property, in entry, in target);
                        }
                    }
                    else // Расширение конфигурации
                    {
                        // Ничего не делаем: смотри пункт 3 примечаний выше.
                    }
                }
            }
        }
        internal static void ConfigureSharedPropertyForTableParts(in SharedProperty property, in DatabaseObject target, in EntityDefinition owner)
        {
            if (target is Publication)
            {
                return;
            }

            if (property.DataSeparationUsage != DataSeparationUsage.Use)
            {
                return;
            }

            if (property.DataSeparationMode != DataSeparationMode.Independent)
            {
                return;
            }

            foreach (EntityDefinition child in owner.Entities)
            {
                //PropertyDefinition definition = new()
                //{
                //    Name = property.Name,
                //    Type = property.Type,
                //    Purpose = PropertyPurpose.Property
                //};

                //ConfigureDatabaseColumns(in property, in definition);

                child.Properties.Add(property.Definition);
            }
        }
        #endregion

        #region "Табличная часть"
        internal static void ConfigureTablePart(in EntityDefinition table, in TablePart metadata, in DatabaseObject owner)
        {
            //foreach (TablePart tablePart in aggregate.TableParts)
            //{
            //    if (cache.Extension != null && string.IsNullOrEmpty(tablePart.TableName))
            //    {
            //        //NOTE (!) Заимствованные из основной конфигурации табличные части в расширениях
            //        //не имеют системных свойств (они их наследуют), если только они их не переопределяют.
            //        continue;
            //    }

            //    if (tablePart.Uuid == Guid.Empty)
            //    {
            //        //NOTE: Системная табличная часть, например, "ВидыСубконто" плана счетов
            //        continue;
            //    }

            //    ConfigurePropertyСсылка(in cache, in owner, in tablePart);
            //    ConfigurePropertyКлючСтроки(in tablePart);
            //    ConfigurePropertyНомерСтроки(in cache, in tablePart);
            //}

            ConfigurePropertyСсылка(in table, in owner);
            ConfigurePropertyКлючСтроки(in table);
            ConfigurePropertyНомерСтроки(in table, in metadata);
        }
        private static void ConfigurePropertyСсылка(in EntityDefinition table, in DatabaseObject owner)
        {
            //MetadataProperty property = new()
            //{
            //    Name = "Ссылка",
            //    Uuid = Guid.Empty,
            //    Purpose = PropertyPurpose.System,
            //    DbName = owner.GetMainDbName() + "_IDRRef"
            //};

            //property.PropertyType.CanBeReference = true;
            //property.PropertyType.TypeCode = owner.TypeCode;
            //property.PropertyType.Reference = owner.Uuid;

            PropertyDefinition property = new()
            {
                Name = "Ссылка",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Entity(owner.TypeCode);

            //if (cache.ResolveReferences)
            //{
            //    property.References.Add(owner.Uuid);
            //}

            // Собственная табличная часть расширения, но добавленная к заимствованному объекту основной конфигурации:
            // в таком случае у заимствованного объекта значения TypeCode и Uuid надо искать в основной конфигурации.
            // Однако, в данный момент мы находимся в контексте расширения и контекст основной конфигруации недоступен!
            //if (owner.Parent != Guid.Empty)
            //{
            // TODO: нужно реализовать алгоритм разрешения ссылок на заимствованные объекты
            // в процедуре применения расширения к основной конфиграции!

            //MetadataObject parent = cache.GetMetadataObject(owner.Parent);
            //}

            //TODO: property.PropertyType.References.Add(new MetadataItem(MetadataTypes.Catalog, owner.Uuid, owner.Name));

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = string.Format("{0}_{1}", owner.GetMainDbName(), MetadataToken.IDRRef),
                    Type = DataType.Binary(16, false),
                    IsPrimaryKey = true
                }
            };

            table.Properties.Add(property);
        }
        private static void ConfigurePropertyКлючСтроки(in EntityDefinition table)
        {
            //MetadataProperty property = new()
            //{
            //    Name = "KeyField",    // Исправлено на латиницу из-за того, что в некоторых конфигурациях 1С
            //    Alias = "КлючСтроки", // для реквизитов табличной части иногда используют имя "КлючСтроки".
            //    Uuid = Guid.Empty,    // Это не запрещено 1С в отличие от имён реквизитов "Ссылка" и "НомерСтроки".
            //    Purpose = PropertyPurpose.System,
            //    DbName = "_KeyField"
            //};
            //property.PropertyType.IsBinary = true;

            PropertyDefinition property = new()
            {
                Name = "KeyField",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Binary(4, false);

            //property.Columns.Add(new MetadataColumn()
            //{
            //    Name = "_KeyField",
            //    Length = 4,
            //    TypeName = "binary",
            //    KeyOrdinal = 2,
            //    IsPrimaryKey = true
            //});

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_KeyField",
                    Type = property.Type,
                    IsPrimaryKey = true
                }
            };

            table.Properties.Add(property);
        }
        private static void ConfigurePropertyНомерСтроки(in EntityDefinition table, in TablePart metadata)
        {
            //MetadataProperty property = new()
            //{
            //    Name = "НомерСтроки",
            //    Uuid = Guid.Empty,
            //    Purpose = PropertyPurpose.System,
            //    DbName = CreateDbName(dbn.Name, dbn.Code)
            //};
            //property.PropertyType.CanBeNumeric = true;
            //property.PropertyType.NumericKind = NumericKind.UnSigned;
            //property.PropertyType.NumericPrecision = 5;

            PropertyDefinition property = new()
            {
                Name = "НомерСтроки",
                Purpose = PropertyPurpose.System
            };
            //TODO: Начиная с 8.3.27, параметр precision может быть равен 9
            property.Type = DataType.Decimal(5, 0);

            //property.Columns.Add(new MetadataColumn()
            //{
            //    Name = property.DbName,
            //    Length = 5,
            //    Precision = 5,
            //    TypeName = "numeric"
            //});

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = metadata.GetColumnNameНомерСтроки(),
                    Type = property.Type
                }
            };

            table.Properties.Add(property);
        }
        #endregion

        #region "Справочник"

        // Последовательность сериализации системных свойств в формат 1С JDTO
        // 1. ЭтоГруппа        = IsFolder           - bool (invert)
        // 2. Ссылка           = Ref                - uuid 
        // 3. ПометкаУдаления  = DeletionMark       - bool
        // 4. Владелец         = Owner              - { #type + #value }
        // 5. Родитель         = Parent             - uuid
        // 6. Код              = Code               - string | number
        // 7. Наименование     = Description        - string
        // 8. Предопределенный = PredefinedDataName - string

        internal static void ConfigurePropertyСсылка(in EntityDefinition table, int typeCode)
        {
            PropertyDefinition property = new()
            {
                Name = "Ссылка",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Entity(typeCode);

            //if (options.ResolveReferences)
            //{
            //    property.References.Add(metadata.Uuid);
            //}

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_IDRRef",
                    Type = DataType.Binary(16, false),
                    IsPrimaryKey = true
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyВерсияДанных(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "ВерсияДанных",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Binary(8, false);

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_Version",
                    Type = property.Type, //TODO: ms = rowversion | pg = integer
                    IsGenerated = true
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyПометкаУдаления(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "ПометкаУдаления",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Boolean;

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_Marked",
                    Type = DataType.Binary(1, false)
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyКод(in EntityDefinition table, CodeType codeType, int codeLength)
        {
            PropertyDefinition property = new()
            {
                Name = "Код",
                Purpose = PropertyPurpose.System
            };

            if (codeType == CodeType.String)
            {
                property.Type = DataType.String((ushort)codeLength);
            }
            else
            {
                property.Type = DataType.Decimal((byte)codeLength, 0);
            }

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_Code",
                    Type = property.Type
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyНаименование(in EntityDefinition table, int nameLength)
        {
            PropertyDefinition property = new()
            {
                Name = "Наименование",
                Purpose = PropertyPurpose.System
            };
            //NOTE: длина наименования ограничена 150 символами
            property.Type = DataType.String((ushort)nameLength);

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_Description",
                    Type = property.Type
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyРодитель(in EntityDefinition table, int typeCode)
        {
            // This hierarchy property always has the single reference type (adjacency list)

            PropertyDefinition property = new()
            {
                Name = "Родитель",
                Purpose = PropertyPurpose.System
            };

            property.Type = DataType.Entity(typeCode);

            //if (options.ResolveReferences)
            //{
            //    property.References.Add(metadata.Uuid);
            //}

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_ParentIDRRef",
                    Type = DataType.Binary(16, false)
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyЭтоГруппа(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "ЭтоГруппа",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Boolean;

            property.Columns = new List<ColumnDefinition>()
            {
                new ColumnDefinition()
                {
                    Name = "_Folder",
                    Type = DataType.Binary(1, false) // инвертировать: в БД ЭтоГруппа = 0x00, а элемент = 0x01
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyВладелец(in EntityDefinition table, in Guid[] owners, int ownerCode)
        {
            PropertyDefinition property = new()
            {
                Name = "Владелец",
                Purpose = PropertyPurpose.System
            };
            
            //if (options.ResolveReferences && owners is not null && owners.Count > 0)
            //{
            //    property.References.AddRange(owners);
            //}

            if (owners.Length == 1) // Single type value
            {
                property.Type = DataType.Entity(ownerCode);

                property.Columns = new List<ColumnDefinition>(1)
                {
                    new ColumnDefinition()
                    {
                        Name = "_OwnerIDRRef",
                        Type = DataType.Binary(16, false)
                    }
                };
            }
            else // Multiple type value
            {
				property.Type = DataType.Entity();

				property.Columns = new List<ColumnDefinition>(3)
                {
                    new ColumnDefinition()
                    {
                        Name = "_OwnerID_TYPE",
                        Type = DataType.Binary(1, false),
                        Purpose = ColumnPurpose.Tag
                    },
                    new ColumnDefinition()
                    {
                        Name = "_OwnerID_RTRef",
                        Type = DataType.Binary(4, false),
                        Purpose = ColumnPurpose.TypeCode
                    },
                    new ColumnDefinition()
                    {
                        Name = "_OwnerID_RRRef",
                        Type = DataType.Binary(16, false),
                        Purpose = ColumnPurpose.Identity
                    }
                };
            }

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyПредопределённый(in EntityDefinition table, bool isPublication, int compatibilityVersion)
        {
            if (compatibilityVersion >= 80303)
            {
                ConfigurePropertyPredefinedID(in table);
            }
            else if (!isPublication)
            {
                ConfigurePropertyIsMetadata(in table);
            }
            else if (compatibilityVersion >= 80216)
            {
                ConfigurePropertyPredefinedID(in table);
            }
        }
        internal static void ConfigurePropertyIsMetadata(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "Предопределенный",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Boolean;

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_IsMetadata",
                    Type = DataType.Binary(1, false)
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyPredefinedID(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "Предопределенный",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Uuid();

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_PredefinedID",
                    Type = DataType.Binary(16, false)
                }
            };

            table.Properties.Add(property);
        }
        #endregion

        #region "Колонки таблиц базы данных"
        internal static void ConfigureDatabaseColumns(in Property metadata, in PropertyDefinition property)
        {
            string databaseName = metadata.GetMainDbName();
            ConfigureDatabaseColumns(in property, in databaseName);
        }
        internal static void ConfigureDatabaseColumns(in SharedProperty metadata, in PropertyDefinition property)
        {
            string databaseName = metadata.GetMainDbName();
            ConfigureDatabaseColumns(in property, in databaseName);
        }
        internal static void ConfigureDatabaseColumns(in PropertyDefinition property, in string databaseName)
        {
            if (property.Type.IsUnion)
            {
                ConfigureDatabaseColumnsForUnionType(in property, in databaseName);
            }
            else
            {
                ConfigureDatabaseColumnsForSimpleType(in property, in databaseName);
            }
        }
        private static void ConfigureDatabaseColumnsForUnionType(in PropertyDefinition property, in string databaseName)
        {
            property.Columns.Add(new ColumnDefinition()
            {
                Name = string.Format("{0}_{1}", databaseName, MetadataToken.TYPE),
                Type = DataType.Binary(1, false),
                Purpose = ColumnPurpose.Tag
            });

            if (property.Type.IsBoolean)
            {
                property.Columns.Add(new ColumnDefinition()
                {
                    Name = string.Format("{0}_{1}", databaseName, MetadataToken.L),
                    Type = DataType.Binary(1, false),
                    Purpose = ColumnPurpose.Boolean
                });
            }

            if (property.Type.IsDecimal)
            {
                property.Columns.Add(new ColumnDefinition()
                {
                    Name = string.Format("{0}_{1}", databaseName, MetadataToken.N),
                    Type = DataType.Decimal(property.Type.Precision, property.Type.Scale), // numeric
                    Purpose = ColumnPurpose.Numeric
                });
            }

            if (property.Type.IsDateTime)
            {
                property.Columns.Add(new ColumnDefinition()
                {
                    Name = string.Format("{0}_{1}", databaseName, MetadataToken.T),
                    Type = DataType.DateTime,
                    Purpose = ColumnPurpose.DateTime
                });
            }

            if (property.Type.IsString)
            {
                property.Columns.Add(new ColumnDefinition()
                {
                    Name = string.Format("{0}_{1}", databaseName, MetadataToken.S),
                    Type = DataType.String(property.Type.Size),
                    Purpose = ColumnPurpose.String
                });
            }

            if (property.Type.IsEntity)
            {
                if (property.Type.TypeCode == 0) // Составной тип ссылки
                {
                    property.Columns.Add(new ColumnDefinition() // Код типа ссылки
                    {
                        Name = string.Format("{0}_{1}", databaseName, MetadataToken.RTRef),
                        Type = DataType.Binary(4, false),
                        Purpose = ColumnPurpose.TypeCode
                    });
                }

                property.Columns.Add(new ColumnDefinition() // Значение ссылки
                {
                    Name = string.Format("{0}_{1}", databaseName, MetadataToken.RRRef),
                    Type = DataType.Binary(16, false),
                    Purpose = ColumnPurpose.Identity
                });
            }
        }
        private static void ConfigureDatabaseColumnsForSimpleType(in PropertyDefinition property, in string databaseName)
        {
            if (property.Type.IsUuid)
            {
                property.Columns.Add(new ColumnDefinition()
                {
                    Name = databaseName, // ms = binary(16)
                    Type = DataType.Binary(16, false) // pg = bytea
                });
            }
            else if (property.Type.IsBinary)
            {
                property.Columns.Add(new ColumnDefinition()
                {
                    Name = databaseName, // ms = varbinary(max)
                    Type = property.Type // pg = bytea
                });
            }
            else if (property.Type.IsString)
            {
                property.Columns.Add(new ColumnDefinition()
                {
                    Name = databaseName, // ms = nchar | nvarchar
                    Type = property.Type // pg = mchar | mvarchar
                });
            }
            else if (property.Type.IsDecimal)
            {
                property.Columns.Add(new ColumnDefinition()
                {
                    Name = databaseName, // ms = numeric
                    Type = property.Type // pg = numeric
                });
            }
            else if (property.Type.IsBoolean)
            {
                property.Columns.Add(new ColumnDefinition()
                {
                    Name = databaseName, // ms = binary(1)
                    Type = DataType.Binary(1, false) // pg = boolean
                });
            }
            else if (property.Type.IsDateTime)
            {
                property.Columns.Add(new ColumnDefinition()
                {
                    Name = databaseName, // ms = datetime2
                    Type = DataType.DateTime // pg = "timestamp without time zone"
                });
            }
            else if (property.Type.IsEntity)
            {
                property.Columns.Add(new ColumnDefinition()
                {
                    Name = string.Format("{0}{1}", databaseName, MetadataToken.RRef), // ms = binary(16)
                    Type = DataType.Binary(16, false) // pg = bytea
                });
            }
        }
        #endregion
    }
}