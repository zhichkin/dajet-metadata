using DaJet.Metadata.Model;
using DaJet.Metadata.Services;
using System;
using System.Collections.Generic;
using System.IO;

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
        InfoBase OpenInfoBase();
        bool TryOpenInfoBase(out InfoBase infoBase, out string errorMessage);
        ///<summary>Выполняет сравнение и слияние свойств объекта метаданных с полями таблицы СУБД</summary>
        ///<param name="metaObject">Объект метаданных</param>
        void EnrichFromDatabase(ApplicationObject metaObject);
        ///<summary>Выполняет сравнение свойств объекта метаданных с полями таблицы СУБД на соответствие</summary>
        ///<param name="metaObject">Объект метаданных, для которого выполняется сравнение</param>
        ///<param name="delete">Список имён полей, которые есть в объекте метаданных, но нет в таблице базы данных</param>
        ///<param name="insert">Список имён полей, которые есть в таблице базы данных, но нет в объекте метаданных</param>
        ///<returns>Результат проверки на соответствие</returns>
        bool CompareWithDatabase(ApplicationObject metaObject, out List<string> delete, out List<string> insert);
        ///<summary>Получает файл метаданных в "сыром" (как есть) бинарном виде</summary>
        ///<param name="fileName">Имя файла метаданных: root, DBNames или значение UUID</param>
        ///<returns>Бинарные данные файла метаданных</returns>
        byte[] ReadConfigFile(string fileName);
        ///<summary>Распаковывает файл метаданных по алгоритму deflate и создаёт поток для чтения в формате UTF-8</summary>
        ///<param name="fileData">Бинарные данные файла метаданных</param>
        ///<returns>Поток для чтения файла метаданных в формате UTF-8</returns>
        StreamReader CreateReader(byte[] fileData);
    }
    public sealed class MetadataService : IMetadataService
    {
        private readonly IConfigFileReader ConfigFileReader;
        private readonly ISqlMetadataReader SqlMetadataReader;
        private readonly MetadataCompareAndMergeService CompareMergeService;

        public string ConnectionString { get; private set; } = string.Empty;
        public DatabaseProvider DatabaseProvider { get; private set; } = DatabaseProvider.SQLServer;

        public MetadataService()
        {
            ConfigFileReader = new ConfigFileReader();
            SqlMetadataReader = new SqlMetadataReader();
            CompareMergeService = new MetadataCompareAndMergeService();
        }
        public IMetadataService UseDatabaseProvider(DatabaseProvider databaseProvider)
        {
            DatabaseProvider = databaseProvider;
            SqlMetadataReader.UseDatabaseProvider(DatabaseProvider);
            ConfigFileReader.UseDatabaseProvider(DatabaseProvider);
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
        public InfoBase LoadInfoBase()
        {
            Configurator configurator = new Configurator(ConfigFileReader);
            return configurator.OpenInfoBase();
        }
        public InfoBase OpenInfoBase()
        {
            Configurator configurator = new Configurator(ConfigFileReader, true);
            return configurator.OpenInfoBase();
        }
        public bool TryOpenInfoBase(out InfoBase infoBase, out string errorMessage)
        {
            bool success = true;

            Configurator configurator = new Configurator(ConfigFileReader, true);

            try
            {
                infoBase = configurator.OpenInfoBase();
                errorMessage = null;
            }
            catch (Exception error)
            {
                success = false;
                infoBase = null;
                errorMessage = ExceptionHelper.GetErrorText(error);
            }

            return success;
        }

        public byte[] ReadConfigFile(string fileName)
        {
            return ConfigFileReader.ReadBytes(fileName);
        }
        public StreamReader CreateReader(byte[] fileData)
        {
            return ConfigFileReader.CreateReader(fileData);
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

            return (delete.Count + insert.Count) == 0;
        }
    }
}