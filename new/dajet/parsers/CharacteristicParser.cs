namespace DaJet
{
    internal sealed class CharacteristicParser : ConfigFileParser
    {
        internal override Type Type => typeof(Characteristic);
        internal override void Parse(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
        {
            if (!registry.TryGetEntry(uuid, out Characteristic metadata))
            {
                metadata = new Characteristic(uuid); //NOTE: сюда не предполагается попадать!
            }

            ConfigFileReader reader = new(file);

            // Идентификатор ссылочного типа данных, например, "ПланВидовХарактеристикСсылка.ВидыСубконтоХозрасчетные"
            if (reader[2][4].Seek())
            {
                Guid reference = reader.ValueAsUuid;
                registry.AddReference(uuid, reference);
            }

            // Идентификатор характеристики, например, "Характеристика.ВидыСубконтоХозрасчетные" (опеределение типа данных свойства)
            if (reader[2][10].Seek())
            {
                Guid characteristic = reader.ValueAsUuid;
                registry.AddCharacteristic(uuid, characteristic);
            }

            // Идентификатор объекта метаданных - значение поля FileName в таблице Config
            //if (reader[2][14][2][2][3].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

            // Имя объекта метаданных конфигурации
            if (reader[2][14][2][3].Seek())
            {
                string name = reader.ValueAsString;
                metadata.Name = name;
                registry.AddMetadataName(MetadataName.Characteristic, in name, uuid);
            }

            //if (options.IsExtension)
            //{
            //    _converter[1][13][1][11] += Parent;
            //}
        }
    }
}