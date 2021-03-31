using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace DaJet.Metadata.Tests
{
    [TestClass] public sealed class Constants
    {
        [TestMethod("MS-01 Строка")] public void MS_01()
        {
            ApplicationObject constant = Test.MS_InfoBase
                .Constants.Values
                .Where(r => r.Name == "Константа1")
                .FirstOrDefault();
            Assert.IsNotNull(constant);

            Test.EnrichAndCompareWithDatabase(DatabaseProvider.SQLServer, constant);
        }
        [TestMethod("MS-02 Составной тип")] public void MS_02()
        {
            ApplicationObject constant = Test.MS_InfoBase
                .Constants.Values
                .Where(r => r.Name == "Константа2")
                .FirstOrDefault();
            Assert.IsNotNull(constant);

            Test.EnrichAndCompareWithDatabase(DatabaseProvider.SQLServer, constant);
        }

        [TestMethod("PG-01 Строка")] public void PG_01()
        {
            ApplicationObject constant = Test.PG_InfoBase
                .Constants.Values
                .Where(r => r.Name == "Константа1")
                .FirstOrDefault();
            Assert.IsNotNull(constant);

            Test.EnrichAndCompareWithDatabase(DatabaseProvider.PostgreSQL, constant);
        }
        [TestMethod("PG-02 Составной тип")] public void PG_02()
        {
            ApplicationObject constant = Test.PG_InfoBase
                .Constants.Values
                .Where(r => r.Name == "Константа2")
                .FirstOrDefault();
            Assert.IsNotNull(constant);

            Test.EnrichAndCompareWithDatabase(DatabaseProvider.PostgreSQL, constant);
        }
    }
}