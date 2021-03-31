using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace DaJet.Metadata.Tests
{
    public static class Test
    {
        #region "Environment variables"
        public static InfoBase MS_InfoBase { get; set; }
        public static InfoBase PG_InfoBase { get; set; }
        public static string MS_ConnectionString { get; set; }
        public static string PG_ConnectionString { get; set; }
        public static IMetadataService MS_MetadataService { get; set; } = new MetadataService();
        public static IMetadataService PG_MetadataService { get; set; } = new MetadataService();
        #endregion
        static Test()
        {
            MS_ConnectionString = "Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True";
            MS_MetadataService.UseConnectionString(MS_ConnectionString).UseDatabaseProvider(DatabaseProvider.SQLServer);
            MS_InfoBase = MS_MetadataService.LoadInfoBase();

            PG_ConnectionString = "Host=127.0.0.1;Port=5432;Database=dajet-metadata-pg;Username=postgres;Password=postgres;";
            PG_MetadataService.UseConnectionString(PG_ConnectionString).UseDatabaseProvider(DatabaseProvider.PostgreSQL);
            PG_InfoBase = PG_MetadataService.LoadInfoBase();
        }
        public static void ShowList(string name, List<string> list)
        {
            Console.WriteLine(name + " (" + list.Count.ToString() + ")" + ":");
            Console.WriteLine("---");
            foreach (string item in list)
            {
                Console.WriteLine(" - " + item);
            }
            Console.WriteLine();
        }
        public static void ShowProperties(ApplicationObject metaObject)
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
        public static void ShowFields(MetadataProperty property)
        {
            foreach (DatabaseField field in property.Fields)
            {
                Console.WriteLine("      - " + field.Name + " (" + field.TypeName + ")");
            }
        }
        public static void EnrichAndCompareWithDatabase(DatabaseProvider provider, ApplicationObject metaObject)
        {
            if (provider == DatabaseProvider.SQLServer)
            {
                EnrichAndCompareWithDatabasePrivate(MS_MetadataService, metaObject);
            }
            else
            {
                EnrichAndCompareWithDatabasePrivate(PG_MetadataService, metaObject);
            }
        }
        private static void EnrichAndCompareWithDatabasePrivate(IMetadataService metadataService, ApplicationObject metaObject)
        {
            ShowProperties(metaObject);
            metadataService.EnrichFromDatabase(metaObject);
            ShowProperties(metaObject);

            List<string> delete, insert;
            bool result = metadataService.CompareWithDatabase(metaObject, out delete, out insert);
            Console.WriteLine("Сравнение с БД прошло " + (result ? "успешно." : "с ошибками."));
            Console.WriteLine();
            if (delete.Count > 0) ShowList("Delete list", delete);
            if (insert.Count > 0) ShowList("Insert list", insert);

            Assert.IsTrue(result);
        }
    }
}