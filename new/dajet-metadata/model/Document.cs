using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    internal sealed class Document : ChangeTrackingObject
    {
        internal static Document Create(Guid uuid, int code, string name)
        {
            return new Document(uuid, code, name);
        }
        internal Document(Guid uuid, int code, string name) : base(uuid, code, name) { }
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.DocumentChngR)
            {
                _ChngR = code;
            }
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.DocumentChngR, _ChngR);
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataNames.Document, Name);
        }

        private static void AddRecorderToRegisters(ref ConfigFileReader reader, Guid document, in MetadataRegistry registry)
        {
            // 2.25.0 - корень коллекции (фигурная скобка)
            // 2.25.1 - UUID коллекции регистров движения !?
            // 2.25.2 - количество регистров движения
            // 2.25.N - описание регистров движения
            // 2.25.N.3.2 - uuid'ы регистров движения (file names)

            if (!reader.Read()) // [2][25][1] - Пропускаем
            {
                return; // Что-то пошло не так =)
            }

            // Количество регистров движения, по которым документ выполняет движения
            int count = reader[2][25][2].SeekNumber();

            if (count == 0)
            {
                return; // Документ не выполняет движений ни по одному регистру
            }

            count += 2; // Cмещение от корневого узла [2][25][0]

            for (uint i = 2; i < count; i++)
            {
                Guid register = reader[2][25][i + 1][3][2].SeekUuid();

                registry.AddRecorderToRegister(document, register);
            }
        }

        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                if (!registry.TryGetEntry(uuid, out Document metadata))
                {
                    return; //NOTE: сюда не предполагается попадать!
                }

                ConfigFileReader reader = new(file);

                // Идентификатор ссылочного типа данных, например, "ДокументСсылка.ЗаказКлиента"
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
                    registry.AddMetadataName(MetadataNames.Document, in name, uuid);
                }

                //if (options.IsExtension)
                //{
                //    _converter[1][9][1][9] += Parent;
                //}

                // Регистры движения документа (uuid'ы объектов метаданных)
                if (reader[2][25][ConfigFileToken.StartObject].Seek())
                {
                    AddRecorderToRegisters(ref reader, metadata.Uuid, in registry);
                }
            }
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry, bool relations)
            {
                EntityDefinition table = new();

                if (!registry.TryGetEntry(uuid, out Document entry))
                {
                    return null; // Идентификатор объекта не найден или не соответствует его типу
                }

                table.Name = entry.Name;
                table.DbName = entry.GetMainDbName();

                Configurator.ConfigurePropertyСсылка(in table, entry.TypeCode);
                Configurator.ConfigurePropertyВерсияДанных(in table);
                Configurator.ConfigurePropertyПометкаУдаления(in table);
                Configurator.ConfigurePropertyДата(in table);

                ConfigFileReader reader = new(file);

                //if (_cache != null && _cache.Extension != null) // 1.9.1.8 = 0 если заимствование отстутствует
                //{
                //    _converter[1][9][1][9] += Parent; // uuid расширяемого объекта метаданных
                //}

                // Тип номера документа (строка или число)
                NumberType numberType = (NumberType)reader[2][12].SeekNumber();

                // Длина номера документа
                int numberLength = reader[2][13].SeekNumber();

                // Периодичность даты документа
                Periodicity periodicity = (Periodicity)reader[2][14].SeekNumber();

                if (numberLength > 0)
                {
                    if (periodicity != Periodicity.None)
                    {
                        Configurator.ConfigurePropertyПериодНомера(in table);
                    }

                    Configurator.ConfigurePropertyНомер(in table, numberType, numberLength);
                }

                Configurator.ConfigurePropertyПроведён(in table);

                uint root = 4; // Коллекция табличных частей объекта

                if (reader[root][ConfigFileToken.StartObject].Seek())
                {
                    TablePart.Parse(ref reader, root, in table, entry, in registry, relations);
                }

                root = 6; // Коллекция свойств объекта

                uint[] vector = [root];

                if (reader[root][ConfigFileToken.StartObject].Seek())
                {
                    Property.Parse(ref reader, vector, in table, in registry, relations);
                }

                entry.ConfigureChangeTrackingTable(in table);

                Configurator.ConfigureSharedProperties(in registry, entry, in table);

                return table;
            }
        }
    }
}