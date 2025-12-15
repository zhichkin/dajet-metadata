using DaJet.TypeSystem;
using System.Data;

namespace DaJet.Metadata
{
    internal sealed class AccountingRegister : ChangeTrackingObject
    {
        internal static AccountingRegister Create(Guid uuid, int code)
        {
            return new AccountingRegister(uuid, code, MetadataToken.AccRg);
        }
        internal AccountingRegister(Guid uuid, int code, string name) : base(uuid, code, name) { }
        internal Guid ChartOfAccounts { get; set; } // План счетов
        internal bool UseCorrespondence { get; set; } // Корреспонденция счетов
        internal bool UseSplitter { get; set; } // Разрешить разделение итогов

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
            return string.Format("{0}.{1}", MetadataNames.AccountingRegister, Name);
        }

        internal override void ConfigureChangeTrackingTable(in EntityDefinition owner)
        {
            if (IsChangeTrackingEnabled)
            {
                EntityDefinition changes = new() // Таблица регистрации изменений
                {
                    Name = "Изменения",
                    DbName = GetTableNameИзменения() //TODO: (extended ? "x1" : string.Empty)
                };

                Configurator.ConfigurePropertyУзелПланаОбмена(in changes);
                Configurator.ConfigurePropertyНомерСообщения(in changes);

                PropertyDefinition recorder = owner.Properties.Where(p => p.Name == "Регистратор").FirstOrDefault();

                if (recorder is not null)
                {
                    changes.Properties.Add(recorder);
                }

                foreach (PropertyDefinition property in owner.Properties)
                {
                    if (property.Purpose.IsSharedProperty() && property.Purpose.UseDataSeparation())
                    {
                        changes.Properties.Add(property);
                    }
                }

                owner.Entities.Add(changes);
            }
        }

        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                ConfigFileReader reader = new(file);

                // Идентификатор объекта метаданных - значение поля FileName в таблице Config
                Guid uuid = reader[2][16][2][2][3].SeekUuid();

                if (!registry.TryGetEntry(uuid, out AccountingRegister metadata))
                {
                    throw new InvalidOperationException();
                }

                // Имя объекта метаданных конфигурации
                metadata.Name = reader[2][16][2][3].SeekString();

                if (metadata.TypeCode > 0)
                {
                    // Объекты основной конфигурации и собственные объекты расширения
                    registry.AddMetadataName(MetadataNames.AccountingRegister, metadata.Name, uuid);
                }
                else // Заимствованный объект расширения
                {
                    if (registry.TryGetEntry(MetadataNames.AccountingRegister, metadata.Name, out AccountingRegister parent))
                    {
                        parent.MarkAsBorrowed();
                        metadata.MarkAsBorrowed();
                        metadata.TypeCode = parent.TypeCode;
                        registry.AddExtension(parent.Uuid, metadata.Uuid);
                    }
                }

                // План счетов
                metadata.ChartOfAccounts = reader[2][19].SeekUuid();

                // Корреспонденция счетов
                metadata.UseCorrespondence = (reader[2][21].SeekNumber() == 1);

                // Разрешить разделение итогов
                metadata.UseSplitter = (reader[2][24].SeekNumber() != 0);
            }
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry, bool relations)
            {
                if (!registry.TryGetEntry(uuid, out AccountingRegister entry))
                {
                    return null; // Идентификатор объекта не найден или не соответствует его типу
                }

                EntityDefinition table = new();

                table.Name = entry.Name;
                table.DbName = entry.GetMainDbName();

                Configurator.ConfigurePropertyПериод(in table);
                Configurator.ConfigurePropertyРегистратор(in table, entry.Uuid, in registry);
                Configurator.ConfigurePropertyНомерЗаписи(in table);
                Configurator.ConfigurePropertyАктивность(in table);

                ConfigFileReader reader = new(file);

                uint[] root = [4]; // Коллекция измерений

                if (reader[root][ConfigFileToken.StartObject].Seek())
                {
                    Property.Parse(ref reader, root, in table, in registry, relations, entry);
                }

                root[0] = 6; // Коллекция ресурсов

                if (reader[root][ConfigFileToken.StartObject].Seek())
                {
                    Property.Parse(ref reader, root, in table, in registry, relations, entry);
                }

                root[0] = 8; // Коллекция реквизитов

                if (reader[root][ConfigFileToken.StartObject].Seek())
                {
                    Property.Parse(ref reader, root, in table, in registry, relations, entry);
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

                Configurator.ConfigureAccountingRegisterDimensions(in table, in entry, in registry);

                //TODO: _EDHashDt
                //TODO: _EDHashCt

                Configurator.ConfigureSharedProperties(in registry, entry, in table);

                entry.ConfigureChangeTrackingTable(in table);

                return table;
            }
        }
    }
}