using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    #region "Перечисления"
    internal enum AutomaticUsage
    {
        Use = 0,
        DoNotUse = 1
    }
    internal enum SharedPropertyUsage
    {
        Auto = 0,
        Use = 1,
        DoNotUse = 2
    }
    ///<summary>Разделение данных между ИБ</summary>
    internal enum DataSeparationUsage
    {
        ///<summary>Разделять</summary>
        Use = 0,
        ///<summary>Не использовать</summary>
        DoNotUse = 1
    }
    ///<summary>Режим использования разделяемых данных</summary>
    internal enum DataSeparationMode
    {
        ///<summary>Независимо</summary>
        Independent = 0,
        ///<summary>Независимо и совместно</summary>
        IndependentAndShared = 1
    }
    #endregion
    internal sealed class SharedProperty : DatabaseObject
    {
        internal SharedProperty(Guid uuid) : base(uuid, 0, MetadataToken.Fld) { }
        internal DataType Type { get; set; }
        internal PropertyDefinition Definition { get; set; }
        internal AutomaticUsage AutomaticUsage { get; set; }
        internal DataSeparationMode DataSeparationMode { get; set; } = DataSeparationMode.Independent;
        internal DataSeparationUsage DataSeparationUsage { get; set; } = DataSeparationUsage.DoNotUse;
        internal Dictionary<Guid, SharedPropertyUsage> UsageSettings { get; } = new();
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.Fld)
            {
                TypeCode = code;
            }
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataNames.SharedProperty, Name);
        }

        private static void ParseUsageSettings(ref ConfigFileReader reader, in SharedProperty metadata)
        {
            int count = reader.ValueAsNumber; // количество настроек использования общего реквизита

            Guid uuid; // file name объекта метаданных, для которого используется настройка
            int usage; // значение настройки использования общего реквизита объектом метаданных

            while (count > 0)
            {
                _ = reader.Read(); // [2][3][3] uuid настраиваемого объекта метаданных

                uuid = reader.ValueAsUuid; // file name объекта метаданных

                if (uuid == Guid.Empty) { throw new FormatException(); }

                _ = reader.Read(); // [2][3][4] {  Начало объекта настройки
                _ = reader.Read(); // [2][3][4][1] 2
                _ = reader.Read(); // [2][3][4][2] 1

                usage = reader.ValueAsNumber; // настройка использования общего реквизита

                if (usage == -1) { throw new FormatException(); }

                _ = reader.Read(); // [2][3][4][3] 00000000-0000-0000-0000-000000000000
                _ = reader.Read(); // [2][3][4] }  Конец объекта настройки

                metadata.UsageSettings.Add(uuid, (SharedPropertyUsage)usage);

                count--; // Конец чтения настройки для объекта метаданных
            }
        }

        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                ConfigFileReader reader = new(file);

                // Идентификатор объекта метаданных - значение поля FileName в таблице Config
                Guid uuid = reader[2][2][2][2][2][3].SeekUuid();

                if (!registry.TryGetEntry(uuid, out SharedProperty metadata))
                {
                    throw new InvalidOperationException();
                }

                // Имя объекта метаданных конфигурации
                metadata.Name = reader[2][2][2][2][3].SeekString();

                if (metadata.TypeCode > 0)
                {
                    // Объекты основной конфигурации и собственные объекты расширения
                    registry.AddMetadataName(MetadataNames.SharedProperty, metadata.Name, uuid);
                }
                else // Заимствованный объект расширения
                {
                    if (registry.TryGetEntry(MetadataNames.SharedProperty, metadata.Name, out SharedProperty parent))
                    {
                        parent.MarkAsBorrowed();
                        metadata.MarkAsBorrowed();
                        metadata.TypeCode = parent.TypeCode;
                        registry.AddExtension(parent.Uuid, metadata.Uuid);
                    }
                }

                //if (options.IsExtension) // 1.1.1.1.14 = 0 если заимствование отстутствует
                //{
                //    _converter[1][1][1][1][15] += Parent; // uuid расширяемого объекта метаданных
                //}

                if (reader[2][2][2][3][ConfigFileToken.StartObject].Seek())
                {
                    uint[] root = [2, 2, 2, 3];

                    metadata.Type = DataTypeParser.Parse(ref reader, root, in registry, out _);
                }

                // Объекты метаданных, у которых значение использования общего реквизита не равно "Автоматически"

                if (reader[2][3][2].Seek())
                {
                    ParseUsageSettings(ref reader, in metadata);
                }

                metadata.DataSeparationUsage = (DataSeparationUsage)reader[2][6].SeekNumber();
                metadata.AutomaticUsage = (AutomaticUsage)reader[2][7].SeekNumber();
                metadata.DataSeparationMode = (DataSeparationMode)reader[2][13].SeekNumber();

                PropertyDefinition definition = new()
                {
                    Name = metadata.Name,
                    Type = metadata.Type,
                    Purpose = PropertyPurpose.SharedProperty
                };

                if (metadata.DataSeparationUsage == DataSeparationUsage.Use)
                {
                    definition.Purpose |= PropertyPurpose.UseDataSeparation;
                }

                Configurator.ConfigureDatabaseColumns(in metadata, in definition);

                metadata.Definition = definition;
            }
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry, bool relations)
            {
                throw new NotImplementedException();
            }
        }
    }
}