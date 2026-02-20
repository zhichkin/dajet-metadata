using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    internal sealed class Enumeration : MetadataObject
    {
        internal static Enumeration Create(Guid uuid)
        {
            return new Enumeration(uuid);
        }
        internal Enumeration(Guid uuid) : base(uuid) { }
        internal Dictionary<string, Guid> Values { get; } = new();
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.Enum)
            {
                Code = code;
            }
        }
        internal override string GetMainDbName()
        {
            return string.Format("_{0}{1}", MetadataToken.Enum, Code);
        }
        internal override string GetTableNameИзменения()
        {
            throw new NotImplementedException();
        }
        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataNames.Enumeration, Name);
        }

        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                ConfigFileReader reader = new(file);

                // Идентификатор ссылочного типа данных, например, "ПеречислениеСсылка.СтавкиНДС"
                Guid reference = reader[2][2].SeekUuid();

                // Идентификатор объекта метаданных - значение поля FileName в таблице Config
                Guid uuid = reader[2][6][2][2][3].SeekUuid();

                if (!registry.TryGetEntry(uuid, out Enumeration metadata))
                {
                    throw new InvalidOperationException();
                }

                registry.AddReference(uuid, reference);

                // Имя объекта метаданных конфигурации
                metadata.Name = reader[2][6][2][3].SeekString();

                if (metadata.Code > 0)
                {
                    // Объекты основной конфигурации и собственные объекты расширения
                    registry.AddMetadataName(MetadataNames.Enumeration, metadata.Name, uuid);

                    if (metadata.IsExtension) // Собственный объект расширения
                    {
                        registry.SetGenericExtensionFlag(GenericExtensionFlags.Enumeration);
                    }
                }
                else // Заимствованный объект расширения
                {
                    if (registry.TryGetEntry(MetadataNames.Enumeration, metadata.Name, out Enumeration parent))
                    {
                        metadata.IsBorrowed = true;
                        metadata.Code = parent.Code;
                        registry.AddBorrowed(parent.Uuid, metadata.Uuid);
                    }
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
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                throw new NotImplementedException();
            }
        }
    }
}