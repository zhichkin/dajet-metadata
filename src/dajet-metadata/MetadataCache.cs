using DaJet.Metadata.Model;
using System;
using System.Collections.Concurrent;

namespace DaJet.Metadata
{
    public interface IMetadataCache : IDisposable
    {
        InfoBase TryGet(string key, out string error);
        void Add(string key, MetadataCacheOptions options);
        void Remove(string key);
    }
    public sealed class MetadataCacheOptions
    {
        public int Expiration { get; set; } = 600; // seconds
        public string ConnectionString { get; set; } = string.Empty;
        public DatabaseProvider DatabaseProvider { get; set; } = DatabaseProvider.SQLServer;
    }
    internal sealed class CacheEntry
    {
        private readonly MetadataCacheOptions _options;
        private readonly RWLockSlim _lock = new RWLockSlim();
        private readonly WeakReference<InfoBase> _value = new WeakReference<InfoBase>(null);
        private long _lastUpdate = 0L; // milliseconds
        internal CacheEntry(MetadataCacheOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }
        internal MetadataCacheOptions Options { get { return _options; } }
        internal RWLockSlim.UpgradeableLockToken UpdateLock() { return _lock.UpgradeableLock(); }
        internal InfoBase Value
        {
            set
            {
                using (_lock.WriteLock())
                {
                    _value.SetTarget(value);
                    _lastUpdate = Environment.TickCount64;
                }
            }
            get
            {
                using (_lock.ReadLock())
                {
                    if (_value.TryGetTarget(out InfoBase value))
                    {
                        return value;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
        internal bool IsExpired
        {
            get
            {
                long elapsed = (Environment.TickCount64 - _lastUpdate) / 1000; // seconds
                return _options.Expiration < elapsed;
            }
        }
        internal void Dispose()
        {
            _lock.Dispose();
            _value.SetTarget(null);
            _options.ConnectionString = null;
        }
    }
    public sealed class MetadataCache : IMetadataCache
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new ConcurrentDictionary<string, CacheEntry>();
        public void Add(string key, MetadataCacheOptions options)
        {
            _ = _cache.TryAdd(key, new CacheEntry(options));
        }
        public InfoBase TryGet(string key, out string error)
        {
            if (!_cache.TryGetValue(key, out CacheEntry entry))
            {
                error = $"Metadata cache entry for key [{key}] is not found.";
                return null;
            }

            return TryGetOrUpdate(in entry, out error);
        }
        private InfoBase TryGetOrUpdate(in CacheEntry entry, out string error)
        {
            InfoBase value = entry.Value;
            
            if (value != null && !entry.IsExpired)
            {
                error = string.Empty;
                return value;
            }

            using (entry.UpdateLock())
            {
                value = entry.Value;

                if (value != null && !entry.IsExpired)
                {
                    error = string.Empty;
                    return value;
                }

                if (new MetadataService()
                    .UseDatabaseProvider(entry.Options.DatabaseProvider)
                    .UseConnectionString(entry.Options.ConnectionString)
                    .TryOpenInfoBase(out value, out error))
                {
                    entry.Value = value; // This assignment internally refreshes the last update timestamp
                }

                return value;
            }
        }
        public void Remove(string key)
        {
            if (_cache.TryRemove(key, out CacheEntry entry))
            {
                entry?.Dispose();
            }
        }
        public void Dispose()
        {
            foreach (CacheEntry entry in _cache.Values)
            {
                entry.Dispose();
            }
            _cache.Clear();
        }
    }
}