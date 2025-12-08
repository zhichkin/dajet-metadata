using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    internal sealed class BusinessProcess : ChangeTrackingObject
    {
        internal static BusinessProcess Create(Guid uuid, int code, string name)
        {
            return new BusinessProcess(uuid, code, name);
        }
        internal BusinessProcess(Guid uuid, int code, string name) : base(uuid, code, name) { }

        private int _BPrPoints;
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.BPrPoints)
            {
                _BPrPoints = code;
            }
            else if (name == MetadataToken.BPrChngR)
            {
                _ChngR = code;
            }
        }
        internal string GetTableNameТочкиМаршрута()
        {
            return string.Format("_{0}{1}", MetadataToken.BPrPoints, _BPrPoints);
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.BPrChngR, _ChngR);
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataNames.BusinessProcess, Name);
        }

        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                if (!registry.TryGetEntry(uuid, out BusinessProcess metadata))
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
                    registry.AddMetadataName(MetadataNames.BusinessProcess, in name, uuid);
                }

                // Идентификатор ссылочного типа данных, например, "БизнесПроцессСсылка.Согласование"
                if (reader[2][6].Seek())
                {
                    Guid reference = reader.ValueAsUuid;
                    registry.AddReference(uuid, reference);
                }

                // Ссылка на объект метаданных "Задача", используемый бизнес-процессом
                if (reader[2][26].Seek())
                {
                    Guid task = reader.ValueAsUuid;
                    registry.AddBusinessProcessToTask(task, uuid);
                }

                //if (options.IsExtension)
                //{
                //    _converter[1][1][9] += Parent;
                //}
            }
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry, bool relations)
            {
                if (!registry.TryGetEntry(uuid, out BusinessProcess entry))
                {
                    return null; // Идентификатор объекта не найден или не соответствует его типу
                }

                //TODO: Зарегистрировать точки маршрута бизнес-процесса в реестре метаданных
                //NOTE: Могут использоваться в качестве значений реквизитов объектов (ссылка)

                EntityDefinition table = new();

                table.Name = entry.Name;
                table.DbName = entry.GetMainDbName();

                Configurator.ConfigurePropertyСсылка(in table, entry.TypeCode);
                Configurator.ConfigurePropertyВерсияДанных(in table);
                Configurator.ConfigurePropertyПометкаУдаления(in table);
                Configurator.ConfigurePropertyДата(in table);

                ConfigFileReader reader = new(file);

                // Тип номера (строка или число)
                NumberType numberType = (NumberType)reader[2][17].SeekNumber();

                // Периодичность даты документа
                Periodicity periodicity = (Periodicity)reader[2][18].SeekNumber();
                
                // Длина номера
                int numberLength = reader[2][19].SeekNumber();

                if (numberLength > 0)
                {
                    if (periodicity != Periodicity.None)
                    {
                        Configurator.ConfigurePropertyПериодНомера(in table);
                    }

                    Configurator.ConfigurePropertyНомер(in table, numberType, numberLength);
                }

                // Ведущая задача бизнес-процесса
                if (reader[2][26].Seek())
                {
                    Guid task = reader.ValueAsUuid;
                    Configurator.ConfigurePropertyВедущаяЗадача(in table, task, in registry);
                }

                Configurator.ConfigurePropertyСтартован(in table);
                Configurator.ConfigurePropertyЗавершён(in table);

                uint[] root = [7]; // Коллекция реквизитов

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

                entry.ConfigureChangeTrackingTable(in table);

                return table;
            }
        }
    }
}