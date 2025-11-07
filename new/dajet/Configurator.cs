namespace DaJet
{
    internal static class Configurator
    {
        #region "Справочники"

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
            property.Type.IsEntity = true;
            property.Type.TypeCode = typeCode;

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
            property.Type.IsBinary = true;

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
            property.Type.IsBoolean = true;

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
                property.Type.IsString = true;
                property.Type.StringKind = StringKind.Variable;
                property.Type.StringLength = codeLength;

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
                property.Type.IsDecimal = true;
                property.Type.NumericKind = NumericKind.AlwaysPositive;
                property.Type.NumericScale = 0;
                property.Type.NumericPrecision = codeLength;

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
            property.Type.IsString = true;
            property.Type.StringLength = nameLength;

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

            property.Type.IsEntity = true;
            property.Type.TypeCode = typeCode;

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
            property.Type.IsBoolean = true;

            property.Columns.Add(new ColumnDefinition()
            {
                Name = "_Folder",
                Type = "binary", // инвертировать !
                Length = 1
            });

            table.Properties.Add(property);
        }
        internal static void ConfigurePropertyВладелец(in TableDefinition table, in Guid[] owners)
        {
            PropertyDefinition property = new()
            {
                Name = "Владелец",
                Purpose = PropertyPurpose.System
            };
            property.Type.IsEntity = true;

            //if (options.ResolveReferences && owners is not null && owners.Count > 0)
            //{
            //    property.References.AddRange(owners);
            //}

            if (owners.Length == 1) // Single type value
            {
                //if (registry.TryGetDbName(owners[0], out DbName dbn))
                //{
                //    property.Type.TypeCode = dbn.Code;
                //}

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
            property.Type.IsBoolean = true;

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
            property.Type.IsUuid = true;

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