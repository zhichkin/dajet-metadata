using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    internal sealed class Account : ChangeTrackingObject
    {
        internal static Account Create(Guid uuid, int code, string name)
        {
            return new Account(uuid, code, name);
        }
        internal Account(Guid uuid, int code, string name) : base(uuid, code, name) { }
        
        internal Guid DimensionTypes { get; set; } // Виды субконто (план видов характеристик)
        internal int MaxDimensionCount { get; set; } // Максимальное количество субконто (обычно 3)

        private int _ExtDim;
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.ExtDim)
            {
                _ExtDim = code;
            }
            else if (name == MetadataToken.AccChngR)
            {
                _ChngR = code;
            }
        }
        internal string GetTableNameВидыСубконто()
        {
            return string.Format("_{0}{1}_{2}{3}", DbName, TypeCode, MetadataToken.ExtDim, _ExtDim);
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.AccChngR, _ChngR);
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataNames.Account, Name);
        }

        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                if (!registry.TryGetEntry(uuid, out Account metadata))
                {
                    return; //NOTE: сюда не предполагается попадать!
                }

                ConfigFileReader reader = new(file);

                // Идентификатор ссылочного типа данных, например, "ПланСчетовСсылка.Управленческий"
                if (reader[2][4].Seek())
                {
                    Guid reference = reader.ValueAsUuid;
                    registry.AddReference(uuid, reference);
                }

                // Идентификатор объекта метаданных - значение поля FileName в таблице Config
                //if (reader[2][16][2][2][3].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

                // Имя объекта метаданных конфигурации
                if (reader[2][16][2][3].Seek())
                {
                    string name = reader.ValueAsString;
                    metadata.Name = name;
                    registry.AddMetadataName(MetadataNames.Account, in name, uuid);
                }

                // Виды субконто (план видов характеристик)
                metadata.DimensionTypes = reader[2][20].SeekUuid();

                // Максимальное количество субконто
                metadata.MaxDimensionCount = reader[2][21].SeekNumber();

                // Маска кода (строка)
                //string CodeMask = reader[2][22].SeekString();
            }
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry, bool relations)
            {
                if (!registry.TryGetEntry(uuid, out Account entry))
                {
                    return null; // Идентификатор объекта не найден или не соответствует его типу
                }

                EntityDefinition table = new();

                table.Name = entry.Name;
                table.DbName = entry.GetMainDbName();

                Configurator.ConfigurePropertyСсылка(in table, entry.TypeCode);
                Configurator.ConfigurePropertyВерсияДанных(in table);
                Configurator.ConfigurePropertyПометкаУдаления(in table);
                Configurator.ConfigurePropertyРодитель(in table, entry.TypeCode); // Всегда иерархический

                ConfigFileReader reader = new(file);

                // Длина кода (всегда строка)
                int CodeLength = reader[2][23].SeekNumber();

                // Длина наименования
                int DescriptionLength = reader[2][24].SeekNumber();

                // Автопорядок по коду
                bool UseAutoOrder = (reader[2][25].SeekNumber() == 1);

                // Длина значения "Порядок"
                int AutoOrderLength = reader[2][26].SeekNumber();

                if (CodeLength > 0)
                {
                    Configurator.ConfigurePropertyКод(in table, CodeType.String, CodeLength);
                }

                if (DescriptionLength > 0)
                {
                    Configurator.ConfigurePropertyНаименование(in table, DescriptionLength);
                }

                if (UseAutoOrder)
                {
                    Configurator.ConfigurePropertyАвтоПорядок(in table, AutoOrderLength);
                }

                Configurator.ConfigurePropertyВидСчёта(in table);
                Configurator.ConfigurePropertyЗабалансовый(in table);
                Configurator.ConfigurePropertyПредопределённый(in table, false, registry.CompatibilityVersion);

                uint root = 6; // Коллекция табличных частей

                if (reader[root][ConfigFileToken.StartObject].Seek())
                {
                    TablePart.Parse(ref reader, root, in table, entry, in registry, relations);
                }

                uint[] offset = [8]; // Коллекция реквизитов

                if (reader[offset][ConfigFileToken.StartObject].Seek())
                {
                    Property.Parse(ref reader, offset, in table, in registry, relations);
                }

                offset[0] = 9; // Коллекция признаков учёта

                if (reader[offset][ConfigFileToken.StartObject].Seek())
                {
                    Property.Parse(ref reader, offset, in table, in registry, relations);
                }

                offset[0] = 10; // Коллекция признаков учёта субконто

                if (reader[offset][ConfigFileToken.StartObject].Seek())
                {
                    Property.Parse(ref reader, offset, in table, in registry, relations);
                }

                List<PropertyDefinition> dimensions = new();

                int index = 0;
                int count = table.Properties.Count;

                while (index < count)
                {
                    PropertyDefinition property = table.Properties[index];

                    if (property.Purpose.IsAccountingDimensionFlag())
                    {
                        dimensions.Add(property);
                        table.Properties.RemoveAt(index);
                        count--;
                    }
                    else
                    {
                        index++;
                    }
                }

                if (entry.MaxDimensionCount > 0)
                {
                    EntityDefinition dimensionTypes = new()
                    {
                        Name = "ВидыСубконто",
                        DbName = entry.GetTableNameВидыСубконто()
                    };

                    Configurator.ConfigurePropertyСсылка(in dimensionTypes, entry);
                    Configurator.ConfigurePropertyКлючСтроки(in dimensionTypes);
                    Configurator.ConfigurePropertyНомерСтроки(in dimensionTypes);
                    Configurator.ConfigurePropertyВидСубконто(in dimensionTypes, entry.DimensionTypes, in registry);
                    Configurator.ConfigurePropertyПредопределённое(in dimensionTypes);
                    Configurator.ConfigurePropertyТолькоОбороты(in dimensionTypes);

                    dimensionTypes.Properties.AddRange(dimensions);

                    table.Entities.Add(dimensionTypes);
                }

                Configurator.ConfigureSharedProperties(in registry, entry, in table);

                entry.ConfigureChangeTrackingTable(in table);

                return table;
            }
        }
    }
}