using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    internal sealed class Characteristic : ChangeTrackingObject
    {
        internal static Characteristic Create(Guid uuid, int code)
        {
            return new Characteristic(uuid, code, MetadataToken.Chrc);
        }
        internal Characteristic(Guid uuid, int code, string name) : base(uuid, code, name) { }
        internal DataType Type { get; set; }
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.ChrcChngR)
            {
                _ChngR = code;
            }
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.ChrcChngR, _ChngR);
        }
        
        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataNames.Characteristic, Name);
        }

        internal static void InitializeDataType(Guid uuid, ReadOnlySpan<byte> file, MetadataRegistry registry)
        {
            if (!registry.TryGetEntry(uuid, out Characteristic metadata))
            {
                return; //NOTE: сюда не предполагается попадать!
            }

            ConfigFileReader reader = new(file);

            if (reader[2][19][ConfigFileToken.StartObject].Seek())
            {
                uint[] root = [2, 19];

                metadata.Type = DataTypeParser.Parse(ref reader, root, in registry, out _);
            }
        }

        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                ConfigFileReader reader = new(file);

                // Идентификатор ссылочного типа данных, например, "ПланВидовХарактеристикСсылка.ВидыСубконтоХозрасчетные"
                Guid reference = reader[2][4].SeekUuid();

                // Идентификатор характеристики, например, "Характеристика.ВидыСубконтоХозрасчетные" (опеределение типа данных свойства)
                Guid characteristic = reader[2][10].SeekUuid();

                // Идентификатор объекта метаданных - значение поля FileName в таблице Config
                Guid uuid = reader[2][14][2][2][3].SeekUuid();

                if (!registry.TryGetEntry(uuid, out Characteristic metadata))
                {
                    throw new InvalidOperationException();
                }

                registry.AddReference(uuid, reference);
                registry.AddCharacteristic(uuid, characteristic);

                // Имя объекта метаданных конфигурации
                metadata.Name = reader[2][14][2][3].SeekString();

                if (metadata.TypeCode > 0)
                {
                    // Объекты основной конфигурации и собственные объекты расширения
                    registry.AddMetadataName(MetadataNames.Characteristic, metadata.Name, uuid);
                }
                else // Заимствованный объект расширения
                {
                    if (registry.TryGetEntry(MetadataNames.Characteristic, metadata.Name, out Characteristic parent))
                    {
                        parent.MarkAsBorrowed();
                        metadata.MarkAsBorrowed();
                        metadata.TypeCode = parent.TypeCode;
                        registry.AddExtension(parent.Uuid, metadata.Uuid);
                    }
                }

                //if (options.IsExtension)
                //{
                //    _converter[1][13][1][11] += Parent;
                //}

                //if (_cache != null && _cache.Extension != null) // 1.13.1.8 = 0 если заимствование отстутствует
                //{
                //    _converter[1][13][1][11] += Parent; // uuid расширяемого объекта метаданных

                //    //FIXME: extensions support (!)
                //    // [1][13][1][15] - Объект описания дополнительных типов данных определяемого типа
                //    // [1][13][1][15][0] = #
                //    // [1][13][1][15][1] = f5c65050-3bbb-11d5-b988-0050bae0a95d (константа)
                //    // [1][13][1][15][2] = {объект описания типов данных - Pattern} аналогично [1][18] += DataTypeDescriptor

                //    _converter[1][13][1][15][2] += ExtensionDataTypeDescriptor;
                //}
            }
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry, bool relations)
            {
                if (!registry.TryGetEntry(uuid, out Characteristic entry))
                {
                    return null; //NOTE: сюда не предполагается попадать!
                }

                EntityDefinition table = new();

                table.Name = entry.Name;
                table.DbName = entry.GetMainDbName();

                Configurator.ConfigurePropertyСсылка(in table, entry.TypeCode);
                Configurator.ConfigurePropertyВерсияДанных(in table);
                Configurator.ConfigurePropertyПометкаУдаления(in table);

                ConfigFileReader reader = new(file);

                // Флаг, является ли справочник иерархическим
                bool IsHierarchical = reader[2][20].SeekNumber() != 0;

                // Длина кода
                int codeLength = reader[2][22].SeekNumber();

                // Длина наименования
                int nameLength = reader[2][24].SeekNumber();

                if (IsHierarchical) // Тип иерархии всегда группы
                {
                    Configurator.ConfigurePropertyРодитель(in table, entry.TypeCode);
                    Configurator.ConfigurePropertyЭтоГруппа(in table);
                    
                }

                if (codeLength > 0) // Тип кода всегда строка
                {
                    Configurator.ConfigurePropertyКод(in table, CodeType.String, codeLength);
                }

                if (nameLength > 0)
                {
                    Configurator.ConfigurePropertyНаименование(in table, nameLength);
                }

                Configurator.ConfigurePropertyПредопределённый(in table, false, registry.CompatibilityVersion);

                Configurator.ConfigurePropertyТипЗначения(in table);

                uint root = 4; // Коллекция свойств объекта

                uint[] vector = [root];

                if (reader[root][ConfigFileToken.StartObject].Seek())
                {
                    Property.Parse(ref reader, vector, in table, in registry, relations);
                }
                
                root = 6; // Коллекция табличных частей объекта

                if (reader[root][ConfigFileToken.StartObject].Seek())
                {
                    TablePart.Parse(ref reader, root, in table, entry, in registry, relations);
                }

                Configurator.ConfigureSharedProperties(in registry, entry, in table);

                entry.ConfigureChangeTrackingTable(in table);

                return table;
            }
        }
    }
}