using Microsoft.Data.SqlClient;
using System.Data.Common;
using System.Text;
using System.Web;

namespace DaJet.Data.SqlServer
{
    internal sealed class MsDataSourceFactory : DataSourceFactory
    {
        private static string BuildConnectionString(in Uri uri)
        {
            string server = string.Empty;
            string database = string.Empty;

            if (uri.Segments.Length == 3)
            {
                server = $"{uri.Host}{(uri.Port > 0 ? ":" + uri.Port.ToString() : string.Empty)}\\{uri.Segments[1].TrimEnd('/')}";
                database = uri.Segments[2].TrimEnd('/');
            }
            else
            {
                server = uri.Host + (uri.Port > 0 ? ":" + uri.Port.ToString() : string.Empty);
                database = uri.Segments[1].TrimEnd('/');
            }

            var builder = new SqlConnectionStringBuilder()
            {
                Encrypt = false,
                DataSource = server,
                InitialCatalog = database
            };

            string[] userpass = uri.UserInfo.Split(':');

            if (userpass is not null && userpass.Length == 2)
            {
                builder.UserID = HttpUtility.UrlDecode(userpass[0], Encoding.UTF8);
                builder.Password = HttpUtility.UrlDecode(userpass[1], Encoding.UTF8);
            }
            else
            {
                builder.IntegratedSecurity = true;
            }

            return builder.ToString();
        }
        public static SqlConnection CreateConnection(in string connectionString)
        {
            return new SqlConnection(connectionString);
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