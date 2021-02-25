using DaJet.Metadata.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;

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
            ConnectionString = "Data Source=ZHICHKIN;Initial Catalog=trade_11_2_3_159_demo;Integrated Security=True";
            metadata.UseConnectionString(ConnectionString);
        }

       [TestMethod]
        public void ReadConfigurationProperties()
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
        public void ReadDBNames()
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
        [TestMethod]
        public void ReadMetadata()
        {
            Stopwatch watch = Stopwatch.StartNew();
            watch.Start();

            IMetadataFileReader reader = new MetadataFileReader();
            reader.UseConnectionString(ConnectionString);
            
            IMetadataReader mdr = new MetadataReader(reader);
            _ = mdr.LoadInfoBase();

            watch.Stop();
            Console.WriteLine("Elapsed in " + watch.ElapsedMilliseconds + " milliseconds.");
        }
    }
}