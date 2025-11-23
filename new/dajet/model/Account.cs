namespace DaJet
{
    internal sealed class Account : ChangeTrackingObject
    {
        internal static Account Create(Guid uuid, int code, string name)
        {
            return new Account(uuid, code, name);
        }
        internal Account(Guid uuid, int code, string name) : base(uuid, code, name) { }
        
        private int _ExtDim;
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.ExtDim)
            {
                _ExtDim = code;
            }
            else if (name == MetadataToken.AccChngR)
            {
                _ChngR = code;
            }
        }
        internal string GetTableNameВидыСубконто()
        {
            return string.Format("_{0}{1}_{2}{3}", DbName, TypeCode, MetadataToken.ExtDim, _ExtDim);
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.AccChngR, _ChngR);
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataName.Account, Name);
        }

        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                if (!registry.TryGetEntry(uuid, out Account metadata))
                {
                    return; //NOTE: сюда не предполагается попадать!
                }

                ConfigFileReader reader = new(file);

                // Идентификатор ссылочного типа данных, например, "ПланСчетовСсылка.Управленческий"
                if (reader[2][4].Seek())
                {
                    Guid reference = reader.ValueAsUuid;
                    registry.AddReference(uuid, reference);
                }

                // Идентификатор объекта метаданных - значение поля FileName в таблице Config
                //if (reader[2][16][2][2][3].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

                // Имя объекта метаданных конфигурации
                if (reader[2][16][2][3].Seek())
                {
                    string name = reader.ValueAsString;
                    metadata.Name = name;
                    registry.AddMetadataName(MetadataName.Account, in name, uuid);
                }
            }
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry, bool relations = false)
            {
                EntityDefinition table = new();

                return table;
            }
        }
    }
}