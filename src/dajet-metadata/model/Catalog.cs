using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    internal sealed class Catalog : ChangeTrackingObject
    {
        internal static Catalog Create(Guid uuid, int code)
        {
            return new Catalog(uuid, code, MetadataToken.Reference);
        }
        internal Catalog(Guid uuid, int code, string name) : base(uuid, code, name) { }
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.ReferenceChngR)
            {
                _ChngR = code;
            }
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.ReferenceChngR, _ChngR);
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataNames.Catalog, Name);
        }

        private static Guid[] GetOwners(ref ConfigFileReader reader)
        {
            // 2.13.0 - корень коллекции (фигурная скобка)
            // 2.13.1 - UUID коллекции владельцев справочника !?
            // 2.13.2 - количество владельцев справочника
            // 2.13.N - описание владельцев
            // 2.13.N.3.2 - uuid'ы владельцев (file names)

            if (!reader[2][13][ConfigFileToken.StartObject].Seek())
            {
                return null;
            }

            if (!reader.Read()) // [2][13][1] - Пропускаем
            {
                return null; // Что-то пошло не так =)
            }

            // Количество владельцев справочника
            int count = reader[2][13][2].SeekNumber();

            if (count == 0)
            {
                return null; // Данный справочник не имеет владельцев
            }

            Guid[] owners = new Guid[count];

            uint offset = 3; // Cмещение от корневого узла [2][13][0]

            for (uint i = 0; i < owners.Length; i++)
            {
                owners[i] = reader[2][13][i + offset][3][2].SeekUuid();
            }

            return owners;
        }

        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                ConfigFileReader reader = new(file);

                // Идентификатор ссылочного типа данных, например, "СправочникСсылка.Номенклатура"
                Guid reference = reader[2][4].SeekUuid();

                // Идентификатор объекта метаданных - значение поля FileName в таблице Config
                Guid uuid = reader[2][10][2][2][3].SeekUuid();

                if (!registry.TryGetEntry(uuid, out Catalog metadata))
                {
                    throw new InvalidOperationException();
                }

                registry.AddReference(uuid, reference);

                // Имя объекта метаданных конфигурации
                metadata.Name = reader[2][10][2][3].SeekString();

                if (metadata.TypeCode > 0)
                {
                    // Объекты основной конфигурации и собственные объекты расширения
                    registry.AddMetadataName(MetadataNames.Catalog, metadata.Name, uuid);
                }
                else // Заимствованный объект расширения
                {
                    //if (registry.CompatibilityVersion >= 80314)
                    //{
                    //    // Заимствованный объект
                    //    uuid = reader[2][10][2][10].SeekUuid();

                    //    if (!registry.TryGetEntry(uuid, out Catalog parent))
                    //    {
                    //        throw new InvalidOperationException();
                    //    }
                    //}

                    if (registry.TryGetEntry(MetadataNames.Catalog, metadata.Name, out Catalog parent))
                    {
                        parent.MarkAsBorrowed();
                        metadata.MarkAsBorrowed();
                        metadata.TypeCode = parent.TypeCode;
                        registry.AddExtension(parent.Uuid, metadata.Uuid);
                    }
                }
            }
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry, bool relations)
            {
                if (!registry.TryGetEntry(uuid, out Catalog entry))
                {
                    return null; // Идентификатор объекта не найден или не соответствует его типу
                }

                EntityDefinition table = new();

                table.Name = entry.Name;
                table.DbName = entry.GetMainDbName();

                ConfigFileReader reader = new(file);

                if (!entry.IsExtension || !entry.IsBorrowed)
                {
                    // Объект основной конфигурации или собственный объект расширения

                    Configurator.ConfigurePropertyСсылка(in table, entry.TypeCode);
                    Configurator.ConfigurePropertyВерсияДанных(in table);
                    Configurator.ConfigurePropertyПометкаУдаления(in table);

                    // Коллекция владельцев данного справочника (uuid'ы объектов метаданных)
                    Guid[] owners = GetOwners(ref reader); // [2][13][0]

                    // Длина кода
                    int codeLength = reader[2][18].SeekNumber();

                    // Тип кода (строка или число)
                    CodeType codeType = (CodeType)reader[2][19].SeekNumber();

                    // Длина наименования
                    int nameLength = reader[2][20].SeekNumber();

                    // Тип иерархии (группы или элементы)
                    HierarchyType hierarchyType = (HierarchyType)reader[2][37].SeekNumber();

                    // Флаг, является ли справочник иерархическим
                    bool isHierarchical = reader[2][38].SeekNumber() != 0;

                    if (codeLength > 0)
                    {
                        Configurator.ConfigurePropertyКод(in table, codeType, codeLength);
                    }

                    if (nameLength > 0)
                    {
                        Configurator.ConfigurePropertyНаименование(in table, nameLength);
                    }

                    if (owners is not null && owners.Length > 0)
                    {
                        int ownerCode = 0;

                        if (owners.Length == 1)
                        {
                            ownerCode = registry.GetTypeCode(owners[0]);
                        }

                        Configurator.ConfigurePropertyВладелец(in table, in owners, ownerCode);
                    }

                    if (isHierarchical)
                    {
                        Configurator.ConfigurePropertyРодитель(in table, entry.TypeCode);
                    }

                    if (isHierarchical && hierarchyType == HierarchyType.Groups)
                    {
                        Configurator.ConfigurePropertyЭтоГруппа(in table);
                    }

                    Configurator.ConfigurePropertyПредопределённый(in table, false, registry.CompatibilityVersion);
                }

                uint root = 6; // Коллекция табличных частей объекта

                if (reader[root][ConfigFileToken.StartObject].Seek())
                {
                    TablePart.Parse(ref reader, root, in table, entry, in registry, relations);
                }

                root = 7; // Коллекция свойств объекта

                uint[] vector = [root];

                if (reader[root][ConfigFileToken.StartObject].Seek())
                {
                    Property.Parse(ref reader, vector, in table, in registry, relations);
                }

                Configurator.ConfigureSharedProperties(in registry, entry, in table);

                entry.ConfigureChangeTrackingTable(in table);

                return table;
            }
        }
    }
}