namespace DaJet
{
    internal sealed class CatalogParser : ConfigFileParser
    {
        internal override void Parse(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
        {
            if (!registry.TryGetEntry(uuid, out Catalog metadata))
            {
                return; //NOTE: сюда не предполагается попадать!
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

        internal override TableDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
        {
            TableDefinition table = new();

            if (!registry.TryGetEntry(uuid, out Catalog entry))
            {
                return null; // Идентификатор объекта не найден или не соответствует его типу
            }

            // Коллекция владельцев данного справочника (uuid'ы объектов метаданных)
            //_converter[1][12] += Owners;

            table.Name = entry.Name;
            table.DbName = entry.GetMainDbName();

            ConfigFileReader reader = new(file);

            int codeLength = reader[2][18].SeekNumber();
            CodeType codeType = (CodeType)reader[2][19].SeekNumber();
            int nameLength = reader[2][20].SeekNumber();
            HierarchyType hierarchyType = (HierarchyType)reader[2][37].SeekNumber();
            bool isHierarchical = reader[2][28].SeekNumber() != 0;

            if (isHierarchical && hierarchyType == HierarchyType.Groups)
            {
                Configurator.ConfigurePropertyЭтоГруппа(in table);
            }

            Configurator.ConfigurePropertyСсылка(in table, entry.TypeCode);
            Configurator.ConfigurePropertyПометкаУдаления(in table);

            //List<Guid> owners = cache.GetCatalogOwners(catalog.Uuid);

            //if (owners != null && owners.Count > 0)
            //{
            //    ConfigurePropertyВладелец(in cache, in catalog, in owners);
            //}

            if (isHierarchical)
            {
                //TODO: Configurator.ConfigurePropertyРодитель(in table);
            }

            if (codeLength > 0)
            {
                Configurator.ConfigurePropertyКод(in table, codeType, codeLength);
            }

            if (nameLength > 0)
            {
                Configurator.ConfigurePropertyНаименование(in table, nameLength);
            }

            //TODO: Configurator.ConfigurePropertyПредопределённый(in table, in infoBase);

            Configurator.ConfigurePropertyВерсияДанных(in table);

            //_converter[5] += TablePartCollection; // 932159f9-95b2-4e76-a8dd-8849fe5c5ded - идентификатор коллекции табличных частей
            //_converter[6] += PropertyCollection; // cf4abea7-37b2-11d4-940f-008048da11f9 - идентификатор коллекции реквизитов

            //TODO: Добавить общие реквизиты

            return table;
        }
    }
}