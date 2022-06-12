using Microsoft.Data.SqlClient;

namespace DaJet.CodeGenerator.SqlServer
{
    public sealed class QueryExecutor : QueryExecutor<SqlConnection, SqlCommand>
    {
        protected override SqlConnection GetDbConnection()
        {
            return new SqlConnection(ConnectionString);
        }
        protected override SqlCommand GetDbCommand(in SqlConnection connection)
        {
            return connection.CreateCommand();
        }
        protected override void ConfigureQueryParameters(in SqlCommand command, in Dictionary<string, object> parameters)
        {
            foreach (var parameter in parameters)
            {
                command.Parameters.AddWithValue(parameter.Key, parameter.Value);
            }
        }
    }
}