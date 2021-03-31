using DaJet.Metadata.Model;
using Microsoft.Data.SqlClient;
using Npgsql;
using System;
using System.Data;
using System.Data.Common;

namespace DaJet.Metadata.Mappers
{
    public sealed class PublicationDataMapper
    {
        private const string MS_SELECT_SUBSCRIBERS_QUERY_TEMPLATE =
            "SELECT _IDRRef, _Code, _Description, CAST(_Marked AS bit), _PredefinedID FROM {0};";
        private const string PG_SELECT_SUBSCRIBERS_QUERY_TEMPLATE =
            "SELECT _idrref, CAST(_code AS varchar), CAST(_description AS varchar), _marked, _predefinedid FROM {0};";
        public string ConnectionString { get; private set; } = string.Empty;
        public DatabaseProvider DatabaseProvider { get; private set; } = DatabaseProvider.SQLServer;
        public void UseConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
        }
        public void UseDatabaseProvider(DatabaseProvider databaseProvider)
        {
            DatabaseProvider = databaseProvider;
        }
        private string CreateSelectSubscribersScript(Publication publication)
        {
            if (DatabaseProvider == DatabaseProvider.SQLServer)
            {
                return string.Format(
                    MS_SELECT_SUBSCRIBERS_QUERY_TEMPLATE,
                    publication.TableName);
            }
            else
            {
                return string.Format(
                    PG_SELECT_SUBSCRIBERS_QUERY_TEMPLATE,
                    publication.TableName.ToLowerInvariant());
            }
        }
        private DbConnection CreateDbConnection()
        {
            if (DatabaseProvider == DatabaseProvider.SQLServer)
            {
                return new SqlConnection(ConnectionString);
            }
            return new NpgsqlConnection(ConnectionString);
        }
        public void SelectSubscribers(Publication publication)
        {
            publication.Subscribers.Clear();

            using (DbConnection connection = CreateDbConnection())
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = CreateSelectSubscribersScript(publication);

                connection.Open();
                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Guid predefinedid = new Guid((byte[])reader[4]);
                        if (predefinedid == Guid.Empty)
                        {
                            Subscriber subscriber = new Subscriber();
                            subscriber.Uuid = new Guid((byte[])reader[0]);
                            subscriber.Code = reader.GetString(1);
                            subscriber.Name = reader.GetString(2);
                            subscriber.IsMarkedForDeletion = reader.GetBoolean(3);
                            publication.Subscribers.Add(subscriber);
                        }
                        else
                        {
                            Publisher publisher = new Publisher();
                            publisher.Uuid = new Guid((byte[])reader[0]);
                            publisher.Code = reader.GetString(1);
                            publisher.Name = reader.GetString(2);
                            publication.Publisher = publisher;
                        }
                    }
                }
            }
        }
    }
}