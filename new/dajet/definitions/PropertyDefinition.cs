namespace DaJet
{
    public sealed class PropertyDefinition
    {
        public string Name { get; set; } = string.Empty;
        public DataType Type { get; set; }
        public PropertyPurpose Purpose { get; set; } = PropertyPurpose.System;

        /// <summary>Вариант использования реквизита для групп и элементов</summary>
        public PropertyUsage PropertyUsage { get; set; } = PropertyUsage.Item;
        /// <summary>Использование измерения периодического или непереодического регистра сведений,
        /// <br>который не подчинён регистратору, в качестве основного отбора при регистрации изменений в плане обмена</br></summary>
        public bool UseForChangeTracking { get; set; } = false;
        /// <summary>Признак измерения регистра сведений (ведущее):
        /// <br>запись будет подчинена объектам, записываемым в данном измерении</br></summary>
        public bool CascadeDelete { get; set; } = false; // Каскадное удаление по внешнему ключу

        #region "Регистр бухгалтерии"
        ///<summary><b>Использование отдельных свойств для дебета и кредита:</b>
        ///<br>true - не использовать</br>
        ///<br>false - использовать</br></summary>
        public bool IsBalance { get; set; } = true; // Балансовый (измерения и ресурсы)
        ///<summary>Признак учёта (для счёта плана счетов)</summary>
        public Guid AccountingFlag { get; set; } = Guid.Empty; // Признак учёта (измерения и ресурсы)
        ///<summary><b>Признак учёта субконто</b>
        ///<br>Используется в стандартной (системной) табличной части "ВидыСубконто"</br>
        ///<br>если <see cref="Account.MaxDimensionCount"/> больше нуля.</br></summary>
        public Guid AccountingDimensionFlag { get; set; } = Guid.Empty; // Признак учёта субконто (только ресурсы)
        #endregion

        #region "Задача"
        ///<summary>Ссылка на измерение регистра сведений,
        ///<br>указанного в свойстве "Адресация" задачи</br></summary>
        public Guid RoutingDimension { get; set; } = Guid.Empty; // Измерение адресации
        #endregion

        public List<ColumnDefinition> Columns { get; set; } = new();
        public override string ToString()
        {
            return string.Format("[{0}] {1}", Purpose, Name);
        }
    }
}