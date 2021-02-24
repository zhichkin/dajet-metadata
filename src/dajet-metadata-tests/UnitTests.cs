using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DaJet.Metadata.Tests
{
    [TestClass]
    public class UnitTests
    {
        private const string DBNAMES_FILE_NAME = "DBNames";

        private string ConnectionString { get; set; }
        private readonly IMetadataProvider metadata = new MetadataProvider();
        public UnitTests()
        {
            // my_exchange
            // trade_11_2_3_159_demo
            // accounting_3_0_72_72_demo
            ConnectionString = "Data Source=ZHICHKIN;Initial Catalog=accounting_3_0_72_72_demo;Integrated Security=True";
            metadata.UseConnectionString(ConnectionString);
        }

        [TestMethod]
        public void ParseDBNames()
        {
            Console.WriteLine("GetMaxCharCount = " + Encoding.UTF8.GetMaxCharCount(1));

            Stopwatch watch = Stopwatch.StartNew();

            using (Stream stream = metadata.GetDBNamesFromDatabase())
            {
                watch.Start();

                //Dictionary<string, DBNameEntry> dbnames = metadata.ParseDBNames(stream);
                List<string[]> dbnames = metadata.ParseDBNamesOptimized(stream);

                watch.Stop();
                Console.WriteLine("Elapsed in " + watch.ElapsedMilliseconds + " milliseconds.");

                if (dbnames != null)
                {
                    Console.WriteLine(dbnames.Count + " meta objects loaded.");
                }
            }

            //foreach (DBNameEntry entry in dbnames.Values)
            //{
            //    Console.WriteLine(entry.ToString());
            //}
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

        [TestMethod]
        public void NewReadConfigurationProperties()
        {
            ConfigInfo config = metadata.ReadConfigurationProperties();

            Console.WriteLine("Name = " + config.Name);
            Console.WriteLine("Alias = " + config.Alias);
            Console.WriteLine("Comment = " + config.Comment);
            Console.WriteLine("Version = " + config.Version);
            Console.WriteLine("ConfigVersion = " + config.ConfigVersion);
            Console.WriteLine("SyncCallsMode = " + config.SyncCallsMode.ToString());
            Console.WriteLine("DataLockingMode = " + config.DataLockingMode.ToString());
            Console.WriteLine("ModalWindowMode = " + config.ModalWindowMode.ToString());
            Console.WriteLine("AutoNumberingMode = " + config.AutoNumberingMode.ToString());
            Console.WriteLine("UICompatibilityMode = " + config.UICompatibilityMode.ToString());
        }
        [TestMethod]
        public void NewReadDBNames()
        {
            Stopwatch watch = Stopwatch.StartNew();
            watch.Start();

            DBNamesCash cash;
            IMetadataFileReader reader = new MetadataFileReader();
            reader.UseConnectionString(ConnectionString);
            byte[] fileData = reader.ReadBytes(DBNAMES_FILE_NAME);
            using (StreamReader stream = reader.CreateReader(fileData))
            {
                IDBNamesFileParser parser = new DBNamesFileParser();
                cash = parser.Parse(stream);
            }

            watch.Stop();
            Console.WriteLine("Elapsed in " + watch.ElapsedMilliseconds + " milliseconds.");
        }
    }
}