using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DaJet.Metadata.Tests
{
    [TestClass] public sealed class InfoRegisters
    {
        [TestMethod("MS-01 Обычный")] public void MS_Simple()
        {
            ApplicationObject register = Test.MS_InfoBase
                .InformationRegisters.Values
                .Where(r => r.Name == "ОбычныйРегистрСведений")
                .FirstOrDefault();
            Assert.IsNotNull(register);

            Test.EnrichAndCompareWithDatabase(DatabaseProvider.SQLServer, register);
        }
        [TestMethod("MS-02 Периодический")] public void MS_Periodical()
        {
            ApplicationObject register = Test.MS_InfoBase
                .InformationRegisters.Values
                .Where(r => r.Name == "ПериодическийРегистрСведений")
                .FirstOrDefault();
            Assert.IsNotNull(register);

            Test.EnrichAndCompareWithDatabase(DatabaseProvider.SQLServer, register);
        }
        [TestMethod("MS-03 Один регистратор")] public void MS_OneDocument()
        {
            ApplicationObject register = Test.MS_InfoBase
                .InformationRegisters.Values
                .Where(r => r.Name == "РегистрСведенийОдинРегистратор")
                .FirstOrDefault();
            Assert.IsNotNull(register);

            Test.EnrichAndCompareWithDatabase(DatabaseProvider.SQLServer, register);
        }
        [TestMethod("MS-04 Несколько регистраторов")] public void MS_MultipleDocuments()
        {
            ApplicationObject register = Test.MS_InfoBase
                .InformationRegisters.Values
                .Where(r => r.Name == "РегистрСведенийМногоРегистраторов")
                .FirstOrDefault();
            Assert.IsNotNull(register);

            Test.EnrichAndCompareWithDatabase(DatabaseProvider.SQLServer, register);
        }

        [TestMethod("PG-01 Обычный")] public void PG_Simple()
        {
            ApplicationObject register = Test.PG_InfoBase
                .InformationRegisters.Values
                .Where(r => r.Name == "ОбычныйРегистрСведений")
                .FirstOrDefault();
            Assert.IsNotNull(register);

            Test.EnrichAndCompareWithDatabase(DatabaseProvider.PostgreSQL, register);
        }
        [TestMethod("PG-02 Периодический")] public void PG_Periodical()
        {
            ApplicationObject register = Test.PG_InfoBase
                .InformationRegisters.Values
                .Where(r => r.Name == "ПериодическийРегистрСведений")
                .FirstOrDefault();
            Assert.IsNotNull(register);

            Test.EnrichAndCompareWithDatabase(DatabaseProvider.PostgreSQL, register);
        }
        [TestMethod("PG-03 Один регистратор")] public void PG_OneDocument()
        {
            ApplicationObject register = Test.PG_InfoBase
                .InformationRegisters.Values
                .Where(r => r.Name == "РегистрСведенийОдинРегистратор")
                .FirstOrDefault();
            Assert.IsNotNull(register);

            Test.EnrichAndCompareWithDatabase(DatabaseProvider.PostgreSQL, register);
        }
        [TestMethod("PG-04 Несколько регистраторов")] public void PG_MultipleDocuments()
        {
            ApplicationObject register = Test.PG_InfoBase
                .InformationRegisters.Values
                .Where(r => r.Name == "РегистрСведенийМногоРегистраторов")
                .FirstOrDefault();
            Assert.IsNotNull(register);

            Test.EnrichAndCompareWithDatabase(DatabaseProvider.PostgreSQL, register);
        }
    }
}