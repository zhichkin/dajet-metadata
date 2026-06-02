using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    internal sealed class AccountingRegister : MetadataObject
    {
        internal static AccountingRegister Create(Guid uuid)
        {
            return new AccountingRegister(uuid);
        }
        internal AccountingRegister(Guid uuid) : base(uuid) { }
        internal Guid ChartOfAccounts { get; set; } // План счетов
        internal bool UseCorrespondence { get; set; } // Корреспонденция счетов
        internal bool UseSplitter { get; set; } // Разрешить разделение итогов
        internal int PeriodAdjustment { get; set; } // Длина уточнения периода

        private int _ChngR;
        private int _AccRgED;
        private int _AccRgOpt;
        private int _AccRgCT;
        private readonly int[] _AccRgAT = new int[6];
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.AccRg)
            {
                Code = code;
            }
            else if (name == MetadataToken.AccRgED)
            {
                _AccRgED = code;
            }
            else if (name == MetadataToken.AccRgOpt)
            {
                _AccRgOpt = code;
            }
            else if (name == MetadataToken.AccRgChngR)
            {
                _ChngR = code;
            }
            else if (name == MetadataToken.AccRgCT) { _AccRgCT = code; }
            else if (name == MetadataToken.AccRgAT0) { _AccRgAT[0] = code; }
            else if (name == MetadataToken.AccRgAT1) { _AccRgAT[1] = code; }
            else if (name == MetadataToken.AccRgAT2) { _AccRgAT[2] = code; }
            else if (name == MetadataToken.AccRgAT3) { _AccRgAT[3] = code; }
            else if (name == MetadataToken.AccRgAT4) { _AccRgAT[4] = code; }
            else if (name == MetadataToken.AccRgAT5) { _AccRgAT[5] = code; }
        }
        internal override string GetMainDbName()
        {
            return string.Format("_{0}{1}", MetadataToken.AccRg, Code);
        }
        internal string GetTableNameНастройки()
        {
            return string.Format("_{0}{1}", MetadataToken.AccRgOpt, _AccRgOpt);
        }
        internal string GetTableNameИтогиМеждуСчетами()
        {
            return string.Format("_{0}{1}", MetadataToken.AccRgCT, _AccRgCT);
        }
        internal string GetTableNameИтогиПоСчетам()
        {
            return string.Format("_{0}0{1}", MetadataToken.AccRgAT, _AccRgAT[0]);
        }
        internal string GetTableNameИтогиПоСубконто(int ordinal)
        {
            return string.Format("_{0}{1}{2}", MetadataToken.AccRgAT, ordinal, _AccRgAT[ordinal]);
        }
        internal string GetTableNameЗначенияСубконто()
        {
            return string.Format("_{0}{1}", MetadataToken.AccRgED, _AccRgED);
        }
        internal override string GetTableNameИзменения()
        {
            return string.Format("_{0}{1}", MetadataToken.AccRgChngR, _ChngR);
        }
        internal bool IsExtDimValuesEnabled { get { return _AccRgED > 0; } }
        internal override bool IsChangeTrackingEnabled { get { return _ChngR > 0; } }
        internal override void SetBorrowedChangeTrackingFlag() { _ChngR = int.MaxValue; }
        public override string ToString()
        {
            return string.Format("{0}.{1}", MetadataNames.AccountingRegister, Name);
        }

        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                ConfigFileReader reader = new(file);

                // Идентификатор объекта метаданных - значение поля FileName в таблице Config
                Guid uuid = reader[2][16][2][2][3].SeekUuid();

                bool adjustment = (uuid == Guid.Empty); // Оптимизация загрузки уточнения периода (см. ниже)

                if (adjustment)
                {
                    uuid = reader[2][17][2][2][3].SeekUuid();
                }

                if (!registry.TryGetEntry(uuid, out AccountingRegister metadata))
                {
                    throw new InvalidOperationException();
                }

                // Имя объекта метаданных конфигурации
                metadata.Name = adjustment
                    ? reader[2][17][2][3].SeekString()
                    : reader[2][16][2][3].SeekString();

                if (metadata.Code > 0)
                {
                    // Объекты основной конфигурации и собственные объекты расширения
                    registry.AddMetadataName(MetadataNames.AccountingRegister, metadata.Name, uuid);
                }
                else // Заимствованный объект расширения
                {
                    if (registry.TryGetEntry(MetadataNames.AccountingRegister, metadata.Name, out AccountingRegister parent))
                    {
                        metadata.IsBorrowed = true;
                        metadata.Code = parent.Code;
                        registry.AddBorrowed(parent.Uuid, metadata.Uuid);
                    }
                }

                // План счетов
                metadata.ChartOfAccounts = adjustment
                    ? reader[2][20].SeekUuid()
                    : reader[2][19].SeekUuid();

                // Корреспонденция счетов
                metadata.UseCorrespondence = adjustment
                    ? (reader[2][22].SeekNumber() == 1)
                    : (reader[2][21].SeekNumber() == 1);

                // Разрешить разделение итогов
                if (registry.Version >= 80100)
                {
                    metadata.UseSplitter = adjustment
                        ? (reader[2][25].SeekNumber() != 0)
                        : (reader[2][24].SeekNumber() != 0);
                }

                // Длина уточнения периода
                if (registry.Version >= 80309)
                {
                    if (reader[2][31].Seek()) // Может отсутствовать если регистр не поддерживает уточнение периода
                    {
                        metadata.PeriodAdjustment = reader.ValueAsNumber; // Значение от 1 до 3 включительно
                    }
                }
            }
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                if (!registry.TryGetEntry(uuid, out AccountingRegister entry))
                {
                    return null; // Идентификатор объекта не найден или не соответствует его типу
                }

                EntityDefinition table = new();

                table.Uuid = entry.Uuid;
                table.Name = entry.Name;
                table.DbName = entry.GetMainDbName();

                Configurator.ConfigurePropertyПериод(in table);
                Configurator.ConfigurePropertyРегистратор(in table, entry.Uuid, in registry);
                Configurator.ConfigurePropertyНомерЗаписи(in table);
                Configurator.ConfigurePropertyАктивность(in table);

                if (entry.PeriodAdjustment > 0)
                {
                    Configurator.ConfigurePropertyУточнениеПериода(in table);
                }

                ConfigFileReader reader = new(file);

                uint[] root = [4]; // Коллекция измерений

                if (reader[root][ConfigFileToken.StartObject].Seek())
                {
                    Property.Parse(ref reader, root, in registry, entry, in table);
                }

                root[0] = 6; // Коллекция ресурсов

                if (reader[root][ConfigFileToken.StartObject].Seek())
                {
                    Property.Parse(ref reader, root, in registry, entry, in table);
                }

                root[0] = 8; // Коллекция реквизитов

                if (reader[root][ConfigFileToken.StartObject].Seek())
                {
                    Property.Parse(ref reader, root, in registry, entry, in table);
                }

                //NOTE: для таблицы ЗначенияСубконто (_AccRgED) обратная логика:
                //NOTE: если UseCorrespondence == true, то есть поле _Correspond
                //NOTE: (ВидДвижения: 0 - Дебет, 1 - Кредит)
                if (!entry.UseCorrespondence)
                {
                    Configurator.ConfigurePropertyВидДвиженияБухгалтерии(in table);
                }

                if (registry.TryGetEntry(entry.ChartOfAccounts, out Account account))
                {
                    Configurator.ConfigurePropertyСчёт(in entry, in account, in table);
                }

                Configurator.ConfigureAccountingDimensions(in table, in entry, in registry);

                //TODO: _EDHashDt
                //TODO: _EDHashCt

                return table;
            }
        }
    }
}