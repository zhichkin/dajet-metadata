using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DaJet.Metadata.Tests
{
    public class TestClassBase
    {
        // dajet-metadata
        // trade_11_2_3_159_demo
        // accounting_3_0_72_72_demo
        protected readonly IMetadataService metadataService = new MetadataService();
        protected InfoBase InfoBase { get; set; }
        public string ConnectionString { get; set; }

        protected bool MSSQL = true;

        public TestClassBase()
        {
            if (MSSQL)
            {
                ConnectionString = "Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True";
                metadataService
                    .UseConnectionString(ConnectionString)
                    .UseDatabaseProvider(DatabaseProviders.SQLServer);
            }
            else
            {
                ConnectionString = "Host=127.0.0.1;Port=5432;Database=dajet-metadata-pg;Username=postgres;Password=postgres;";
                metadataService
                    .UseConnectionString(ConnectionString)
                    .UseDatabaseProvider(DatabaseProviders.PostgreSQL);
            }
            Console.WriteLine("DatabaseProvider: " + metadataService.DatabaseProvider);
            Console.WriteLine("ConnectionString: " + metadataService.ConnectionString);
        }
        protected virtual void SetupInfoBase()
        {
            if (InfoBase != null) return;
            InfoBase = metadataService.LoadInfoBase();
            Assert.IsNotNull(InfoBase);
        }
        protected void ShowList(string name, List<string> list)
        {
            Console.WriteLine(name + " (" + list.Count.ToString() + ")" + ":");
            foreach (string item in list)
            {
                Console.WriteLine(" - " + item);
            }
        }
        protected void ShowProperties(MetadataObject metaObject)
        {
            Console.WriteLine(metaObject.Name + " (" + metaObject.TableName + "):");
            foreach (MetadataProperty property in metaObject.Properties)
            {
                Console.WriteLine(" - " + property.Name + " (" + property.DbName + ")");
            }
        }
        protected MetadataProperty TestPropertyExists(MetadataObject metaObject, string name)
        {
            MetadataProperty property = metaObject.Properties.Where(p => p.Name == name).FirstOrDefault();
            Assert.IsNotNull(property);
            return property;
            //TODO: проверить наличие полей в базе данных
        }
        protected void TestPropertyNotExists(MetadataObject metaObject, string name)
        {
            MetadataProperty property = metaObject.Properties.Where(p => p.Name == name).FirstOrDefault();
            Assert.IsNull(property);
        }
        protected MetadataObject TestTablePartExists(MetadataObject metaObject, string name)
        {
            MetadataObject tablePart = metaObject.MetadataObjects.Where(t => t.Name == name).FirstOrDefault();
            Assert.IsNotNull(tablePart);
            return tablePart;
            //TODO: проверить наличие таблицы в базе данных tablePart.TableName
        }
        protected DatabaseField TestFieldExists(MetadataProperty property, string name)
        {
            DatabaseField field = property.Fields.Where(f => f.Name == name).FirstOrDefault();
            Assert.IsNotNull(field);
            return field;
            //TODO: проверить наличие поля в базе данных - ?
        }
    }
}