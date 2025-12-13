using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    internal sealed class DefinedType : MetadataObject
    {
        internal DefinedType(Guid uuid) : base(uuid) { }
        internal DataType Type { get; set; }
        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataNames.DefinedType, Name);
        }
        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                ConfigFileReader reader = new(file);

                // Идентификатор ссылочного типа данных, например, "ОпределяемыйТипСсылка.ПриходныеДокументы"
                Guid reference = reader[2][2].SeekUuid();

                // Идентификатор объекта метаданных - значение поля FileName в таблице Config
                Guid uuid = reader[2][4][2][3].SeekUuid();

                if (!registry.TryGetEntry(uuid, out DefinedType metadata))
                {
                    throw new InvalidOperationException();
                }

                registry.AddDefinedType(uuid, reference);

                // Имя объекта метаданных конфигурации
                if (reader[2][4][3].Seek())
                {
                    string name = reader.ValueAsString;
                    metadata.Name = name;
                    registry.AddMetadataName(MetadataNames.DefinedType, in name, uuid);
                }

                if (reader[2][5][ConfigFileToken.StartObject].Seek())
                {
                    uint[] root = [2, 5];

                    metadata.Type = DataTypeParser.Parse(ref reader, root, in registry, out _);
                }

                //if (options.IsExtension)
                //{
                //    _converter[1][3][11] += Parent;
                //}
            }
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry, bool relations)
            {
                throw new NotImplementedException();
            }
        }
    }
}