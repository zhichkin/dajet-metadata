using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    internal sealed class Enumeration : DatabaseObject
    {
        internal static Enumeration Create(Guid uuid, int code, string name)
        {
            return new Enumeration(uuid, code, name);
        }
        internal Enumeration(Guid uuid, int code, string name) : base(uuid, code, name) { }
        internal Dictionary<string, Guid> Values { get; } = new();
        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataNames.Enumeration, Name);
        }

        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                if (!registry.TryGetEntry(uuid, out Enumeration metadata))
                {
                    return; //NOTE: сюда не предполагается попадать!
                }

                ConfigFileReader reader = new(file);

                // Идентификатор ссылочного типа данных, например, "ПеречислениеСсылка.СтавкиНДС"
                if (reader[2][2].Seek())
                {
                    Guid reference = reader.ValueAsUuid;
                    registry.AddReference(uuid, reference);
                }

                // Идентификатор объекта метаданных - значение поля FileName в таблице Config
                //if (reader[2][6][2][2][3].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

                // Имя объекта метаданных конфигурации
                if (reader[2][6][2][3].Seek())
                {
                    string name = reader.ValueAsString;
                    metadata.Name = name;
                    registry.AddMetadataName(MetadataNames.Enumeration, in name, uuid);
                }

                // Значения перечисления расширения хранятся так же, как у заимствованного объекта
                // [6] += EnumerationValues (добавляются к значениям объекта основной конфигурации)
                //if (_cache != null && _cache.Extension != null) // 1.5.1.8 = 0 если заимствование отстутствует
                //{
                //    _converter[1][5][1][9] += Parent; // uuid расширяемого объекта метаданных
                //}

                if (reader[7][ConfigFileToken.StartObject].Seek())
                {
                    //Guid uuid = reader[7][1].SeekUuid();
                    int count = reader[7][2].SeekNumber(); // Количество значений перечисления

                    count += 3;

                    for (uint offset = 3; offset < count; offset++)
                    {
                        Guid value = reader[7][offset][1][2][2][3].SeekUuid();
                        string name = reader[7][offset][1][2][3].SeekString();
                        //string alias = reader[7][offset][0][1][3][2].SeekString();

                        _ = metadata.Values.TryAdd(name, value);
                    }
                }
            }
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry, bool relations)
            {
                throw new NotImplementedException();
            }
        }
    }
}