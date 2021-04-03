using DaJet.Metadata.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DaJet.Metadata
{
    /// <summary>
    /// Интерфейс для чтения метаданных прикладных объектов конфигурации 1С.
    /// Является точкой входа для использования библиотеки и играет роль фасада для всех остальных интерфейсов и парсеров.
    /// Реализует логику многопоточной загрузки объектов конфигурации 1С.
    /// </summary>
    public interface IMetadataService
    {
        ///<summary>Используемый провайдер баз данных</summary>
        DatabaseProvider DatabaseProvider { get; }
        ///<summary>Строка подключения к базе данных СУБД</summary>
        string ConnectionString { get; }
        ///<summary>Устанавливает провайдера базы данных СУБД</summary>
        ///<param name="provider">Значение перечисления <see cref="DatabaseProvider"/> (Microsoft SQL Server или PostgreSQL)</param>
        ///<returns>Возвращает ссылку на самого себя</returns>
        IMetadataService UseDatabaseProvider(DatabaseProvider databaseProvider);
        ///<summary>Устанавливает строку подключения к базе данных СУБД</summary>
        ///<param name="connectionString">Строка подключения к базе данных СУБД</param>
        ///<returns>Возвращает ссылку на самого себя</returns>
        IMetadataService UseConnectionString(string connectionString);
        ///<summary>Формирует строку подключения к базе данных по параметрам</summary>
        ///<param name="server">Имя или сетевой адрес сервера СУБД</param>
        ///<param name="database">Имя базы данных</param>
        ///<param name="userName">Имя пользователя (если не указано, то используется Windows аутентификация)</param>
        ///<param name="password">Пароль пользователя (используется в случае аутентификации средствами СУБД)</param>
        ///<returns>Возвращает ссылку на самого себя</returns>
        IMetadataService ConfigureConnectionString(string server, string database, string userName, string password);
        ///<summary>Загружает метаданные прикладных объектов конфигурации 1С из таблиц СУБД</summary>
        ///<returns>Возвращает объект, содержащий метаданные прикладных объектов конфигурации 1С</returns>
        InfoBase LoadInfoBase();
        ///<summary>Выполняет чтение свойств конфигурации 1С</summary>
        ///<returns>Значения свойств конфигурации <see cref="ConfigInfo"/></returns>
        ConfigInfo ReadConfigurationProperties();
        ///<summary>Выполняет сравнение и слияние свойств объекта метаданных с полями таблицы СУБД</summary>
        ///<param name="metaObject">Объект метаданных</param>
        void EnrichFromDatabase(ApplicationObject metaObject);
        ///<summary>Выполняет сравнение свойств объекта метаданных с полями таблицы СУБД на соответствие</summary>
        ///<param name="metaObject">Объект метаданных, для которого выполняется сравнение</param>
        ///<param name="delete">Список имён полей, которые есть в объекте метаданных, но нет в таблице базы данных</param>
        ///<param name="insert">Список имён полей, которые есть в таблице базы данных, но нет в объекте метаданных</param>
        ///<returns>Результат проверки на соответствие</returns>
        bool CompareWithDatabase(ApplicationObject metaObject, out List<string> delete, out List<string> insert);
        ///<summary>Фасад для интерфейса <see cref="IConfigurationFileParser"/></summary>
        string GetConfigurationFileName();
        ///<summary>Фасад для интерфейса <see cref="IConfigFileReader"/></summary>
        byte[] ReadBytes(string fileName);
        ///<summary>Фасад для интерфейса <see cref="IConfigFileReader"/></summary>
        StreamReader CreateReader(byte[] fileData);
    }
    /// <summary>
    /// Класс, реализующий интерфейс <see cref="IMetadataReader"/>, для чтения метаданных из таблиц СУБД
    /// </summary>
    public sealed class MetadataService : IMetadataService
    {
        private sealed class ReadMetaUuidParameters
        {
            internal InfoBase InfoBase { get; set; }
            internal ApplicationObject ApplicationObject { get; set; }
        }

        private const string DBNAMES_FILE_NAME = "DBNames";

        private readonly IDBNamesFileParser DBNamesFileParser;
        private readonly IConfigFileReader ConfigFileReader;
        private readonly IConfigurationFileParser ConfigurationFileParser;
        private readonly IApplicationObjectFileParser ApplicationObjectFileParser;
        private readonly ISqlMetadataReader SqlMetadataReader;
        private readonly MetadataCompareAndMergeService CompareMergeService;

        public string ConnectionString { get; private set; } = string.Empty;
        public DatabaseProvider DatabaseProvider { get; private set; } = DatabaseProvider.SQLServer;

        public MetadataService()
        {
            DBNamesFileParser = new DBNamesFileParser();
            ConfigFileReader = new ConfigFileReader();
            ConfigurationFileParser = new ConfigurationFileParser(ConfigFileReader);
            ApplicationObjectFileParser = new ApplicationObjectFileParser();
            SqlMetadataReader = new SqlMetadataReader();
            CompareMergeService = new MetadataCompareAndMergeService();
        }
        public IMetadataService UseDatabaseProvider(DatabaseProvider databaseProvider)
        {
            DatabaseProvider = databaseProvider;
            DBNamesFileParser.UseDatabaseProvider(DatabaseProvider);
            SqlMetadataReader.UseDatabaseProvider(DatabaseProvider);
            ConfigFileReader.UseDatabaseProvider(DatabaseProvider);
            ApplicationObjectFileParser.UseDatabaseProvider(DatabaseProvider);
            return this;
        }
        public IMetadataService UseConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
            SqlMetadataReader.UseConnectionString(ConnectionString);
            ConfigFileReader.UseConnectionString(ConnectionString);
            return this;
        }
        public IMetadataService ConfigureConnectionString(string server, string database, string userName, string password)
        {
            ConfigFileReader.ConfigureConnectionString(server, database, userName, password);
            ConnectionString = ConfigFileReader.ConnectionString;
            SqlMetadataReader.UseConnectionString(ConnectionString);
            return this;
        }

        public byte[] ReadBytes(string fileName)
        {
            return ConfigFileReader.ReadBytes(fileName);
        }
        public StreamReader CreateReader(byte[] fileData)
        {
            return ConfigFileReader.CreateReader(fileData);
        }

        public string GetConfigurationFileName()
        {
            return ConfigurationFileParser.GetConfigurationFileName();
        }
        public ConfigInfo ReadConfigurationProperties()
        {
            return ConfigurationFileParser.ReadConfigurationProperties();
        }

        public void EnrichFromDatabase(ApplicationObject metaObject)
        {
            List<SqlFieldInfo> sqlFields = SqlMetadataReader.GetSqlFieldsOrderedByName(metaObject.TableName);
            if (sqlFields.Count == 0) return;
            CompareMergeService.MergeProperties(metaObject, sqlFields);
        }
        public bool CompareWithDatabase(ApplicationObject metaObject, out List<string> delete, out List<string> insert)
        {
            delete = new List<string>();
            insert = new List<string>();

            List<SqlFieldInfo> sqlFields = SqlMetadataReader.GetSqlFieldsOrderedByName(metaObject.TableName);
            if (sqlFields.Count == 0) return false;

            List<string> targetFields = CompareMergeService.PrepareComparison(metaObject.Properties);
            List<string> sourceFields = CompareMergeService.PrepareComparison(sqlFields);

            CompareMergeService.Compare(targetFields, sourceFields, out delete, out insert);

            //int match = targetFields.Count - delete.Count;
            //int unmatch = sourceFields.Count - match;
            //return (insert.Count == unmatch);

            return (delete.Count + insert.Count) == 0;
        }

        public InfoBase LoadInfoBase()
        {
            InfoBase infoBase = new InfoBase();
            ReadDBNames(infoBase);
            ApplicationObjectFileParser.UseInfoBase(infoBase);
            ReadMetaUuids(infoBase);
            ReadApplicationObjects(infoBase);
            return infoBase;
        }
        private void ReadDBNames(InfoBase infoBase)
        {
            byte[] fileData = ConfigFileReader.ReadBytes(DBNAMES_FILE_NAME);
            using (StreamReader reader = ConfigFileReader.CreateReader(fileData))
            {
                DBNamesFileParser.Parse(reader, infoBase);
            }
        }
        private void ReadMetaUuids(InfoBase infoBase)
        {
            foreach (var collection in infoBase.ReferenceTypes)
            {
                int i = 0;
                Task[] tasks = new Task[collection.Count];
                foreach (var item in collection)
                {
                    ReadMetaUuidParameters parameters = new ReadMetaUuidParameters()
                    {
                        InfoBase = infoBase,
                        ApplicationObject = item.Value
                    };
                    tasks[i] = Task.Factory.StartNew(
                        ReadMetaUuid,
                        parameters,
                        CancellationToken.None,
                        TaskCreationOptions.DenyChildAttach,
                        TaskScheduler.Default);
                    ++i;
                }

                try
                {
                    Task.WaitAll(tasks);
                }
                catch (AggregateException ex)
                {
                    foreach (Exception ie in ex.InnerExceptions)
                    {
                        if (ie is OperationCanceledException)
                        {
                            //TODO: log exception
                            //break;
                        }
                        else
                        {
                            //TODO: log exception
                        }
                    }
                }
            }
        }
        private void ReadMetaUuid(object parameters)
        {
            if (!(parameters is ReadMetaUuidParameters input)) return;

            byte[] fileData = ConfigFileReader.ReadBytes(input.ApplicationObject.FileName.ToString());
            using (StreamReader stream = ConfigFileReader.CreateReader(fileData))
            {
                ApplicationObjectFileParser.ParseMetaUuid(stream, input.ApplicationObject);
            }
            input.InfoBase.ReferenceTypeUuids.TryAdd(input.ApplicationObject.Uuid, input.ApplicationObject);
        }
        private void ReadApplicationObjects(InfoBase infoBase)
        {
            ReadValueTypes(infoBase);
            ReadReferenceTypes(infoBase);
        }
        private void ReadValueTypes(InfoBase infoBase)
        {
            foreach (var collection in infoBase.ValueTypes)
            {
                int i = 0;
                Task[] tasks = new Task[collection.Count];
                foreach (var item in collection)
                {
                    tasks[i] = Task.Factory.StartNew(
                        ReadApplicationObject,
                        item.Value,
                        CancellationToken.None,
                        TaskCreationOptions.DenyChildAttach,
                        TaskScheduler.Default);
                    ++i;
                }

                try
                {
                    Task.WaitAll(tasks);
                }
                catch (AggregateException ex)
                {
                    foreach (Exception ie in ex.InnerExceptions)
                    {
                        if (ie is OperationCanceledException)
                        {
                            //TODO: log exception
                            //break;
                        }
                        else
                        {
                            //TODO: log exception
                        }
                    }
                }
            }
        }
        private void ReadReferenceTypes(InfoBase infoBase)
        {
            foreach (var collection in infoBase.ReferenceTypes)
            {
                int i = 0;
                Task[] tasks = new Task[collection.Count];
                foreach (var item in collection)
                {
                    tasks[i] = Task.Factory.StartNew(
                        ReadApplicationObject,
                        item.Value,
                        CancellationToken.None,
                        TaskCreationOptions.DenyChildAttach,
                        TaskScheduler.Default);
                    ++i;
                }

                try
                {
                    Task.WaitAll(tasks);
                }
                catch (AggregateException ex)
                {
                    foreach (Exception ie in ex.InnerExceptions)
                    {
                        if (ie is OperationCanceledException)
                        {
                            //TODO: log exception
                            //break;
                        }
                        else
                        {
                            //TODO: log exception
                        }
                    }
                }
            }
        }
        private void ReadApplicationObject(object metaObject)
        {
            ApplicationObject obj = (ApplicationObject)metaObject;
            byte[] fileData = ConfigFileReader.ReadBytes(obj.FileName.ToString());
            if (fileData == null)
            {
                return; // TODO: log error "Metadata file is not found"
            }
            using (StreamReader stream = ConfigFileReader.CreateReader(fileData))
            {
                ApplicationObjectFileParser.ParseApplicationObject(stream, obj);
            }
        }
    }
}