using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace DaJet.Metadata.Tests
{
    [TestClass] public sealed class Enumerations
    {
        [TestMethod("MS-01")] public void MS_01()
        {
            MetadataObject enumeration = Test.MS_InfoBase
                .Enumerations.Values
                .Where(r => r.Name == "Перечисление1")
                .FirstOrDefault();
            Assert.IsNotNull(enumeration);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.SQLServer, enumeration);
        }
        [TestMethod("PG-01")] public void PG_01()
        {
            MetadataObject enumeration = Test.PG_InfoBase
                .Enumerations.Values
                .Where(r => r.Name == "Перечисление1")
                .FirstOrDefault();
            Assert.IsNotNull(enumeration);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.PostgreSQL, enumeration);
        }
    }
}