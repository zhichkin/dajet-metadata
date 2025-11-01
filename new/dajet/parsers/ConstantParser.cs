namespace DaJet
{
    internal sealed class ConstantParser : ConfigFileParser
    {
        internal override Type Type => typeof(Constant);
        internal override void Parse(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
        {
            if (!registry.TryGetEntry(uuid, out Constant metadata))
            {
                metadata = new Constant(uuid); //NOTE: сюда не предполагается попадать!
            }

            ConfigFileReader reader = new(file);

            // Идентификатор объекта метаданных - значение поля FileName в таблице Config
            //if (reader[2][2][2][2][2][3].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

            // Имя объекта метаданных конфигурации
            if (reader[2][2][2][2][3].Seek())
            {
                string name = reader.ValueAsString;
                metadata.Name = name;
                registry.AddMetadataName(MetadataName.Constant, in name, uuid);
            }

            //if (options.IsExtension)
            //{
            //    _converter[1][1][1][1][11] += Parent; // uuid расширяемого объекта метаданных
            //}
        }
    }
}