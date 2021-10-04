using DaJet.Metadata.Model;
using DaJet.Metadata.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace DaJet.Metadata.Tests
{
    [TestClass] public sealed class PredefinedCatalogValues
    {
        [TestMethod] public void MS_ShowPredefinedValues()
        {
            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True");

            InfoBase infoBase = metadata.LoadInfoBase();

            string catalogName = "СправочникПредопределённые";

            ApplicationObject model = infoBase.Catalogs.Values.Where(c => c.Name == catalogName).FirstOrDefault();
            if (model == null)
            {
                Console.WriteLine($"Catalog \"{catalogName}\" is not found.");
                return;
            }

            Catalog catalog = model as Catalog;

            ShowPredefinedValues(catalog);
        }
        [TestMethod] public void PG_ShowPredefinedValues()
        {
            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.PostgreSQL)
                .UseConnectionString("Host=127.0.0.1;Port=5432;Database=dajet-metadata-pg;Username=postgres;Password=postgres;");

            InfoBase infoBase = metadata.LoadInfoBase();

            string catalogName = "СправочникПредопределённые";

            ApplicationObject model = infoBase.Catalogs.Values.Where(c => c.Name == catalogName).FirstOrDefault();
            if (model == null)
            {
                Console.WriteLine($"Catalog \"{catalogName}\" is not found.");
                return;
            }

            Catalog catalog = model as Catalog;
            
            ShowPredefinedValues(catalog);
        }
        private void ShowPredefinedValues(Catalog catalog)
        {
            Console.WriteLine($"Catalog \"{catalog.Name}\":");
            Console.WriteLine($"- FileName = {catalog.FileName}");
            Console.WriteLine($"- TableName = {catalog.TableName}");
            for (int i = 0; i < catalog.PredefinedValues.Count; i++)
            {
                PredefinedValue pv = catalog.PredefinedValues[i];
                Console.WriteLine($"{i + 1}. {pv.Name} ({pv.Uuid}) = {pv.Description}");
            }
        }

        [TestMethod] public void TestPredefinedValues()
        {
            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=trade_11_2_3_159_demo;Integrated Security=True");

            InfoBase infoBase = metadata.OpenInfoBase();

            foreach (ApplicationObject model in infoBase.Catalogs.Values)
            {
                Catalog catalog = model as Catalog;

                if (catalog.PredefinedValues.Count == 0) continue;

                try
                {
                    ShowPredefinedValues(catalog);
                }
                catch
                {
                    Console.WriteLine($"Error: {catalog.Name} ({catalog.FileName})");
                }
            }
        }
    }
}