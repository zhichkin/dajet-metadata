namespace DaJet
{
    internal sealed class AccountingRegisterParser : ConfigFileParser
    {
        internal override Type Type => typeof(AccountingRegister);
        internal override void Parse(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
        {
            if (!registry.TryGetEntry(uuid, out AccountingRegister metadata))
            {
                metadata = new AccountingRegister(uuid); //NOTE: сюда не предполагается попадать!
            }

            ConfigFileReader reader = new(file);

            // Идентификатор объекта метаданных - значение поля FileName в таблице Config
            //if (reader[2][16][2][2][3].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

            // Имя объекта метаданных конфигурации
            if (reader[2][16][2][3].Seek())
            {
                string name = reader.ValueAsString;
                metadata.Name = name;
                registry.AddMetadataName(MetadataName.AccountingRegister, in name, uuid);
            }
        }
    }
}