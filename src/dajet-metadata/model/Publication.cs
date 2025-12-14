using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    internal sealed class Publication : DatabaseObject
    {
        internal static Publication Create(Guid uuid, int code)
        {
            return new Publication(uuid, code, MetadataToken.Node);
        }
        internal Publication(Guid uuid, int code, string name) : base(uuid, code, name) { }

        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataNames.Publication, Name);
        }

        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                ConfigFileReader reader = new(file);

                // Идентификатор ссылочного типа данных, например, "ПланОбменаСсылка.ОбменДаннымиРИБ"
                Guid reference = reader[2][4].SeekUuid();

                // Идентификатор объекта метаданных - значение поля FileName в таблице Config
                Guid uuid = reader[2][13][2][3].SeekUuid();

                if (!registry.TryGetEntry(uuid, out Publication metadata))
                {
                    throw new InvalidOperationException();
                }

                registry.AddReference(uuid, reference);

                // Имя объекта метаданных конфигурации
                metadata.Name = reader[2][13][3].SeekString();

                if (metadata.TypeCode > 0)
                {
                    // Объекты основной конфигурации и собственные объекты расширения
                    registry.AddMetadataName(MetadataNames.Publication, metadata.Name, uuid);
                }
                else // Заимствованный объект расширения
                {
                    if (registry.TryGetEntry(MetadataNames.Publication, metadata.Name, out Publication parent))
                    {
                        parent.MarkAsBorrowed();
                        metadata.MarkAsBorrowed();
                        metadata.TypeCode = parent.TypeCode;
                        registry.AddExtension(parent.Uuid, metadata.Uuid);
                    }
                }

                //if (IsExtension) // 1.12.8 = 0 если заимствование отстутствует
                //{
                //    _converter[1][12][9] += Parent; // uuid расширяемого объекта метаданных
                //}
            }
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry, bool relations)
            {
                //TODO: состав плана обмена

                if (!registry.TryGetEntry(uuid, out Publication entry))
                {
                    return null; // Идентификатор объекта не найден или не соответствует его типу
                }

                EntityDefinition table = new();

                table.Name = entry.Name;
                table.DbName = entry.GetMainDbName();

                Configurator.ConfigurePropertyСсылка(in table, entry.TypeCode);
                Configurator.ConfigurePropertyВерсияДанных(in table);
                Configurator.ConfigurePropertyПометкаУдаления(in table);

                ConfigFileReader reader = new(file);

                // Длина кода (всегда строка и минимум 1 символ)
                int codeLength = reader[2][16].SeekNumber();

                // Длина наименования (всегда строка и минимум 1 символ)
                int nameLength = reader[2][18].SeekNumber();

                Configurator.ConfigurePropertyКод(in table, CodeType.String, codeLength);
                Configurator.ConfigurePropertyНаименование(in table, nameLength);
                Configurator.ConfigurePropertyНомерОтправленного(in table);
                Configurator.ConfigurePropertyНомерПринятого(in table);
                Configurator.ConfigurePropertyПредопределённый(in table, true, registry.CompatibilityVersion);

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

                return table;
            }
        }
    }
}