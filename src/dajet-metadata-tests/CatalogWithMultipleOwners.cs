using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace DaJet.Metadata.Tests
{
    [TestClass] public class CatalogWithMutlipleOwners : TestClassBase
    {
        private MetadataObject Catalog { get; set; }
        public CatalogWithMutlipleOwners() : base() { }
        private void SetupMetadataObject()
        {
            if (Catalog != null) return;
            SetupInfoBase();
            Catalog = InfoBase.Catalogs.Values.Where(i => i.Name == "СправочникПодчинённыйСоставной").FirstOrDefault();
            Assert.IsNotNull(Catalog);
            //TODO: проверить наличие таблицы в базе данных Catalog.TableName
        }
        [TestMethod("Владелец")] public void TestPropertyВладелец()
        {
            SetupMetadataObject();
            MetadataProperty property = TestPropertyExists(Catalog, "Владелец");

            Assert.AreEqual(property.Purpose, PropertyPurpose.System);
            Assert.IsFalse(property.PropertyType.IsUuid);
            Assert.IsFalse(property.PropertyType.IsBinary);
            Assert.IsFalse(property.PropertyType.IsValueStorage);
            Assert.IsFalse(property.PropertyType.CanBeString);
            Assert.IsFalse(property.PropertyType.CanBeBoolean);
            Assert.IsFalse(property.PropertyType.CanBeNumeric);
            Assert.IsFalse(property.PropertyType.CanBeDateTime);
            Assert.IsTrue(property.PropertyType.CanBeReference);
            Assert.IsTrue(property.PropertyType.IsMultipleType);
            Assert.AreEqual(property.PropertyType.ReferenceTypeCode, 0);
            DatabaseField field;
            if (MSSQL)
            {
                field = TestFieldExists(property, "_OwnerID_TYPE");
                Assert.AreEqual(field.Length, 1);
                Assert.AreEqual(field.TypeName, "binary");
            }
            else
            {
                field = TestFieldExists(property, "_OwnerID_TYPE".ToLowerInvariant());
            }
            if (MSSQL)
            {
                field = TestFieldExists(property, "_OwnerID_RTRef");
                Assert.AreEqual(field.Length, 4);
                Assert.AreEqual(field.TypeName, "binary");
            }
            else
            {
                field = TestFieldExists(property, "_OwnerID_RTRef".ToLowerInvariant());
            }
            if (MSSQL)
            {
                field = TestFieldExists(property, "_OwnerID_RRRef");
                Assert.AreEqual(field.Length, 16);
                Assert.AreEqual(field.TypeName, "binary");
            }
            else
            {
                field = TestFieldExists(property, "_OwnerID_RRRef".ToLowerInvariant());
            }
        }
        [TestMethod("Подчинённый справочник: много владельцев")]
        public void MultipleOwners()
        {
            SetupInfoBase();

            MetadataObject register = InfoBase.Catalogs.Values.Where(r => r.Name == "СправочникПодчинённыйСоставной").FirstOrDefault();
            Assert.IsNotNull(register);

            ShowProperties(register);
            metadataService.EnrichFromDatabase(register);
            ShowProperties(register);

            List<string> delete, insert;
            bool result = metadataService.CompareWithDatabase(register, out delete, out insert);
            ShowList("Delete list", delete);
            ShowList("Insert list", insert);
        }
    }
}