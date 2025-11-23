namespace DaJet
{
    internal sealed class DefinedType : MetadataObject
    {
        internal DefinedType(Guid uuid) : base(uuid) { }
        internal DataType Type { get; set; }
        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataName.DefinedType, Name);
        }
        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                if (!registry.TryGetEntry(uuid, out DefinedType metadata))
                {
                    return; //NOTE: сюда не предполагается попадать!
                }

                ConfigFileReader reader = new(file);

                // Идентификатор ссылочного типа данных, например, "ОпределяемыйТипСсылка.ПриходныеДокументы"
                if (reader[2][2].Seek())
                {
                    Guid reference = reader.ValueAsUuid;
                    registry.AddDefinedType(uuid, reference);
                }

                // Идентификатор объекта метаданных - значение поля FileName в таблице Config
                //if (reader[2][4][2][3].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

                // Имя объекта метаданных конфигурации
                if (reader[2][4][3].Seek())
                {
                    string name = reader.ValueAsString;
                    metadata.Name = name;
                    registry.AddMetadataName(MetadataName.DefinedType, in name, uuid);
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