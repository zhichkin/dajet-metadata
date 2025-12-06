using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    internal sealed class BusinessTask : ChangeTrackingObject
    {
        internal static BusinessTask Create(Guid uuid, int code, string name)
        {
            return new BusinessTask(uuid, code, name);
        }
        internal BusinessTask(Guid uuid, int code, string name) : base(uuid, code, name) { }
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.TaskChngR)
            {
                _ChngR = code;
            }
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.TaskChngR, _ChngR);
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataNames.BusinessTask, Name);
        }

        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                if (!registry.TryGetEntry(uuid, out BusinessTask metadata))
                {
                    return; //NOTE: сюда не предполагается попадать!
                }

                ConfigFileReader reader = new(file);

                // Идентификатор объекта метаданных - значение поля FileName в таблице Config
                //if (reader[2][2][2][3].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

                // Имя объекта метаданных конфигурации
                if (reader[2][2][3].Seek())
                {
                    string name = reader.ValueAsString;
                    metadata.Name = name;
                    registry.AddMetadataName(MetadataNames.BusinessTask, in name, uuid);
                }

                // Идентификатор ссылочного типа данных, например, "ЗадачаСсылка.Задача"
                if (reader[2][6].Seek())
                {
                    Guid reference = reader.ValueAsUuid;
                    registry.AddReference(uuid, reference);
                }

                //if (options.IsExtension)
                //{
                //    _converter[1][1][9] += Parent;
                //}
            }
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry, bool relations)
            {
                if (!registry.TryGetEntry(uuid, out BusinessTask entry))
                {
                    return null; // Идентификатор объекта не найден или не соответствует его типу
                }

                EntityDefinition table = new();

                table.Name = entry.Name;
                table.DbName = entry.GetMainDbName();

                Configurator.ConfigurePropertyСсылка(in table, entry.TypeCode);
                Configurator.ConfigurePropertyВерсияДанных(in table);
                Configurator.ConfigurePropertyПометкаУдаления(in table);
                Configurator.ConfigurePropertyДата(in table);

                ConfigFileReader reader = new(file);

                // Тип номера (строка или число)
                NumberType numberType = (NumberType)reader[2][19].SeekNumber();

                // Длина номера
                int numberLength = reader[2][20].SeekNumber();

                // Длина имени
                int nameLength = reader[2][23].SeekNumber();

                //_converter[1][25] += RoutingTable; // Идентификатор регистра сведений, используемого для адресации задачи
                //_converter[1][26] += MainRoutingProperty; // Основной реквизит адресации задачи

                if (numberLength > 0)
                {
                    Configurator.ConfigurePropertyНомер(in table, numberType, numberLength);
                }

                if (nameLength > 0)
                {
                    Configurator.ConfigurePropertyИмя(in table, nameLength);
                }

                Configurator.ConfigurePropertyВыполнена(in table);

                //TODO: Поиск бизнес-процессов в расширении не реализован
                //NOTE: Необходимо заполнить коллекцию _tasks провайдера метаданных расширения

                Configurator.ConfigurePropertyБизнесПроцесс(in table, uuid, in registry);
                Configurator.ConfigurePropertyТочкаМаршрута(in table, uuid, in registry);

                uint[] root = [6]; // Коллекция реквизитов

                if (reader[root][ConfigFileToken.StartObject].Seek())
                {
                    Property.Parse(ref reader, root, in table, in registry, relations);
                }

                root[0] = 7; // Коллекция реквизитов адресации

                if (reader[root][ConfigFileToken.StartObject].Seek())
                {
                    Property.Parse(ref reader, root, in table, in registry, relations);
                }

                uint offset = 8; // Коллекция табличных частей объекта

                if (reader[offset][ConfigFileToken.StartObject].Seek())
                {
                    TablePart.Parse(ref reader, offset, in table, entry, in registry, relations);
                }

                Configurator.ConfigureSharedProperties(in registry, entry, in table);

                return table;
            }
        }
    }
}