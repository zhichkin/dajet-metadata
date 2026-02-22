using DaJet.Data;
using DaJet.Metadata.Services;
using DaJet.TypeSystem;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;

namespace DaJet.Metadata
{
    public sealed class MetadataProvider
    {
        #region "STATIC CACHE"

        private static readonly object _cache_lock = new();
        private static readonly ConcurrentDictionary<string, MetadataProvider> _cache = new();
        public static MetadataProvider Create(DataSourceType dataSource, in string connectionString)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));

            MetadataProvider provider = new(dataSource, in connectionString);

            provider.EnsureInitialized();

            return provider;
        }
        public static void Add(in string cacheKey, DataSourceType dataSource, in string connectionString)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(cacheKey, nameof(cacheKey));
            ArgumentNullException.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));

            bool locked = false;

            try
            {
                Monitor.Enter(_cache_lock, ref locked);

                MetadataProvider provider = new(dataSource, in connectionString);

                if (!_cache.TryAdd(cacheKey, provider))
                {
                    //THINK: provider.Dispose();
                }
            }
            finally
            {
                if (locked)
                {
                    Monitor.Exit(_cache_lock);
                }
            }
        }
        public static bool TryAdd(in string cacheKey, DataSourceType dataSource, in string connectionString)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(cacheKey, nameof(cacheKey));
            ArgumentNullException.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));

            bool locked = false;

            try
            {
                Monitor.Enter(_cache_lock, ref locked);

                MetadataProvider provider = new(dataSource, in connectionString);

                return _cache.TryAdd(cacheKey, provider);
            }
            finally
            {
                if (locked)
                {
                    Monitor.Exit(_cache_lock);
                }
            }
        }
        public static bool TryUpdate(in string cacheKey, DataSourceType dataSource, in string connectionString)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(cacheKey, nameof(cacheKey));
            ArgumentNullException.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));

            bool locked = false;

            try
            {
                Monitor.Enter(_cache_lock, ref locked);

                if (!_cache.TryGetValue(cacheKey, out MetadataProvider provider))
                {
                    return false; // Провайдер не существует - обновление не состоялось
                }

                if (provider.DataSource == dataSource &&
                    provider.ConnectionString.CompareTo(connectionString, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true; // Идемпотентность - считаем, что обновление прошло успешно
                }

                Remove(in cacheKey); //NOTE: Внутреннее поле _loader класса MetadataProvider является readonly !!!

                provider = new MetadataProvider(dataSource, in connectionString);

                return _cache.TryAdd(cacheKey, provider); // Если добавились, то обновление прошло успешно
            }
            finally
            {
                if (locked)
                {
                    Monitor.Exit(_cache_lock);
                }
            }
        }
        public static MetadataProvider Get(in string cacheKey)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(cacheKey, nameof(cacheKey));

            if (!_cache.TryGetValue(cacheKey, out MetadataProvider provider))
            {
                return null;
            }

            provider.EnsureInitialized();

            return provider;
        }
        public static MetadataProvider GetOrCreate(DataSourceType dataSource, in string connectionString, out string cacheKey)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));

            cacheKey = connectionString.ToLowerInvariant();
            cacheKey = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(cacheKey)));

            return GetOrCreate(in cacheKey, dataSource, in connectionString);
        }
        public static MetadataProvider GetOrCreate(in string cacheKey, DataSourceType dataSource, in string connectionString)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(cacheKey, nameof(cacheKey));
            ArgumentNullException.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));

            MetadataProvider provider = Get(cacheKey);

            if (provider is not null)
            {
                return provider; // fast path - return existing provider
            }

            bool locked = false;

            try
            {
                Monitor.Enter(_cache_lock, ref locked);

                provider = Get(cacheKey);

                if (provider is not null)
                {
                    return provider; // double-checking
                }

                // long path - create new initialized provider

                provider = Create(dataSource, in connectionString);

                _ = _cache.TryAdd(cacheKey, provider); // add provider to the cache
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
        public static void Reset(in string cacheKey)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(cacheKey, nameof(cacheKey));

            if (_cache.TryGetValue(cacheKey, out MetadataProvider provider))
            {
                provider.Reset();
            }
        }
        public static void Remove(in string cacheKey)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(cacheKey, nameof(cacheKey));
            
            if (_cache.TryRemove(cacheKey, out _))
            {
                //THINK: notify provider users ?
            }
        }
        public static List<MetadataProviderStatus> ToList()
        {
            List<MetadataProviderStatus> list = new();

            MetadataProvider provider;

            foreach (var entry in _cache)
            {
                provider = entry.Value;

                int lastUpdated;

                long elapsed = provider.ElapsedSinceLastUpdate;

                if (elapsed == 0L)
                {
                    lastUpdated = 0;
                }
                else
                {
                    double seconds = TimeSpan.FromMilliseconds(elapsed).TotalSeconds;

                    lastUpdated = seconds > int.MaxValue ? int.MaxValue : (int)seconds;
                }

                list.Add(new MetadataProviderStatus()
                {
                    Name = entry.Key,
                    DataSource = provider.DataSource,
                    ConnectionString = provider.ConnectionString,
                    LastUpdated = lastUpdated,
                    IsInitialized = provider.IsInitialized
                });
            }

            return list;
        }

        #endregion

        private readonly MetadataLoader _loader;
        private readonly DataSourceType _dataSource;
        private readonly string _connectionString;

        private long _lastUpdate = 0L; // milliseconds
        private MetadataRegistry _registry;
        internal MetadataProvider(DataSourceType dataSource, in string connectionString)
        {
            _dataSource = dataSource;
            _connectionString = connectionString;

            _loader = MetadataLoader.Create(_dataSource, in _connectionString);
        }

        #region "PRIVATE INTERFACE"
        private void Reset() { Initialize(true); }
        private void EnsureInitialized() { Initialize(); }
        private void Initialize(bool reset = false)
        {
            if (!reset && IsInitialized) { return; }

            bool locked = false;

            try
            {
                Monitor.Enter(this, ref locked);

                if (!reset && IsInitialized) { return; }

                _registry = _loader.CreateMetadataRegistry();

                RefreshLastUpdateValue();
            }
            finally
            {
                if (locked)
                {
                    Monitor.Exit(this);
                }
            }
        }
        private bool IsInitialized { get { return _registry is not null; } }
        private void ThrowIfNotInitialized()
        {
            if (_registry is null)
            {
                throw new InvalidOperationException("Metadata provider is not initialized");
            }
        }
        private void RefreshLastUpdateValue()
        {
            if (IntPtr.Size == 8) // x64
            {
                _lastUpdate = Environment.TickCount64;
            }
            else // x32
            {
                Volatile.Write(ref _lastUpdate, Environment.TickCount64);
            }
        }
        public long ElapsedSinceLastUpdate
        {
            get
            {
                long lastUpdate = IntPtr.Size == 8 ? _lastUpdate : Volatile.Read(ref _lastUpdate);

                long elapsed = lastUpdate == 0L ? lastUpdate : (Environment.TickCount64 - lastUpdate);

                return elapsed;
            }
        }
        #endregion

        #region "PUBLIC INTERFACE"
        public void Dump(in string tableName, in string fileName, in string outputPath)
        {
            _loader.Dump(in tableName, in fileName, in outputPath);
        }
        public void DumpRaw(in string tableName, in string fileName, in string outputPath)
        {
            _loader.DumpRaw(in tableName, in fileName, in outputPath);
        }
        public DbConnection CreateConnection()
        {
            return DataSourceFactory.GetFactory(_dataSource).Create(in _connectionString);
        }
        public DataSourceType DataSource { get { return _dataSource; } }
        public string ConnectionString { get { return _connectionString; } }

        public int GetYearOffset()
        {
            ThrowIfNotInitialized();

            return _registry.YearOffset;
        }
        public List<ExtensionInfo> GetExtensions()
        {
            ThrowIfNotInitialized();

            return _loader.GetExtensions();
        }
        public List<Configuration> GetConfigurations()
        {
            ThrowIfNotInitialized();

            return _registry.Configurations;
        }
        public Configuration GetConfiguration(in string name = null)
        {
            ThrowIfNotInitialized();

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

        public List<string> GetMetadataNames(in string typeName)
        {
            ThrowIfNotInitialized();

            List<string> names = new();

            Guid type = MetadataLookup.GetMetadataType(in typeName);

            if (type == Guid.Empty)
            {
                return names;
            }

            if (!_registry.TryGetMetadataNames(in typeName, out Dictionary<string, Guid> items))
            {
                return names;
            }

            names = items.Keys.ToList();

            if (names is null)
            {
                return names;
            }

            return names;
        }
        public List<string> GetMetadataNames(in string configurationName, in string typeName)
        {
            ThrowIfNotInitialized();

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

        public EntityDefinition GetMetadataObject(int typeCode)
        {
            ThrowIfNotInitialized();

            if (!_registry.TryGetEntry(typeCode, out MetadataObject entry))
            {
                return null;
            }

            return GetMetadataObject(entry.ToString());
        }
        public EntityDefinition GetMetadataObject(in string fullName)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(fullName, nameof(fullName));

            ThrowIfNotInitialized();

            StringSplitOptions TrimAndRemoveEmpty = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

            Span<Range> names = stackalloc Range[3];

            int count = fullName.Split(names, '.', TrimAndRemoveEmpty);

            //TODO: ReadOnlySpan<char> source = fullName.AsSpan();
            //TODO: Use ReadOnlySpan<char> for type, name and table variables

            string type = count > 0 ? fullName[names[0]] : string.Empty;
            string name = count > 1 ? fullName[names[1]] : string.Empty;
            string table = count > 2 ? fullName[names[2]] : string.Empty;

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
            ThrowIfNotInitialized();

            foreach (MetadataObject entry in _registry.GetMetadataObjects(typeName))
            {
                yield return _loader.Load(in typeName, in entry, in _registry);
            }
        }

        public List<string> ResolveReferences(in List<Guid> references)
        {
            ThrowIfNotInitialized();

            return _registry.ResolveReferences(in references);
        }

        public Guid GetEnumerationValue(in string fullName)
        {
            ThrowIfNotInitialized();

            string[] names = fullName.Split('.');

            if (!_registry.TryGetEntry(names[0], names[1], out Enumeration entry))
            {
                return Guid.Empty;
            }

            return entry.Values[names[2]];
        }
        public Dictionary<string, Guid> GetEnumerationValues(in string fullName)
        {
            ThrowIfNotInitialized();

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

        #endregion

        #region "DATABASE SERVICES"

        public string CompareMetadataToDatabase(List<string> names = null)
        {
            ThrowIfNotInitialized();

            return new MetadataComparer(this, _loader).CompareMetadataToDatabase(names);
        }

        #endregion
    }
}