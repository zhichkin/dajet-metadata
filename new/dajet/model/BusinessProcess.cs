namespace DaJet
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
            return string.Format("{0}.{1}", MetadataName.BusinessProcess, Name);
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
                    registry.AddMetadataName(MetadataName.BusinessProcess, in name, uuid);
                }

                // Идентификатор ссылочного типа данных, например, "БизнесПроцессСсылка.Согласование"
                if (reader[2][6].Seek())
                {
                    Guid reference = reader.ValueAsUuid;
                    registry.AddReference(uuid, reference);
                }

                //_converter[1][25] += BusinessTask; // Ссылка на объект метаданных "Задача", используемый бизнес-процессом

                //if (options.IsExtension)
                //{
                //    _converter[1][1][9] += Parent;
                //}
            }
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry, bool relations = false)
            {
                EntityDefinition table = new();

                return table;
            }
        }
    }
}