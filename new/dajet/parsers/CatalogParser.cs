namespace DaJet
{
    internal sealed class CatalogParser : ConfigFileParser
    {
        internal override Type Type => typeof(Catalog);
        internal override void Parse(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
        {
            if (!registry.TryGetEntry(uuid, out Catalog metadata))
            {
                metadata = new Catalog(uuid); //NOTE: сюда не предполагается попадать!
            }

            ConfigFileReader reader = new(file);

            // Идентификатор ссылочного типа данных, например, "СправочникСсылка.Номенклатура"
            if (reader[2][4].Seek())
            {
                Guid reference = reader.ValueAsUuid;
                registry.AddReference(uuid, reference);
            }

            // Идентификатор объекта метаданных - значение поля FileName в таблице Config
            //if (reader[2][10][2][2][3].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

            // Имя объекта метаданных конфигурации
            if (reader[2][10][2][3].Seek())
            {
                string name = reader.ValueAsString;
                metadata.Name = name;
                registry.AddMetadataName(MetadataName.Catalog, in name, uuid);
            }

            //if (options.IsExtension)
            //{
            //    _converter[1][9][1][9] += Parent;
            //}

            // Коллекция владельцев данного справочника (uuid'ы объектов метаданных)
            //_converter[1][12] += Owners;
        }
        private void Owners(ref ConfigFileReader source)
        {
            // 1.12.0 - UUID коллекции владельцев справочника !?
            // 1.12.1 - количество владельцев справочника
            // 1.12.N - описание владельцев
            // 1.12.N.2.1 - uuid'ы владельцев (file names)

            _ = source.Read(); // [1][12][0] - UUID коллекции владельцев справочника
            _ = source.Read(); // [1][12][1] - количество владельцев справочника

            int count = source.ValueAsNumber;

            if (count == 0)
            {
                return;
            }

            int offset = 2; // начальный индекс N [1][12][2]

            for (int n = 0; n < count; n++)
            {
                //_converter[1][12][offset + n][2][1] += OwnerUuid;
            }
        }
        private void OwnerUuid(ref ConfigFileReader source)
        {
            //if (_entry != null)
            //{
            //    _entry.CatalogOwners.Add(source.GetUuid());
            //}
        }
    }
}