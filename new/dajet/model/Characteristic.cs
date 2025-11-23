namespace DaJet
{
    internal sealed class Characteristic : ChangeTrackingObject
    {
        internal static Characteristic Create(Guid uuid, int code, string name)
        {
            return new Characteristic(uuid, code, name);
        }
        internal Characteristic(Guid uuid, int code, string name) : base(uuid, code, name) { }
        internal DataType Type { get; set; }
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.ChrcChngR)
            {
                _ChngR = code;
            }
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.ChrcChngR, _ChngR);
        }
        
        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataName.Characteristic, Name);
        }

        internal static void InitializeDataType(Guid uuid, ReadOnlySpan<byte> file, MetadataRegistry registry)
        {
            if (!registry.TryGetEntry(uuid, out Characteristic metadata))
            {
                return; //NOTE: сюда не предполагается попадать!
            }

            ConfigFileReader reader = new(file);

            if (reader[2][19][ConfigFileToken.StartObject].Seek())
            {
                uint[] root = [2, 19];

                metadata.Type = DataTypeParser.Parse(ref reader, root, in registry, out _);
            }
        }

        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                if (!registry.TryGetEntry(uuid, out Characteristic metadata))
                {
                    return; //NOTE: сюда не предполагается попадать!
                }

                ConfigFileReader reader = new(file);

                // Идентификатор ссылочного типа данных, например, "ПланВидовХарактеристикСсылка.ВидыСубконтоХозрасчетные"
                if (reader[2][4].Seek())
                {
                    Guid reference = reader.ValueAsUuid;
                    registry.AddReference(uuid, reference);
                }

                // Идентификатор характеристики, например, "Характеристика.ВидыСубконтоХозрасчетные" (опеределение типа данных свойства)
                if (reader[2][10].Seek())
                {
                    Guid characteristic = reader.ValueAsUuid;
                    registry.AddCharacteristic(uuid, characteristic);
                }

                // Идентификатор объекта метаданных - значение поля FileName в таблице Config
                //if (reader[2][14][2][2][3].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

                // Имя объекта метаданных конфигурации
                if (reader[2][14][2][3].Seek())
                {
                    string name = reader.ValueAsString;
                    metadata.Name = name;
                    registry.AddMetadataName(MetadataName.Characteristic, in name, uuid);
                }

                //if (options.IsExtension)
                //{
                //    _converter[1][13][1][11] += Parent;
                //}
            }
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry, bool relations)
            {
                EntityDefinition table = new();

                return table;
            }
        }
    }
}