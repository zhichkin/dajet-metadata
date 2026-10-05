using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    internal sealed class DefinedType : MetadataObject
    {
        internal static DefinedType Create(Guid uuid)
        {
            return new DefinedType(uuid);
        }
        internal DefinedType(Guid uuid) : base(uuid) { }
        internal DataType Type { get; set; }
        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataNames.DefinedType, Name);
        }
        internal override string GetMainDbName()
        {
            throw new NotImplementedException();
        }
        internal override string GetTableNameИзменения()
        {
            throw new NotImplementedException();
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
                metadata.Name = reader[2][4][3].SeekString();

                if (metadata.IsExtension) // Объект расширения
                {
                    if (registry.TryGetEntry(MetadataNames.DefinedType, metadata.Name, out DefinedType parent))
                    {
                        // Заимствованный объект расширения
                        metadata.IsBorrowed = true;
                        registry.AddBorrowed(parent.Uuid, metadata.Uuid);
                    }
                    else // Cобственный объект расширения
                    {
                        registry.AddMetadataName(MetadataNames.DefinedType, metadata.Name, uuid);
                    }
                }
                else // Объект основной конфигурации
                {
                    registry.AddMetadataName(MetadataNames.DefinedType, metadata.Name, uuid);
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
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                if (!registry.TryGetEntry(uuid, out DefinedType entry))
                {
                    throw new InvalidOperationException($"Metadata object \"ОпределяемыйТип\" not found by UUID {{{uuid}}}");
                }

                ConfigFileReader reader = new(file);

                EntityDefinition entity = new()
                {
                    Uuid = entry.Uuid,
                    Name = entry.Name,
                    DbName = "ОпределяемыйТип" // Определяемый тип не имеет таблиц для хранения данных
                };

                PropertyDefinition property = new()
                {
                    Name = "Type"
                };

                entity.Properties.Add(property);

                if (reader[2][5][ConfigFileToken.StartObject].Seek())
                {
                    uint[] root = [2, 5];

                    property.Type = DataTypeParser.Parse(ref reader, root, in registry, out List<Guid> references);

                    property.References = references;
                }
                
                return entity;
            }
        }
    }
}