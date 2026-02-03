using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    internal sealed class InformationRegister : MetadataObject
    {
        internal static InformationRegister Create(Guid uuid)
        {
            return new InformationRegister(uuid);
        }
        internal InformationRegister(Guid uuid) : base(uuid) { }
        
        internal bool UseRecorder { get; set; } // Регистр сведений, подчинённый регистратору
        internal RegisterPeriodicity Periodicity { get; set; } // Периодичность записей регистра сведений
        internal bool UsePeriodForChangeTracking { get; set; } // Регистрация изменений в разрезе реквизита "Период"

        private int _ChngR;
        private int _InfoRgSF;
        private int _InfoRgSL;
        private int _InfoRgOpt;
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.InfoRg)
            {
                Code = code;
            }
            else if (name == MetadataToken.InfoRgOpt)
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
        internal override string GetMainDbName()
        {
            return string.Format("_{0}{1}", MetadataToken.InfoRg, Code);
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

        //internal override void ConfigureChangeTrackingTable(in EntityDefinition owner)
        //{
        //    if (IsChangeTrackingEnabled)
        //    {
        //        EntityDefinition changes = new() // Таблица регистрации изменений
        //        {
        //            Name = "Изменения",
        //            DbName = GetTableNameИзменения() //TODO: (extended ? "x1" : string.Empty)
        //        };

        //        Configurator.ConfigurePropertyУзелПланаОбмена(in changes);
        //        Configurator.ConfigurePropertyНомерСообщения(in changes);

        //        if (UseRecorder) // Регистр, подчинённый регистратору
        //        {
        //            PropertyDefinition recorder = owner.Properties.Where(p => p.Name == "Регистратор").FirstOrDefault();

        //            if (recorder is not null)
        //            {
        //                changes.Properties.Add(recorder);
        //            }
        //        }
        //        else if (Periodicity != RegisterPeriodicity.None) // Периодический регистр сведений
        //        {
        //            if (UsePeriodForChangeTracking)
        //            {
        //                PropertyDefinition period = owner.Properties.Where(p => p.Name == "Период").FirstOrDefault();

        //                if (period is not null)
        //                {
        //                    changes.Properties.Add(period);
        //                }
        //            }

        //            foreach (PropertyDefinition property in owner.Properties)
        //            {
        //                if (property.Purpose.IsDimension() && property.Purpose.UseForChangeTracking())
        //                {
        //                    changes.Properties.Add(property);
        //                }
        //            }
        //        }
        //        else // Непериодический и независимый регистр сведений
        //        {
        //            foreach (PropertyDefinition property in owner.Properties)
        //            {
        //                if (property.Purpose.IsDimension() && property.Purpose.UseForChangeTracking())
        //                {
        //                    changes.Properties.Add(property);
        //                }
        //            }
        //        }

        //        foreach (PropertyDefinition property in owner.Properties)
        //        {
        //            if (property.Purpose.IsSharedProperty() && property.Purpose.UseDataSeparation())
        //            {
        //                changes.Properties.Add(property);
        //            }
        //        }

        //        owner.Entities.Add(changes);
        //    }
        //}

        internal sealed class Parser : ConfigFileParser
        {
            internal override void Initialize(ReadOnlySpan<byte> file, in MetadataRegistry registry)
            {
                ConfigFileReader reader = new(file);

                // Идентификатор объекта метаданных - значение поля FileName в таблице Config
                Guid uuid = reader[2][16][2][2][3].SeekUuid();

                if (!registry.TryGetEntry(uuid, out InformationRegister metadata))
                {
                    throw new InvalidOperationException();
                }

                // Имя объекта метаданных конфигурации
                metadata.Name = reader[2][16][2][3].SeekString();

                if (metadata.Code > 0)
                {
                    // Объекты основной конфигурации и собственные объекты расширения
                    registry.AddMetadataName(MetadataNames.InformationRegister, metadata.Name, uuid);
                }
                else // Заимствованный объект расширения
                {
                    if (registry.TryGetEntry(MetadataNames.InformationRegister, metadata.Name, out InformationRegister parent))
                    {
                        parent.MarkAsBorrowed();
                        metadata.MarkAsBorrowed();
                        metadata.Code = parent.Code;
                        registry.AddBorrowed(parent.Uuid, metadata.Uuid);
                    }
                }

                // Периодичность стандартного реквизита "Период"
                metadata.Periodicity = (RegisterPeriodicity)reader[2][19].SeekNumber();

                // Длина кода (всегда строка и минимум 1 символ)
                metadata.UseRecorder = reader[2][20].SeekNumber() != 0;

                // Используется для определения наличия поля "Период" в таблице
                // регистрации изменений если это периодический регистр сведений
                metadata.UsePeriodForChangeTracking = reader[2][24].SeekNumber() == 1;

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

                // Определяет наличие служебных таблиц итогов "СрезПоследних" и "СрезПервых"
                //if (_cache is not null && _cache.InfoBase is not null && _cache.InfoBase.CompatibilityVersion >= 80302)
                //{
                //    _converter[1][34] += UseSliceLast;
                //    _converter[1][35] += UseSliceFirst;
                //}

                if (entry.UseRecorder)
                {
                    Configurator.ConfigurePropertyРегистратор(in table, entry.Uuid, in registry);
                    Configurator.ConfigurePropertyАктивность(in table);
                    Configurator.ConfigurePropertyНомерЗаписи(in table);
                }

                if (entry.Periodicity != RegisterPeriodicity.None)
                {
                    Configurator.ConfigurePropertyПериод(in table);
                }

                ConfigFileReader reader = new(file);

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

                if (registry.Version < 80303 && entry.Periodicity == RegisterPeriodicity.None)
                {
                    int count = 0;

                    for (int i = 0; i < table.Properties.Count; i++)
                    {
                        if (table.Properties[i].Purpose.IsDimension())
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