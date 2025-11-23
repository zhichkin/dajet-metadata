namespace DaJet
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
            return string.Format("{0}.{1}", MetadataName.BusinessTask, Name);
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
                    registry.AddMetadataName(MetadataName.BusinessTask, in name, uuid);
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
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry, bool relations = false)
            {
                EntityDefinition table = new();

                return table;
            }
        }
    }
}