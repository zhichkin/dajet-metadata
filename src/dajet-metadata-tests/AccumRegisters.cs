using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DaJet.Metadata.Tests
{
    [TestClass]
    public sealed class AccumRegisters
    {
        private InfoBase InfoBase { get; set; }
        private string ConnectionString { get; set; }
        private IMetadataService MetadataService { get; set; } = new MetadataService();

        public AccumRegisters()
        {
            ConnectionString = "Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True";
            MetadataService.UseConnectionString(ConnectionString).UseDatabaseProvider(DatabaseProviders.SQLServer);

            //ConnectionString = "Host=127.0.0.1;Port=5432;Database=dajet-metadata-pg;Username=postgres;Password=postgres;";
            //metadataService.UseConnectionString(ConnectionString).UseDatabaseProvider(DatabaseProviders.PostgreSQL);

            InfoBase = MetadataService.LoadInfoBase();
        }
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

        [TestMethod("01 Регистр накопления: остатки")]
        public void Balance()
        {
            MetadataObject register = InfoBase.AccumulationRegisters.Values.Where(r => r.Name == "РегистрНакопленияОстатки").FirstOrDefault();
            Assert.IsNotNull(register);

            ShowProperties(register);
            MetadataService.EnrichFromDatabase(register);
            ShowProperties(register);

            List<string> delete, insert;
            bool result = MetadataService.CompareWithDatabase(register, out delete, out insert);
            ShowList("Delete list", delete);
            ShowList("Insert list", insert);
        }
        [TestMethod("02 Регистр накопления: обороты")]
        public void Turmover()
        {
            MetadataObject register = InfoBase.AccumulationRegisters.Values.Where(r => r.Name == "РегистрНакопленияОбороты").FirstOrDefault();
            Assert.IsNotNull(register);

            ShowProperties(register);
            MetadataService.EnrichFromDatabase(register);
            ShowProperties(register);

            List<string> delete, insert;
            bool result = MetadataService.CompareWithDatabase(register, out delete, out insert);
            ShowList("Delete list", delete);
            ShowList("Insert list", insert);
        }
    }
}
