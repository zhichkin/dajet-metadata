namespace DaJet
{
    internal sealed class AccountParser : ConfigFileParser
    {
        internal override void Parse(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
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
                registry.AddMetadataName(MetadataName.Account, in name, uuid);
            }
        }
        internal override TableDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
        {
            TableDefinition table = new();

            return table;
        }
    }
}