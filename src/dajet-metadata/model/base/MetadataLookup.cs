using DaJet.TypeSystem;
using System.Collections.Frozen;

namespace DaJet.Metadata
{
    internal class MetadataLookup
    {
        private static readonly FrozenDictionary<string, Guid> NameToUuidLookup = CreateNameToUuidLookup();
        private static FrozenDictionary<string, Guid> CreateNameToUuidLookup()
        {
            List<KeyValuePair<string, Guid>> list =
            [
                new KeyValuePair<string, Guid>(MetadataNames.SharedProperty, MetadataTypes.SharedProperty),
                new KeyValuePair<string, Guid>(MetadataNames.Publication, MetadataTypes.Publication),
                new KeyValuePair<string, Guid>(MetadataNames.DefinedType, MetadataTypes.DefinedType),
                new KeyValuePair<string, Guid>(MetadataNames.Constant, MetadataTypes.Constant),
                new KeyValuePair<string, Guid>(MetadataNames.Catalog, MetadataTypes.Catalog),
                new KeyValuePair<string, Guid>(MetadataNames.Document, MetadataTypes.Document),
                new KeyValuePair<string, Guid>(MetadataNames.Enumeration, MetadataTypes.Enumeration),
                new KeyValuePair<string, Guid>(MetadataNames.Characteristic, MetadataTypes.Characteristic),
                new KeyValuePair<string, Guid>(MetadataNames.Account, MetadataTypes.Account),
                new KeyValuePair<string, Guid>(MetadataNames.InformationRegister, MetadataTypes.InformationRegister),
                new KeyValuePair<string, Guid>(MetadataNames.AccumulationRegister, MetadataTypes.AccumulationRegister),
                new KeyValuePair<string, Guid>(MetadataNames.AccountingRegister, MetadataTypes.AccountingRegister),
                new KeyValuePair<string, Guid>(MetadataNames.BusinessProcess, MetadataTypes.BusinessProcess),
                new KeyValuePair<string, Guid>(MetadataNames.BusinessTask, MetadataTypes.BusinessTask)
            ];
            return FrozenDictionary.ToFrozenDictionary(list, StringComparer.Ordinal);
        }
        internal static Guid GetMetadataType(in string name)
        {
            if (NameToUuidLookup.TryGetValue(name, out Guid type))
            {
                return type;
            }

            return Guid.Empty;
        }

        private static readonly FrozenDictionary<Guid, string> UuidToNameLookup = CreateUuidToNameLookup();
        private static FrozenDictionary<Guid, string> CreateUuidToNameLookup()
        {
            List<KeyValuePair<Guid, string>> list =
            [
                new KeyValuePair<Guid, string>(MetadataTypes.SharedProperty, MetadataNames.SharedProperty),
                new KeyValuePair<Guid, string>(MetadataTypes.Publication, MetadataNames.Publication),
                new KeyValuePair<Guid, string>(MetadataTypes.DefinedType, MetadataNames.DefinedType),
                new KeyValuePair<Guid, string>(MetadataTypes.Constant, MetadataNames.Constant),
                new KeyValuePair<Guid, string>(MetadataTypes.Catalog, MetadataNames.Catalog),
                new KeyValuePair<Guid, string>(MetadataTypes.Document, MetadataNames.Document),
                new KeyValuePair<Guid, string>(MetadataTypes.Enumeration, MetadataNames.Enumeration),
                new KeyValuePair<Guid, string>(MetadataTypes.Characteristic, MetadataNames.Characteristic),
                new KeyValuePair<Guid, string>(MetadataTypes.Account, MetadataNames.Account),
                new KeyValuePair<Guid, string>(MetadataTypes.InformationRegister, MetadataNames.InformationRegister),
                new KeyValuePair<Guid, string>(MetadataTypes.AccumulationRegister, MetadataNames.AccumulationRegister),
                new KeyValuePair<Guid, string>(MetadataTypes.AccountingRegister, MetadataNames.AccountingRegister),
                new KeyValuePair<Guid, string>(MetadataTypes.BusinessProcess, MetadataNames.BusinessProcess),
                new KeyValuePair<Guid, string>(MetadataTypes.BusinessTask, MetadataNames.BusinessTask)
            ];
            return FrozenDictionary.ToFrozenDictionary(list);
        }
        internal static string GetMetadataName(Guid type)
        {
            if (UuidToNameLookup.TryGetValue(type, out string name))
            {
                return name;
            }

            return string.Empty;
        }

        private static readonly FrozenDictionary<Type, string> TypeToNameLookup = CreateTypeToNameLookup();
        private static FrozenDictionary<Type, string> CreateTypeToNameLookup()
        {
            List<KeyValuePair<Type, string>> list =
            [
                new KeyValuePair<Type, string>(typeof(SharedProperty), MetadataNames.SharedProperty),
                new KeyValuePair<Type, string>(typeof(Publication), MetadataNames.Publication),
                new KeyValuePair<Type, string>(typeof(DefinedType), MetadataNames.DefinedType),
                new KeyValuePair<Type, string>(typeof(Constant), MetadataNames.Constant),
                new KeyValuePair<Type, string>(typeof(Catalog), MetadataNames.Catalog),
                new KeyValuePair<Type, string>(typeof(Document), MetadataNames.Document),
                new KeyValuePair<Type, string>(typeof(Enumeration), MetadataNames.Enumeration),
                new KeyValuePair<Type, string>(typeof(Characteristic), MetadataNames.Characteristic),
                new KeyValuePair<Type, string>(typeof(Account), MetadataNames.Account),
                new KeyValuePair<Type, string>(typeof(InformationRegister), MetadataNames.InformationRegister),
                new KeyValuePair<Type, string>(typeof(AccumulationRegister), MetadataNames.AccumulationRegister),
                new KeyValuePair<Type, string>(typeof(AccountingRegister), MetadataNames.AccountingRegister),
                new KeyValuePair<Type, string>(typeof(BusinessProcess), MetadataNames.BusinessProcess),
                new KeyValuePair<Type, string>(typeof(BusinessTask), MetadataNames.BusinessTask)
            ];
            return FrozenDictionary.ToFrozenDictionary(list);
        }
        internal static string GetMetadataName(Type type)
        {
            if (TypeToNameLookup.TryGetValue(type, out string name))
            {
                return name;
            }

            return string.Empty;
        }
    }
}