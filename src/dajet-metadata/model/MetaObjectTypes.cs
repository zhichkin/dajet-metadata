namespace DaJet.Metadata.Model
{
    public static class MetadataObjectTypes
    {
        public static string Unknown { get; } = "Unknown";
        /// <summary> Справочник </summary>
        public static string Catalog { get; } = "Catalog";
        /// <summary> План счетов </summary>
        public static string Account { get; } = "Account";
        /// <summary> Документ </summary>
        public static string Document { get; } = "Document";
        /// <summary> Константа </summary>
        public static string Constant { get; } = "Constant";
        /// <summary> Табличная часть </summary>
        public static string TablePart { get; } = "TablePart";
        /// <summary> Перечисление </summary>
        public static string Enumeration { get; } = "Enumeration";
        /// <summary> План обмена </summary>
        public static string Publication { get; } = "Publication";
        /// <summary> План видов характеристик </summary>
        public static string Characteristic { get; } = "Characteristic";
        /// <summary> Регистр бухгалтерского учёта </summary>
        public static string AccountingRegister { get; } = "AccountingRegister";
        /// <summary> Регистр сведений </summary>
        public static string InformationRegister { get; } = "InformationRegister";
        /// <summary> Регистр накопления </summary>
        public static string AccumulationRegister { get; } = "AccumulationRegister";
    }
}