using DaJet.Metadata.Mappers;
using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DaJet.Metadata.Tests
{
    [TestClass]
    public sealed class PublicationTests : TestClassBase
    {
        private readonly IMetadataReader metadata;
        private readonly ISqlMetadataReader sqlReader;
        private readonly IMetadataFileReader fileReader;
        private readonly IConfigurationFileParser configReader;

        private readonly bool MSSQL = true;
        private MetadataObject Publication { get; set; }
        public PublicationTests() : base()
        {
            if (MSSQL)
            {
                ConnectionString = "Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True";
                fileReader = new MetadataFileReader();
                fileReader.UseConnectionString(ConnectionString);
                metadata = new MetadataReader(fileReader);
                sqlReader = new SqlMetadataReader();
                sqlReader.UseConnectionString(ConnectionString);
                configReader = new ConfigurationFileParser(fileReader);
            }
            else
            {
                ConnectionString = "Host=127.0.0.1;Port=5432;Database=test_node_2;Username=postgres;Password=postgres;";
                fileReader = new PostgresMetadataFileReader();
                fileReader.UseConnectionString(ConnectionString);
                metadata = new MetadataReader(fileReader);
                sqlReader = new PostgresMetadataReader();
                sqlReader.UseConnectionString(ConnectionString);
                configReader = new ConfigurationFileParser(fileReader);
            }
        }
        protected override void SetupInfoBase()
        {
            if (InfoBase != null) return;
            InfoBase = metadata.LoadInfoBase();
            Assert.IsNotNull(InfoBase);
        }
        private void SetupMetadataObject()
        {
            if (Publication != null) return;
            SetupInfoBase();
            string objectName = MSSQL ? "ПланОбмена" : "Тестовый";
            Publication = InfoBase.Publications.Values.Where(i => i.Name == objectName).FirstOrDefault();
            Assert.IsNotNull(Publication);
            //TODO: проверить наличие таблицы в базе данных Publication.TableName
        }
        [TestMethod("Свойства плана обмена")] public void TestPublicationProperties()
        {
            SetupMetadataObject();

            string alias = MSSQL ? "План обмена" : "Тестовый";
            string table = string.Format(MSSQL ? "_Node{0}" : "_node{0}", Publication.TypeCode);

            Assert.IsTrue(Publication.IsReferenceType);
            Assert.AreEqual(Publication.Alias, alias);
            Assert.AreEqual(Publication.TypeName, MetadataObjectTypes.Publication);
            Assert.AreEqual(Publication.TableName.ToLowerInvariant(), table.ToLowerInvariant());

            Console.WriteLine("Name: " + Publication.Name);
            Console.WriteLine("Alias: " + Publication.Alias);
            Console.WriteLine("TypeName: " + Publication.TypeName);
            Console.WriteLine("TableName: " + Publication.TableName);
            Console.WriteLine("IsDistributed: " + ((Publication)Publication).IsDistributed.ToString());

            // Обязательные реквизиты:
            // -----------------------
            // 1. Свойство "Ссылка" _IDRRef - binary(16)
            // 2. Свойство "ВерсияДанных" _Version - timestamp - строка в формате BASE64 - появился в версии 8.2
            // 3. Свойство "ПометкаУдаления" _Marked - binary(1)
            // 4. Свойство "ИмяПредопределенныхДанных" _PredefinedID - binary(16) только этот узел
            // 5. Свойство "Код" _Code - строка - nvarchar - min 1 symbol
            // 6. Свойство "Наименование" _Description - строка - nvarchar - min 1 symbol
            //
            // Специфические реквизиты:
            // -------------------------
            // 7. Свойство "НомерОтправленного" (readonly) _SentNo - numeric(10,0) not null
            // 8. Свойство "НомерПринятого" (readonly) _ReceivedNo - numeric(10,0) not null
            //
            // Обязательный метод:
            // -------------------------
            // 9. "ЭтотУзел()" - ПланОбменаСсылка (_PredefinedID != Guid.Empty), доступен начиная с платформы версии 8.3.9
        }
        [TestMethod("Ссылка")] public void TestPropertyСсылка()
        {
            SetupMetadataObject();
            MetadataProperty property = TestPropertyExists(Publication, "Ссылка");

            Assert.AreEqual(property.Purpose, PropertyPurpose.System);
            Assert.IsTrue(property.PropertyType.IsUuid);
            Assert.IsFalse(property.PropertyType.IsBinary);
            Assert.IsFalse(property.PropertyType.IsValueStorage);
            Assert.IsFalse(property.PropertyType.IsMultipleType);
            Assert.IsFalse(property.PropertyType.CanBeString);
            Assert.IsFalse(property.PropertyType.CanBeBoolean);
            Assert.IsFalse(property.PropertyType.CanBeNumeric);
            Assert.IsFalse(property.PropertyType.CanBeDateTime);
            Assert.IsFalse(property.PropertyType.CanBeReference);
            Assert.AreEqual(property.PropertyType.ReferenceTypeCode, 0);

            DatabaseField field = TestFieldExists(property, "_IDRRef");
            Assert.AreEqual(field.Length, 16);
            Assert.AreEqual(field.TypeName, "binary");
            Assert.AreEqual(field.KeyOrdinal, 1);
            Assert.AreEqual(field.IsPrimaryKey, true);
        }
        [TestMethod("ВерсияДанных")] public void TestPropertyВерсияДанных()
        {
            SetupMetadataObject();
            MetadataProperty property = TestPropertyExists(Publication, "ВерсияДанных");

            Assert.AreEqual(property.Purpose, PropertyPurpose.System);
            Assert.IsFalse(property.PropertyType.IsUuid);
            Assert.IsTrue(property.PropertyType.IsBinary);
            Assert.IsFalse(property.PropertyType.IsValueStorage);
            Assert.IsFalse(property.PropertyType.IsMultipleType);
            Assert.IsFalse(property.PropertyType.CanBeString);
            Assert.IsFalse(property.PropertyType.CanBeBoolean);
            Assert.IsFalse(property.PropertyType.CanBeNumeric);
            Assert.IsFalse(property.PropertyType.CanBeDateTime);
            Assert.IsFalse(property.PropertyType.CanBeReference);
            Assert.AreEqual(property.PropertyType.ReferenceTypeCode, 0);

            DatabaseField field = TestFieldExists(property, "_Version");
            Assert.AreEqual(field.Length, 8);
            Assert.AreEqual(field.TypeName, "timestamp");
        }
        [TestMethod("ПометкаУдаления")] public void TestPropertyПометкаУдаления()
        {
            SetupMetadataObject();
            MetadataProperty property = TestPropertyExists(Publication, "ПометкаУдаления");

            Assert.AreEqual(property.Purpose, PropertyPurpose.System);
            Assert.IsFalse(property.PropertyType.IsUuid);
            Assert.IsFalse(property.PropertyType.IsBinary);
            Assert.IsFalse(property.PropertyType.IsValueStorage);
            Assert.IsFalse(property.PropertyType.IsMultipleType);
            Assert.IsFalse(property.PropertyType.CanBeString);
            Assert.IsTrue(property.PropertyType.CanBeBoolean);
            Assert.IsFalse(property.PropertyType.CanBeNumeric);
            Assert.IsFalse(property.PropertyType.CanBeDateTime);
            Assert.IsFalse(property.PropertyType.CanBeReference);
            Assert.AreEqual(property.PropertyType.ReferenceTypeCode, 0);

            DatabaseField field = TestFieldExists(property, "_Marked");
            Assert.AreEqual(field.Length, 1);
            Assert.AreEqual(field.TypeName, "binary");
        }
        [TestMethod("Предопределённый")] public void TestPropertyПредопределённый()
        {
            SetupMetadataObject();
            MetadataProperty property = TestPropertyExists(Publication, "Предопределённый");

            Assert.AreEqual(property.Purpose, PropertyPurpose.System);
            Assert.IsTrue(property.PropertyType.IsUuid);
            Assert.IsFalse(property.PropertyType.IsBinary);
            Assert.IsFalse(property.PropertyType.IsValueStorage);
            Assert.IsFalse(property.PropertyType.IsMultipleType);
            Assert.IsFalse(property.PropertyType.CanBeString);
            Assert.IsFalse(property.PropertyType.CanBeBoolean);
            Assert.IsFalse(property.PropertyType.CanBeNumeric);
            Assert.IsFalse(property.PropertyType.CanBeDateTime);
            Assert.IsFalse(property.PropertyType.CanBeReference);
            Assert.AreEqual(property.PropertyType.ReferenceTypeCode, 0);

            DatabaseField field = TestFieldExists(property, "_PredefinedID");
            Assert.AreEqual(field.Length, 16);
            Assert.AreEqual(field.TypeName, "binary");
        }
        [TestMethod("НомерПринятого")] public void TestPropertyНомерПринятого()
        {
            SetupMetadataObject();
            MetadataProperty property = TestPropertyExists(Publication, "НомерПринятого");

            Assert.AreEqual(property.Purpose, PropertyPurpose.System);
            Assert.IsFalse(property.PropertyType.IsUuid);
            Assert.IsFalse(property.PropertyType.IsBinary);
            Assert.IsFalse(property.PropertyType.IsValueStorage);
            Assert.IsFalse(property.PropertyType.IsMultipleType);
            Assert.IsFalse(property.PropertyType.CanBeString);
            Assert.IsFalse(property.PropertyType.CanBeBoolean);
            Assert.IsTrue(property.PropertyType.CanBeNumeric);
            Assert.IsFalse(property.PropertyType.CanBeDateTime);
            Assert.IsFalse(property.PropertyType.CanBeReference);
            Assert.AreEqual(property.PropertyType.ReferenceTypeCode, 0);

            DatabaseField field = TestFieldExists(property, "_ReceivedNo");
            Assert.AreEqual(field.Length, 9);
            Assert.AreEqual(field.Scale, 0);
            Assert.AreEqual(field.Precision, 10);
            Assert.AreEqual(field.TypeName, "numeric");
        }
        [TestMethod("НомерОтправленного")] public void TestPropertyНомерОтправленного()
        {
            SetupMetadataObject();
            MetadataProperty property = TestPropertyExists(Publication, "НомерОтправленного");

            Assert.AreEqual(property.Purpose, PropertyPurpose.System);
            Assert.IsFalse(property.PropertyType.IsUuid);
            Assert.IsFalse(property.PropertyType.IsBinary);
            Assert.IsFalse(property.PropertyType.IsValueStorage);
            Assert.IsFalse(property.PropertyType.IsMultipleType);
            Assert.IsFalse(property.PropertyType.CanBeString);
            Assert.IsFalse(property.PropertyType.CanBeBoolean);
            Assert.IsTrue(property.PropertyType.CanBeNumeric);
            Assert.IsFalse(property.PropertyType.CanBeDateTime);
            Assert.IsFalse(property.PropertyType.CanBeReference);
            Assert.AreEqual(property.PropertyType.ReferenceTypeCode, 0);

            DatabaseField field = TestFieldExists(property, "_SentNo");
            Assert.AreEqual(field.Length, 9);
            Assert.AreEqual(field.Scale, 0);
            Assert.AreEqual(field.Precision, 10);
            Assert.AreEqual(field.TypeName, "numeric");
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
        [TestMethod("Добавление свойств по метаданным СУБД")] public void MergeProperties()
        {
            SetupMetadataObject();

            ShowProperties(Publication);
            Console.WriteLine();
            Console.WriteLine("************");
            Console.WriteLine();

            Stopwatch watch = Stopwatch.StartNew();
            watch.Start();

            List<SqlFieldInfo> sqlFields = sqlReader.GetSqlFieldsOrderedByName(Publication.TableName);
            if (sqlFields.Count == 0)
            {
                Console.WriteLine("SQL fields are not found.");
                return;
            }

            MetadataCompareAndMergeService merger = new MetadataCompareAndMergeService();
            merger.MergeProperties(Publication, sqlFields);

            ShowProperties(Publication);
            Console.WriteLine();
            Console.WriteLine("************");
            Console.WriteLine();

            List<string> targetFields = merger.PrepareComparison(Publication.Properties);
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

        [TestMethod("Загрузка узлов плана обмена")] public void SelectSubscribers()
        {
            SetupMetadataObject();

            Publication publication = (Publication)Publication;

            PublicationDataMapper mapper = new PublicationDataMapper();
            mapper.UseConnectionString(fileReader.ConnectionString);
            mapper.UseDatabaseProvider(MSSQL ? DatabaseProviders.SQLServer : DatabaseProviders.PostgreSQL);
            mapper.SelectSubscribers(publication);

            Console.WriteLine(string.Format("Publisher: ({0}) {1}",
                publication.Publisher.Code,
                publication.Publisher.Name));

            Console.WriteLine("Subscribers:");
            foreach (Subscriber subscriber in publication.Subscribers)
            {
                Console.WriteLine(string.Format(" - ({0}) {1} [{2}]",
                    subscriber.Code,
                    subscriber.Name,
                    subscriber.IsMarkedForDeletion ? "x" : "+"));
            }
        }
    }
}