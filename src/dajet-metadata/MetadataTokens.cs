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

        internal const string VT = "VT"; // /Табличная часть
        internal const string Enum = "Enum"; // Перечисление
        internal const string Chrc = "Chrc"; // План видов характеристик
        internal const string Const = "Const"; // Константа
        internal const string InfoRg = "InfoRg"; // Регистр сведений
        internal const string Acc = "Acc"; // План счетов
        internal const string AccRg = "AccRg"; // Регистр бухгалтерии
        internal const string AccRgED = "AccRgED"; // Операции регистра бухгалтерии (журнал проводок)
        internal const string AccumRg = "AccumRg"; // Регистр накопления
        internal const string AccumRgT = "AccumRgT"; // Таблица итогов регистра накопления
        internal const string AccumRgOpt = "AccumRgOpt"; // Таблица настроек регистра накопления
        internal const string AccumRgChngR = "AccumRgChngR"; // Таблица изменений регистра накопления
        internal const string Document = "Document"; // Документ
        internal const string Reference = "Reference"; // Справочник
        internal const string Node = "Node"; // План обмена
        internal const string ChngR = "ChngR"; // Таблица изменений планов обмена (одна на каждый объект метаданных)
        internal const string Config = "Config"; // Хранилище метаданных конфигурации 1С

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