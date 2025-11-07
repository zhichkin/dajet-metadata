namespace DaJet
{
    internal sealed class AccumulationRegisterParser : ConfigFileParser
    {
        internal override void Parse(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
        {
            if (!registry.TryGetEntry(uuid, out AccumulationRegister metadata))
            {
                return; //NOTE: сюда не предполагается попадать!
            }

            ConfigFileReader reader = new(file);

            // Идентификатор объекта метаданных - значение поля FileName в таблице Config
            //if (reader[2][14][2][2][3].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

            // Имя объекта метаданных конфигурации
            if (reader[2][14][2][3].Seek())
            {
                string name = reader.ValueAsString;
                metadata.Name = name;
                registry.AddMetadataName(MetadataName.AccumulationRegister, in name, uuid);
            }

            //if (options.IsExtension)
            //{
            //    _converter[1][13][1][11] += Parent;
            //}
        }
        internal override TableDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
        {
            TableDefinition table = new();

            return table;
        }
    }
}