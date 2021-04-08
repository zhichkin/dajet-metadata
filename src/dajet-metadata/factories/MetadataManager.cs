using System;
using System.Collections.Generic;

namespace DaJet.Metadata.Model
{
    public interface IMetadataManager
    {
        DatabaseProvider DatabaseProvider { get; }
        void UseDatabaseProvider(DatabaseProvider databaseProvider);
        Type GetTypeByToken(string token);
        IApplicationObjectFactory GetFactory(Type type);
        IApplicationObjectFactory GetFactory(string token);
        IApplicationObjectFactory GetFactory<T>() where T : ApplicationObject, new();
    }
    public sealed class MetadataManager: IMetadataManager
    {
        private readonly Dictionary<string, Type> MetadataTypes = new Dictionary<string, Type>()
        {
            { MetadataTokens.Acc, typeof(Account) },
            { MetadataTokens.AccRg, typeof(AccountingRegister) },
            { MetadataTokens.AccumRg, typeof(AccumulationRegister) },
            { MetadataTokens.Reference, typeof(Catalog) },
            { MetadataTokens.Chrc, typeof(Characteristic) },
            { MetadataTokens.Const, typeof(Constant) },
            { MetadataTokens.Document, typeof(Document) },
            { MetadataTokens.Enum, typeof(Enumeration) },
            { MetadataTokens.InfoRg, typeof(InformationRegister) },
            { MetadataTokens.Node, typeof(Publication) },
            { MetadataTokens.VT, typeof(TablePart) }
        };
        private readonly Dictionary<Type, IApplicationObjectFactory> ObjectFactories = new Dictionary<Type, IApplicationObjectFactory>()
        {
            { typeof(Account), new ApplicationObjectFactory<Account>(new AccountPropertyFactory()) },
            { typeof(AccountingRegister), new ApplicationObjectFactory<AccountingRegister>(new AccountingRegisterPropertyFactory()) },
            { typeof(AccumulationRegister), new ApplicationObjectFactory<AccumulationRegister>(new AccumulationRegisterPropertyFactory()) },
            { typeof(Catalog), new ApplicationObjectFactory<Catalog>(new CatalogPropertyFactory()) },
            { typeof(Characteristic), new ApplicationObjectFactory<Characteristic>(new CharacteristicPropertyFactory()) },
            { typeof(Constant), new ApplicationObjectFactory<Constant>(new ConstantPropertyFactory()) },
            { typeof(Document), new ApplicationObjectFactory<Document>(new DocumentPropertyFactory()) },
            { typeof(Enumeration), new ApplicationObjectFactory<Enumeration>(new EnumerationPropertyFactory()) },
            { typeof(InformationRegister), new ApplicationObjectFactory<InformationRegister>(new InformationRegisterPropertyFactory()) },
            { typeof(Publication), new ApplicationObjectFactory<Publication>(new PublicationPropertyFactory()) },
            { typeof(TablePart), new ApplicationObjectFactory<TablePart>(new TablePartPropertyFactory()) }
        };
        public DatabaseProvider DatabaseProvider { get; private set; } = DatabaseProvider.SQLServer;
        public void UseDatabaseProvider(DatabaseProvider databaseProvider)
        {
            DatabaseProvider = databaseProvider;
        }
        public Type GetTypeByToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            if (MetadataTypes.TryGetValue(token, out Type type))
            {
                return type;
            }

            return null;
        }
        public IApplicationObjectFactory GetFactory(string token)
        {
            return GetFactory(GetTypeByToken(token));
        }
        public IApplicationObjectFactory GetFactory(Type type)
        {
            if (type == null) return null;

            if (ObjectFactories.TryGetValue(type, out IApplicationObjectFactory factory))
            {
                return factory;
            }

            return null;
        }
        public IApplicationObjectFactory GetFactory<T>() where T : ApplicationObject, new()
        {
            return GetFactory(typeof(T));
        }
    }
}