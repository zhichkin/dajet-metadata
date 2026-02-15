using DaJet.Data.PostgreSql;
using DaJet.Data.Sqlite;
using DaJet.Data.SqlServer;
using System.Data.Common;

namespace DaJet.Data
{
    public abstract class DataSourceFactory
    {
        private static readonly DataSourceFactory[] _factories = new DataSourceFactory[3];
        static DataSourceFactory()
        {
            _factories[(int)DataSourceType.Sqlite] = new SqliteDataSourceFactory();
            _factories[(int)DataSourceType.SqlServer] = new MsDataSourceFactory();
            _factories[(int)DataSourceType.PostgreSql] = new PgDataSourceFactory();
        }
        public static DataSourceFactory GetFactory(DataSourceType dataSource)
        {
            return _factories[(int)dataSource];
        }
        public static DataSourceFactory GetFactory(in Uri uri)
        {
            return GetFactory(GetDataSourceType(in uri));
        }
        public static DataSourceType GetDataSourceType(in Uri uri)
        {
            if (uri.Scheme == "mssql")
            {
                return DataSourceType.SqlServer;
            }
            else if (uri.Scheme == "pgsql")
            {
                return DataSourceType.PostgreSql;
            }
            else if (uri.Scheme == "sqlite")
            {
                return DataSourceType.Sqlite;
            }

            throw new InvalidOperationException($"Unknown data source: [{uri.Scheme}]");
        }

        public abstract DbConnection Create(in Uri uri);
        public abstract DbConnection Create(in string connectionString);
    }
}