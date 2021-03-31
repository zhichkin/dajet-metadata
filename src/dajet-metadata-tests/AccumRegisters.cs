using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace DaJet.Metadata.Tests
{
    [TestClass] public sealed class AccumRegisters
    {
        [TestMethod("MS-01 Остатки")] public void MS_Balance()
        {
            ApplicationObject register = Test.MS_InfoBase
                .AccumulationRegisters.Values
                .Where(r => r.Name == "РегистрНакопленияОстатки")
                .FirstOrDefault();
            Assert.IsNotNull(register);

            Test.EnrichAndCompareWithDatabase(DatabaseProvider.SQLServer, register);
        }
        [TestMethod("MS-02 Обороты")] public void MS_Turnover()
        {
            ApplicationObject register = Test.MS_InfoBase
                .AccumulationRegisters.Values
                .Where(r => r.Name == "РегистрНакопленияОбороты")
                .FirstOrDefault();
            Assert.IsNotNull(register);

            Test.EnrichAndCompareWithDatabase(DatabaseProvider.SQLServer, register);
        }

        [TestMethod("PG-01 Остатки")] public void PG_Balance()
        {
            ApplicationObject register = Test.PG_InfoBase
                .AccumulationRegisters.Values
                .Where(r => r.Name == "РегистрНакопленияОстатки")
                .FirstOrDefault();
            Assert.IsNotNull(register);

            Test.EnrichAndCompareWithDatabase(DatabaseProvider.PostgreSQL, register);
        }
        [TestMethod("PG-02 Обороты")] public void PG_Turnover()
        {
            ApplicationObject register = Test.PG_InfoBase
                .AccumulationRegisters.Values
                .Where(r => r.Name == "РегистрНакопленияОбороты")
                .FirstOrDefault();
            Assert.IsNotNull(register);

            Test.EnrichAndCompareWithDatabase(DatabaseProvider.PostgreSQL, register);
        }
    }
}