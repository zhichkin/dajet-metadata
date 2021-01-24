using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DaJet.Metadata.Tests
{
    [TestClass]
    public class UnitTests
    {
        private readonly IMetadataProvider metadata = new MetadataProvider();
        public UnitTests()
        {
            // my_exchange
            // trade_11_2_3_159_demo
            // accounting_3_0_72_72_demo
            metadata.UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=accounting_3_0_72_72_demo;Integrated Security=True");
        }

        [TestMethod]
        public void LoadDBNames()
        {
            Dictionary<string, DBNameEntry> dbnames = new Dictionary<string, DBNameEntry>();
            
            Stopwatch watch = Stopwatch.StartNew();
            watch.Start();
            
            metadata.LoadDBNames(dbnames);
            
            watch.Stop();
            Console.WriteLine(dbnames.Count + " meta objects loaded.");
            Console.WriteLine("Elapsed in " + watch.ElapsedMilliseconds + " milliseconds.");
            foreach (DBNameEntry entry in dbnames.Values)
            {
                Console.WriteLine(entry.ToString());
            }
        }

        [TestMethod]
        public void LoadInfoBase()
        {
            Stopwatch watch = Stopwatch.StartNew();
            watch.Start();

            InfoBase infoBase = metadata.LoadInfoBase();

            watch.Stop();
            Console.WriteLine(infoBase.Name);
            if (!string.IsNullOrWhiteSpace(infoBase.Alias))
            {
                int length = infoBase.Alias.Length;
                if (length > 1024) length = 1024;
                Console.WriteLine(infoBase.Alias.Substring(0, length));
            }
            Console.WriteLine("Elapsed in " + watch.ElapsedMilliseconds + " milliseconds.");
        }

        [TestMethod]
        public void LoadMetaObject()
        {
            Stopwatch watch = Stopwatch.StartNew();

            watch.Start();
            MetaObject metaObject = metadata.LoadMetaObject("Catalog", "DaJetExchangeQueue");
            watch.Stop();
            if (metaObject == null)
            {
                Console.WriteLine("Metaobject \"Catalog.DaJetExchangeQueue\" is not found.");
            }
            Console.WriteLine("First call elapsed in " + watch.ElapsedMilliseconds + " milliseconds.");

            watch.Reset();
            watch.Start();
            metaObject = metadata.LoadMetaObject("Catalog", "DaJetExchangeQueue");
            watch.Stop();
            if (metaObject == null)
            {
                Console.WriteLine("Metaobject \"Catalog.DaJetExchangeQueue\" is not found.");
            }
            Console.WriteLine("Second call elapsed in " + watch.ElapsedMilliseconds + " milliseconds.");

            if (metaObject != null)
            {
                Console.WriteLine(metaObject.Name);
                foreach (MetaProperty property in metaObject.Properties)
                {
                    string propertyName = property.Name + " (";
                    foreach (MetaField field in property.Fields)
                    {
                        propertyName += field.Name + ",";
                    }
                    propertyName = propertyName.TrimEnd(',');
                    propertyName += ")";
                    Console.WriteLine(" - " + propertyName);
                }
            }
        }

        [TestMethod]
        public void LoadMetaObjectForDebuging()
        {
            MetaObject metaObject = metadata.LoadMetaObject("Publication", "—инхронизаци€ƒанных„ерез”ниверсальный‘ормат");
        }
    }
}