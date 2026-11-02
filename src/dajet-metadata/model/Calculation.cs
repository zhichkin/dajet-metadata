using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    internal sealed class Calculation : MetadataObject
    {
        internal static Calculation Create(Guid uuid)
        {
            return new Calculation(uuid);
        }
        internal Calculation(Guid uuid) : base(uuid) { }
        internal bool ActionPeriodUse { get; set; } // Флаг использования периода действия

        private int _ChngR;
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.CKinds)
            {
                Code = code;
            }
            else if (name == MetadataToken.CKindsChngR)
            {
                _ChngR = code;
            }
        }
        internal override string GetMainDbName()
        {
            return string.Format("_{0}{1}", MetadataToken.CKinds, Code);
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.CKindsChngR, _ChngR);
        }
        internal override int ChangeTrackingCode { get { return _ChngR; } }
        internal override bool IsChangeTrackingEnabled { get { return _ChngR > 0; } }
        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataNames.Calculation, Name);
        }

        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                ConfigFileReader reader = new(file);
                
                // Идентификатор объекта метаданных - значение поля FileName в таблице Config
                Guid uuid = reader[2][2][2][2][3].SeekUuid();

                // Имя объекта метаданных конфигурации
                string name = reader[2][2][2][3].SeekString();

                // Идентификатор ссылочного типа данных, например, "ПланВидовРасчетаСсылка.Начисления"
                Guid reference = reader[2][5].SeekUuid();

                if (!registry.TryGetEntry(uuid, out Calculation metadata))
                {
                    throw new InvalidOperationException();
                }

                registry.AddReference(uuid, reference);

                // Имя объекта метаданных конфигурации
                metadata.Name = name;

                if (metadata.Code > 0)
                {
                    // Объекты основной конфигурации и собственные объекты расширения
                    registry.AddMetadataName(MetadataNames.Calculation, metadata.Name, uuid);
                }
                else // Заимствованный объект расширения
                {
                    if (registry.TryGetEntry(MetadataNames.Calculation, metadata.Name, out Calculation parent))
                    {
                        metadata.IsBorrowed = true;
                        metadata.Code = parent.Code;
                        registry.AddBorrowed(parent.Uuid, metadata.Uuid);
                    }
                }

                // Использует период действия [ _BaseAsAct binary(1) ]
                metadata.ActionPeriodUse = (reader[2][30].SeekNumber() == 1);
            }
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                if (!registry.TryGetEntry(uuid, out Calculation entry))
                {
                    return null; // Идентификатор объекта не найден или не соответствует его типу
                }

                ConfigFileReader reader = new(file);

                EntityDefinition table = new()
                {
                    Uuid = entry.Uuid,
                    Name = entry.Name
                };
                
                if (!entry.IsBorrowed)
                {
                    table.DbName = entry.GetMainDbName();

                    Configurator.ConfigurePropertyСсылка(in table, entry.Code);
                    Configurator.ConfigurePropertyВерсияДанных(in table);
                    Configurator.ConfigurePropertyПометкаУдаления(in table);
                    Configurator.ConfigurePropertyПредопределённый(in table, true, registry.Version);

                    // Длина кода
                    int codeLength = reader[2][26].SeekNumber();

                    // Тип кода (строка или число)
                    CodeType codeType = (CodeType)reader[2][27].SeekNumber();

                    // Длина наименования
                    int nameLength = reader[2][31].SeekNumber();

                    if (codeLength > 0)
                    {
                        Configurator.ConfigurePropertyКод(in table, codeType, codeLength);
                    }

                    if (nameLength > 0)
                    {
                        Configurator.ConfigurePropertyНаименование(in table, nameLength);
                    }

                    if (entry.ActionPeriodUse)
                    {
                        Configurator.ConfigurePropertyИспользуетПериодДействия(in table);
                    }
                }

                uint root = 4; // Коллекция табличных частей объекта

                if (reader[root][ConfigFileToken.StartObject].Seek())
                {
                    TablePart.Parse(ref reader, root, in registry, entry, in table);
                }

                root = 5; // Коллекция свойств объекта

                uint[] vector = [root];

                if (reader[root][ConfigFileToken.StartObject].Seek())
                {
                    Property.Parse(ref reader, vector, in registry, entry, in table);
                }

                return table;
            }
        }
    }
}