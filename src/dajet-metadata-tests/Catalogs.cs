using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace DaJet.Metadata.Tests
{
    [TestClass]
    public sealed class Catalogs
    {
        [TestMethod("MS-01 Простой")] public void MS_01()
        {
            MetadataObject catalog = Test.MS_InfoBase
                .Catalogs.Values
                .Where(r => r.Name == "ПростойСправочник")
                .FirstOrDefault();
            Assert.IsNotNull(catalog);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.SQLServer, catalog);
        }
        [TestMethod("MS-02 Один владелец")] public void MS_02()
        {
            MetadataObject catalog = Test.MS_InfoBase
                .Catalogs.Values
                .Where(r => r.Name == "СправочникПодчинённый")
                .FirstOrDefault();
            Assert.IsNotNull(catalog);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.SQLServer, catalog);
        }
        [TestMethod("MS-03 Несколько владельцев")] public void MS_03()
        {
            MetadataObject catalog = Test.MS_InfoBase
                .Catalogs.Values
                .Where(r => r.Name == "СправочникПодчинённыйСоставной")
                .FirstOrDefault();
            Assert.IsNotNull(catalog);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.SQLServer, catalog);
        }
        [TestMethod("MS-04 Иерархия групп")] public void MS_04()
        {
            MetadataObject catalog = Test.MS_InfoBase
                .Catalogs.Values
                .Where(r => r.Name == "СправочникИерархическийГруппы")
                .FirstOrDefault();
            Assert.IsNotNull(catalog);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.SQLServer, catalog);
        }
        [TestMethod("MS-05 Иерархия элементов")] public void MS_05()
        {
            MetadataObject catalog = Test.MS_InfoBase
                .Catalogs.Values
                .Where(r => r.Name == "СправочникИерархическийЭлементы")
                .FirstOrDefault();
            Assert.IsNotNull(catalog);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.SQLServer, catalog);
        }
        [TestMethod("MS-06 Без кода и наименования")] public void MS_06()
        {
            MetadataObject catalog = Test.MS_InfoBase
                .Catalogs.Values
                .Where(r => r.Name == "СправочникБезКодаИНаименования")
                .FirstOrDefault();
            Assert.IsNotNull(catalog);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.SQLServer, catalog);
        }
        [TestMethod("MS-07 Табличная часть")] public void MS_07()
        {
            MetadataObject catalog = Test.MS_InfoBase
                .Catalogs.Values
                .Where(r => r.Name == "ПростойСправочник")
                .FirstOrDefault();
            Assert.IsNotNull(catalog);

            MetadataObject tablePart = catalog.MetadataObjects
                .Where(mo => mo.Name == "ТабличнаяЧасть3")
                .FirstOrDefault();
            Assert.IsNotNull(tablePart);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.SQLServer, tablePart);
        }

        [TestMethod("PG-01 Простой")] public void PG_01()
        {
            MetadataObject catalog = Test.PG_InfoBase
                .Catalogs.Values
                .Where(r => r.Name == "ПростойСправочник")
                .FirstOrDefault();
            Assert.IsNotNull(catalog);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.PostgreSQL, catalog);
        }
        [TestMethod("PG-02 Один владелец")] public void PG_02()
        {
            MetadataObject catalog = Test.PG_InfoBase
                .Catalogs.Values
                .Where(r => r.Name == "СправочникПодчинённый")
                .FirstOrDefault();
            Assert.IsNotNull(catalog);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.PostgreSQL, catalog);
        }
        [TestMethod("PG-03 Несколько владельцев")] public void PG_03()
        {
            MetadataObject catalog = Test.PG_InfoBase
                .Catalogs.Values
                .Where(r => r.Name == "СправочникПодчинённыйСоставной")
                .FirstOrDefault();
            Assert.IsNotNull(catalog);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.PostgreSQL, catalog);
        }
        [TestMethod("PG-04 Иерархия групп")] public void PG_04()
        {
            MetadataObject catalog = Test.PG_InfoBase
                .Catalogs.Values
                .Where(r => r.Name == "СправочникИерархическийГруппы")
                .FirstOrDefault();
            Assert.IsNotNull(catalog);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.PostgreSQL, catalog);
        }
        [TestMethod("PG-05 Иерархия элементов")] public void PG_05()
        {
            MetadataObject catalog = Test.PG_InfoBase
                .Catalogs.Values
                .Where(r => r.Name == "СправочникИерархическийЭлементы")
                .FirstOrDefault();
            Assert.IsNotNull(catalog);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.PostgreSQL, catalog);
        }
        [TestMethod("PG-06 Без кода и наименования")] public void PG_06()
        {
            MetadataObject catalog = Test.PG_InfoBase
                .Catalogs.Values
                .Where(r => r.Name == "СправочникБезКодаИНаименования")
                .FirstOrDefault();
            Assert.IsNotNull(catalog);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.PostgreSQL, catalog);
        }
        [TestMethod("PG-07 Табличная часть")] public void PG_07()
        {
            MetadataObject catalog = Test.PG_InfoBase
                .Catalogs.Values
                .Where(r => r.Name == "ПростойСправочник")
                .FirstOrDefault();
            Assert.IsNotNull(catalog);

            MetadataObject tablePart = catalog.MetadataObjects
                .Where(mo => mo.Name == "ТабличнаяЧасть3")
                .FirstOrDefault();
            Assert.IsNotNull(tablePart);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.PostgreSQL, catalog);
        }
    }
}
