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
    /// Реализует логику многопоточной загрузи объектов конфигурации 1С.
    /// </summary>
    public interface IMetadataService
    {
        ///<summary>Используемый провайдер баз данных</summary>
        DatabaseProviders DatabaseProvider { get; }
        ///<summary>Строка подключения к базе данных СУБД</summary>
        string ConnectionString { get; }
        ///<summary>Устанавливает провайдера базы данных СУБД</summary>
        ///<param name="provider">Значение перечисления <see cref="DatabaseProviders"/> (Microsoft SQL Server или PostgreSQL)</param>
        ///<returns>Возвращает ссылку на самого себя</returns>
        IMetadataService UseDatabaseProvider(DatabaseProviders databaseProvider);
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
        void EnrichFromDatabase(MetadataObject metaObject);
        ///<summary>Выполняет сравнение свойств объекта метаданных с полями таблицы СУБД на соответствие</summary>
        ///<param name="metaObject">Объект метаданных, для которого выполняется сравнение</param>
        ///<param name="delete">Список имён полей, которые есть в объекте метаданных, но нет в таблице базы данных</param>
        ///<param name="insert">Список имён полей, которые есть в таблице базы данных, но нет в объекте метаданных</param>
        ///<returns>Результат проверки на соответствие</returns>
        bool CompareWithDatabase(MetadataObject metaObject, out List<string> delete, out List<string> insert);
        ///<summary>Фасад для интерфейса <see cref="IConfigurationFileParser"/></summary>
        string GetConfigurationFileName();
        ///<summary>Фасад для интерфейса <see cref="IMetadataFileReader"/></summary>
        byte[] ReadBytes(string fileName);
        ///<summary>Фасад для интерфейса <see cref="IMetadataFileReader"/></summary>
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
            internal MetadataObject MetadataObject { get; set; }
        }

        private const string DBNAMES_FILE_NAME = "DBNames";

        private readonly IDBNamesFileParser DBNamesFileParser;
        private readonly IMetadataFileReader MetadataFileReader;
        private readonly IConfigurationFileParser ConfigurationFileParser;
        private readonly IMetadataObjectFileParser MetadataObjectFileParser;
        private readonly ISqlMetadataReader SqlMetadataReader;
        private readonly MetadataCompareAndMergeService CompareMergeService;

        public string ConnectionString { get; private set; } = string.Empty;
        public DatabaseProviders DatabaseProvider { get; private set; } = DatabaseProviders.SQLServer;

        public MetadataService()
        {
            DBNamesFileParser = new DBNamesFileParser();
            MetadataFileReader = new MetadataFileReader();
            ConfigurationFileParser = new ConfigurationFileParser(MetadataFileReader);
            MetadataObjectFileParser = new MetadataObjectFileParser();
            SqlMetadataReader = new SqlMetadataReader();
            CompareMergeService = new MetadataCompareAndMergeService();
        }
        public IMetadataService UseDatabaseProvider(DatabaseProviders databaseProvider)
        {
            DatabaseProvider = databaseProvider;
            SqlMetadataReader.UseDatabaseProvider(DatabaseProvider);
            MetadataFileReader.UseDatabaseProvider(DatabaseProvider);
            return this;
        }
        public IMetadataService UseConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
            SqlMetadataReader.UseConnectionString(ConnectionString);
            MetadataFileReader.UseConnectionString(ConnectionString);
            return this;
        }
        public IMetadataService ConfigureConnectionString(string server, string database, string userName, string password)
        {
            MetadataFileReader.ConfigureConnectionString(server, database, userName, password);
            ConnectionString = MetadataFileReader.ConnectionString;
            SqlMetadataReader.UseConnectionString(ConnectionString);
            return this;
        }

        public byte[] ReadBytes(string fileName)
        {
            return MetadataFileReader.ReadBytes(fileName);
        }
        public StreamReader CreateReader(byte[] fileData)
        {
            return MetadataFileReader.CreateReader(fileData);
        }

        public string GetConfigurationFileName()
        {
            return ConfigurationFileParser.GetConfigurationFileName();
        }
        public ConfigInfo ReadConfigurationProperties()
        {
            return ConfigurationFileParser.ReadConfigurationProperties();
        }

        public void EnrichFromDatabase(MetadataObject metaObject)
        {
            List<SqlFieldInfo> sqlFields = SqlMetadataReader.GetSqlFieldsOrderedByName(metaObject.TableName);
            if (sqlFields.Count == 0) return;
            CompareMergeService.MergeProperties(metaObject, sqlFields);
        }
        public bool CompareWithDatabase(MetadataObject metaObject, out List<string> delete, out List<string> insert)
        {
            delete = new List<string>();
            insert = new List<string>();

            List<SqlFieldInfo> sqlFields = SqlMetadataReader.GetSqlFieldsOrderedByName(metaObject.TableName);
            if (sqlFields.Count == 0) return false;

            List<string> targetFields = CompareMergeService.PrepareComparison(metaObject.Properties);
            List<string> sourceFields = CompareMergeService.PrepareComparison(sqlFields);

            CompareMergeService.Compare(targetFields, sourceFields, out delete, out insert);

            int match = targetFields.Count - delete.Count;
            int unmatch = sourceFields.Count - match;
            return (insert.Count == unmatch);
        }

        public InfoBase LoadInfoBase()
        {
            InfoBase infoBase = new InfoBase();
            ReadDBNames(infoBase);
            MetadataObjectFileParser.UseInfoBase(infoBase);
            ReadMetaUuids(infoBase);
            ReadMetadataObjects(infoBase);
            return infoBase;
        }
        private void ReadDBNames(InfoBase infoBase)
        {
            byte[] fileData = MetadataFileReader.ReadBytes(DBNAMES_FILE_NAME);
            using (StreamReader reader = MetadataFileReader.CreateReader(fileData))
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
                        MetadataObject = item.Value
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

            byte[] fileData = MetadataFileReader.ReadBytes(input.MetadataObject.FileName.ToString());
            using (StreamReader stream = MetadataFileReader.CreateReader(fileData))
            {
                MetadataObjectFileParser.ParseMetaUuid(stream, input.MetadataObject);
            }
            input.InfoBase.MetaReferenceTypes.TryAdd(input.MetadataObject.Uuid, input.MetadataObject);
        }
        private void ReadMetadataObjects(InfoBase infoBase)
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
                        ReadMetadataObject,
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
                        ReadMetadataObject,
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
        private void ReadMetadataObject(object metaObject)
        {
            MetadataObject obj = (MetadataObject)metaObject;
            byte[] fileData = MetadataFileReader.ReadBytes(obj.FileName.ToString());
            if (fileData == null)
            {
                return; // TODO: log error "Metadata file is not found"
            }
            using (StreamReader stream = MetadataFileReader.CreateReader(fileData))
            {
                MetadataObjectFileParser.ParseMetadataObject(stream, obj);
            }
        }
    }
}