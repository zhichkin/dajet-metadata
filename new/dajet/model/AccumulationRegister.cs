namespace DaJet
{
    internal sealed class AccumulationRegister : ChangeTrackingObject
    {
        internal static AccumulationRegister Create(Guid uuid, int code, string name)
        {
            return new AccumulationRegister(uuid, code, name);
        }
        internal AccumulationRegister(Guid uuid, int code, string name) : base(uuid, code, name) { }

        private int _AccumRgT;
        private int _AccumRgTn;
        private int _AccumRgOpt;
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.AccumRgT)
            {
                _AccumRgT = code;
            }
            else if (name == MetadataToken.AccumRgTn)
            {
                _AccumRgTn = code;
            }
            else if (name == MetadataToken.AccumRgOpt)
            {
                _AccumRgOpt = code;
            }
            else if (name == MetadataToken.AccumRgChngR)
            {
                _ChngR = code;
            }
        }
        internal string GetTableNameНастройки()
        {
            return string.Format("_{0}{1}", MetadataToken.AccumRgOpt, _AccumRgOpt);
        }
        internal string GetTableNameИтоги()
        {
            if (_AccumRgT > 0)
            {
                return string.Format("_{0}{1}", MetadataToken.AccumRgT, _AccumRgT);
            }
            else
            {
                return string.Format("_{0}{1}", MetadataToken.AccumRgTn, _AccumRgTn);
            }
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.AccumRgChngR, _ChngR);
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataName.AccumulationRegister, Name);
        }

        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                if (!registry.TryGetEntry(uuid, out AccumulationRegister metadata))
                {
                    return; //NOTE: сюда не предполагается попадать!
                }

                ConfigFileReader reader = new(file);

                // Идентификатор объекта метаданных - значение поля FileName в таблице Config
                //if (reader[2][14][2][2][3].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

                // Имя объекта метаданных конфигурации
                if (reader[2][14][2][3].Seek())
                {
                    string name = reader.ValueAsString;
                    metadata.Name = name;
                    registry.AddMetadataName(MetadataName.AccumulationRegister, in name, uuid);
                }

                //if (options.IsExtension)
                //{
                //    _converter[1][13][1][11] += Parent;
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