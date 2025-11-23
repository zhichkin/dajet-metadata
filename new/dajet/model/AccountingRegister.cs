namespace DaJet
{
    internal sealed class AccountingRegister : ChangeTrackingObject
    {
        internal static AccountingRegister Create(Guid uuid, int code, string name)
        {
            return new AccountingRegister(uuid, code, name);
        }
        internal AccountingRegister(Guid uuid, int code, string name) : base(uuid, code, name) { }
        
        private int _AccRgED;
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.AccRgED)
            {
                _AccRgED = code;
            }
            else if (name == MetadataToken.AccRgChngR)
            {
                _ChngR = code;
            }
        }
        internal string GetTableNameЗначенияСубконто()
        {
            return string.Format("_{0}{1}", MetadataToken.AccRgED, _AccRgED);
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.AccRgChngR, _ChngR);
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataName.AccountingRegister, Name);
        }

        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                if (!registry.TryGetEntry(uuid, out AccountingRegister metadata))
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
                    registry.AddMetadataName(MetadataName.AccountingRegister, in name, uuid);
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