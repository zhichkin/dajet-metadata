using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    internal sealed class Publication : MetadataObject
    {
        internal static Publication Create(Guid uuid)
        {
            return new Publication(uuid);
        }
        internal Publication(Guid uuid) : base(uuid) { }
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.Node)
            {
                Code = code;
            }
        }
        internal override string GetMainDbName()
        {
            return string.Format("_{0}{1}", MetadataToken.Node, Code);
        }
        internal override string GetTableNameИзменения()
        {
            throw new NotImplementedException();
        }
        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataNames.Publication, Name);
        }

        internal static Dictionary<Guid, AutoPublication> ParsePublicationArticles(ReadOnlySpan<byte> file)
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

                if (metadata.Code > 0)
                {
                    // Объекты основной конфигурации и собственные объекты расширения
                    registry.AddMetadataName(MetadataNames.Publication, metadata.Name, uuid);

                    if (metadata.IsExtension) // Собственный объект расширения
                    {
                        registry.SetGenericExtensionFlag(GenericExtensionFlags.Publication);
                    }
                }
                else // Заимствованный объект расширения
                {
                    if (registry.TryGetEntry(MetadataNames.Publication, metadata.Name, out Publication parent))
                    {
                        metadata.IsBorrowed = true;
                        metadata.Code = parent.Code;
                        registry.AddBorrowed(parent.Uuid, metadata.Uuid);
                    }

                    //if (IsExtension) // 1.12.8 = 0 если заимствование отстутствует
                    //{
                    //    _converter[1][12][9] += Parent; // uuid расширяемого объекта метаданных
                    //}
                }
            }
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry, bool relations)
            {
                if (!registry.TryGetEntry(uuid, out Publication entry))
                {
                    return null; // Идентификатор объекта не найден или не соответствует его типу
                }

                EntityDefinition table = new();

                table.Name = entry.Name;
                table.DbName = entry.GetMainDbName();

                Configurator.ConfigurePropertyСсылка(in table, entry.Code);
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
                Configurator.ConfigurePropertyПредопределённый(in table, true, registry.Version);

                if (registry.Version >= 80323)
                {
                    Configurator.ConfigurePropertyДатаОбмена(in table);
                }
                
                uint root = 4; // Коллекция свойств объекта

                uint[] vector = [root];

                if (reader[root][ConfigFileToken.StartObject].Seek())
                {
                    Property.Parse(ref reader, vector, in registry, entry, in table);
                }

                root = 6; // Коллекция табличных частей объекта

                if (reader[root][ConfigFileToken.StartObject].Seek())
                {
                    TablePart.Parse(ref reader, root, in registry, entry, in table);
                }

                return table;
            }
        }
    }
}