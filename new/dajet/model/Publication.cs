namespace DaJet
{
    internal sealed class Publication : DatabaseObject
    {
        internal static Publication Create(Guid uuid, int code, string name)
        {
            return new Publication(uuid, code, name);
        }
        internal Publication(Guid uuid, int code, string name) : base(uuid, code, name) { }

        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataName.Publication, Name);
        }

        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                if (!registry.TryGetEntry(uuid, out Publication metadata))
                {
                    return; //NOTE: сюда не предполагается попадать!
                }

                ConfigFileReader reader = new(file);

                // Идентификатор ссылочного типа данных, например, "ПланОбменаСсылка.ОбменДаннымиРИБ"
                if (reader[2][4].Seek())
                {
                    Guid reference = reader.ValueAsUuid;
                    registry.AddReference(uuid, reference);
                }

                // Идентификатор объекта метаданных - значение поля FileName в таблице Config
                //if (reader[2][13][2][3].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

                // Имя объекта метаданных конфигурации
                if (reader[2][13][3].Seek())
                {
                    string name = reader.ValueAsString;
                    metadata.Name = name;
                    registry.AddMetadataName(MetadataName.Publication, in name, uuid);
                }

                //if (options.IsExtension)
                //{
                //    _converter[1][12][9] += Parent;
                //}
            }
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry, bool relations)
            {
                EntityDefinition table = new();

                return table;
            }
        }
    }
}