namespace DaJet
{
    internal sealed class PublicationParser : ConfigFileParser
    {
        internal override void Parse(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
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
        internal override TableDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
        {
            TableDefinition table = new();

            return table;
        }
    }
}