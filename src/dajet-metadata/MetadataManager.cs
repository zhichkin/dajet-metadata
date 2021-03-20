using System;
using System.Collections.Generic;

namespace DaJet.Metadata.Model
{
    public interface IMetadataManager
    {
        DatabaseProviders DatabaseProvider { get; }
        void UseDatabaseProvider(DatabaseProviders databaseProvider);
        Type GetTypeByToken(string token);
        string CreateDbName(string token, int code);
        MetadataObject CreateObject(Guid uuid, string token, int code);
        MetadataProperty CreateProperty(Guid uuid, string token, int code);
        IMetadataObjectFactory GetFactory(Type type);
        IMetadataObjectFactory GetFactory(string token);
        IMetadataObjectFactory GetFactory<T>() where T : MetadataObject, new();
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
        private readonly Dictionary<Type, IMetadataObjectFactory> ObjectFactories = new Dictionary<Type, IMetadataObjectFactory>()
        {
            { typeof(Account), new MetadataObjectFactory<Account>(new AccountPropertyFactory()) },
            { typeof(AccountingRegister), new MetadataObjectFactory<AccountingRegister>(new AccountingRegisterPropertyFactory()) },
            { typeof(AccumulationRegister), new MetadataObjectFactory<AccumulationRegister>(new AccumulationRegisterPropertyFactory()) },
            { typeof(Catalog), new MetadataObjectFactory<Catalog>(new CatalogPropertyFactory()) },
            { typeof(Characteristic), new MetadataObjectFactory<Characteristic>(new CharacteristicPropertyFactory()) },
            { typeof(Constant), new MetadataObjectFactory<Constant>(new ConstantPropertyFactory()) },
            { typeof(Document), new MetadataObjectFactory<Document>(new DocumentPropertyFactory()) },
            { typeof(Enumeration), new MetadataObjectFactory<Enumeration>(new EnumerationPropertyFactory()) },
            { typeof(InformationRegister), new MetadataObjectFactory<InformationRegister>(new InformationRegisterPropertyFactory()) },
            { typeof(Publication), new MetadataObjectFactory<Publication>(new PublicationPropertyFactory()) },
            { typeof(TablePart), new MetadataObjectFactory<TablePart>(new TablePartPropertyFactory()) }
        };
        public DatabaseProviders DatabaseProvider { get; private set; } = DatabaseProviders.SQLServer;
        public void UseDatabaseProvider(DatabaseProviders databaseProvider)
        {
            DatabaseProvider = databaseProvider;
        }
        public string CreateDbName(string token, int code)
        {
            if (DatabaseProvider == DatabaseProviders.SQLServer)
            {
                return $"_{token}{code}";
            }
            return $"_{token}{code}".ToLowerInvariant();
        }
        public MetadataObject CreateObject(Guid uuid, string token, int code)
        {
            IMetadataObjectFactory factory = GetFactory(token);
            if (factory == null) return null;

            MetadataObject metaObject = factory.CreateObject();
            metaObject.FileName = uuid;
            metaObject.TypeCode = code;
            metaObject.TableName = CreateDbName(token, code);

            return metaObject;
        }
        public MetadataProperty CreateProperty(Guid uuid, string token, int code)
        {
            return new MetadataProperty()
            {
                FileName = uuid,
                DbName = CreateDbName(token, code)
            };
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
        public IMetadataObjectFactory GetFactory(string token)
        {
            return GetFactory(GetTypeByToken(token));
        }
        public IMetadataObjectFactory GetFactory(Type type)
        {
            if (type == null) return null;

            if (ObjectFactories.TryGetValue(type, out IMetadataObjectFactory factory))
            {
                return factory;
            }

            return null;
        }
        public IMetadataObjectFactory GetFactory<T>() where T : MetadataObject, new()
        {
            return GetFactory(typeof(T));
        }
    }
}