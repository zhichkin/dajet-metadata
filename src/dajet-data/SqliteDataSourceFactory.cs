using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace DaJet.Data.Sqlite
{
    public sealed class SqliteDataSourceFactory : DataSourceFactory
    {
        private static string GetDatabaseFilePath(in Uri uri)
        {
            if (uri.Scheme != "sqlite")
            {
                throw new InvalidOperationException(uri.ToString());
            }

            string filePath = uri.AbsoluteUri.Replace("sqlite://", string.Empty);

            int question = filePath.IndexOf('?');

            if (question > -1)
            {
                filePath = filePath[..question];
            }

            filePath = filePath.TrimEnd('/').TrimEnd('\\').Replace('/', '\\');

            string databasePath = Path.Combine(AppContext.BaseDirectory, filePath);

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                databasePath = databasePath.Replace('\\', '/');
            }

            return databasePath;
        }
        private static SqliteOpenMode GetConnectionMode(in Uri uri)
        {
            if (uri.Query is null)
            {
                return SqliteOpenMode.ReadWriteCreate;
            }

            string mode = string.Empty;

            string[] parameters = uri.Query.Split('?', '&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parameters is not null && parameters.Length > 0)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    string[] parameter = parameters[i].Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    if (parameter.Length == 2 && parameter[0] == "mode")
                    {
                        mode = parameter[1]; break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(mode))
            {
                return SqliteOpenMode.ReadWriteCreate;
            }

            if (!Enum.TryParse(mode, out SqliteOpenMode value))
            {
                return SqliteOpenMode.ReadWriteCreate;
            }

            return value;
        }
        private static string BuildConnectionString(in Uri uri)
        {
            var builder = new SqliteConnectionStringBuilder()
            {
                Mode = GetConnectionMode(in uri),
                DataSource = GetDatabaseFilePath(in uri)
            };

            return builder.ToString();
        }
        public static SqliteConnection CreateConnection(in string connectionString)
        {
            return new SqliteConnection(connectionString);
        }

        public override DbConnection Create(in Uri uri)
        {
            return CreateConnection(BuildConnectionString(in uri));
        }
        public override DbConnection Create(in string connectionString)
        {
            return CreateConnection(in connectionString);
        }
    }
}