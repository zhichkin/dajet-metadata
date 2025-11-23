using System.Collections.Frozen;

namespace DaJet
{
    public static class MetadataName
    {
        public static readonly string SharedProperty = "ОбщийРеквизит";
        public static readonly string Publication = "ПланОбмена";
        public static readonly string DefinedType = "ОпределяемыйТип";
        public static readonly string Constant = "Константа";
        public static readonly string Catalog = "Справочник";
        public static readonly string Document = "Документ";
        public static readonly string Enumeration = "Перечисление";
        public static readonly string Characteristic = "ПланВидовХарактеристик";
        public static readonly string Account = "ПланСчетов";
        public static readonly string InformationRegister = "РегистрСведений";
        public static readonly string AccumulationRegister = "РегистрНакопления";
        public static readonly string AccountingRegister = "РегистрБухгалтерии";
        public static readonly string BusinessProcess = "БизнесПроцесс";
        public static readonly string BusinessTask = "Задача";
        
        private static readonly FrozenDictionary<string, Guid> MetadataNames = CreateMetadataNamesLookup();
        private static FrozenDictionary<string, Guid> CreateMetadataNamesLookup()
        {
            List<KeyValuePair<string, Guid>> list =
            [
                new KeyValuePair<string, Guid>(SharedProperty, MetadataType.SharedProperty),
                new KeyValuePair<string, Guid>(Publication, MetadataType.Publication),
                new KeyValuePair<string, Guid>(DefinedType, MetadataType.DefinedType),
                new KeyValuePair<string, Guid>(Constant, MetadataType.Constant),
                new KeyValuePair<string, Guid>(Catalog, MetadataType.Catalog),
                new KeyValuePair<string, Guid>(Document, MetadataType.Document),
                new KeyValuePair<string, Guid>(Enumeration, MetadataType.Enumeration),
                new KeyValuePair<string, Guid>(Characteristic, MetadataType.Characteristic),
                new KeyValuePair<string, Guid>(Account, MetadataType.Account),
                new KeyValuePair<string, Guid>(InformationRegister, MetadataType.InformationRegister),
                new KeyValuePair<string, Guid>(AccumulationRegister, MetadataType.AccumulationRegister),
                new KeyValuePair<string, Guid>(AccountingRegister, MetadataType.AccountingRegister),
                new KeyValuePair<string, Guid>(BusinessProcess, MetadataType.BusinessProcess),
                new KeyValuePair<string, Guid>(BusinessTask, MetadataType.BusinessTask)
            ];
            return FrozenDictionary.ToFrozenDictionary(list, StringComparer.Ordinal);
        }
        public static Guid GetMetadataType(in string name)
        {
            if (MetadataNames.TryGetValue(name, out Guid type))
            {
                return type;
            }

            return Guid.Empty;
        }

        private static readonly FrozenDictionary<Type, string> MetadataTypes = CreateMetadataTypesLookup();
        private static FrozenDictionary<Type, string> CreateMetadataTypesLookup()
        {
            List<KeyValuePair<Type, string>> list =
            [
                new KeyValuePair<Type, string>(typeof(SharedProperty), SharedProperty),
                new KeyValuePair<Type, string>(typeof(Publication), Publication),
                new KeyValuePair<Type, string>(typeof(DefinedType), DefinedType),
                new KeyValuePair<Type, string>(typeof(Constant), Constant),
                new KeyValuePair<Type, string>(typeof(Catalog), Catalog),
                new KeyValuePair<Type, string>(typeof(Document), Document),
                new KeyValuePair<Type, string>(typeof(Enumeration), Enumeration),
                new KeyValuePair<Type, string>(typeof(Characteristic), Characteristic),
                new KeyValuePair<Type, string>(typeof(Account), Account),
                new KeyValuePair<Type, string>(typeof(InformationRegister), InformationRegister),
                new KeyValuePair<Type, string>(typeof(AccumulationRegister), AccumulationRegister),
                new KeyValuePair<Type, string>(typeof(AccountingRegister), AccountingRegister),
                new KeyValuePair<Type, string>(typeof(BusinessProcess), BusinessProcess),
                new KeyValuePair<Type, string>(typeof(BusinessTask), BusinessTask)
            ];
            return FrozenDictionary.ToFrozenDictionary(list);
        }
        public static string GetMetadataName(Type type)
        {
            if (MetadataTypes.TryGetValue(type, out string name))
            {
                return name;
            }

            return string.Empty;
        }
    }
}