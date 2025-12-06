namespace DaJet.TypeSystem
{
    public static class MetadataTypes
    {
        public static readonly Guid Subsystem = new("37f2fa9a-b276-11d4-9435-004095e12fc7"); // Подсистемы
        public static readonly Guid SharedProperty = new("15794563-ccec-41f6-a83c-ec5f7b9a5bc1"); // Общие реквизиты
        public static readonly Guid DefinedType = new("c045099e-13b9-4fb6-9d50-fca00202971e"); // Определяемые типы
        public static readonly Guid Catalog = new("cf4abea6-37b2-11d4-940f-008048da11f9"); // Справочники
        public static readonly Guid Constant = new("0195e80c-b157-11d4-9435-004095e12fc7"); // Константы
        public static readonly Guid Document = new("061d872a-5787-460e-95ac-ed74ea3a3e84"); // Документы
        public static readonly Guid Enumeration = new("f6a80749-5ad7-400b-8519-39dc5dff2542"); // Перечисления
        public static readonly Guid Publication = new("857c4a91-e5f4-4fac-86ec-787626f1c108"); // Планы обмена
        public static readonly Guid Characteristic = new("82a1b659-b220-4d94-a9bd-14d757b95a48"); // Планы видов характеристик
        public static readonly Guid InformationRegister = new("13134201-f60b-11d5-a3c7-0050bae0a776"); // Регистры сведений
        public static readonly Guid AccumulationRegister = new("b64d9a40-1642-11d6-a3c7-0050bae0a776"); // Регистры накопления
        public static readonly Guid Account = new("238e7e88-3c5f-48b2-8a3b-81ebbecb20ed"); // Планы счетов 
        public static readonly Guid AccountingRegister = new("2deed9b8-0056-4ffe-a473-c20a6c32a0bc"); // Регистры бухгатерии
        public static readonly Guid BusinessTask = new("3e63355c-1378-4953-be9b-1deb5fb6bec5"); // Задача бизнес-процесса
        public static readonly Guid BusinessProcess = new("fcd3404e-1523-48ce-9bc0-ecdb822684a1"); // Бизнес-процесс
    }
}