namespace DaJet
{
    internal sealed class DefinedTypeParser : ConfigFileParser
    {
        internal override void Parse(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
        {
            if (!registry.TryGetEntry(uuid, out DefinedType metadata))
            {
                return; //NOTE: сюда не предполагается попадать!
            }

            ConfigFileReader reader = new(file);

            // Идентификатор ссылочного типа данных, например, "ОпределяемыйТипСсылка.ПриходныеДокументы"
            if (reader[2][2].Seek())
            {
                Guid reference = reader.ValueAsUuid;
                registry.AddDefinedType(uuid, reference);
            }

            // Идентификатор объекта метаданных - значение поля FileName в таблице Config
            //if (reader[2][4][2][3].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

            // Имя объекта метаданных конфигурации
            if (reader[2][4][3].Seek())
            {
                string name = reader.ValueAsString;
                metadata.Name = name;
                registry.AddMetadataName(MetadataName.DefinedType, in name, uuid);
            }

            if (reader[2][5][ConfigFileToken.StartObject].Seek())
            {
                uint[] root = [2, 5];

                metadata.Type = DataTypeParser.Parse(ref reader, root, in registry, out List<Guid> references);
            }

            //if (options.IsExtension)
            //{
            //    _converter[1][3][11] += Parent;
            //}
        }
        internal override TableDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
        {
            TableDefinition table = new();

            return table;
        }
    }
}