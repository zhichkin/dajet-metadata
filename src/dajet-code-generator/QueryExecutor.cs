using DaJet.Metadata;
using Microsoft.Data.SqlClient;
using Npgsql;
using System.Data;
using System.Data.Common;

namespace DaJet.CodeGenerator
{
    internal sealed class QueryExecutor
    {
        private string? _connectionString;
        private readonly DatabaseProvider _provider;
        internal QueryExecutor(DatabaseProvider provider)
        {
            _provider = provider;
        }
        internal void UseConnectionString(string connectionString)
        {
            _connectionString = connectionString;
        }
        private DbConnection GetDbConnection()
        {
            if (_provider == DatabaseProvider.SQLServer)
            {
                return new SqlConnection(_connectionString);
            }
            return new NpgsqlConnection(_connectionString);
        }
        internal T ExecuteScalar<T>(in string script, int timeout)
        {
            T? result = default;

            using (DbConnection connection = GetDbConnection())
            {
                connection.Open();

                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = script;
                    command.CommandTimeout = timeout; // seconds

                    object? value = command.ExecuteScalar();

                    if (value != null)
                    {
                        result = (T)value;
                    }
                }
            }

            return result!;
        }
        internal void ExecuteNonQuery(in string script, int timeout)
        {
            using (DbConnection connection = GetDbConnection())
            {
                connection.Open();

                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = script;
                    command.CommandTimeout = timeout;

                    _ = command.ExecuteNonQuery();
                }
            }
        }
        internal void TxExecuteNonQuery(in List<string> scripts, int timeout)
        {
            using (DbConnection connection = GetDbConnection())
            {
                connection.Open();

                using (DbTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.Connection = connection;
                        command.Transaction = transaction;
                        command.CommandType = CommandType.Text;
                        command.CommandTimeout = timeout;

                        try
                        {
                            foreach (string script in scripts)
                            {
                                command.CommandText = script;

                                _ = command.ExecuteNonQuery();
                            }

                            transaction.Commit();
                        }
                        catch
                        {
                            try
                            {
                                transaction.Rollback();
                            }
                            finally
                            {
                                // do nothing
                            }
                            throw;
                        }
                    }
                }
            }
        }
        internal IEnumerable<IDataReader> ExecuteReader(string script, int timeout)
        {
            using (DbConnection connection = GetDbConnection())
            {
                connection.Open();

                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = script;
                    command.CommandTimeout = timeout;

                    using (IDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return reader;
                        }
                        reader.Close();
                    }
                }
            }
        }
        internal IEnumerable<IDataReader> ExecuteReader(string script, int timeout, Dictionary<string, object> parameters)
        {
            using (DbConnection connection = GetDbConnection())
            {
                connection.Open();

                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = script;
                    command.CommandTimeout = timeout;

                    ConfigureQueryParameters(in command, in parameters);

                    using (IDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return reader;
                        }
                        reader.Close();
                    }
                }
            }
        }
        private void ConfigureQueryParameters(in DbCommand command, in Dictionary<string, object> parameters)
        {
            if (command is SqlCommand ms_command)
            {
                foreach (var parameter in parameters)
                {
                    ms_command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                }
            }
            else if(command is NpgsqlCommand pg_command)
            {
                foreach (var parameter in parameters)
                {
                    pg_command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                }
            }
        }
    }
}