using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace DaJet.Metadata.Tests
{
    [TestClass] public sealed class Documents
    {
        [TestMethod("MS-01 Обычный")] public void MS_01()
        {
            MetadataObject document = Test.MS_InfoBase
                .Documents.Values
                .Where(r => r.Name == "ОбычныйДокумент")
                .FirstOrDefault();
            Assert.IsNotNull(document);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.SQLServer, document);
        }
        [TestMethod("MS-02 Без номера")] public void MS_02()
        {
            MetadataObject document = Test.MS_InfoBase
                .Documents.Values
                .Where(r => r.Name == "ДокументБезНомера")
                .FirstOrDefault();
            Assert.IsNotNull(document);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.SQLServer, document);
        }
        [TestMethod("MS-03 Табличная часть")] public void MS_03()
        {
            MetadataObject document = Test.MS_InfoBase
                .Documents.Values
                .Where(r => r.Name == "ОбычныйДокумент")
                .FirstOrDefault();
            Assert.IsNotNull(document);

            MetadataObject tablePart = document.MetadataObjects
                .Where(mo => mo.Name == "ТабличнаяЧасть1")
                .FirstOrDefault();
            Assert.IsNotNull(tablePart);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.SQLServer, tablePart);
        }

        [TestMethod("PG-01 Обычный")] public void PG_01()
        {
            MetadataObject document = Test.PG_InfoBase
                .Documents.Values
                .Where(r => r.Name == "ОбычныйДокумент")
                .FirstOrDefault();
            Assert.IsNotNull(document);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.PostgreSQL, document);
        }
        [TestMethod("PG-02 Без номера")] public void PG_02()
        {
            MetadataObject document = Test.PG_InfoBase
                .Documents.Values
                .Where(r => r.Name == "ДокументБезНомера")
                .FirstOrDefault();
            Assert.IsNotNull(document);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.PostgreSQL, document);
        }
        [TestMethod("PG-03 Табличная часть")] public void PG_03()
        {
            MetadataObject document = Test.PG_InfoBase
                .Documents.Values
                .Where(r => r.Name == "ОбычныйДокумент")
                .FirstOrDefault();
            Assert.IsNotNull(document);

            MetadataObject tablePart = document.MetadataObjects
                .Where(mo => mo.Name == "ТабличнаяЧасть1")
                .FirstOrDefault();
            Assert.IsNotNull(tablePart);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.PostgreSQL, tablePart);
        }
    }
}