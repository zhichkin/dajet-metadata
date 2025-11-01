namespace DaJet
{
    internal sealed class BusinessProcessParser : ConfigFileParser
    {
        internal override Type Type => typeof(BusinessProcess);
        internal override void Parse(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
        {
            if (!registry.TryGetEntry(uuid, out BusinessProcess metadata))
            {
                metadata = new BusinessProcess(uuid); //NOTE: сюда не предполагается попадать!
            }

            ConfigFileReader reader = new(file);

            // Идентификатор объекта метаданных - значение поля FileName в таблице Config
            //if (reader[2][2][2][3].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

            // Имя объекта метаданных конфигурации
            if (reader[2][2][3].Seek())
            {
                string name = reader.ValueAsString;
                metadata.Name = name;
                registry.AddMetadataName(MetadataName.BusinessProcess, in name, uuid);
            }

            // Идентификатор ссылочного типа данных, например, "БизнесПроцессСсылка.Согласование"
            if (reader[2][6].Seek())
            {
                Guid reference = reader.ValueAsUuid;
                registry.AddReference(uuid, reference);
            }

            //_converter[1][25] += BusinessTask; // Ссылка на объект метаданных "Задача", используемый бизнес-процессом

            //if (options.IsExtension)
            //{
            //    _converter[1][1][9] += Parent;
            //}
        }
    }
}