namespace DaJet.Metadata
{
    internal static class MetadataTokens
    {
        internal const string L = "L"; // Boolean (SQL table fields postfix)
        internal const string B = "B"; // Boolean (config metadata)
        internal const string N = "N"; // Numeric
        internal const string S = "S"; // String
        internal const string D = "D"; // DateTime (config metadata)
        internal const string T = "T"; // DateTime (SQL table fields postfix)

        internal const string RRef = "RRef";
        internal const string TRef = "TRef";
        internal const string RRRef = "RRRef";
        internal const string RTRef = "RTRef";
        internal const string TYPE = "TYPE"; // 0x08 - reference data type

        internal const string Fld = "Fld";
        internal const string IDRRef = "IDRRef";
        internal const string Version = "Version";
        internal const string Marked = "Marked";
        internal const string DateTime = "Date_Time";
        internal const string NumberPrefix = "NumberPrefix";
        internal const string Number = "Number";
        internal const string Posted = "Posted";
        internal const string PredefinedID = "PredefinedID";
        internal const string Description = "Description";
        internal const string Code = "Code";
        internal const string OwnerID = "OwnerID";
        internal const string Folder = "Folder";
        internal const string ParentIDRRef = "ParentIDRRef";

        internal const string KeyField = "KeyField";
        internal const string LineNo = "LineNo";
        internal const string EnumOrder = "EnumOrder";
        internal const string Type = "Type"; // ТипЗначения (ПланВидовХарактеристик)

        internal const string Kind = "Kind"; // Тип счёта плана счетов (активный, пассивный, активно-пассивный)
        internal const string OrderField = "OrderField"; // Порядок счёта в плане счетов
        internal const string OffBalance = "OffBalance"; // Признак забалансового счёта плана счетов
        internal const string AccountDtRRef = "AccountDtRRef"; // Cчёт по дебету проводки регистра бухгалтерского учёта
        internal const string AccountCtRRef = "AccountCtRRef"; // Cчёт по кредиту проводки регистра бухгалтерского учёта
        internal const string EDHashDt = "EDHashDt"; // Хэш проводки по дебету регистра бухгалтерского учёта
        internal const string EDHashCt = "EDHashCt"; // Хэш проводки по кредиту регистра бухгалтерского учёта
        internal const string Period = "Period";
        internal const string Periodicity = "Periodicity";
        internal const string ActualPeriod = "ActualPeriod";
        internal const string Recorder = "Recorder";
        internal const string RecorderRRef = "RecorderRRef";
        internal const string RecorderTRef = "RecorderTRef";
        internal const string Active = "Active";
        internal const string RecordKind = "RecordKind";
        internal const string SentNo = "SentNo";
        internal const string ReceivedNo = "ReceivedNo";

        ///<summary>Табличная часть (вложенный значимый тип данных)</summary>
        internal const string VT = "VT";
        ///<summary>Перечисление (ссылочный тип данных)</summary>
        internal const string Enum = "Enum";
        ///<summary>План видов характеристик (ссылочный тип данных)</summary>
        internal const string Chrc = "Chrc";
        ///<summary>Константа (значимый тип данных)</summary>
        internal const string Const = "Const";
        ///<summary>Регистр сведений (значимый тип данных)</summary>
        internal const string InfoRg = "InfoRg";
        ///<summary>План счетов (ссылочный тип данных)</summary>
        internal const string Acc = "Acc";
        ///<summary>Регистр бухгалтерии (значимый тип данных)</summary>
        internal const string AccRg = "AccRg";
        ///<summary>Операции регистра бухгалтерии, журнал проводок (зависимый значимый тип данных)</summary>
        internal const string AccRgED = "AccRgED";
        ///<summary>Регистр накопления (значимый тип данных)</summary>
        internal const string AccumRg = "AccumRg";
        ///<summary>Таблица итогов регистра накопления (зависимый значимый тип данных)</summary>
        internal const string AccumRgT = "AccumRgT";
        ///<summary>Таблица настроек регистра накопления (зависимый значимый тип данных)</summary>
        internal const string AccumRgOpt = "AccumRgOpt";
        ///<summary>Таблица изменений регистра накопления (зависимый значимый тип данных)</summary>
        internal const string AccumRgChngR = "AccumRgChngR";
        ///<summary>Документ (ссылочный тип данных)</summary>
        internal const string Document = "Document";
        ///<summary>Справочник (ссылочный тип данных)</summary>
        internal const string Reference = "Reference";
        ///<summary>План обмена (ссылочный тип данных)</summary>
        internal const string Node = "Node";
        ///<summary>Таблица изменений планов обмена (одна на каждый объект метаданных)</summary>
        internal const string ChngR = "ChngR";
        ///<summary>Хранилище метаданных конфигурации 1С</summary>
        internal const string Config = "Config";

        internal const string Splitter = "Splitter";
        internal const string NodeTRef = "NodeTRef";
        internal const string NodeRRef = "NodeRRef";
        internal const string MessageNo = "MessageNo";
        internal const string UseTotals = "UseTotals";
        internal const string UseSplitter = "UseSplitter";
        internal const string MinPeriod = "MinPeriod";
        internal const string MinCalculatedPeriod = "MinCalculatedPeriod";
        internal const string RepetitionFactor = "RepetitionFactor";
    }
}