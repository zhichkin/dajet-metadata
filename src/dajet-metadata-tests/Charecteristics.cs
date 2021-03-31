using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace DaJet.Metadata.Tests
{
    [TestClass] public sealed class Characteristics
    {
        [TestMethod("MS-01 Обычный")] public void MS_01()
        {
            ApplicationObject characteristic = Test.MS_InfoBase
                .Characteristics.Values
                .Where(r => r.Name == "ПланВидовХарактеристик1")
                .FirstOrDefault();
            Assert.IsNotNull(characteristic);

            Test.EnrichAndCompareWithDatabase(DatabaseProvider.SQLServer, characteristic);
        }
        [TestMethod("PG-01 Обычный")] public void PG_01()
        {
            ApplicationObject characteristic = Test.PG_InfoBase
                .Characteristics.Values
                .Where(r => r.Name == "ПланВидовХарактеристик1")
                .FirstOrDefault();
            Assert.IsNotNull(characteristic);

            Test.EnrichAndCompareWithDatabase(DatabaseProvider.PostgreSQL, characteristic);
        }
    }
}