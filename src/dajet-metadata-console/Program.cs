using DaJet.Metadata.Model;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text;

namespace DaJet.Metadata.CLI
{
    public static class Program
    {
        private const string SERVER_IS_NOT_DEFINED_ERROR = "Server address is not defined.";
        private const string DATABASE_IS_NOT_DEFINED_ERROR = "Database name is not defined.";

        public static int Main(string[] args)
        {
            RootCommand command = new RootCommand()
            {
                new Option<string>("--ms", "Microsoft SQL Server address or name"),
                new Option<string>("--pg", "PostgresSQL server address or name"),
                new Option<string>("--d", "Database name"),
                new Option<string>("--u", "User name (Windows authentication is used if not defined)"),
                new Option<string>("--p", "User password if SQL Server authentication is used"),
                new Option<string>("--m", "MetadataObject name (example: \"Справочник.Номенклатура\")"),
                new Option<FileInfo>("--out-file", "File path to save metaobject information"),
                new Option<FileInfo>("--out-root", "File path to save configuration information")
            };
            command.Description = "DaJet (metadata reader utility)";
            command.Handler = CommandHandler.Create<string, string, string, string, string, FileInfo, string, FileInfo>(ExecuteCommand);
            return command.Invoke(args);
        }
        private static void ShowErrorMessage(string errorText)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(errorText);
            Console.ForegroundColor = ConsoleColor.White;
        }
        private static void ExecuteCommand(string ms, string pg, string d, string u, string p, FileInfo outRoot, string m, FileInfo outFile)
        {
            if (string.IsNullOrWhiteSpace(ms) && string.IsNullOrWhiteSpace(pg))
            {
                ShowErrorMessage(SERVER_IS_NOT_DEFINED_ERROR); return;
            }
            if (string.IsNullOrWhiteSpace(d))
            {
                ShowErrorMessage(DATABASE_IS_NOT_DEFINED_ERROR); return;
            }

            IMetadataService metadataService = new MetadataService();
            if (!string.IsNullOrWhiteSpace(ms))
            {
                metadataService
                    .UseDatabaseProvider(DatabaseProviders.SQLServer)
                    .ConfigureConnectionString(ms, d, u, p);
            }
            else if (!string.IsNullOrWhiteSpace(pg))
            {
                metadataService
                    .UseDatabaseProvider(DatabaseProviders.PostgreSQL)
                    .ConfigureConnectionString(pg, d, u, p);
            }

            ConfigInfo config = metadataService.ReadConfigurationProperties();

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

            if (outRoot != null)
            {
                SaveConfigToFile(outRoot.FullName, metadataService);
            }

            if (outFile != null && !string.IsNullOrWhiteSpace(m))
            {
                SaveMetadataObjectToFile(outFile.FullName, metadataService, m);
            }
        }
        private static void SaveConfigToFile(string filePath, IMetadataService metadataService)
        {
            string fileName = metadataService.GetConfigurationFileName();
            byte[] fileData = metadataService.ReadBytes(fileName);
            using (StreamReader reader = metadataService.CreateReader(fileData))
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.Write(reader.ReadToEnd());
            }
        }
        private static void SaveMetadataObjectToFile(string filePath, IMetadataService metadataService, string metadataName)
        {
            string[] names = metadataName.Split('.');
            if (names.Length != 2) return;
            string typeName = names[0];
            string objectName = names[1];

            MetadataObject metaObject = null;
            Dictionary<Guid, MetadataObject> collection = null;
            InfoBase infoBase = metadataService.LoadInfoBase();
            if (typeName == "Справочник") collection = infoBase.Catalogs;
            else if (typeName == "Документ") collection = infoBase.Documents;
            else if (typeName == "ПланОбмена") collection = infoBase.Publications;
            else if (typeName == "РегистрСведений") collection = infoBase.InformationRegisters;
            else if (typeName == "РегистрНакопления") collection = infoBase.AccumulationRegisters;
            if (collection == null) return;

            metaObject = collection.Values.Where(o => o.Name == objectName).FirstOrDefault();
            if (metaObject == null) return;

            byte[] fileData = metadataService.ReadBytes(metaObject.FileName.ToString());
            if (fileData == null) return;

            using (StreamReader reader = metadataService.CreateReader(fileData))
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.Write(reader.ReadToEnd());
            }
        }
    }
}