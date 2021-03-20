using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DaJet.Metadata.Tests
{
    [TestClass] public sealed class InfoRegisters
    {
        #region "Initializing test environment"

        private InfoBase MS_InfoBase { get; set; }
        private InfoBase PG_InfoBase { get; set; }
        private string MS_ConnectionString { get; set; }
        private string PG_ConnectionString { get; set; }
        private IMetadataService MS_MetadataService { get; set; } = new MetadataService();
        private IMetadataService PG_MetadataService { get; set; } = new MetadataService();

        public InfoRegisters()
        {
            MS_ConnectionString = "Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True";
            MS_MetadataService.UseConnectionString(MS_ConnectionString).UseDatabaseProvider(DatabaseProviders.SQLServer);
            MS_InfoBase = MS_MetadataService.LoadInfoBase();

            PG_ConnectionString = "Host=127.0.0.1;Port=5432;Database=dajet-metadata-pg;Username=postgres;Password=postgres;";
            PG_MetadataService.UseConnectionString(PG_ConnectionString).UseDatabaseProvider(DatabaseProviders.PostgreSQL);
            PG_InfoBase = PG_MetadataService.LoadInfoBase();
        }

        #endregion

        #region "Helping functions"
        private void ShowList(string name, List<string> list)
        {
            Console.WriteLine(name + " (" + list.Count.ToString() + ")" + ":");
            Console.WriteLine("---");
            foreach (string item in list)
            {
                Console.WriteLine(" - " + item);
            }
            Console.WriteLine();
        }
        private void ShowProperties(MetadataObject metaObject)
        {
            Console.WriteLine(metaObject.Name + " (" + metaObject.TableName + "):");
            Console.WriteLine("---");
            foreach (MetadataProperty property in metaObject.Properties)
            {
                Console.WriteLine("   - " + property.Name + " (" + property.DbName + ")");
                ShowFields(property);
            }
            Console.WriteLine();
        }
        private void ShowFields(MetadataProperty property)
        {
            foreach (DatabaseField field in property.Fields)
            {
                Console.WriteLine("      - " + field.Name + " (" + field.TypeName + ")");
            }
        }
        #endregion

        [TestMethod("MS-01 Регистр сведений: обычный")] public void MS_Simple()
        {
            MetadataObject register = MS_InfoBase.InformationRegisters.Values.Where(r => r.Name == "ОбычныйРегистрСведений").FirstOrDefault();
            Assert.IsNotNull(register);

            ShowProperties(register);
            MS_MetadataService.EnrichFromDatabase(register);
            ShowProperties(register);

            List<string> delete, insert;
            bool result = MS_MetadataService.CompareWithDatabase(register, out delete, out insert);
            Console.WriteLine("Сравнение с БД прошло " + (result ? "успешно." : "с ошибками."));
            Console.WriteLine();
            if (delete.Count > 0) ShowList("Delete list", delete);
            if (insert.Count > 0) ShowList("Insert list", insert);

            Assert.IsTrue(result);
        }
        [TestMethod("MS-02 Регистр сведений: периодический")] public void MS_Periodical()
        {
            MetadataObject register = MS_InfoBase.InformationRegisters.Values.Where(r => r.Name == "ПериодическийРегистрСведений").FirstOrDefault();
            Assert.IsNotNull(register);

            ShowProperties(register);
            MS_MetadataService.EnrichFromDatabase(register);
            ShowProperties(register);

            List<string> delete, insert;
            bool result = MS_MetadataService.CompareWithDatabase(register, out delete, out insert);
            Console.WriteLine("Сравнение с БД прошло " + (result ? "успешно." : "с ошибками."));
            Console.WriteLine();
            if (delete.Count > 0) ShowList("Delete list", delete);
            if (insert.Count > 0) ShowList("Insert list", insert);

            Assert.IsTrue(result);
        }
        [TestMethod("MS-03 Регистр сведений: один регистратор")] public void MS_OneDocument()
        {
            MetadataObject register = MS_InfoBase.InformationRegisters.Values.Where(r => r.Name == "РегистрСведенийОдинРегистратор").FirstOrDefault();
            Assert.IsNotNull(register);

            ShowProperties(register);
            MS_MetadataService.EnrichFromDatabase(register);
            ShowProperties(register);

            List<string> delete, insert;
            bool result = MS_MetadataService.CompareWithDatabase(register, out delete, out insert);
            Console.WriteLine("Сравнение с БД прошло " + (result ? "успешно." : "с ошибками."));
            Console.WriteLine();
            if (delete.Count > 0) ShowList("Delete list", delete);
            if (insert.Count > 0) ShowList("Insert list", insert);

            Assert.IsTrue(result);
        }
        [TestMethod("MS-04 Регистр сведений: несколько регистраторов")] public void MS_MultipleDocuments()
        {
            MetadataObject register = MS_InfoBase.InformationRegisters.Values.Where(r => r.Name == "РегистрСведенийМногоРегистраторов").FirstOrDefault();
            Assert.IsNotNull(register);

            ShowProperties(register);
            MS_MetadataService.EnrichFromDatabase(register);
            ShowProperties(register);

            List<string> delete, insert;
            bool result = MS_MetadataService.CompareWithDatabase(register, out delete, out insert);
            Console.WriteLine("Сравнение с БД прошло " + (result ? "успешно." : "с ошибками."));
            Console.WriteLine();
            if (delete.Count > 0) ShowList("Delete list", delete);
            if (insert.Count > 0) ShowList("Insert list", insert);

            Assert.IsTrue(result);
        }

        [TestMethod("PG-01 Регистр сведений: обычный")] public void PG_Simple()
        {
            MetadataObject register = PG_InfoBase.InformationRegisters.Values.Where(r => r.Name == "ОбычныйРегистрСведений").FirstOrDefault();
            Assert.IsNotNull(register);

            ShowProperties(register);
            PG_MetadataService.EnrichFromDatabase(register);
            ShowProperties(register);

            List<string> delete, insert;
            bool result = PG_MetadataService.CompareWithDatabase(register, out delete, out insert);
            Console.WriteLine("Сравнение с БД прошло " + (result ? "успешно." : "с ошибками."));
            Console.WriteLine();
            if (delete.Count > 0) ShowList("Delete list", delete);
            if (insert.Count > 0) ShowList("Insert list", insert);

            Assert.IsTrue(result);
        }
        [TestMethod("PG-02 Регистр сведений: периодический")] public void PG_Periodical()
        {
            MetadataObject register = PG_InfoBase.InformationRegisters.Values.Where(r => r.Name == "ПериодическийРегистрСведений").FirstOrDefault();
            Assert.IsNotNull(register);

            ShowProperties(register);
            PG_MetadataService.EnrichFromDatabase(register);
            ShowProperties(register);

            List<string> delete, insert;
            bool result = PG_MetadataService.CompareWithDatabase(register, out delete, out insert);
            Console.WriteLine("Сравнение с БД прошло " + (result ? "успешно." : "с ошибками."));
            Console.WriteLine();
            if (delete.Count > 0) ShowList("Delete list", delete);
            if (insert.Count > 0) ShowList("Insert list", insert);

            Assert.IsTrue(result);
        }
        [TestMethod("PG-03 Регистр сведений: один регистратор")] public void PG_OneDocument()
        {
            MetadataObject register = PG_InfoBase.InformationRegisters.Values.Where(r => r.Name == "РегистрСведенийОдинРегистратор").FirstOrDefault();
            Assert.IsNotNull(register);

            ShowProperties(register);
            PG_MetadataService.EnrichFromDatabase(register);
            ShowProperties(register);

            List<string> delete, insert;
            bool result = PG_MetadataService.CompareWithDatabase(register, out delete, out insert);
            Console.WriteLine("Сравнение с БД прошло " + (result ? "успешно." : "с ошибками."));
            Console.WriteLine();
            if (delete.Count > 0) ShowList("Delete list", delete);
            if (insert.Count > 0) ShowList("Insert list", insert);

            Assert.IsTrue(result);
        }
        [TestMethod("PG-04 Регистр сведений: несколько регистраторов")] public void PG_MultipleDocuments()
        {
            MetadataObject register = PG_InfoBase.InformationRegisters.Values.Where(r => r.Name == "РегистрСведенийМногоРегистраторов").FirstOrDefault();
            Assert.IsNotNull(register);

            ShowProperties(register);
            PG_MetadataService.EnrichFromDatabase(register);
            ShowProperties(register);

            List<string> delete, insert;
            bool result = PG_MetadataService.CompareWithDatabase(register, out delete, out insert);
            Console.WriteLine("Сравнение с БД прошло " + (result ? "успешно." : "с ошибками."));
            Console.WriteLine();
            if (delete.Count > 0) ShowList("Delete list", delete);
            if (insert.Count > 0) ShowList("Insert list", insert);

            Assert.IsTrue(result);
        }
    }
}