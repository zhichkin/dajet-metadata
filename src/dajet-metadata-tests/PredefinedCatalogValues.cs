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

            Console.WriteLine($"Catalog \"{catalog.Name}\":");
            Console.WriteLine($"- FileName = {catalog.FileName}");
            Console.WriteLine($"- TableName = {catalog.TableName}");

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

            Console.WriteLine($"Catalog \"{catalog.Name}\":");
            Console.WriteLine($"- FileName = {catalog.FileName}");
            Console.WriteLine($"- TableName = {catalog.TableName}");

            ShowPredefinedValues(catalog);
        }
        private void ShowPredefinedValues(IPredefinedValues owner)
        {
            for (int i = 0; i < owner.PredefinedValues.Count; i++)
            {
                PredefinedValue pv = owner.PredefinedValues[i];
                if (pv.IsFolder)
                {
                    Console.WriteLine($"[{i + 1}]. {{{pv.Code}}} {pv.Name} ({pv.Uuid}) = {pv.Description}");
                }
                else
                {
                    Console.WriteLine($"{i + 1}. {{{pv.Code}}} {pv.Name} ({pv.Uuid}) = {pv.Description}");
                }

                if (pv.Children.Count > 0)
                {
                    ShowPredefinedValue(pv, 0);
                }
            }
        }
        private void ShowPredefinedValue(PredefinedValue predefinedValue, int level)
        {
            level++;
            string indent = "-".PadLeft(level, '-');

            for (int i = 0; i < predefinedValue.Children.Count; i++)
            {
                PredefinedValue pv = predefinedValue.Children[i];
                if (pv.IsFolder)
                {
                    Console.WriteLine($"{indent}[{i + 1}]. {{{pv.Code}}} {pv.Name} ({pv.Uuid}) = {pv.Description}");
                }
                else
                {
                    Console.WriteLine($"{indent}{i + 1}. {{{pv.Code}}} {pv.Name} ({pv.Uuid}) = {pv.Description}");
                }

                if (pv.Children.Count > 0)
                {
                    ShowPredefinedValue(pv, level);
                }
            }
        }

        [TestMethod] public void TestPredefinedValues()
        {
            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=cerberus;Integrated Security=True");

            InfoBase infoBase = metadata.OpenInfoBase();

            //foreach (ApplicationObject model in infoBase.Catalogs.Values)
            //{
            //    Catalog catalog = model as Catalog;

            //    if (catalog.PredefinedValues.Count == 0) continue;

            //    Console.WriteLine($"Catalog \"{catalog.Name}\":");
            //    Console.WriteLine($"- FileName = {catalog.FileName}");
            //    Console.WriteLine($"- TableName = {catalog.TableName}");

            //    try
            //    {
            //        ShowPredefinedValues(catalog);
            //    }
            //    catch
            //    {
            //        Console.WriteLine($"Error: {catalog.Name} ({catalog.FileName})");
            //    }

            //    Console.WriteLine();
            //}

            foreach (ApplicationObject model in infoBase.Characteristics.Values)
            {
                Characteristic characteristic = model as Characteristic;

                if (characteristic.PredefinedValues.Count == 0) continue;

                Console.WriteLine($"Characteristic \"{characteristic.Name}\":");
                Console.WriteLine($"- FileName = {characteristic.FileName}");
                Console.WriteLine($"- TableName = {characteristic.TableName}");

                try
                {
                    ShowPredefinedValues(characteristic);
                }
                catch
                {
                    Console.WriteLine($"Error: {characteristic.Name} ({characteristic.FileName})");
                }

                Console.WriteLine();
            }
        }
    }
}