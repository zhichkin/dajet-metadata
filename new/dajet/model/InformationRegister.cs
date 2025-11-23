namespace DaJet
{
    internal sealed class InformationRegister : ChangeTrackingObject
    {
        internal static InformationRegister Create(Guid uuid, int code, string name)
        {
            return new InformationRegister(uuid, code, name);
        }
        internal InformationRegister(Guid uuid, int code, string name) : base(uuid, code, name) { }

        private int _InfoRgSF;
        private int _InfoRgSL;
        private int _InfoRgOpt;
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.InfoRgOpt)
            {
                _InfoRgOpt = code;
            }
            else if (name == MetadataToken.InfoRgSF)
            {
                _InfoRgSF = code;
            }
            else if (name == MetadataToken.InfoRgSL)
            {
                _InfoRgSL = code;
            }
            else if (name == MetadataToken.InfoRgChngR)
            {
                _ChngR = code;
            }
        }
        internal string GetTableNameНастройки()
        {
            return string.Format("_{0}{1}", MetadataToken.InfoRgOpt, _InfoRgOpt);
        }
        internal string GetTableNameСрезПервых()
        {
            return string.Format("_{0}{1}", MetadataToken.InfoRgSF, _InfoRgSF);
        }
        internal string GetTableNameСрезПоследних()
        {
            return string.Format("_{0}{1}", MetadataToken.InfoRgSL, _InfoRgSL);
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.InfoRgChngR, _ChngR);
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataName.InformationRegister, Name);
        }

        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                if (!registry.TryGetEntry(uuid, out InformationRegister metadata))
                {
                    return; //NOTE: сюда не предполагается попадать!
                }

                ConfigFileReader reader = new(file);

                // Идентификатор объекта метаданных - значение поля FileName в таблице Config
                //if (reader[2][16][2][2][3].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

                // Имя объекта метаданных конфигурации
                if (reader[2][16][2][3].Seek())
                {
                    string name = reader.ValueAsString;
                    metadata.Name = name;
                    registry.AddMetadataName(MetadataName.InformationRegister, in name, uuid);
                }

                //if (options.IsExtension)
                //{
                //    _converter[1][15][1][13] += Parent;
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