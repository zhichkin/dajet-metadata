namespace DaJet.TypeSystem
{
    [Flags] public enum PropertyPurpose
    {
        /// <summary>
        /// <b>Стандартный реквизит</b>
        /// <br>Состав стандартных реквизитов зависит от вида объекта метаданных.</br>
        /// </summary>
        System = 0,
        /// <summary>
        /// <b>Реквизит</b>
        /// <br>Определяемый пользователем реквизит объекта метаданных.</br>
        /// </summary>
        Property = 1,
        /// <summary>
        /// <b>Общий реквизит</b>
        /// <br>Применяется к объектам метаданных согласно настроек и логики платформы.</br>
        /// </summary>
        SharedProperty = 2,
        /// <summary>
        /// <b>Измерение</b>
        /// <br>Определяемое пользователем для регистра измерение.</br>
        /// </summary>
        Dimension = 4,
        /// <summary>
        /// <b>Ресурс</b>
        /// <br>Определяемый пользователем для регистра ресурс.</br>
        /// </summary>
        Measure = 8,
        /// <summary>
        /// <b>Признак учёта плана счетов</b>
        /// </summary>
        AccountingFlag = 16,
        /// <summary>
        /// <b>Признак учёта субконто плана счетов</b>
        /// </summary>
        AccountingDimensionFlag = 32,
        /// <summary>
        /// <b>Реквизит адресации задачи</b>
        /// </summary>
        RoutingProperty = 64,
        /// <summary>
        /// <b>Использование измерения регистра сведений для регистрации изменений</b>
        /// </summary>
        UseForChangeTracking = 128,
        /// <summary>
        /// <b>Использование разделения данных внутри одной информационной базы</b>
        /// </summary>
        UseDataSeparation = 256
    }
    public static class PropertyPurposeExtensions
    {
        public static bool IsDimension(this PropertyPurpose purpose)
        {
            return (purpose & PropertyPurpose.Dimension) == PropertyPurpose.Dimension;
        }
        public static bool UseForChangeTracking(this PropertyPurpose purpose)
        {
            return (purpose & PropertyPurpose.UseForChangeTracking) == PropertyPurpose.UseForChangeTracking;
        }

        public static bool IsSharedProperty(this PropertyPurpose purpose)
        {
            return (purpose & PropertyPurpose.SharedProperty) == PropertyPurpose.SharedProperty;
        }
        public static bool UseDataSeparation(this PropertyPurpose purpose)
        {
            return (purpose & PropertyPurpose.UseDataSeparation) == PropertyPurpose.UseDataSeparation;
        }

        public static bool IsAccountingDimensionFlag(this PropertyPurpose purpose)
        {
            return (purpose & PropertyPurpose.AccountingDimensionFlag) == PropertyPurpose.AccountingDimensionFlag;
        }

        public static string GetName(this PropertyPurpose purpose)
        {
            if (purpose == PropertyPurpose.System) { return "СтандартныйРеквизит"; }
            else if (purpose == PropertyPurpose.Measure) { return "Ресурс"; }
            else if (purpose == PropertyPurpose.Property) { return "Реквизит"; }
            else if (purpose == PropertyPurpose.SharedProperty) { return "ОбщийРеквизит"; }
            else if ((purpose & PropertyPurpose.Dimension) == PropertyPurpose.Dimension) { return "Измерение"; }
            else if (purpose == PropertyPurpose.RoutingProperty) { return "РеквизитАдресации"; }
            else if (purpose == PropertyPurpose.AccountingFlag) { return "ПризнакУчёта"; }
            else if (purpose == PropertyPurpose.AccountingDimensionFlag) { return "ПризнакУчётаСубконто"; }
            else
            {
                return "Свойство";
            }
        }
    }
}