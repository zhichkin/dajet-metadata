using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    internal sealed class Constant : MetadataObject
    {
        internal static Constant Create(Guid uuid)
        {
            return new Constant(uuid);
        }
        internal Constant(Guid uuid) : base(uuid) { }

        private int _Fld;
        private int _ChngR;
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.Const)
            {
                Code = code;
            }
            else if (name == MetadataToken.Fld)
            {
                _Fld = code;
            }
            else if (name == MetadataToken.ConstChngR)
            {
                _ChngR = code;
            }
        }
        internal override string GetMainDbName()
        {
            return string.Format("_{0}{1}", MetadataToken.Const, Code);
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
        //        Configurator.ConfigurePropertyConstID(in changes);

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
                Guid uuid = reader[2][2][2][2][2][3].SeekUuid();

                if (!registry.TryGetEntry(uuid, out Constant metadata))
                {
                    throw new InvalidOperationException();
                }

                // Имя объекта метаданных конфигурации
                metadata.Name = reader[2][2][2][2][3].SeekString();

                if (metadata.Code > 0)
                {
                    // Объекты основной конфигурации и собственные объекты расширения
                    registry.AddMetadataName(MetadataNames.Constant, metadata.Name, uuid);
                }
                else // Заимствованный объект расширения
                {
                    if (registry.TryGetEntry(MetadataNames.Constant, metadata.Name, out Constant parent))
                    {
                        metadata.IsBorrowed = true;
                        metadata.Code = parent.Code;
                        registry.AddBorrowed(parent.Uuid, metadata.Uuid);
                    }
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