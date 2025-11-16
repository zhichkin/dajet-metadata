namespace DaJet
{
    internal static class Configurator
    {
        #region "Конфигурирование типа данных"
        internal static void ConfigureDataTypeReferences(in MetadataRegistry registry, ref DataType type, in List<Guid> references)
        {
            bool found = false;

            for (int i = 0; i < references.Count; i++)
            {
                Guid reference = references[i];

                if (i == 0) // Единственно допустимая ссылка данного типа
                {
                    // Тип "Определяемый тип" (переопределяет входной тип данных)

                    if (registry.TryGetDefinedType(reference, out DefinedType defined))
                    {
                        type = defined.Type; return; // Описание типа берётся из определяемого типа
                    }

                    // Тип "Характеристика" (переопределяет входной тип данных)

                    if (registry.TryGetCharacteristic(reference, out Characteristic characteristic))
                    {
                        type = characteristic.Type; return; // Описание типа берётся из характеристики
                    }
                }

                // Конкретный ссылочный тип

                if (registry.TryGetReference(reference, out DatabaseObject entry))
                {
                    if (found) // Ранее минимум одна ссылка уже была найдена
                    {
                        type.IsEntity = true;
                        type.TypeCode = 0;
                        return; // Составной ссылочный тип
                    }
                    else // Пока что единственный найденный ссылочный тип
                    {
                        found = true;
                        type.IsEntity = true;
                        type.TypeCode = entry.TypeCode;
                    }
                }
                else // Общий ссылочный тип
                {
                    int typeCode; // Результат анализа возможного количества объектов метаданных

                    if (reference == ReferenceType.AnyReference)
                    {
                        typeCode = registry.GetGenericTypeCode(ReferenceType.AllReferenceTypes);
                    }
                    else
                    {
                        typeCode = registry.GetGenericTypeCode(reference);
                    }

                    if (typeCode == 0) // Составной ссылочный тип
                    {
                        type.IsEntity = true;
                        type.TypeCode = 0;
                        return;
                    }
                    else if (typeCode > 0) // Единственный ссылочный тип
                    {
                        if (found) // Ранее минимум одна ссылка уже была найдена
                        {
                            type.IsEntity = true;
                            type.TypeCode = 0;
                            return; // Составной ссылочный тип
                        }
                        else // Пока что единственный найденный ссылочный тип
                        {
                            found = true;
                            type.IsEntity = true;
                            type.TypeCode = typeCode;
                        }
                    }
                }
            }

            // Если не удалось найти хотя бы один конкретный ссылочный тип,
            // а входной тип данных не содержит ни одного простого типа,
            // тогда применяем следующее правило - ссылка составного типа

            if (!found && type.IsUndefined)
            {
                type.IsEntity = true;
                type.TypeCode = 0;
            }
        }
        #endregion

        #region "Табличная часть"
        internal static void ConfigureTablePart(in TableDefinition table, in TablePart metadata, in DatabaseObject owner)
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
        private static void ConfigurePropertyСсылка(in TableDefinition table, in DatabaseObject owner)
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
                    Type = "binary",
                    Length = 16,
                    IsPrimaryKey = true
                }
            };

            table.Properties.Add(property);
        }
        private static void ConfigurePropertyКлючСтроки(in TableDefinition table)
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
            property.Type = DataType.Integer();

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
                    Type = "binary",
                    Length = 4,
                    IsPrimaryKey = true
                }
            };

            table.Properties.Add(property);
        }
        private static void ConfigurePropertyНомерСтроки(in TableDefinition table, in TablePart metadata)
        {
            //MetadataProperty property = new()
            //{
            //    Name = "НомерСтроки",
            //    Uuid = Guid.Empty,
            //    Purpose = PropertyPurpose.System,
            //    DbName = CreateDbName(dbn.Name, dbn.Code)
            //};
            //property.PropertyType.CanBeNumeric = true;
            //property.PropertyType.NumericKind = NumericKind.AlwaysPositive;
            //property.PropertyType.NumericPrecision = 5;

            PropertyDefinition property = new()
            {
                Name = "НомерСтроки",
                Purpose = PropertyPurpose.System
            };
            //TODO: Начиная с 8.3.27, параметр precision может быть равен 9
            property.Type = DataType.Decimal(5, 0, NumericKind.AlwaysPositive);

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
                    Type = "numeric",
                    Scale = 0,
                    Precision = 5
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

        internal static void ConfigurePropertyСсылка(in TableDefinition table, int typeCode)
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
                    Type = "binary",
                    Length = 16,
                    IsPrimaryKey = true
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyВерсияДанных(in TableDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "ВерсияДанных",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Integer(8);

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_Version",
                    Type = "timestamp",
                    Length = 8,
                    IsGenerated = true
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyПометкаУдаления(in TableDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "ПометкаУдаления",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Boolean();

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_Marked",
                    Type = "binary",
                    Length = 1
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyКод(in TableDefinition table, CodeType codeType, int codeLength)
        {
            PropertyDefinition property = new()
            {
                Name = "Код",
                Purpose = PropertyPurpose.System
            };

            if (codeType == CodeType.String)
            {
                property.Type = DataType.String((ushort)codeLength, StringKind.Variable);

                property.Columns = new List<ColumnDefinition>(1)
                {
                    new ColumnDefinition()
                    {
                        Name = "_Code",
                        Type = "nvarchar",
                        Length = codeLength
                    }
                };
            }
            else
            {
                property.Type = DataType.Decimal((byte)codeLength, 0, NumericKind.AlwaysPositive);

                property.Columns = new List<ColumnDefinition>(1)
                {
                    new ColumnDefinition()
                    {
                        Name = "_Code",
                        Type = "numeric",
                        Scale = 0,
                        Precision = codeLength
                    }
                };
            }

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyНаименование(in TableDefinition table, int nameLength)
        {
            PropertyDefinition property = new()
            {
                Name = "Наименование",
                Purpose = PropertyPurpose.System
            };
            //NOTE: длина наименования ограничена 150 символами
            property.Type = DataType.String((ushort)nameLength, StringKind.Variable);

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_Description",
                    Type = "nvarchar",
                    Length = nameLength
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyРодитель(in TableDefinition table, int typeCode)
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
                    Type = "binary",
                    Length = 16
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyЭтоГруппа(in TableDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "ЭтоГруппа",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Boolean();

            property.Columns = new List<ColumnDefinition>()
            {
                new ColumnDefinition()
                {
                    Name = "_Folder",
                    Type = "binary", // инвертировать !
                    Length = 1
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyВладелец(in TableDefinition table, in Guid[] owners, int ownerCode)
        {
            PropertyDefinition property = new()
            {
                Name = "Владелец",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Entity();

            //if (options.ResolveReferences && owners is not null && owners.Count > 0)
            //{
            //    property.References.AddRange(owners);
            //}

            if (owners.Length == 1) // Single type value
            {
                property.Type.TypeCode = ownerCode;

                property.Columns = new List<ColumnDefinition>(1)
                {
                    new ColumnDefinition()
                    {
                        Name = "_OwnerIDRRef",
                        Type = "binary",
                        Length = 16
                    }
                };
            }
            else // Multiple type value
            {
                property.Type.TypeCode = 0;

                property.Columns = new List<ColumnDefinition>(3)
                {
                    new ColumnDefinition()
                    {
                        Name = "_OwnerID_TYPE",
                        Type = "binary",
                        Length = 1,
                        Purpose = ColumnPurpose.Tag
                    },
                    new ColumnDefinition()
                    {
                        Name = "_OwnerID_RTRef",
                        Type = "binary",
                        Length = 4,
                        Purpose = ColumnPurpose.TypeCode
                    },
                    new ColumnDefinition()
                    {
                        Name = "_OwnerID_RRRef",
                        Type = "binary",
                        Length = 16,
                        Purpose = ColumnPurpose.Identity
                    }
                };
            }

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyПредопределённый(in TableDefinition table, bool isPublication, int compatibilityVersion)
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
        internal static void ConfigurePropertyIsMetadata(in TableDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "Предопределенный",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Boolean();

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_IsMetadata",
                    Type = "binary",
                    Length = 1
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyPredefinedID(in TableDefinition table)
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
                    Type = "binary",
                    Length = 16
                }
            };

            table.Properties.Add(property);
        }
        #endregion
    }
}