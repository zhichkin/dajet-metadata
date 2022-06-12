using Npgsql;

namespace DaJet.CodeGenerator.PostgreSql
{
    public sealed class QueryExecutor : QueryExecutor<NpgsqlConnection, NpgsqlCommand>
    {
        protected override NpgsqlConnection GetDbConnection()
        {
            return new NpgsqlConnection(ConnectionString);
        }
        protected override NpgsqlCommand GetDbCommand(in NpgsqlConnection connection)
        {
            return connection.CreateCommand();
        }
        protected override void ConfigureQueryParameters(in NpgsqlCommand command, in Dictionary<string, object> parameters)
        {
            foreach (var parameter in parameters)
            {
                command.Parameters.AddWithValue(parameter.Key, parameter.Value);
            }
        }
    }
}