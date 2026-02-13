using DaJet.Data;
using DaJet.Metadata.Services;
using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    public sealed class MetadataProvider
    {
        private static readonly object _cache_lock = new();
        private static readonly Dictionary<string, MetadataProvider> _cache = new();
        public static MetadataProvider GetOrCreate(DataSourceType dataSource, in string connectionString)
        {
            return GetOrCreateProvider(in connectionString, dataSource, in connectionString);
        }
        private static MetadataProvider GetOrCreateProvider(in string cacheKey, DataSourceType dataSource, in string connectionString)
        {
            if (_cache.TryGetValue(cacheKey, out MetadataProvider provider))
            {
                return provider; // fast path - return existing resource
            }

            bool locked = false;

            try
            {
                Monitor.Enter(_cache_lock, ref locked);

                if (_cache.TryGetValue(cacheKey, out provider))
                {
                    return provider; // double-checking
                }

                // long path - create new resource

                provider = new MetadataProvider(dataSource, in connectionString);

                provider.Initialize(); // initialize resource - is not thread safe

                _cache.Add(cacheKey, provider);
            }
            finally
            {
                if (locked)
                {
                    Monitor.Exit(_cache_lock);
                }
            }

            return provider;
        }

        private MetadataRegistry _registry;
        private readonly MetadataLoader _loader;
        public MetadataProvider(DataSourceType dataSource, in string connectionString)
        {
            _loader = MetadataLoader.Create(dataSource, in connectionString);
        }
        public void Dump(in string tableName, in string fileName, in string outputPath)
        {
            _loader.Dump(in tableName, in fileName, in outputPath);
        }
        public void DumpRaw(in string tableName, in string fileName, in string outputPath)
        {
            _loader.DumpRaw(in tableName, in fileName, in outputPath);
        }

        public void Initialize()
        {
            _registry = _loader.GetMetadataRegistry();
        }
        public List<ExtensionInfo> GetExtensions()
        {
            return _loader.GetExtensions();
        }
        public List<Configuration> GetConfigurations()
        {
            return _registry.Configurations;
        }
        public Configuration GetConfiguration(in string name = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                return _registry.Configurations[0]; // Основная конфигурация
            }

            foreach (Configuration configuration in _registry.Configurations)
            {
                if (configuration.Name == name)
                {
                    return configuration;
                }
            }

            return null;
        }
        
        public EntityDefinition GetMetadataObject(int typeCode)
        {
            if (!_registry.TryGetEntry(typeCode, out MetadataObject entry))
            {
                return null;
            }

            return GetMetadataObject(entry.ToString());
        }
        public EntityDefinition GetMetadataObject(in string fullName)
        {
            string type = string.Empty;
            string name = string.Empty;
            string table = string.Empty;

            int dot = fullName.IndexOf('.');

            if (dot > 0)
            {
                type = fullName[..dot];
                name = fullName[(dot + 1)..];
            }

            dot = name.IndexOf('.');

            if (dot > 0)
            {
                table = name[(dot + 1)..];
                name = name[..dot];
            }

            if (!_registry.TryGetEntry(in type, in name, out MetadataObject entry))
            {
                return _loader.GetDbTableSchema(fullName); // Обычная таблица базы данных
            }

            EntityDefinition entity = _loader.Load(in type, in entry, in _registry);

            if (string.IsNullOrEmpty(table))
            {
                return entity; // Основная таблица объекта метаданных
            }

            if (table == "Изменения") // Таблица регистрации изменений
            {
                return Configurator.GetChangeTrackingTable(in entry, in entity, in _registry);
            }
            
            // Табличная часть объекта метаданных
            
            return entity.Entities.Where(e => e.Name == table).FirstOrDefault();
        }
        public IEnumerable<EntityDefinition> GetMetadataObjects(string typeName)
        {
            foreach (MetadataObject entry in _registry.GetMetadataObjects(typeName))
            {
                yield return _loader.Load(in typeName, in entry, in _registry);
            }
        }
        public List<string> GetMetadataNames(in string configurationName, in string typeName)
        {
            Configuration configuration = GetConfiguration(in configurationName);

            if (configuration is null)
            {
                return new List<string>();
            }

            Guid type = MetadataLookup.GetMetadataType(in typeName);

            if (type == Guid.Empty)
            {
                return new List<string>();
            }

            if (!configuration.Metadata.TryGetValue(type, out Guid[] items))
            {
                return new List<string>();
            }

            if (items is null || items.Length == 0)
            {
                return new List<string>();
            }

            List<string> names = new(items.Length);

            foreach (Guid uuid in items)
            {
                if (_registry.TryGetEntry(uuid, out MetadataObject entry))
                {
                    names.Add(entry.Name);
                }
            }
            
            return names;
        }

        public List<string> ResolveReferences(in List<Guid> references)
        {
            return _registry.ResolveReferences(in references);
        }

        public Guid GetEnumerationValue(in string fullName)
        {
            string[] names = fullName.Split('.');

            if (!_registry.TryGetEntry(names[0], names[1], out Enumeration entry))
            {
                return Guid.Empty;
            }

            return entry.Values[names[2]];
        }
        public Dictionary<string, Guid> GetEnumerationValues(in string fullName)
        {
            int dot = fullName.IndexOf('.');

            if (dot < 0)
            {
                return null;
            }

            string type = fullName[..dot];
            string name = fullName[(dot + 1)..];

            if (!_registry.TryGetEntry(in type, in name, out Enumeration entry))
            {
                return null;
            }

            return entry.Values;
        }
        
        public string CompareMetadataToDatabase(List<string> names = null)
        {
             return new MetadataComparer(this, _loader).CompareMetadataToDatabase(names);
        }
    }
}