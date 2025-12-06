using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    internal sealed class Constant : ChangeTrackingObject
    {
        internal static Constant Create(Guid uuid, int code, string name)
        {
            return new Constant(uuid, code, name);
        }
        internal Constant(Guid uuid, int code, string name) : base(uuid, code, name) { }

        private int _Fld;
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.Fld)
            {
                _Fld = code;
            }
            else if (name == MetadataToken.ConstChngR)
            {
                _ChngR = code;
            }
        }
        internal string GetColumnNameЗначение()
        {
            return string.Format("_{0}{1}", MetadataToken.Fld, _Fld);
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.ConstChngR, _ChngR);
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataNames.Constant, Name);
        }

        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                if (!registry.TryGetEntry(uuid, out Constant metadata))
                {
                    return; //NOTE: сюда не предполагается попадать!
                }

                ConfigFileReader reader = new(file);

                // Идентификатор объекта метаданных - значение поля FileName в таблице Config
                //if (reader[2][2][2][2][2][3].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

                // Имя объекта метаданных конфигурации
                if (reader[2][2][2][2][3].Seek())
                {
                    string name = reader.ValueAsString;
                    metadata.Name = name;
                    registry.AddMetadataName(MetadataNames.Constant, in name, uuid);
                }

                //if (options.IsExtension)
                //{
                //    _converter[1][1][1][1][11] += Parent; // uuid расширяемого объекта метаданных
                //}
            }
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry, bool relations)
            {
                if (!registry.TryGetEntry(uuid, out Constant entry))
                {
                    return null; // Идентификатор объекта не найден или не соответствует его типу
                }

                EntityDefinition table = new();

                table.Name = entry.Name;
                table.DbName = entry.GetMainDbName();
                string columnName = entry.GetColumnNameЗначение();

                ConfigFileReader reader = new(file);

                uint[] root = [2, 2, 2, 3]; // Описание типа значения константы

                DataType constantType = DataTypeParser.Parse(ref reader, root, in registry, out _);

                Configurator.ConfigurePropertyЗначение(in table, constantType, in columnName);
                Configurator.ConfigureSharedProperties(in registry, entry, in table);
                Configurator.ConfigurePropertyRecordKey(in table);

                return table;
            }
        }
    }
}