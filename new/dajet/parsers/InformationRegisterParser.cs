namespace DaJet
{
    internal sealed class InformationRegisterParser : ConfigFileParser
    {
        internal override Type Type => typeof(InformationRegister);
        internal override void Parse(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
        {
            if (!registry.TryGetEntry(uuid, out InformationRegister metadata))
            {
                metadata = new InformationRegister(uuid); //NOTE: сюда не предполагается попадать!
            }

            ConfigFileReader reader = new(file);

            // Идентификатор объекта метаданных - значение поля FileName в таблице Config
            //if (reader[2][16][2][2][3].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

            // Имя объекта метаданных конфигурации
            if (reader[2][16][2][3].Seek())
            {
                string name = reader.ValueAsString;
                metadata.Name = name;
                registry.AddMetadataName(MetadataName.InformationRegister, in name, uuid);
            }

            //if (options.IsExtension)
            //{
            //    _converter[1][15][1][13] += Parent;
            //}
        }
    }
}