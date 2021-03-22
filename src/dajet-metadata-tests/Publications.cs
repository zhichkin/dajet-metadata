using DaJet.Metadata.Mappers;
using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace DaJet.Metadata.Tests
{
    [TestClass] public sealed class Publications
    {
        [TestMethod("MS-01 Обычный")] public void MS_01()
        {
            MetadataObject publication = Test.MS_InfoBase
                .Publications.Values
                .Where(r => r.Name == "ПланОбмена")
                .FirstOrDefault();
            Assert.IsNotNull(publication);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.SQLServer, publication);
        }
        [TestMethod("MS-02 Загрузка узлов")] public void MS_02()
        {
            MetadataObject publication = Test.MS_InfoBase
                .Publications.Values
                .Where(r => r.Name == "ПланОбмена")
                .FirstOrDefault();
            Assert.IsNotNull(publication);

            PublicationDataMapper mapper = new PublicationDataMapper();
            mapper.UseConnectionString(Test.MS_ConnectionString);
            mapper.UseDatabaseProvider(DatabaseProviders.SQLServer);
            mapper.SelectSubscribers((Publication)publication);

            Assert.IsNotNull(((Publication)publication).Publisher);
        }

        [TestMethod("PG-01 Обычный")] public void PG_01()
        {
            MetadataObject publication = Test.PG_InfoBase
                .Publications.Values
                .Where(r => r.Name == "ПланОбмена")
                .FirstOrDefault();
            Assert.IsNotNull(publication);

            Test.EnrichAndCompareWithDatabase(DatabaseProviders.PostgreSQL, publication);
        }
        [TestMethod("PG-02 Загрузка узлов")] public void PG_02()
        {
            MetadataObject publication = Test.PG_InfoBase
                .Publications.Values
                .Where(r => r.Name == "ПланОбмена")
                .FirstOrDefault();
            Assert.IsNotNull(publication);

            PublicationDataMapper mapper = new PublicationDataMapper();
            mapper.UseConnectionString(Test.PG_ConnectionString);
            mapper.UseDatabaseProvider(DatabaseProviders.PostgreSQL);
            mapper.SelectSubscribers((Publication)publication);

            Assert.IsNotNull(((Publication)publication).Publisher);
        }
    }
}