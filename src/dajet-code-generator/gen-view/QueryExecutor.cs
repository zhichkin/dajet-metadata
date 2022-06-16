using System.Data;
using System.Data.Common;

namespace DaJet.CodeGenerator
{
    internal interface IQueryExecutor
    {
        T ExecuteScalar<T>(in string script, int timeout);
        void ExecuteNonQuery(in string script, int timeout);
        void TxExecuteNonQuery(in List<string> scripts, int timeout);
        IEnumerable<IDataReader> ExecuteReader(string script, int timeout);
        IEnumerable<IDataReader> ExecuteReader(string script, int timeout, Dictionary<string, object> parameters);
    }
    public abstract class QueryExecutor<TConnection, TCommand> : IQueryExecutor
        where TConnection : DbConnection, new()
        where TCommand : DbCommand, new()
    {
        public string ConnectionString { get; set; } = string.Empty;
        protected abstract TConnection GetDbConnection();
        protected abstract TCommand GetDbCommand(in TConnection connection);
        protected abstract void ConfigureQueryParameters(in TCommand command, in Dictionary<string, object> parameters);
        public T ExecuteScalar<T>(in string script, int timeout)
        {
            T? result = default;

            using (TConnection connection = GetDbConnection())
            {
                connection.Open();

                using (TCommand command = GetDbCommand(in connection))
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
        public void ExecuteNonQuery(in string script, int timeout)
        {
            using (TConnection connection = GetDbConnection())
            {
                connection.Open();

                using (TCommand command = GetDbCommand(in connection))
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = script;
                    command.CommandTimeout = timeout;

                    _ = command.ExecuteNonQuery();
                }
            }
        }
        public void TxExecuteNonQuery(in List<string> scripts, int timeout)
        {
            using (TConnection connection = GetDbConnection())
            {
                connection.Open();

                using (DbTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    using (TCommand command = GetDbCommand(in connection))
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
        public IEnumerable<IDataReader> ExecuteReader(string script, int timeout)
        {
            using (TConnection connection = GetDbConnection())
            {
                connection.Open();

                using (TCommand command = GetDbCommand(in connection))
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
        public IEnumerable<IDataReader> ExecuteReader(string script, int timeout, Dictionary<string, object> parameters)
        {
            using (TConnection connection = GetDbConnection())
            {
                connection.Open();

                using (TCommand command = GetDbCommand(in connection))
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
    }
}