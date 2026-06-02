using DaJet.TypeSystem;
using System.Runtime.CompilerServices;

namespace DaJet.Metadata
{
    internal sealed class Property : MetadataObject
    {
        internal Property(Guid uuid) : base(uuid) { }

        private int _Turnover;
        private int _TurnoverDt;
        private int _TurnoverCt;
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.Fld)
            {
                Code = code;
            }
            else if (name == MetadataToken.Turnover)
            {
                _Turnover = code;
            }
            else if (name == MetadataToken.TurnoverDt)
            {
                _TurnoverDt = code;
            }
            else if (name == MetadataToken.TurnoverCt)
            {
                _TurnoverCt = code;
            }
        }
        internal override string GetMainDbName()
        {
            return string.Format("_{0}{1}", MetadataToken.Fld, Code);
        }
        internal bool HasTurnover { get { return _Turnover > 0; } }
        internal string GetFieldNameTurnover()
        {
            return string.Format("_{0}{1}", MetadataToken.Turnover, _Turnover);
        }
        internal bool HasTurnoverDt { get { return _TurnoverDt > 0; } }
        internal string GetFieldNameTurnoverDt()
        {
            return string.Format("_{0}{1}", MetadataToken.TurnoverDt, _TurnoverDt);
        }
        internal bool HasTurnoverCt { get { return _TurnoverCt > 0; } }
        internal string GetFieldNameTurnoverCt()
        {
            return string.Format("_{0}{1}", MetadataToken.TurnoverCt, _TurnoverCt);
        }
        internal override string GetTableNameИзменения()
        {
            throw new NotImplementedException();
        }

        internal static void Parse(ref ConfigFileReader reader, ReadOnlySpan<uint> root,
            in MetadataRegistry registry, in MetadataObject owner, in EntityDefinition table)
        {
            Guid type = reader[root][1].SeekUuid(); // идентификатор типа коллекции
            int count = reader[root][2].SeekNumber(); // количество элементов коллекции

            if (count == 0) { return; }

            table.Properties.EnsureCapacity(table.Properties.Count + count);

            int N = root.Length;
            uint[] vector = new uint[N + 1]; // Адрес конкретного свойства [root][N]

            root.CopyTo(vector);

            for (uint i = 0; i < count; i++)
            {
                vector[N] = i + 3; // Смещение от корневого узла [root][0]

                if (reader[vector][ConfigFileToken.StartObject].Seek())
                {
                    ParseProperty(type, ref reader, vector, in registry, in owner, in table);
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ParseProperty(Guid type, ref ConfigFileReader reader, ReadOnlySpan<uint> root,
            in MetadataRegistry registry, in MetadataObject owner, in EntityDefinition table)
        {
            // Свойство объекта:
            // -----------------
            // [root][offset] 0.1.1.1.8 = 0 если заимствование отстутствует
            // [root][offset] 0.1.1.1.11 - uuid расширяемого объекта метаданных
            // [root][offset] 0.1.1.1.15 - Объект описания дополнительных типов данных свойства
            // [root][offset] 0.1.1.1.15.0 = #
            // [root][offset] 0.1.1.1.15.1 = f5c65050-3bbb-11d5-b988-0050bae0a95d (константа)
            // [root][offset] 0.1.1.1.15.2 = {объект описания типов данных - Pattern} [0][1][1][2] += PropertyType

            PropertyDefinition property = new()
            {
                Purpose = PropertyTypes.GetPropertyPurpose(type)
            };

            // Идентификатор объекта метаданных (свойства)
            property.Uuid = reader[root][1][2][2][2][2][3].SeekUuid();
            property.Name = reader[root][1][2][2][2][3].SeekString();

            if (!registry.TryGetEntry(property.Uuid, out Property entry))
            {
                return; // Заимствованное расширением свойство

                //if (_cache != null && _cache.Extension != null) // 0.1.1.1.8 = 0 если заимствование отстутствует
                //{
                //    _converter[0][1][1][1][11] += Parent; // uuid расширяемого объекта метаданных
                //    _converter[0][1][1][1][15][2] += ExtensionPropertyType;
                //}
            }

            //_converter[0][1][1][1][3][2] += PropertyAlias;

            if (reader[root][1][2][2][3][ConfigFileToken.StartObject].Seek())
            {
                // Необходимо указать смещение от корня

                uint[] offset = new uint[root.Length + 4];

                root.CopyTo(offset);

                offset[root.Length + 0] = 1;
                offset[root.Length + 1] = 2;
                offset[root.Length + 2] = 2;
                offset[root.Length + 3] = 3;

                property.Type = DataTypeParser.Parse(ref reader, offset, in registry, out List<Guid> references);

                property.References = references;
            }

            if (type == PropertyTypes.AccountingRegister_Measure ||
                type == PropertyTypes.AccountingRegister_Dimension)
            {
                //NOTE: Особый случай: регистр бухгалтерии, который использует корреспонденцию счетов,
                //NOTE: согласно настройке измерения или ресурса "Балансовый" делит их на дебет и кредит.

                bool IsBalance = reader[root][1][3].SeekNumber() == 1; // Балансовый

                if (IsBalance) // Балансовое измерение или ресурс
                {
                    property.Purpose |= PropertyPurpose.IsBalance;
                }
                else if (owner is AccountingRegister register && register.UseCorrespondence)
                {
                    // Не балансовый реквизит (не сальдо) - делим на дебет и кредит
                    //NOTE: Логика формирования свойств основной таблицы регистра бухгалтерии _AccRg
                    //NOTE: В таблице итогов по счетам _AccRgAT0 свойство не делится на дебет и кредит

                    string propertyName = property.Name;

                    string databaseName = entry.GetMainDbName();

                    property.Name = string.Format("{0}{1}", propertyName, "Дт");

                    string columnName = string.Format("{0}{1}", databaseName, "Dt");

                    Configurator.ConfigureDatabaseColumns(in property, columnName);
                    
                    table.Properties.Add(property); // Дебет

                    property = new PropertyDefinition()
                    {
                        Uuid = property.Uuid,
                        Name = string.Format("{0}{1}", propertyName, "Кт"),
                        Type = property.Type,
                        Purpose = property.Purpose,
                        References = property.References
                    };

                    columnName = string.Format("{0}{1}", databaseName, "Ct");

                    Configurator.ConfigureDatabaseColumns(in property, columnName);

                    table.Properties.Add(property); // Кредит

                    return;
                }
            }

            Configurator.ConfigureDatabaseColumns(in entry, in property);

            table.Properties.Add(property);

            if (type == PropertyTypes.InformationRegister_Dimension)
            {
                //NOTE: Использование измерения периодического или непереодического регистра сведений,
                //NOTE: который не подчинён регистратору, в качестве основного отбора при регистрации изменений в плане обмена
                
                bool UseForChangeTracking = reader[root][1][6].SeekNumber() == 1;

                if (UseForChangeTracking)
                {
                    property.Purpose |= PropertyPurpose.UseForChangeTracking;
                }
            }
            else if (type == PropertyTypes.AccumulationRegister_Dimension)
            {
                if (owner is AccumulationRegister register && register.Purpose == RegisterKind.Turnovers)
                {
                    if (registry.Version >= 80100)
                    {
                        bool UseForTurnoverTotals = reader[root][1][6].SeekNumber() == 1;

                        if (UseForTurnoverTotals)
                        {
                            property.Purpose |= PropertyPurpose.UseForTurnoverTotals;
                        }
                    }
                    else
                    {
                        property.Purpose |= PropertyPurpose.UseForTurnoverTotals;
                    }
                }
            }

            //if (type == PropertyTypes.Catalog_Properties ||
            //    type == PropertyTypes.Characteristic_Properties)
            //{
            //    /// <summary>Вариант использования реквизита для групп и элементов</summary>
            //    property.PropertyUsage = (PropertyUsage)reader[root][1][4].SeekNumber();
            //}
            //else if (type == PropertyTypes.InformationRegister_Dimension)
            //{
            //    /// <summary>Признак измерения регистра сведений (ведущее):
            //    /// <br>запись будет подчинена объектам, записываемым в данном измерении</br></summary>
            //    property.CascadeDelete = reader[root][1][3].SeekNumber() == 1; // Каскадное удаление по внешнему ключу

            //    /// <summary>Использование измерения периодического или непереодического регистра сведений,
            //    /// <br>который не подчинён регистратору, в качестве основного отбора при регистрации изменений в плане обмена</br></summary>
            //    property.UseForChangeTracking = reader[root][1][6].SeekNumber() == 1;
            //}
            //else if (type == PropertyTypes.AccountingRegister_Dimension)
            //{
            //    property.IsBalance = reader[root][1][3].SeekNumber() == 1; // Балансовый
            //    property.AccountingFlag = reader[root][1][4].SeekUuid(); // Признак учёта
            //}
            //else if (type == PropertyTypes.AccountingRegister_Measure)
            //{
            //    ///<summary><b>Использование отдельных свойств для дебета и кредита:</b>
            //    ///<br>true - не использовать</br>
            //    ///<br>false - использовать</br></summary>
            //    property.IsBalance = reader[root][1][3].SeekNumber() == 1; // Балансовый (измерения и ресурсы)

            //    ///<summary>Признак учёта (для счёта плана счетов)</summary>
            //    property.AccountingFlag = reader[root][1][4].SeekUuid(); // Признак учёта (измерения и ресурсы)

            //    ///<summary><b>Признак учёта субконто</b>
            //    ///<br>Используется в стандартной (системной) табличной части "ВидыСубконто"</br>
            //    ///<br>если <see cref="Account.MaxDimensionCount"/> больше нуля.</br></summary>
            //    property.AccountingDimensionFlag = reader[root][1][5].SeekUuid(); // Признак учёта субконто (только ресурсы)
            //}
            //else if (type == PropertyTypes.BusinessTask_Routing_Property)
            //{
            //    ///<summary>Ссылка на измерение регистра сведений,
            //    ///<br>указанного в свойстве "Адресация" задачи</br></summary>
            //    property.RoutingDimension = reader[root][1][4].SeekUuid(); // Измерение адресации
            //}
        }
    }
}