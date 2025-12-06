using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    internal static class PropertyTypes
    {
        ///<summary>Идентификатор коллекции значений перечисления</summary>
        internal static Guid Enumeration_Values = new("bee0a08c-07eb-40c0-8544-5c364c171465");

        ///<summary>Идентификатор коллекции реквизитов табличной части</summary>
        internal static Guid TablePart_Properties = new("888744e1-b616-11d4-9436-004095e12fc7");

        ///<summary>Идентификатор коллекции реквизитов плана счетов</summary>
        internal static Guid Account_Properties = new("6e65cbf5-daa8-4d8d-bef8-59723f4e5777");
        ///<summary>Идентификатор коллекции табличных частей плана счетов</summary>
        internal static Guid Account_TableParts = new("4c7fec95-d1bd-4508-8a01-f1db090d9af8");
        ///<summary>Идентификатор коллекции признаков учёта плана счетов</summary>
        internal static Guid Account_AccountingFlags = new("78bd1243-c4df-46c3-8138-e147465cb9a4");
        ///<summary>Идентификатор коллекции признаков учёта субконто плана счетов</summary>
        internal static Guid Account_AccountingDimensionFlags = new("c70ca527-5042-4cad-a315-dcb4007e32a3");

        ///<summary>Идентификатор коллекции реквизитов справочника</summary>
        internal static Guid Catalog_Properties = new("cf4abea7-37b2-11d4-940f-008048da11f9");
        ///<summary>Идентификатор коллекции табличных частей справочника</summary>
        internal static Guid Catalog_TableParts = new("932159f9-95b2-4e76-a8dd-8849fe5c5ded");

        ///<summary>Идентификатор коллекции реквизитов документа</summary>
        internal static Guid Document_Properties = new("45e46cbc-3e24-4165-8b7b-cc98a6f80211");
        ///<summary>Идентификатор коллекции табличных частей документа</summary>
        internal static Guid Document_TableParts = new("21c53e09-8950-4b5e-a6a0-1054f1bbc274");

        ///<summary>Идентификатор коллекции реквизитов плана видов характеристик</summary>
        internal static Guid Characteristic_Properties = new("31182525-9346-4595-81f8-6f91a72ebe06");
        ///<summary>Идентификатор коллекции табличных частей плана видов характеристик</summary>
        internal static Guid Characteristic_TableParts = new("54e36536-7863-42fd-bea3-c5edd3122fdc");

        ///<summary>Идентификатор коллекции реквизитов плана обмена</summary>
        internal static Guid Publication_Properties = new("1a1b4fea-e093-470d-94ff-1d2f16cda2ab");
        ///<summary>Идентификатор коллекции табличных частей плана обмена</summary>
        internal static Guid Publication_TableParts = new("52293f4b-f98c-43ea-a80f-41047ae7ab58");

        ///<summary>Идентификатор коллекции ресурсов регистра сведений</summary>
        internal static Guid InformationRegister_Measure = new("13134202-f60b-11d5-a3c7-0050bae0a776");
        ///<summary>Идентификатор коллекции реквизитов регистра сведений</summary>
        internal static Guid InformationRegister_Property = new("a2207540-1400-11d6-a3c7-0050bae0a776");
        ///<summary>Идентификатор коллекции измерений регистра сведений</summary>
        internal static Guid InformationRegister_Dimension = new("13134203-f60b-11d5-a3c7-0050bae0a776");

        ///<summary>Идентификатор коллекции ресурсов регистра накопления</summary>
        internal static Guid AccumulationRegister_Measure = new("b64d9a41-1642-11d6-a3c7-0050bae0a776");
        ///<summary>Идентификатор коллекции реквизитов регистра накопления</summary>
        internal static Guid AccumulationRegister_Property = new("b64d9a42-1642-11d6-a3c7-0050bae0a776");
        ///<summary>Идентификатор коллекции измерений регистра накопления</summary>
        internal static Guid AccumulationRegister_Dimension = new("b64d9a43-1642-11d6-a3c7-0050bae0a776");

        ///<summary>Идентификатор коллекции ресурсов регистра бухгалтерии</summary>
        internal static Guid AccountingRegister_Measure = new("63405499-7491-4ce3-ac72-43433cbe4112");
        ///<summary>Идентификатор коллекции реквизитов регистра бухгалтерии</summary>
        internal static Guid AccountingRegister_Property = new("9d28ee33-9c7e-4a1b-8f13-50aa9b36607b");
        ///<summary>Идентификатор коллекции измерений регистра бухгалтерии</summary>
        internal static Guid AccountingRegister_Dimension = new("35b63b9d-0adf-4625-a047-10ae874c19a3");

        ///<summary>Идентификатор коллекции реквизитов задачи</summary>
        internal static Guid BusinessTask_Properties = new("8ddfb495-c5fc-46b9-bdc5-bcf58341bff0");
        ///<summary>Идентификатор коллекции реквизитов адресации задачи</summary>
        internal static Guid BusinessTask_Routing_Property = new("e97c0570-251c-4566-b0f1-10686820f143");
        ///<summary>Идентификатор коллекции табличных частей задачи</summary>
        internal static Guid BusinessTask_TableParts = new("ee865d4b-a458-48a0-b38f-5a26898feeb0");

        ///<summary>Идентификатор коллекции макетов объекта метаданных</summary>
        internal static Guid Template_Collection = new("3daea016-69b7-4ed4-9453-127911372fe6");

        internal static PropertyPurpose GetPropertyPurpose(Guid type)
        {
            if (type == InformationRegister_Measure ||
                type == AccumulationRegister_Measure ||
                type == AccountingRegister_Measure)
            {
                return PropertyPurpose.Measure;
            }

            if (type == InformationRegister_Dimension ||
                type == AccumulationRegister_Dimension ||
                type == AccountingRegister_Dimension)
            {
                return PropertyPurpose.Dimension;
            }

            if (type == Account_AccountingFlags)
            {
                return PropertyPurpose.AccountingFlag;
            }

            if (type == Account_AccountingDimensionFlags)
            {
                return PropertyPurpose.AccountingDimensionFlag;
            }

            if (type == BusinessTask_Routing_Property)
            {
                return PropertyPurpose.RoutingProperty;
            }

            return PropertyPurpose.Property;
        }
    }
}