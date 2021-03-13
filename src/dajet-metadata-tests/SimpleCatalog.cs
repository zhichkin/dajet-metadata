using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;

namespace DaJet.Metadata.Tests
{
    [TestClass] public class SimpleCatalog : TestClassBase
    {
        private MetaObject Catalog { get; set; }
        public SimpleCatalog() : base() { }
        private void SetupMetaObject()
        {
            if (Catalog != null) return;
            SetupInfoBase();
            Catalog = InfoBase.Catalogs.Values.Where(i => i.Name == "ПростойСправочник").FirstOrDefault();
            Assert.IsNotNull(Catalog);
            //TODO: проверить наличие таблицы в базе данных Catalog.TableName
        }
        [TestMethod("Основные свойства")] public void TestCatalogBasicProperties()
        {
            SetupMetaObject();

            Assert.IsTrue(Catalog.IsReferenceType);
            Assert.AreEqual(Catalog.Alias, "Простой справочник");
            Assert.AreEqual(Catalog.TypeName, MetaObjectTypes.Catalog);
            Assert.AreEqual(Catalog.TableName, string.Format("_Reference{0}", Catalog.TypeCode));

            Console.WriteLine("Name: " + Catalog.Name);
            Console.WriteLine("Alias: " + Catalog.Alias);
            Console.WriteLine("TypeName: " + Catalog.TypeName);
            Console.WriteLine("TableName: " + Catalog.TableName);

            TestPropertyNotExists(Catalog, "Владелец");

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
        [TestMethod("Ссылка")] public void TestPropertyСсылка()
        {
            SetupMetaObject();
            MetaProperty property = TestPropertyExists(Catalog, "Ссылка");

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
            
            MetaField field = TestFieldExists(property, "_IDRRef");
            Assert.AreEqual(field.Length, 16);
            Assert.AreEqual(field.TypeName, "binary");
            Assert.AreEqual(field.KeyOrdinal, 1);
            Assert.AreEqual(field.IsPrimaryKey, true);
        }
        [TestMethod("ВерсияДанных")] public void TestPropertyВерсияДанных()
        {
            SetupMetaObject();
            MetaProperty property = TestPropertyExists(Catalog, "ВерсияДанных");

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

            MetaField field = TestFieldExists(property, "_Version");
            Assert.AreEqual(field.Length, 8);
            Assert.AreEqual(field.TypeName, "timestamp");
        }
        [TestMethod("ПометкаУдаления")] public void TestPropertyПометкаУдаления()
        {
            SetupMetaObject();
            MetaProperty property = TestPropertyExists(Catalog, "ПометкаУдаления");

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

            MetaField field = TestFieldExists(property, "_Marked");
            Assert.AreEqual(field.Length, 1);
            Assert.AreEqual(field.TypeName, "binary");
        }
        [TestMethod("Предопределённый")] public void TestPropertyПредопределённый()
        {
            SetupMetaObject();
            MetaProperty property = TestPropertyExists(Catalog, "Предопределённый");

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

            MetaField field = TestFieldExists(property, "_PredefinedID");
            Assert.AreEqual(field.Length, 16);
            Assert.AreEqual(field.TypeName, "binary");
        }
        [TestMethod("РеквизитБулево")] public void TestPropertyРеквизитБулево()
        {
            SetupMetaObject();
            MetaProperty property = TestPropertyExists(Catalog, "РеквизитБулево");

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
            MetaProperty property = TestPropertyExists(Catalog, "РеквизитДата");

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
            MetaProperty property = TestPropertyExists(Catalog, "РеквизитВремя");

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
            MetaProperty property = TestPropertyExists(Catalog, "РеквизитДатаВремя");

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
            MetaProperty property = TestPropertyExists(Catalog, "РеквизитСтрокаФикс");

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
            MetaProperty property = TestPropertyExists(Catalog, "РеквизитСтрокаПерем");

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
            MetaProperty property = TestPropertyExists(Catalog, "РеквизитСтрокаМакс");

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
            MetaProperty property = TestPropertyExists(Catalog, "РеквизитЧисло");

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
            MetaProperty property = TestPropertyExists(Catalog, "РеквизитЧислоНеотрицательное");

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
            MetaProperty property = TestPropertyExists(Catalog, "РеквизитUUID");

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
            MetaProperty property = TestPropertyExists(Catalog, "РеквизитХранилищеЗначения");

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
            MetaProperty property = TestPropertyExists(Catalog, "РеквизитСправочник");

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
            MetaProperty property = TestPropertyExists(Catalog, "РеквизитСоставной");

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
            MetaProperty property = TestPropertyExists(Catalog, "РеквизитСоставнойСсылки");

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
            MetaProperty property = TestPropertyExists(Catalog, "РеквизитСоставнойПростые");

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
            MetaObject tablePart = TestTablePartExists(Catalog, name);

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