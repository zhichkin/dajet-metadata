using Npgsql;
using System.Data.Common;
using System.Text;
using System.Web;

namespace DaJet.Data.PostgreSql
{
    internal sealed class PgDataSourceFactory : DataSourceFactory
    {
        private static readonly object _cache_lock = new();
        private static readonly Dictionary<string, NpgsqlDataSource> _cache = new();
        static PgDataSourceFactory()
        {
            AppContext.SetSwitch("Npgsql.EnableSqlRewriting", false);
        }
        private static NpgsqlDataSource GetOrCreateDataSource(in string cacheKey, in string connectionString)
        {
            if (_cache.TryGetValue(cacheKey, out NpgsqlDataSource source))
            {
                return source; // fast path
            }

            bool locked = false;

            try
            {
                Monitor.Enter(_cache_lock, ref locked);

                if (_cache.TryGetValue(cacheKey, out source))
                {
                    return source; // double-checking
                }

                source = new NpgsqlDataSourceBuilder(connectionString).Build();

                _cache.Add(cacheKey, source);
            }
            finally
            {
                if (locked)
                {
                    Monitor.Exit(_cache_lock);
                }
            }

            return source;
        }
        private static string CreateCacheKey(in string connectionString)
        {
            NpgsqlConnectionStringBuilder builder = new(connectionString);

            string key = string.Format("pgsql:{0}:{1}:{2}",
                builder.Host,
                builder.Port == 0 ? 5432 : builder.Port,
                builder.Database).ToLowerInvariant();

            return key;
        }
        private static string BuildConnectionString(in Uri uri)
        {
            var builder = new NpgsqlConnectionStringBuilder()
            {
                Host = uri.Host,
                Port = uri.Port,
                Database = uri.Segments[1].TrimEnd('/')
            };

            string[] userpass = uri.UserInfo.Split(':');

            if (userpass is not null && userpass.Length == 2)
            {
                builder.Username = HttpUtility.UrlDecode(userpass[0], Encoding.UTF8);
                builder.Password = HttpUtility.UrlDecode(userpass[1], Encoding.UTF8);
            }

            return builder.ToString();
        }
        
        public static NpgsqlDataSource GetDataSource(in Uri uri)
        {
            string connectionString = BuildConnectionString(in uri);

            string cacheKey = CreateCacheKey(in connectionString);

            return GetOrCreateDataSource(in cacheKey, in connectionString);
        }
        public static NpgsqlDataSource GetDataSource(in string connectionString)
        {
            string cacheKey = CreateCacheKey(in connectionString);

            return GetOrCreateDataSource(in cacheKey, in connectionString);
        }
        
        public static NpgsqlConnection CreateConnection(in string connectionString)
        {
            return GetDataSource(in connectionString).CreateConnection();
        }

        public override DbConnection Create(in Uri uri)
        {
            return GetDataSource(in uri).CreateConnection();
        }
        public override DbConnection Create(in string connectionString)
        {
            return GetDataSource(in connectionString).CreateConnection();
        }
    }
}