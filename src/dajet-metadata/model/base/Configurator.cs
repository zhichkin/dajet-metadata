using DaJet.TypeSystem;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace DaJet.Metadata
{
    internal static class Configurator
    {
        #region "Фабричные методы объектов метаданных"
        private static readonly FrozenSet<string> SupportedTokens = FrozenSet.ToFrozenSet(
        [
            MetadataToken.VT,
            MetadataToken.LineNo,
            MetadataToken.Fld,
            MetadataToken.Enum,
            MetadataToken.Chrc,
            MetadataToken.Node,
            MetadataToken.Const,
            MetadataToken.Document,
            MetadataToken.Reference,
            MetadataToken.BPr,
            MetadataToken.Task,
            MetadataToken.BPrPoints,
            MetadataToken.InfoRg,
            MetadataToken.InfoRgOpt,
            MetadataToken.InfoRgSF,
            MetadataToken.InfoRgSL,
            MetadataToken.AccumRg,
            MetadataToken.AccumRgT,
            MetadataToken.AccumRgOpt,
            MetadataToken.Acc,
            MetadataToken.AccRg,
            MetadataToken.ExtDim,
            MetadataToken.AccRgED,
            MetadataToken.AccChngR,
            MetadataToken.AccRgChngR,
            MetadataToken.AccumRgChngR,
            MetadataToken.BPrChngR,
            MetadataToken.TaskChngR,
            MetadataToken.ReferenceChngR,
            MetadataToken.ChrcChngR,
            MetadataToken.ConstChngR,
            MetadataToken.DocumentChngR,
            MetadataToken.InfoRgChngR
        ], StringComparer.Ordinal);
        private static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> SupportedTokensLookup = SupportedTokens.GetAlternateLookup<ReadOnlySpan<char>>();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsSupportedToken(ReadOnlySpan<char> token, out string name)
        {
            return SupportedTokensLookup.TryGetValue(token, out name);
        }

        private static readonly FrozenSet<string> ReferenceTypeTokens = FrozenSet.ToFrozenSet(
        [
            MetadataToken.Acc,
            MetadataToken.Enum,
            MetadataToken.Chrc,
            MetadataToken.Node,
            MetadataToken.BPr,
            MetadataToken.Task,
            MetadataToken.Document,
            MetadataToken.Reference
        ], StringComparer.Ordinal);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsReferenceTypeToken(in string token)
        {
            return ReferenceTypeTokens.Contains(token);
        }

        private static readonly FrozenDictionary<string, Func<Guid, MetadataObject>> MainEntryTokens = CreateMainEntryTokenLookup();
        private static FrozenDictionary<string, Func<Guid, MetadataObject>> CreateMainEntryTokenLookup()
        {
            List<KeyValuePair<string, Func<Guid, MetadataObject>>> list =
            [
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.VT, TablePart.Create),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.Fld, Property.Create),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.Acc, Account.Create),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.Enum, Enumeration.Create),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.Chrc, Characteristic.Create),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.Node, Publication.Create),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.BPr, BusinessProcess.Create),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.Task, BusinessTask.Create),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.Const, Constant.Create),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.Document, Document.Create),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.Reference, Catalog.Create),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.AccRg, AccountingRegister.Create),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.InfoRg, InformationRegister.Create),
                new KeyValuePair<string, Func<Guid, MetadataObject>>(MetadataToken.AccumRg, AccumulationRegister.Create)
            ];
            return FrozenDictionary.ToFrozenDictionary(list, StringComparer.Ordinal);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryGetMetadataObjectFactory(in string token, out Func<Guid, MetadataObject> factory)
        {
            return MainEntryTokens.TryGetValue(token, out factory);
        }

        private static readonly FrozenDictionary<Guid, Func<Guid, MetadataObject>> MainEntryFactories = CreateMainEntryFactoryLookup();
        private static FrozenDictionary<Guid, Func<Guid, MetadataObject>> CreateMainEntryFactoryLookup()
        {
            List<KeyValuePair<Guid, Func<Guid, MetadataObject>>> list =
            [
                new KeyValuePair<Guid, Func<Guid, MetadataObject>>(MetadataTypes.DefinedType, DefinedType.Create),
                new KeyValuePair<Guid, Func<Guid, MetadataObject>>(MetadataTypes.SharedProperty, SharedProperty.Create),
                new KeyValuePair<Guid, Func<Guid, MetadataObject>>(MetadataTypes.Account, Account.Create),
                new KeyValuePair<Guid, Func<Guid, MetadataObject>>(MetadataTypes.Enumeration, Enumeration.Create),
                new KeyValuePair<Guid, Func<Guid, MetadataObject>>(MetadataTypes.Characteristic, Characteristic.Create),
                new KeyValuePair<Guid, Func<Guid, MetadataObject>>(MetadataTypes.Publication, Publication.Create),
                new KeyValuePair<Guid, Func<Guid, MetadataObject>>(MetadataTypes.BusinessProcess, BusinessProcess.Create),
                new KeyValuePair<Guid, Func<Guid, MetadataObject>>(MetadataTypes.BusinessTask, BusinessTask.Create),
                new KeyValuePair<Guid, Func<Guid, MetadataObject>>(MetadataTypes.Constant, Constant.Create),
                new KeyValuePair<Guid, Func<Guid, MetadataObject>>(MetadataTypes.Document, Document.Create),
                new KeyValuePair<Guid, Func<Guid, MetadataObject>>(MetadataTypes.Catalog, Catalog.Create),
                new KeyValuePair<Guid, Func<Guid, MetadataObject>>(MetadataTypes.AccountingRegister, AccountingRegister.Create),
                new KeyValuePair<Guid, Func<Guid, MetadataObject>>(MetadataTypes.InformationRegister, InformationRegister.Create),
                new KeyValuePair<Guid, Func<Guid, MetadataObject>>(MetadataTypes.AccumulationRegister, AccumulationRegister.Create)
            ];
            return FrozenDictionary.ToFrozenDictionary(list);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryGetMetadataObjectFactory(Guid type, out Func<Guid, MetadataObject> factory)
        {
            return MainEntryFactories.TryGetValue(type, out factory);
        }
        #endregion

        #region "Общие реквизиты"
        internal static void ConfigureSharedProperties(in MetadataRegistry registry, in MetadataObject entry, in EntityDefinition target)
        {
            // Актуально для версии 8.3.27 и ниже:
            // 1. Общие реквизиты могут быть добавлены только в основной конфигурации.
            // 2. Расширения могут ТОЛЬКО заимствовать общие реквизиты из основной конфигурации.
            // 3. Использование общих реквизитов для собственных объектов расширений должно быть указано ЯВНО, так как
            //    собственные объекты расширения не имеют настройки "Авто", ТОЛЬКО "Использовать" или "Не использовать".
            // 4. Необходимость добавления общего реквизита для собственных объектов расширений определяется 
            //    настройками заимствованного общего реквизита из той же самой конфигурации,
            //    однако сами настройки берутся из общего ренквизита основной конфигурации.

            if (entry.IsBorrowed)
            {
                return; //NOTE: Сюда попадать не должны: проверка на всякий случай.
            }

            Configuration configuration = registry.Configurations[entry.Cfid];

            if (configuration.Metadata.TryGetValue(MetadataTypes.SharedProperty, out Guid[] items))
            {
                foreach (Guid item in items)
                {
                    if (registry.TryGetEntry(item, out SharedProperty property))
                    {
                        if (property.UsageSettings.TryGetValue(entry.Uuid, out SharedPropertyUsage usage))
                        {
                            if (usage == SharedPropertyUsage.Use)
                            {
                                if (entry.IsMain) // Основная конфигурация
                                {
                                    target.Properties.Add(property.Definition);
                                    ConfigureSharedPropertyForTableParts(in property, in entry, in target);
                                }
                                else // Расширение конфигурации
                                {
                                    // Получаем общий реквизит основной конфигурации
                                    if (registry.TryGetEntry(MetadataNames.SharedProperty, property.Name, out property))
                                    {
                                        target.Properties.Add(property.Definition);
                                        ConfigureSharedPropertyForTableParts(in property, in entry, in target);
                                    }
                                }
                            }
                        }
                        else // SharedPropertyUsage.Auto
                        {
                            if (entry.IsMain) // Основная конфигурация
                            {
                                if (property.AutomaticUsage == AutomaticUsage.Use)
                                {
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
            }
        }
        internal static void ConfigureSharedPropertyForTableParts(in SharedProperty property, in MetadataObject target, in EntityDefinition owner)
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

            foreach (EntityDefinition table in owner.Entities)
            {
                table.Properties.Add(property.Definition);
            }
        }
        #endregion

        #region "Константа"
        internal static void ConfigurePropertyRecordKey(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "RecordKey",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Binary(1, false);

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_RecordKey",
                    Type = property.Type,
                    IsPrimaryKey = true
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyЗначение(in EntityDefinition table, DataType propertyType, in string columnName)
        {
            PropertyDefinition property = new()
            {
                Name = "Значение",
                Purpose = PropertyPurpose.System
            };
            property.Type = propertyType;

            ConfigureDatabaseColumns(in property, in columnName);

            table.Properties.Add(property);

            //if (cache.ResolveReferences && constant.References is not null && constant.References.Count > 0)
            //{
            //    property.References.AddRange(constant.References);
            //}
        }
        ///<summary>
        ///Идентификатор объекта метаданных "Константа", значение которой было изменено
        ///</summary>
        internal static void ConfigurePropertyConstID(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "ConstID",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Binary(16, false);

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_ConstID",
                    Type = DataType.Binary(16, false)
                }
            };

            table.Properties.Add(property);
        }
        #endregion

        #region "Перечисление"
        internal static void ConfigurePropertyПорядок(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "Порядок",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Decimal(10, 0);

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_EnumOrder",
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

        #region "Документ"

        // Последовательность сериализации системных свойств в формат 1С JDTO
        // 1. Ссылка          = Ref          - uuid
        // 2. ПометкаУдаления = DeletionMark - bool
        // 3. Дата            = Date         - DateTime
        // 4. Номер           = Number       - string | number
        // 5. Проведён        = Posted       - bool

        internal static void ConfigurePropertyДата(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "Дата",
                Purpose = PropertyPurpose.System
            };

            property.Type = DataType.DateTime;

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_Date_Time",
                    Type = property.Type
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyПериодНомера(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "ПериодНомера",
                Purpose = PropertyPurpose.System
            };

            property.Type = DataType.Date;

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_NumberPrefix",
                    Type = DataType.DateTime
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyНомер(in EntityDefinition table, NumberType numberType, int numberLength)
        {
            PropertyDefinition property = new()
            {
                Name = "Номер",
                Purpose = PropertyPurpose.System
            };

            if (numberType == NumberType.Number)
            {
                property.Type = DataType.Decimal((byte)numberLength);
            }
            else
            {
                property.Type = DataType.String((ushort)numberLength);
            }

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_Number",
                    Type = property.Type
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyПроведён(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "Проведен",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Boolean;

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_Posted",
                    Type = DataType.Binary(1, false)
                }
            };

            table.Properties.Add(property);
        }
        #endregion

        #region "План видов характеристик"
        internal static void ConfigurePropertyТипЗначения(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "ТипЗначения",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Binary();

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_Type",
                    Type = property.Type //TODO: IsNullable = true
                }
            };

            table.Properties.Add(property);
        }
        #endregion

        #region "Регистр сведений"
        // Последовательность сериализации системных свойств в формат 1С JDTO
        // 1. "Регистратор" = Recorder   - uuid { #type + #value }
        // 2. "Период"      = Period     - DateTime
        // 3. "ВидДвижения" = RecordType - string { "Receipt", "Expense" }
        // 4. "Активность"  = Active     - bool
        // 5. _SimpleKey    = binary(16) - uuid (УникальныйИдентификатор)
        //    - версия платформы 8.3.2 и ниже
        //    - только непериодические регистры сведений
        //    - регистр имеет больше одного измерения

        internal static void ConfigurePropertyПериод(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "Период",
                Purpose = PropertyPurpose.System
            };

            property.Type = DataType.DateTime;

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_Period",
                    Type = property.Type
                }
            };

            table.Properties.Add(property);

            //if (register is InformationRegister inforeg)
            //{
            //    if (inforeg.Periodicity == RegisterPeriodicity.Second)
            //    {
            //        property.PropertyType.DateTimePart = DateTimePart.DateTime;
            //    }
            //}
        }
        internal static void ConfigurePropertyНомерЗаписи(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "НомерСтроки",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Decimal(9, 0);

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_LineNo",
                    Type = property.Type
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyАктивность(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "Активность",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Boolean;

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_Active",
                    Type = DataType.Binary(1, false)
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyРегистратор(in EntityDefinition table, Guid uuid, in MetadataRegistry registry)
        {
            if (!registry.TryGetRegisterRecorders(uuid, out List<Guid> recorders))
            {
                return;
            }

            if (recorders is null || recorders.Count == 0) { return; }

            PropertyDefinition property = new()
            {
                Name = "Регистратор",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Entity(); // Предварительно множественная ссылка

            //if (options.ResolveReferences)
            //{
            //    property.References.Add(metadata.Uuid);
            //}

            //if (cache.ResolveReferences && recorders is not null || recorders.Count > 0)
            //{
            //    property.References.AddRange(recorders);
            //}

            if (recorders.Count == 1) // Single type value
            {
                if (registry.TryGetEntry(recorders[0], out MetadataObject entry))
                {
                    property.Type = DataType.Entity(entry.Code);
                }

                property.Columns = new List<ColumnDefinition>(1)
                {
                    new ColumnDefinition()
                    {
                        Name = "_RecorderRRef",
                        Type = DataType.Binary(16, false),
                        IsPrimaryKey = true
                    }
                };
            }
            else // Multiple type value
            {
                property.Columns = new List<ColumnDefinition>(2)
                {
                    new ColumnDefinition()
                    {
                        Name = "_RecorderTRef",
                        Type = DataType.Binary(4, false),
                        Purpose = ColumnPurpose.TypeCode,
                        IsPrimaryKey = true
                    },
                    new ColumnDefinition()
                    {
                        Name = "_RecorderRRef",
                        Type = DataType.Binary(16, false),
                        Purpose = ColumnPurpose.Identity,
                        IsPrimaryKey = true
                    }
                };
            }

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertySimpleKey(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "SimpleKey",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Uuid();

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_SimpleKey",
                    Type = DataType.Binary(16, false)
                }
            };

            table.Properties.Add(property);
        }
        #endregion

        #region "Регистр накопления"
        internal static void ConfigurePropertyВидДвиженияНакопления(in EntityDefinition table)
        {
            // Приход = Receipt = 0 
            // Расход = Expense = 1 

            PropertyDefinition property = new()
            {
                Name = "ВидДвижения",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Decimal(1, 0);

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_RecordKind",
                    Type = property.Type
                }
            };

            table.Properties.Add(property);
        }
        #endregion

        #region "Задача"
        internal static void ConfigurePropertyБизнесПроцесс(in EntityDefinition table, Guid uuid, in MetadataRegistry registry)
        {
            if (!registry.TryGetBusinessProcesses(uuid, out List<Guid> processes))
            {
                return;
            }

            if (processes is null || processes.Count == 0) { return; }

            PropertyDefinition property = new()
            {
                Name = "БизнесПроцесс",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Entity(); // Предварительно множественная ссылка

            //if (cache.ResolveReferences && processes is not null && processes.Count > 0)
            //{
            //    property.References.AddRange(processes);
            //}

            if (processes.Count == 1) // Single type value
            {
                if (registry.TryGetEntry(processes[0], out MetadataObject entry))
                {
                    property.Type = DataType.Entity(entry.Code);
                }

                property.Columns = new List<ColumnDefinition>(1)
                {
                    new ColumnDefinition()
                    {
                        Name = "_BusinessProcessRRef",
                        Type = DataType.Binary(16, false)
                    }
                };
            }
            else // Multiple type value
            {
                property.Columns = new List<ColumnDefinition>(2)
                {
                    new ColumnDefinition()
                    {
                        Name = "_BusinessProcess_TYPE",
                        Type = DataType.Binary(1, false),
                        Purpose = ColumnPurpose.Tag
                    },
                    new ColumnDefinition()
                    {
                        Name = "_BusinessProcess_RTRef",
                        Type = DataType.Binary(4, false),
                        Purpose = ColumnPurpose.TypeCode
                    },
                    new ColumnDefinition()
                    {
                        Name = "_BusinessProcess_RRRef",
                        Type = DataType.Binary(16, false),
                        Purpose = ColumnPurpose.Identity
                    }
                };
            }

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyТочкаМаршрута(in EntityDefinition table, Guid uuid, in MetadataRegistry registry)
        {
            if (!registry.TryGetBusinessProcesses(uuid, out List<Guid> processes))
            {
                return;
            }

            if (processes is null || processes.Count == 0) { return; }

            PropertyDefinition property = new()
            {
                Name = "ТочкаМаршрута",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Entity(); // Предварительно множественная ссылка

            //if (cache.ResolveReferences && processes is not null && processes.Count > 0)
            //{
            //    property.References.AddRange(processes);
            //}

            if (processes.Count == 1) // Single type value
            {
                if (registry.TryGetEntry(processes[0], out MetadataObject entry))
                {
                    property.Type = DataType.Entity(entry.Code);
                }

                property.Columns = new List<ColumnDefinition>(1)
                {
                    new ColumnDefinition()
                    {
                        Name = "_PointRRef",
                        Type = DataType.Binary(16, false)
                    }
                };
            }
            else // Multiple type value
            {
                property.Columns = new List<ColumnDefinition>(2)
                {
                    new ColumnDefinition()
                    {
                        Name = "_Point_TYPE",
                        Type = DataType.Binary(1, false),
                        Purpose = ColumnPurpose.Tag
                    },
                    new ColumnDefinition()
                    {
                        Name = "_Point_RTRef",
                        Type = DataType.Binary(4, false),
                        Purpose = ColumnPurpose.TypeCode
                    },
                    new ColumnDefinition()
                    {
                        Name = "_Point_RRRef",
                        Type = DataType.Binary(16, false),
                        Purpose = ColumnPurpose.Identity
                    }
                };
            }

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyИмя(in EntityDefinition table, int nameLength)
        {
            PropertyDefinition property = new()
            {
                Name = "Наименование",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.String((ushort)nameLength);

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_Name",
                    Type = property.Type
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyВыполнена(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "Выполнена",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Boolean;

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_Executed",
                    Type = DataType.Binary(1, false)
                }
            };

            table.Properties.Add(property);
        }
        #endregion

        #region "Бизнес-процесс"
        //NOTE: Карта маршрута бизнес-процесса хранится в файле {metadata-object-uuid}.7
        //NOTE: Идентификаторы точек маршрута в этом файле соответствуют идентификаторам в таблице BPrPoints
        //NOTE: Также в этом файле содержатся уникальные имена точек и прочая информация для отрисовки
        internal static void ConfigurePropertyВедущаяЗадача(in EntityDefinition table, Guid task, in MetadataRegistry registry)
        {
            if (!registry.TryGetMetadataNames(MetadataNames.BusinessTask, out Dictionary<string, Guid> tasks))
            {
                return;
            }

            if (tasks is null || tasks.Count == 0) { return; }

            PropertyDefinition property = new()
            {
                Name = "ВедущаяЗадача",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Entity(); // Предварительно множественная ссылка

            int count = tasks.Count;

            //if (cache.ResolveReferences)
            //{
            //    if (count == 1)
            //    {
            //        property.References.Add(process.BusinessTask); //NOTE: Единственная задача конфигурации
            //    }
            //    else
            //    {
            //        property.References.Add(ReferenceTypes.BusinessTask); //NOTE: Любые задачи, которые есть в конфигурации
            //    }
            //}

            if (count == 1) // Single type value
            {
                if (registry.TryGetEntry(task, out MetadataObject entry))
                {
                    property.Type = DataType.Entity(entry.Code);
                }

                property.Columns = new List<ColumnDefinition>(1)
                {
                    new ColumnDefinition()
                    {
                        Name = "_HeadTaskRRef",
                        Type = DataType.Binary(16, false)
                    }
                };
            }
            else // Multiple type value
            {
                property.Columns = new List<ColumnDefinition>(2)
                {
                    new ColumnDefinition()
                    {
                        Name = "_HeadTask_TYPE",
                        Type = DataType.Binary(1, false),
                        Purpose = ColumnPurpose.Tag
                    },
                    new ColumnDefinition()
                    {
                        Name = "_HeadTask_RTRef",
                        Type = DataType.Binary(4, false),
                        Purpose = ColumnPurpose.TypeCode
                    },
                    new ColumnDefinition()
                    {
                        Name = "_HeadTask_RRRef",
                        Type = DataType.Binary(16, false),
                        Purpose = ColumnPurpose.Identity
                    }
                };
            }

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyСтартован(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "Стартован",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Boolean;

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_Started",
                    Type = DataType.Binary(1, false)
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyЗавершён(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "Завершен",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Boolean;

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_Completed",
                    Type = DataType.Binary(1, false)
                }
            };

            table.Properties.Add(property);
        }
        #endregion

        #region "План счетов"
        internal static void ConfigurePropertyАвтоПорядок(in EntityDefinition table, int AutoOrderLength)
        {
            PropertyDefinition property = new()
            {
                Name = "Порядок",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.String((ushort)AutoOrderLength);

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_OrderField",
                    Type = property.Type
                }
            };

            table.Properties.Add(property);
        }
        ///<summary>Вид счёта <see cref="AccountType"/> (активный, пассивный или активно-пассивный)</summary>
        internal static void ConfigurePropertyВидСчёта(in EntityDefinition table)
        {
            // Активный = Active = 0
            // Пассивный = Passive = 1
            // Активно-пассивный = 2 ActivePassive

            PropertyDefinition property = new()
            {
                Name = "Вид",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Decimal(1);

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_Kind",
                    Type = property.Type
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyЗабалансовый(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "Забалансовый",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Boolean;

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_OffBalance",
                    Type = DataType.Binary(1, false)
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyНомерСтроки(in EntityDefinition dimensionTypes)
        {
            PropertyDefinition property = new()
            {
                Name = "НомерСтроки",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Decimal(5, 0);

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_LineNo",
                    Type = property.Type
                }
            };

            dimensionTypes.Properties.Add(property);
        }
        internal static void ConfigurePropertyВидСубконто(in EntityDefinition dimensionTypesTable, Guid dimensionTypesUuid, in MetadataRegistry registry)
        {
            if (dimensionTypesUuid == Guid.Empty)
            {
                throw new InvalidOperationException("Account dimension types are not defined");
            }

            if (!registry.TryGetEntry(dimensionTypesUuid, out Characteristic characteristic))
            {
                throw new InvalidOperationException("Failed to get type code for account dimension types");
            }

            PropertyDefinition property = new()
            {
                Name = "ВидСубконто",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Entity(characteristic.Code);

            //if (cache.ResolveReferences)
            //{
            //    property.References.Add(dimensionTypesUuid); account.DimensionTypes
            //}

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_DimKindRRef",
                    Type = DataType.Binary(16, false)
                }
            };

            dimensionTypesTable.Properties.Add(property);
        }
        internal static void ConfigurePropertyПредопределённое(in EntityDefinition dimensionTypes)
        {
            PropertyDefinition property = new()
            {
                Name = "Предопределенное",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Boolean;

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_DimIsMetadata",
                    Type = DataType.Binary(1, false)
                }
            };

            dimensionTypes.Properties.Add(property);
        }
        internal static void ConfigurePropertyТолькоОбороты(in EntityDefinition dimensionTypes)
        {
            PropertyDefinition property = new()
            {
                Name = "ТолькоОбороты",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Boolean;

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_TurnoverOnly",
                    Type = DataType.Binary(1, false)
                }
            };
            
            dimensionTypes.Properties.Add(property);
        }
        #endregion

        #region "Регистр бухгалтерии"
        internal static void ConfigurePropertyВидДвиженияБухгалтерии(in EntityDefinition table)
        {
            // 0 = Дебет
            // 1 = Кредит

            PropertyDefinition property = new()
            {
                Name = "ВидДвижения",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Decimal(1, 0);

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_Correspond",
                    Type = property.Type
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyСчёт(in AccountingRegister register, in Account account, in EntityDefinition table)
        {
            if (register.ChartOfAccounts == Guid.Empty)
            {
                throw new InvalidOperationException("Chart of accounts is not defined");
            }

            PropertyDefinition property = null;

            if (register.UseCorrespondence)
            {
                property = new PropertyDefinition
                {
                    Name = "СчетДт",
                    Purpose = PropertyPurpose.System,
                    Type = DataType.Entity(account.Code)
                };

                property.Columns = new List<ColumnDefinition>(1)
                {
                    new ColumnDefinition()
                    {
                        Name = "_AccountDtRRef",
                        Type = DataType.Binary(16, false)
                    }
                };

                table.Properties.Add(property);

                property = new PropertyDefinition
                {
                    Name = "СчетКт",
                    Purpose = PropertyPurpose.System,
                    Type = DataType.Entity(account.Code)
                };

                property.Columns = new List<ColumnDefinition>(1)
                {
                    new ColumnDefinition()
                    {
                        Name = "_AccountCtRRef",
                        Type = DataType.Binary(16, false)
                    }
                };

                table.Properties.Add(property);
            }
            else
            {
                property = new PropertyDefinition
                {
                    Name = "Счет",
                    Purpose = PropertyPurpose.System,
                    Type = DataType.Entity(account.Code)
                };

                property.Columns = new List<ColumnDefinition>(1)
                {
                    new ColumnDefinition()
                    {
                        Name = "_AccountRRef",
                        Type = DataType.Binary(16, false)
                    }
                };

                table.Properties.Add(property);
            }
        }
        internal static void ConfigureAccountingRegisterDimensions(in EntityDefinition table, in AccountingRegister register, in MetadataRegistry registry)
        {
            if (register.ChartOfAccounts == Guid.Empty) // План счетов
            {
                return; // Субконто не используются
            }

            if (!registry.TryGetEntry(register.ChartOfAccounts, out Account account))
            {
                throw new InvalidOperationException("Объект метаданных \"План счетов\" не найден!");
            }

            if (account.MaxDimensionCount == 0)
            {
                return; // Субконто не используются
            }

            if (account.DimensionTypes == Guid.Empty) // Значения субконто (план видов характеристик)
            {
                return; // Субконто не используются
            }

            if (!registry.TryGetEntry(account.DimensionTypes, out Characteristic characteristic))
            {
                throw new InvalidOperationException("Объект метаданных \"План видов характеристик\" (субконто) плана счетов не найден!");
            }

            if (registry.Version >= 80315)
            {
                int count = account.MaxDimensionCount + 1;

                //Значения субконто хранятся в основной таблице регистра бухгалтерии
                //наряду с обычным хранением в системной таблице ЗначенияСубконто (_AccRegED)

                DataType kind = DataType.Entity(characteristic.Code); // Вид субконто
                DataType value = characteristic.Type; // Типы значений субконто

                for (int order = 1; order < count; order++)
                {
                    if (register.UseCorrespondence)
                    {
                        ConfigureAccountingDimensionType(in table, order, kind, "Дт", "Dt");
                        ConfigureAccountingDimensionValue(in table, order, value, "Дт", "Dt");
                        ConfigureAccountingDimensionType(in table, order, kind, "Кт", "Ct");
                        ConfigureAccountingDimensionValue(in table, order, value, "Кт", "Ct");
                    }
                    else
                    {
                        ConfigureAccountingDimensionType(in table, order, kind, string.Empty, string.Empty);
                        ConfigureAccountingDimensionValue(in table, order, value, string.Empty, string.Empty);
                    }
                }
            }
        }
        internal static void ConfigureAccountingDimensionType(in EntityDefinition table, int order, DataType propertyType, string propertyName, string databaseName)
        {
            PropertyDefinition property = new()
            {
                Name = string.Format("ВидСубконто{0}{1}", propertyName, order),
                Type = propertyType,
                Purpose = PropertyPurpose.System
            };

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = string.Format("_Kind{0}{1}RRef", databaseName, order),
                    Type = DataType.Binary(16, false)
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigureAccountingDimensionValue(in EntityDefinition table, int order, DataType propertyType, string propertyName, string databaseName)
        {
            PropertyDefinition property = new()
            {
                Name = string.Format("Субконто{0}{1}", propertyName, order),
                Type = propertyType,
                Purpose = PropertyPurpose.System
            };

            ConfigureDatabaseColumns(in property, string.Format("_Value{0}{1}", databaseName, order));

            table.Properties.Add(property);
        }

        //internal static void ConfigureTableЗначенияСубконто(in OneDbMetadataProvider cache, in AccountingDimensionValuesTable table)
        //{
        //    if (table.Entity is not AccountingRegister register)
        //    {
        //        return;
        //    }

        //    if (!cache.TryGetAccRgED(register.Uuid, out DbName dbn))
        //    {
        //        return;
        //    }

        //    table.Uuid = register.Uuid;
        //    table.Name = register.Name + ".ЗначенияСубконто";
        //    table.Alias = "Таблица значений субконто регистра бухгалтерии";
        //    table.TypeCode = register.TypeCode;
        //    table.TableName = $"_{dbn.Name}{dbn.Code}";

        //    ConfigurePropertyПериод(table);
        //    ConfigurePropertyРегистратор(in cache, table);
        //    ConfigurePropertyНомерЗаписи(table);

        //    if (register.UseCorrespondence)
        //    {
        //        ConfigurePropertyВидДвиженияБухгалтерии(table);
        //    }

        //    Guid account_uuid = register.ChartOfAccounts;

        //    if (account_uuid == Guid.Empty)
        //    {
        //        return; // Субконто не используются
        //    }

        //    int account_code;

        //    if (cache.TryGetDbName(account_uuid, out DbName dbn1))
        //    {
        //        account_code = dbn1.Code;
        //    }
        //    else
        //    {
        //        throw new InvalidOperationException("Ошибка получения кода типа для плана счетов!");
        //    }

        //    MetadataObject metadata1 = cache.GetMetadataObject(MetadataTypes.Account, account_uuid);

        //    if (metadata1 is not Account account)
        //    {
        //        throw new InvalidOperationException("Объект метаданных плана счетов не найден!");
        //    }

        //    if (account.MaxDimensionCount == 0)
        //    {
        //        return; // Субконто не используются
        //    }

        //    Guid dimension_uuid = account.DimensionTypes;

        //    if (dimension_uuid == Guid.Empty)
        //    {
        //        return; // Субконто не используются
        //    }

        //    int dimension_code;

        //    if (cache.TryGetDbName(dimension_uuid, out DbName dbn2))
        //    {
        //        dimension_code = dbn2.Code;
        //    }
        //    else
        //    {
        //        throw new InvalidOperationException("Ошибка получения кода типа для плана видов характеристик (субконто) плана счетов!");
        //    }

        //    MetadataObject metadata2 = cache.GetMetadataObject(MetadataTypes.Characteristic, dimension_uuid);

        //    if (metadata2 is not Characteristic characteristic)
        //    {
        //        throw new InvalidOperationException("Объект метаданных плана видов характеристик (субконто) плана счетов не найден!");
        //    }

        //    ConfigureAccountingDimensionType(table, dimension_code, dimension_uuid, "ВидСубконто", "_KindRRef");
        //    ConfigureAccountingDimensionValue(in cache, table, in characteristic, "Значение", "_Value");

        //    foreach (MetadataProperty property in register.Properties)
        //    {
        //        if (property is SharedProperty shared)
        //        {
        //            table.Properties.Add(shared.Copy());
        //        }
        //    }
        //}
        #endregion

        #region "Табличная часть"
        internal static void ConfigureTablePart(in EntityDefinition table, in TablePart metadata, in MetadataObject owner)
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
        internal static void ConfigurePropertyСсылка(in EntityDefinition table, in MetadataObject owner)
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
            property.Type = DataType.Entity(owner.Code);

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

            //TODO: property.PropertyType.References.Add(new MetadataItem(MetadataTypess.Catalog, owner.Uuid, owner.Name));

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
        internal static void ConfigurePropertyКлючСтроки(in EntityDefinition table)
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

        #region "План обмена"
        //NOTE: Состав плана обмена хранится в файле {metadata-object-uuid}.1
        internal static void ConfigurePropertyНомерОтправленного(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "НомерОтправленного",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Decimal(10, 0);

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_SentNo",
                    Type = property.Type
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyНомерПринятого(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "НомерПринятого",
                Purpose = PropertyPurpose.System
            };
            property.Type = DataType.Decimal(10, 0);

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_ReceivedNo",
                    Type = property.Type
                }
            };

            table.Properties.Add(property);
        }

        #region "Таблица регистрации изменений"
        internal static void ConfigurePropertyУзелПланаОбмена(in EntityDefinition table)
        {
            // This property always has the multiple refrence type,
            // even if there is only one exchange plan configured.

            PropertyDefinition property = new()
            {
                Name = "УзелОбмена",
                Type = DataType.Entity(),
                Purpose = PropertyPurpose.System
            };

            property.Columns = new List<ColumnDefinition>(2)
            {
                new ColumnDefinition()
                {
                    Name = "_NodeTRef",
                    Type = DataType.Binary(4, false),
                    Purpose = ColumnPurpose.TypeCode,
                    IsPrimaryKey = true
                },
                new ColumnDefinition()
                {
                    Name = "_NodeRRef",
                    Type = DataType.Binary(16, false),
                    Purpose = ColumnPurpose.Identity,
                    IsPrimaryKey = true
                }
            };

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyНомерСообщения(in EntityDefinition table)
        {
            PropertyDefinition property = new()
            {
                Name = "НомерСообщения",
                Type = DataType.Decimal(10, 0),
                Purpose = PropertyPurpose.System
            };

            property.Columns = new List<ColumnDefinition>(1)
            {
                new ColumnDefinition()
                {
                    Name = "_MessageNo",
                    Type = property.Type
                }
            };

            table.Properties.Add(property);
        }
        #endregion

        private static Dictionary<Guid, AutoPublication> ParsePublicationArticles(ReadOnlySpan<byte> file)
        {
            Dictionary<Guid, AutoPublication> articles = new();

            if (file == ReadOnlySpan<byte>.Empty)
            {
                return articles; //NOTE: Таблица состава плана обмена отсутствует
            }

            ConfigFileReader reader = new(file);

            int count = reader[2].SeekNumber();

            if (count == 0)
            {
                return articles; //NOTE: Состав плана обмена пуст
            }

            articles.EnsureCapacity(count);

            uint offset = 2;

            for (uint i = 1; i <= count; i++)
            {
                Guid uuid = reader[i * offset + 1].SeekUuid();

                AutoPublication setting = (AutoPublication)reader[i * offset + 2].SeekNumber();

                articles.Add(uuid, setting);
            }

            articles.TrimExcess();

            return articles;
        }

        internal static EntityDefinition GetChangeTrackingTable(in MetadataObject entry, in EntityDefinition entity, in MetadataRegistry registry, in MetadataLoader loader)
        {
            if (entry is Catalog || entry is Document)
            {
                return ConfigureChangeTrackingTableForReferenceObject(in entry, in entity, in registry, in loader);
            }

            return null;
        }
        private static EntityDefinition ConfigureChangeTrackingTableForReferenceObject(in MetadataObject entry, in EntityDefinition owner, in MetadataRegistry registry, in MetadataLoader loader)
        {
            if (!entry.IsChangeTrackingEnabled)
            {
                return null; // Данный объект не включён в состав какого-либо плана обмена
            }

            EntityDefinition changes = new() // Таблица регистрации изменений
            {
                Name = "Изменения",
                DbName = entry.GetTableNameИзменения() //TODO: (extended ? "x1" : string.Empty)
            };

            ConfigurePropertyУзелПланаОбмена(in changes);
            ConfigurePropertyНомерСообщения(in changes);
            ConfigurePropertyСсылка(in changes, entry.Code);

            foreach (PropertyDefinition property in owner.Properties)
            {
                if (property.Purpose.IsSharedProperty() && property.Purpose.UseDataSeparation())
                {
                    changes.Properties.Add(property);
                }
            }

            if (entry.IsExtension) // Собственный объект расширения
            {
                changes.DbName += "x1"; return changes;
            }

            if (!registry.TryGetBorrowed(entry.Uuid, out List<Guid> borrowed))
            {
                return changes; // Объект основной конфигурации без заимствований
            }

            foreach (Guid uuid in borrowed)
            {
                if (registry.TryGetEntry(uuid, out MetadataObject mdo))
                {
                    Configuration configuration = registry.Configurations[mdo.Cfid];

                    if (configuration.Metadata.TryGetValue(MetadataTypes.Publication, out Guid[] publications))
                    {
                        foreach (Guid publication in publications)
                        {
                            string fileName = string.Format("{0}.1", publication.ToString().ToLowerInvariant());
                            
                            if (!registry.TryGetFileName(in fileName, out fileName))
                            {
                                throw new InvalidOperationException("File not found!");
                            }

                            Dictionary<Guid, AutoPublication> articles;

                            using (ConfigFileBuffer file = loader.Load(ConfigTables.ConfigCAS, in fileName))
                            {
                                articles = ParsePublicationArticles(file.AsReadOnlySpan());
                            }

                            if (articles.TryGetValue(uuid, out AutoPublication setting))
                            {
                                changes.DbName += "x1"; return changes;
                            }
                        }
                    }
                }
            }
            
            return changes;
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

        #region "Заимствованные объекты расширений"
        internal static void ApplyBorrowedObject(in EntityDefinition main, in EntityDefinition borrowed)
        {
            ///THINK: <see cref="EntityDefinition.GetPropertyByColumnName"/>

            bool extend = false;

            foreach (PropertyDefinition property in borrowed.Properties)
            {
                if (!PropertyExists(main.Properties, property.Name))
                {
                    main.Properties.Add(property); extend = true;
                }
            }
            
            foreach (EntityDefinition test in borrowed.Entities)
            {
                if (TryGetTableByName(main.Entities, test.Name, out EntityDefinition table))
                {
                    foreach (PropertyDefinition property in test.Properties)
                    {
                        if (!PropertyExists(table.Properties, property.Name))
                        {
                            table.Properties.Add(property); extend = true;
                        }
                    }
                }
                else
                {
                    main.Entities.Add(test); extend = true;
                }
            }

            if (extend)
            {
                main.DbName += "x1";

                foreach (EntityDefinition table in main.Entities)
                {
                    table.DbName += "x1";
                }
            }
        }
        private static bool PropertyExists(in List<PropertyDefinition> list, in string name)
        {
            if (list.Count == 0)
            {
                return false;
            }

            PropertyDefinition property;

            for (int i = 0; i < list.Count; i++)
            {
                property = list[i];

                if (property.Name == name)
                {
                    return true;
                }
            }
            
            return false;
        }
        private static bool TryGetTableByName(in List<EntityDefinition> list, in string name, out EntityDefinition table)
        {
            table = null;

            if (list.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < list.Count; i++)
            {
                table = list[i];

                if (table.Name == name)
                {
                    return true;
                }
            }

            table = null;

            return false;
        }
        #endregion
    }
}