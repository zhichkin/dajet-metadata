namespace DaJet
{
    internal sealed class BusinessTaskParser : ConfigFileParser
    {
        internal override void Parse(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
        {
            if (!registry.TryGetEntry(uuid, out BusinessTask metadata))
            {
                return; //NOTE: сюда не предполагается попадать!
            }

            ConfigFileReader reader = new(file);

            // Идентификатор объекта метаданных - значение поля FileName в таблице Config
            //if (reader[2][2][2][3].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

            // Имя объекта метаданных конфигурации
            if (reader[2][2][3].Seek())
            {
                string name = reader.ValueAsString;
                metadata.Name = name;
                registry.AddMetadataName(MetadataName.BusinessTask, in name, uuid);
            }

            // Идентификатор ссылочного типа данных, например, "ЗадачаСсылка.Задача"
            if (reader[2][6].Seek())
            {
                Guid reference = reader.ValueAsUuid;
                registry.AddReference(uuid, reference);
            }

            //if (options.IsExtension)
            //{
            //    _converter[1][1][9] += Parent;
            //}
        }
        internal override TableDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
        {
            TableDefinition table = new();

            return table;
        }
    }
}