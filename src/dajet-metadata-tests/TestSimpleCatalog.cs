using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;

namespace DaJet.Metadata.Tests
{
    [TestClass]
    public class TestSimpleCatalog
    {
        private string ConnectionString { get; set; }
        private readonly IMetadataReader metadata;
        private readonly IMetadataFileReader fileReader;
        private readonly IConfigurationFileParser configReader;

        private InfoBase InfoBase { get; set; }
        private MetaObject Catalog { get; set; }

        public TestSimpleCatalog()
        {
            // dajet-metadata
            // trade_11_2_3_159_demo
            // accounting_3_0_72_72_demo
            ConnectionString = "Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True";
            fileReader = new MetadataFileReader();
            fileReader.UseConnectionString(ConnectionString);
            metadata = new MetadataReader(fileReader);
            configReader = new ConfigurationFileParser(fileReader);
        }
        private void SetupInfoBase()
        {
            if (InfoBase != null) return;
            InfoBase = metadata.LoadInfoBase();
            Assert.IsNotNull(InfoBase);
        }
        private void SetupMetaObject()
        {
            if (Catalog != null) return;
            if (InfoBase == null) SetupInfoBase();
            Catalog = InfoBase.Catalogs.Values.Where(i => i.Name == "ПростойСправочник").FirstOrDefault();
            Assert.IsNotNull(Catalog);
            //TODO: проверить наличие таблицы в базе данных Catalog.TableName
        }
        [TestMethod("Загрузка InfoBase")] public void LoadInfoBase()
        {
            Stopwatch watch = Stopwatch.StartNew();
            watch.Start();

            SetupInfoBase();

            watch.Stop();
            Console.WriteLine("Elapsed in " + watch.ElapsedMilliseconds + " milliseconds.");
        }
        [TestMethod("Загрузка справочника")] public void LoadMetaObject()
        {
            SetupMetaObject();
        }
        [TestMethod("Основные свойства")] public void TestCatalogBasicProperties()
        {
            SetupMetaObject();

            Assert.IsTrue(Catalog.IsReferenceType);
            Assert.AreEqual(Catalog.Alias, "Простой справочник");
            Assert.AreEqual(Catalog.TypeName, MetaObjectTypes.Catalog);
            Assert.AreEqual(Catalog.TableName, string.Format("_Reference{0}", Catalog.TypeCode));

            // TODO: Выполнить проверку на правильность загрузки/создания стандартных реквизитов.
            //
            // Обязательные реквизиты:
            // -----------------------
            // 1. Свойство "Ссылка" _IDRRef - binary(16)
            // 2. Свойство "ВерсияДанных" _Version - timestamp - строка в формате BASE64 - появился в версии 8.2
            // 3. Свойство "ПометкаУдаления" _Marked - binary(1)
            // 4. Свойство "ИмяПредопределенныхДанных" _PredefinedID - binary(16) + "Предопределенный" Булево из версии 8.0 binary(1) _IsPredefined
            //             "Предопределенный" = (_IDRRef == _PredefinedID)
            //             Значение по умолчанию для _PredefinedID равно нулевому UUID, появился в версии 8.3.3
            //
            // Необязательные реквизиты:
            // -------------------------
            // 5. Свойство "Код" (наличие или отсутствие) _Code - строка или число
            // 6. Свойство "Наименование" (наличие или отсутствие) _Description - строка или число
            // 7. Свойство "Владелец" (ноль, один или несколько) _OwnerIDRRef - binary(16)
            //             _OwnerID_TYPE - binary(1) всегда равно 0x08 + _OwnerID_RTRef - binary(4) + _OwnerID_RRRef - binary(16)
            // 8. Свойство "ЭтоГруппа" (только если есть иерархия групп) _Folder - binary(1) 0x00 = Истина 0x01 = Ложь (инвертированное значение)
            // 9. Свойство "Родитель" (группа или элемент) _ParentIDRRef - binary(16)
        }
        private MetaProperty TestPropertyExists(string name)
        {
            MetaProperty property = Catalog.Properties.Where(p => p.Name == name).FirstOrDefault();
            Assert.IsNotNull(property);
            return property;
            //TODO: проверить наличие полей в базе данных
        }
        private MetaObject TestTablePartExists(string name)
        {
            MetaObject tablePart = Catalog.MetaObjects.Where(t => t.Name == name).FirstOrDefault();
            Assert.IsNotNull(tablePart);
            return tablePart;
            //TODO: проверить наличие таблицы в базе данных tablePart.TableName
        }
        [TestMethod("РеквизитБулево")] public void TestPropertyРеквизитБулево()
        {
            SetupMetaObject();
            MetaProperty property = TestPropertyExists("РеквизитБулево");

            Assert.AreEqual(property.Purpose, PropertyPurpose.Property);
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
        }
        [TestMethod("РеквизитДата")] public void TestPropertyРеквизитДата()
        {
            SetupMetaObject();
            MetaProperty property = TestPropertyExists("РеквизитДата");

            Assert.AreEqual(property.Purpose, PropertyPurpose.Property);
            Assert.IsFalse(property.PropertyType.IsUuid);
            Assert.IsFalse(property.PropertyType.IsBinary);
            Assert.IsFalse(property.PropertyType.IsValueStorage);
            Assert.IsFalse(property.PropertyType.IsMultipleType);
            Assert.IsFalse(property.PropertyType.CanBeString);
            Assert.IsFalse(property.PropertyType.CanBeBoolean);
            Assert.IsFalse(property.PropertyType.CanBeNumeric);
            Assert.IsTrue(property.PropertyType.CanBeDateTime);
            Assert.IsFalse(property.PropertyType.CanBeReference);
            Assert.AreEqual(property.PropertyType.ReferenceTypeCode, 0);

            //TestTablePart(Catalog, "ТабличнаяЧасть1", 1);
            //TestTablePart(Catalog, "ТабличнаяЧасть2", 2);
            //TestTablePart(Catalog, "ТабличнаяЧасть3", 3);
        }
        [TestMethod("РеквизитВремя")] public void TestPropertyРеквизитВремя()
        {
            SetupMetaObject();
            MetaProperty property = TestPropertyExists("РеквизитВремя");

            Assert.AreEqual(property.Purpose, PropertyPurpose.Property);
            Assert.IsFalse(property.PropertyType.IsUuid);
            Assert.IsFalse(property.PropertyType.IsBinary);
            Assert.IsFalse(property.PropertyType.IsValueStorage);
            Assert.IsFalse(property.PropertyType.IsMultipleType);
            Assert.IsFalse(property.PropertyType.CanBeString);
            Assert.IsFalse(property.PropertyType.CanBeBoolean);
            Assert.IsFalse(property.PropertyType.CanBeNumeric);
            Assert.IsTrue(property.PropertyType.CanBeDateTime);
            Assert.IsFalse(property.PropertyType.CanBeReference);
            Assert.AreEqual(property.PropertyType.ReferenceTypeCode, 0);
        }
        [TestMethod("РеквизитДатаВремя")] public void TestPropertyРеквизитДатаВремя()
        {
            SetupMetaObject();
            MetaProperty property = TestPropertyExists("РеквизитДатаВремя");

            Assert.AreEqual(property.Purpose, PropertyPurpose.Property);
            Assert.IsFalse(property.PropertyType.IsUuid);
            Assert.IsFalse(property.PropertyType.IsBinary);
            Assert.IsFalse(property.PropertyType.IsValueStorage);
            Assert.IsFalse(property.PropertyType.IsMultipleType);
            Assert.IsFalse(property.PropertyType.CanBeString);
            Assert.IsFalse(property.PropertyType.CanBeBoolean);
            Assert.IsFalse(property.PropertyType.CanBeNumeric);
            Assert.IsTrue(property.PropertyType.CanBeDateTime);
            Assert.IsFalse(property.PropertyType.CanBeReference);
            Assert.AreEqual(property.PropertyType.ReferenceTypeCode, 0);
        }
        [TestMethod("РеквизитСтрокаФикс")] public void TestPropertyРеквизитСтрокаФикс()
        {
            SetupMetaObject();
            MetaProperty property = TestPropertyExists("РеквизитСтрокаФикс");

            Assert.AreEqual(property.Purpose, PropertyPurpose.Property);
            Assert.IsFalse(property.PropertyType.IsUuid);
            Assert.IsFalse(property.PropertyType.IsBinary);
            Assert.IsFalse(property.PropertyType.IsValueStorage);
            Assert.IsFalse(property.PropertyType.IsMultipleType);
            Assert.IsTrue(property.PropertyType.CanBeString);
            Assert.IsFalse(property.PropertyType.CanBeBoolean);
            Assert.IsFalse(property.PropertyType.CanBeNumeric);
            Assert.IsFalse(property.PropertyType.CanBeDateTime);
            Assert.IsFalse(property.PropertyType.CanBeReference);
            Assert.AreEqual(property.PropertyType.ReferenceTypeCode, 0);
        }
        [TestMethod("РеквизитСтрокаПерем")] public void TestPropertyРеквизитСтрокаПерем()
        {
            SetupMetaObject();
            MetaProperty property = TestPropertyExists("РеквизитСтрокаПерем");

            Assert.AreEqual(property.Purpose, PropertyPurpose.Property);
            Assert.IsFalse(property.PropertyType.IsUuid);
            Assert.IsFalse(property.PropertyType.IsBinary);
            Assert.IsFalse(property.PropertyType.IsValueStorage);
            Assert.IsFalse(property.PropertyType.IsMultipleType);
            Assert.IsTrue(property.PropertyType.CanBeString);
            Assert.IsFalse(property.PropertyType.CanBeBoolean);
            Assert.IsFalse(property.PropertyType.CanBeNumeric);
            Assert.IsFalse(property.PropertyType.CanBeDateTime);
            Assert.IsFalse(property.PropertyType.CanBeReference);
            Assert.AreEqual(property.PropertyType.ReferenceTypeCode, 0);
        }
        [TestMethod("РеквизитСтрокаМакс")] public void TestPropertyРеквизитСтрокаМакс()
        {
            SetupMetaObject();
            MetaProperty property = TestPropertyExists("РеквизитСтрокаМакс");

            Assert.AreEqual(property.Purpose, PropertyPurpose.Property);
            Assert.IsFalse(property.PropertyType.IsUuid);
            Assert.IsFalse(property.PropertyType.IsBinary);
            Assert.IsFalse(property.PropertyType.IsValueStorage);
            Assert.IsFalse(property.PropertyType.IsMultipleType);
            Assert.IsTrue(property.PropertyType.CanBeString);
            Assert.IsFalse(property.PropertyType.CanBeBoolean);
            Assert.IsFalse(property.PropertyType.CanBeNumeric);
            Assert.IsFalse(property.PropertyType.CanBeDateTime);
            Assert.IsFalse(property.PropertyType.CanBeReference);
            Assert.AreEqual(property.PropertyType.ReferenceTypeCode, 0);
        }
        [TestMethod("РеквизитЧисло")] public void TestPropertyРеквизитЧисло()
        {
            SetupMetaObject();
            MetaProperty property = TestPropertyExists("РеквизитЧисло");

            Assert.AreEqual(property.Purpose, PropertyPurpose.Property);
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
        }
        [TestMethod("РеквизитЧислоНеотрицательное")] public void TestPropertyРеквизитЧислоНеотрицательное()
        {
            SetupMetaObject();
            MetaProperty property = TestPropertyExists("РеквизитЧислоНеотрицательное");

            Assert.AreEqual(property.Purpose, PropertyPurpose.Property);
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
        }
        [TestMethod("РеквизитUUID")] public void TestPropertyРеквизитUUID()
        {
            SetupMetaObject();
            MetaProperty property = TestPropertyExists("РеквизитUUID");

            Assert.AreEqual(property.Purpose, PropertyPurpose.Property);
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
        }
        [TestMethod("РеквизитХранилищеЗначения")] public void TestPropertyРеквизитХранилищеЗначения()
        {
            SetupMetaObject();
            MetaProperty property = TestPropertyExists("РеквизитХранилищеЗначения");

            Assert.AreEqual(property.Purpose, PropertyPurpose.Property);
            Assert.IsFalse(property.PropertyType.IsUuid);
            Assert.IsFalse(property.PropertyType.IsBinary);
            Assert.IsTrue(property.PropertyType.IsValueStorage);
            Assert.IsFalse(property.PropertyType.IsMultipleType);
            Assert.IsFalse(property.PropertyType.CanBeString);
            Assert.IsFalse(property.PropertyType.CanBeBoolean);
            Assert.IsFalse(property.PropertyType.CanBeNumeric);
            Assert.IsFalse(property.PropertyType.CanBeDateTime);
            Assert.IsFalse(property.PropertyType.CanBeReference);
            Assert.AreEqual(property.PropertyType.ReferenceTypeCode, 0);
        }
        [TestMethod("РеквизитСправочник")] public void TestPropertyРеквизитСправочник()
        {
            SetupMetaObject();
            MetaProperty property = TestPropertyExists("РеквизитСправочник");

            Assert.AreEqual(property.Purpose, PropertyPurpose.Property);
            Assert.IsFalse(property.PropertyType.IsUuid);
            Assert.IsFalse(property.PropertyType.IsBinary);
            Assert.IsFalse(property.PropertyType.IsValueStorage);
            Assert.IsFalse(property.PropertyType.IsMultipleType);
            Assert.IsFalse(property.PropertyType.CanBeString);
            Assert.IsFalse(property.PropertyType.CanBeBoolean);
            Assert.IsFalse(property.PropertyType.CanBeNumeric);
            Assert.IsFalse(property.PropertyType.CanBeDateTime);
            Assert.IsTrue(property.PropertyType.CanBeReference);
            Assert.AreEqual(property.PropertyType.ReferenceTypeCode, Catalog.TypeCode);
        }
        [TestMethod("РеквизитСоставной")] public void TestPropertyРеквизитСоставной()
        {
            SetupMetaObject();
            MetaProperty property = TestPropertyExists("РеквизитСоставной");

            Assert.AreEqual(property.Purpose, PropertyPurpose.Property);
            Assert.IsFalse(property.PropertyType.IsUuid);
            Assert.IsFalse(property.PropertyType.IsBinary);
            Assert.IsFalse(property.PropertyType.IsValueStorage);
            Assert.IsTrue(property.PropertyType.IsMultipleType);
            Assert.IsTrue(property.PropertyType.CanBeString);
            Assert.IsTrue(property.PropertyType.CanBeBoolean);
            Assert.IsTrue(property.PropertyType.CanBeNumeric);
            Assert.IsTrue(property.PropertyType.CanBeDateTime);
            Assert.IsTrue(property.PropertyType.CanBeReference);
            Assert.AreEqual(property.PropertyType.ReferenceTypeCode, Catalog.TypeCode);
        }
        [TestMethod("РеквизитСоставнойСсылки")] public void TestPropertyРеквизитСоставнойСсылки()
        {
            SetupMetaObject();
            MetaProperty property = TestPropertyExists("РеквизитСоставнойСсылки");

            Assert.AreEqual(property.Purpose, PropertyPurpose.Property);
            Assert.IsFalse(property.PropertyType.IsUuid);
            Assert.IsFalse(property.PropertyType.IsBinary);
            Assert.IsFalse(property.PropertyType.IsValueStorage);
            Assert.IsTrue(property.PropertyType.IsMultipleType);
            Assert.IsFalse(property.PropertyType.CanBeString);
            Assert.IsFalse(property.PropertyType.CanBeBoolean);
            Assert.IsFalse(property.PropertyType.CanBeNumeric);
            Assert.IsFalse(property.PropertyType.CanBeDateTime);
            Assert.IsTrue(property.PropertyType.CanBeReference);
            Assert.AreEqual(property.PropertyType.ReferenceTypeCode, 0);
        }
        [TestMethod("РеквизитСоставнойПростые")] public void TestPropertyРеквизитСоставнойПростые()
        {
            SetupMetaObject();
            MetaProperty property = TestPropertyExists("РеквизитСоставнойПростые");

            Assert.AreEqual(property.Purpose, PropertyPurpose.Property);
            Assert.IsFalse(property.PropertyType.IsUuid);
            Assert.IsFalse(property.PropertyType.IsBinary);
            Assert.IsFalse(property.PropertyType.IsValueStorage);
            Assert.IsTrue(property.PropertyType.IsMultipleType);
            Assert.IsTrue(property.PropertyType.CanBeString);
            Assert.IsTrue(property.PropertyType.CanBeBoolean);
            Assert.IsTrue(property.PropertyType.CanBeNumeric);
            Assert.IsTrue(property.PropertyType.CanBeDateTime);
            Assert.IsFalse(property.PropertyType.CanBeReference);
            Assert.AreEqual(property.PropertyType.ReferenceTypeCode, 0);
        }
        [TestMethod("ТабличнаяЧасть1")] public void TestTablePartТабличнаяЧасть1()
        {
            SetupMetaObject();
            TestTablePart("ТабличнаяЧасть1", 1);
        }
        [TestMethod("ТабличнаяЧасть2")] public void TestTablePartТабличнаяЧасть2()
        {
            SetupMetaObject();
            TestTablePart("ТабличнаяЧасть2", 2);
        }
        [TestMethod("ТабличнаяЧасть3")] public void TestTablePartТабличнаяЧасть3()
        {
            SetupMetaObject();
            TestTablePart("ТабличнаяЧасть3", 3);
        }
        private void TestTablePart(string name, int propertiesCount)
        {
            MetaObject tablePart = TestTablePartExists(name);

            Assert.AreEqual(Catalog, tablePart.Owner);
            Assert.AreEqual(tablePart.TableName, string.Format("{0}_VT{1}", Catalog.TableName, tablePart.TypeCode));
            
            for (int i = 1; i <= propertiesCount; i++)
            {
                string propertyName = string.Format("{0}Реквизит{1}", tablePart.Name, i);
                MetaProperty property = tablePart.Properties.Where(p => p.Name == propertyName).FirstOrDefault();
                Assert.IsNotNull(property);

                Assert.AreEqual(property.Purpose, PropertyPurpose.Property);
                Assert.IsFalse(property.PropertyType.IsUuid);
                Assert.IsFalse(property.PropertyType.IsBinary);
                Assert.IsFalse(property.PropertyType.IsValueStorage);
                Assert.IsFalse(property.PropertyType.IsMultipleType);
                Assert.IsTrue(property.PropertyType.CanBeString);
                Assert.IsFalse(property.PropertyType.CanBeBoolean);
                Assert.IsFalse(property.PropertyType.CanBeNumeric);
                Assert.IsFalse(property.PropertyType.CanBeDateTime);
                Assert.IsFalse(property.PropertyType.CanBeReference);
                Assert.AreEqual(property.PropertyType.ReferenceTypeCode, 0);
            }
            //TODO: проверить наличие полей основных реквизитов в базе данных _Reference31_IDRRef + _KeyField + _LineNo(49)
        }
    }
}