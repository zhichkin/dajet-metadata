using DaJet.TypeSystem;

namespace DaJet.Metadata
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
            return string.Format("{0}.{1}", MetadataNames.InformationRegister, Name);
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
                    registry.AddMetadataName(MetadataNames.InformationRegister, in name, uuid);
                }

                //if (options.IsExtension)
                //{
                //    _converter[1][15][1][13] += Parent;
                //}
            }
            internal override EntityDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry, bool relations)
            {
                if (!registry.TryGetEntry(uuid, out InformationRegister entry))
                {
                    return null; // Идентификатор объекта не найден или не соответствует его типу
                }

                EntityDefinition table = new();

                table.Name = entry.Name;
                table.DbName = entry.GetMainDbName();

                ConfigFileReader reader = new(file);

                // Периодичность стандартного реквизита "Период"
                RegisterPeriodicity Periodicity = (RegisterPeriodicity)reader[2][19].SeekNumber();

                // Длина кода (всегда строка и минимум 1 символ)
                bool UseRecorder = reader[2][20].SeekNumber() != 0;

                // Используется для определения наличия поля "Период" в таблице
                // регистрации изменений если это периодический регистр сведений
                //_converter[1][23] += UsePeriodForChangeTracking;

                // Определяет наличие служебных таблиц итогов "СрезПоследних" и "СрезПервых"
                //if (_cache is not null && _cache.InfoBase is not null && _cache.InfoBase.CompatibilityVersion >= 80302)
                //{
                //    _converter[1][34] += UseSliceLast;
                //    _converter[1][35] += UseSliceFirst;
                //}

                if (UseRecorder)
                {
                    Configurator.ConfigurePropertyРегистратор(in table, entry.Uuid, in registry);
                    Configurator.ConfigurePropertyАктивность(in table);
                    Configurator.ConfigurePropertyНомерЗаписи(in table);
                }

                if (Periodicity != RegisterPeriodicity.None)
                {
                    Configurator.ConfigurePropertyПериод(in table);
                }

                uint[] root = [4]; // Коллекция ресурсов

                if (reader[root][ConfigFileToken.StartObject].Seek())
                {
                    Property.Parse(ref reader, root, in table, in registry, relations);
                }

                root[0] = 5; // Коллекция измерений

                if (reader[root][ConfigFileToken.StartObject].Seek())
                {
                    Property.Parse(ref reader, root, in table, in registry, relations);
                }

                root[0] = 8; // Коллекция реквизитов

                if (reader[root][ConfigFileToken.StartObject].Seek())
                {
                    Property.Parse(ref reader, root, in table, in registry, relations);
                }

                Configurator.ConfigureSharedProperties(in registry, entry, in table);

                if (registry.CompatibilityVersion < 80303 && Periodicity == RegisterPeriodicity.None)
                {
                    int count = 0;

                    for (int i = 0; i < table.Properties.Count; i++)
                    {
                        if (table.Properties[i].Purpose == PropertyPurpose.Dimension)
                        {
                            count++;
                        }

                        if (count > 1) { break; }
                    }

                    if (count > 1)
                    {
                        Configurator.ConfigurePropertySimpleKey(in table);
                    }
                }

                return table;
            }
        }
    }
}