using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DaJet.Metadata.Tests
{
    [TestClass]
    public sealed class PostgresTests
    {
        private const string DBNAMES_FILE_NAME = "DBNames";
        private string ConnectionString { get; set; }
        private readonly IMetadataReader metadata;
        private readonly IMetadataFileReader fileReader;
        private readonly IConfigurationFileParser configReader;

        public PostgresTests()
        {
            // dajet-metadata
            // trade_11_2_3_159_demo
            // accounting_3_0_72_72_demo
            ConnectionString = "Host=127.0.0.1;Port=5432;Database=test_node_2;Username=postgres;Password=postgres;";
            fileReader = new PostgresMetadataFileReader();
            fileReader.UseConnectionString(ConnectionString);
            metadata = new MetadataReader(fileReader);
            configReader = new ConfigurationFileParser(fileReader);
        }

        [TestMethod("Загрузка свойств конфигурации")] public void ReadConfigurationProperties()
        {
            ConfigInfo config = configReader.ReadConfigurationProperties();

            Console.WriteLine("Name = " + config.Name);
            Console.WriteLine("Alias = " + config.Alias);
            Console.WriteLine("Comment = " + config.Comment);
            Console.WriteLine("Version = " + config.Version);
            Console.WriteLine("ConfigVersion = " + config.ConfigVersion);
            Console.WriteLine("SyncCallsMode = " + config.SyncCallsMode.ToString());
            Console.WriteLine("DataLockingMode = " + config.DataLockingMode.ToString());
            Console.WriteLine("ModalWindowMode = " + config.ModalWindowMode.ToString());
            Console.WriteLine("AutoNumberingMode = " + config.AutoNumberingMode.ToString());
            Console.WriteLine("UICompatibilityMode = " + config.UICompatibilityMode.ToString());
        }
        [TestMethod("Загрузка файла DBNames")] public void ReadDBNames()
        {
            Stopwatch watch = Stopwatch.StartNew();
            watch.Start();

            InfoBase infoBase = new InfoBase();
            byte[] fileData = fileReader.ReadBytes(DBNAMES_FILE_NAME);
            using (StreamReader stream = fileReader.CreateReader(fileData))
            {
                IDBNamesFileParser parser = new DBNamesFileParser();
                parser.Parse(stream, infoBase);
            }

            watch.Stop();
            Console.WriteLine("Elapsed in " + watch.ElapsedMilliseconds + " milliseconds.");
        }
        [TestMethod("Загрузка всех метаданных")] public void ReadMetadata()
        {
            Stopwatch watch = Stopwatch.StartNew();
            watch.Start();

            InfoBase infoBase = metadata.LoadInfoBase();

            watch.Stop();
            Console.WriteLine("Elapsed in " + watch.ElapsedMilliseconds + " milliseconds.");
        }
        [TestMethod("Загрузка метаданных СУБД")] public void MergeFields()
        {
            string metadataName = "Справочник.ВходящаяОчередьRabbitMQ"; //"Справочник.ИсходящаяОчередьRabbitMQ";
            string[] names = metadataName.Split('.');
            if (names.Length != 2) return;
            string typeName = names[0];
            string objectName = names[1];

            MetadataObject metaObject = null;
            Dictionary<Guid, MetadataObject> collection = null;
            InfoBase infoBase = metadata.LoadInfoBase();
            if (typeName == "Справочник") collection = infoBase.Catalogs;
            else if (typeName == "Документ") collection = infoBase.Documents;
            else if (typeName == "РегистрСведений") collection = infoBase.InformationRegisters;
            else if (typeName == "РегистрНакопления") collection = infoBase.AccumulationRegisters;
            if (collection == null) return;

            metaObject = collection.Values.Where(o => o.Name == objectName).FirstOrDefault();
            if (metaObject == null) return;

            Stopwatch watch = Stopwatch.StartNew();
            watch.Start();

            ISqlMetadataReader sqlReader = new PostgresMetadataReader();
            sqlReader.UseConnectionString(ConnectionString);
            List<SqlFieldInfo> sqlFields = sqlReader.GetSqlFieldsOrderedByName(metaObject.TableName);
            if (sqlFields.Count == 0) return;

            MetadataCompareAndMergeService merger = new MetadataCompareAndMergeService();
            List<string> targetFields = merger.PrepareComparison(metaObject.Properties);
            List<string> sourceFields = merger.PrepareComparison(sqlFields);

            List<string> delete_list;
            List<string> insert_list;
            merger.Compare(targetFields, sourceFields, out delete_list, out insert_list);

            watch.Stop();
            Console.WriteLine("Elapsed in " + watch.ElapsedMilliseconds + " milliseconds.");
            Console.WriteLine();

            int match = targetFields.Count - delete_list.Count;
            int unmatch = sourceFields.Count - match;
            Console.WriteLine("Всё сходится = " + (insert_list.Count == unmatch).ToString());
            Console.WriteLine();

            ShowList("target", targetFields);
            Console.WriteLine();
            ShowList("source", sourceFields);
            Console.WriteLine();
            ShowList("delete", delete_list);
            Console.WriteLine();
            ShowList("insert", insert_list);
        }
        private void ShowList(string name, List<string> list)
        {
            Console.WriteLine(name + " (" + list.Count.ToString() + ")" + ":");
            foreach (string item in list)
            {
                Console.WriteLine(" - " + item);
            }
        }
        private void ShowProperties(MetadataObject metaObject)
        {
            Console.WriteLine(metaObject.Name + " (" + metaObject.TableName + "):");
            foreach (MetadataProperty property in metaObject.Properties)
            {
                Console.WriteLine(" - " + property.Name + " (" + property.Field + ")");
            }
        }
        private MetadataObject GetMetadataObjectByName(string metadataName)
        {
            string[] names = metadataName.Split('.');
            if (names.Length != 2) return null;
            string typeName = names[0];
            string objectName = names[1];

            Dictionary<Guid, MetadataObject> collection = null;
            InfoBase infoBase = metadata.LoadInfoBase();
            if (typeName == "Справочник") collection = infoBase.Catalogs;
            else if (typeName == "Документ") collection = infoBase.Documents;
            else if (typeName == "РегистрСведений") collection = infoBase.InformationRegisters;
            else if (typeName == "РегистрНакопления") collection = infoBase.AccumulationRegisters;
            if (collection == null) return null;

            return collection.Values.Where(o => o.Name == objectName).FirstOrDefault();
        }
        [TestMethod("Добавление свойств по метаданным СУБД")] public void MergeProperties()
        {
            string[] metadataName = { "Справочник.ВходящаяОчередьRabbitMQ", "Справочник.ИсходящаяОчередьRabbitMQ" };
            MetadataObject metaObject = GetMetadataObjectByName(metadataName[0]);
            if (metaObject == null)
            {
                Console.WriteLine($"Metaobject \"{metadataName[0]}\" is not found.");
                return;
            }

            if (metaObject != null)
            {
                ShowProperties(metaObject);
                Console.WriteLine();
                Console.WriteLine("************");
                Console.WriteLine();
            }

            Stopwatch watch = Stopwatch.StartNew();
            watch.Start();

            ISqlMetadataReader sqlReader = new PostgresMetadataReader();
            sqlReader.UseConnectionString(ConnectionString);
            List<SqlFieldInfo> sqlFields = sqlReader.GetSqlFieldsOrderedByName(metaObject.TableName);
            if (sqlFields.Count == 0)
            {
                Console.WriteLine("SQL fields are not found.");
                return;
            }

            MetadataCompareAndMergeService merger = new MetadataCompareAndMergeService();
            merger.MergeProperties(metaObject, sqlFields);

            ShowProperties(metaObject);
            Console.WriteLine();
            Console.WriteLine("************");
            Console.WriteLine();

            List<string> targetFields = merger.PrepareComparison(metaObject.Properties);
            List<string> sourceFields = merger.PrepareComparison(sqlFields);
            List<string> delete_list;
            List<string> insert_list;
            merger.Compare(targetFields, sourceFields, out delete_list, out insert_list);

            watch.Stop();
            Console.WriteLine("Elapsed in " + watch.ElapsedMilliseconds + " milliseconds.");
            Console.WriteLine();

            int match = targetFields.Count - delete_list.Count;
            int unmatch = sourceFields.Count - match;
            Console.WriteLine("Всё сходится = " + (insert_list.Count == unmatch).ToString());
            Console.WriteLine();

            ShowList("delete", delete_list);
            Console.WriteLine();
            ShowList("insert", insert_list);
            Console.WriteLine();
            ShowList("target", targetFields);
            Console.WriteLine();
            ShowList("source", sourceFields);
        }
    }
}