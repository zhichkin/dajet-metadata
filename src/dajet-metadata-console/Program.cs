using DaJet.Metadata.Model;
using Microsoft.Data.SqlClient;
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
            //args = new string[] { "--ms", "ZHICHKIN", "--d", "dajet-metadata", "--out-root", "C:\\temp\\root.txt" };
            //args = new string[] { "--ms", "172.18.17.32", "--d", "cerberus", "--schema", "Справочник.Клиенты.КонтактнаяИнформация" };
            //args = new string[] { "--pg", "172.18.17.32", "--d", "test_node_2", "--u", "postgres", "--p", "postgres", "--schema", "РегистрСведений.ВходящаяОчередьRabbitMQ" };

            RootCommand command = new RootCommand()
            {
                new Option<string>("--ms", "Microsoft SQL Server address or name"),
                new Option<string>("--pg", "PostgresSQL server address or name"),
                new Option<string>("--d", "Database name"),
                new Option<string>("--u", "User name (Windows authentication is used if not defined)"),
                new Option<string>("--p", "User password if SQL Server authentication is used"),
                new Option<string>("--m", "ApplicationObject name to save to file (example: \"Справочник.Номенклатура\")"),
                new Option<string>("--schema", "Metadata object to get SQL schema for (example: \"Справочник.Номенклатура\")"),
                new Option<FileInfo>("--out-file", "File path to save metaobject information"),
                new Option<FileInfo>("--out-root", "File path to save configuration information")
            };
            command.Description = "DaJet (metadata reader utility)";
            command.Handler = CommandHandler.Create<string, string, string, string, string, FileInfo, string, string, FileInfo>(ExecuteCommand);
            return command.Invoke(args);
        }
        private static void ShowErrorMessage(string errorText)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(errorText);
            Console.ForegroundColor = ConsoleColor.White;
        }
        private static void ExecuteCommand(string ms, string pg, string d, string u, string p, FileInfo outRoot, string m, string schema, FileInfo outFile)
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
                string connectionString = BuildConnectionString(ms, d, u, p);
                metadataService
                    .UseDatabaseProvider(DatabaseProvider.SQLServer)
                    .UseConnectionString(connectionString);
            }
            else if (!string.IsNullOrWhiteSpace(pg))
            {
                metadataService
                    .UseDatabaseProvider(DatabaseProvider.PostgreSQL)
                    .ConfigureConnectionString(pg, d, u, p);
            }

            InfoBase infoBase = metadataService.OpenInfoBase();

            ConfigInfo config = infoBase.ConfigInfo;

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
                SaveConfigToFile(outRoot, metadataService);
            }

            if (outFile != null && !string.IsNullOrWhiteSpace(m))
            {
                SaveApplicationObjectToFile(outFile.FullName, metadataService, m);
            }

            if (!string.IsNullOrWhiteSpace(schema))
            {
                ShowApplicationObjectSchema(infoBase, schema);
            }
        }
        private static void SaveConfigToFile(FileInfo outRoot, IMetadataService metadataService)
        {
            string filePath = outRoot.FullName;
            byte[] fileData = metadataService.ReadConfigFile("root");
            using (StreamReader reader = metadataService.CreateReader(fileData))
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.Write(reader.ReadToEnd());
            }

            if (!metadataService.TryOpenInfoBase(out InfoBase infoBase, out string errorMessage))
            {
                ShowErrorMessage(errorMessage);
                return;
            }

            string configFile = infoBase.FileName.ToString();
            filePath = Path.Combine(outRoot.DirectoryName, Path.GetFileNameWithoutExtension(outRoot.Name) + "_" + configFile) + outRoot.Extension;

            fileData = metadataService.ReadConfigFile(configFile);
            using (StreamReader reader = metadataService.CreateReader(fileData))
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.Write(reader.ReadToEnd());
            }
        }
        private static void SaveApplicationObjectToFile(string filePath, IMetadataService metadataService, string metadataName)
        {
            string[] names = metadataName.Split('.');
            if (names.Length != 2) return;
            string typeName = names[0];
            string objectName = names[1];

            ApplicationObject metaObject = null;
            Dictionary<Guid, ApplicationObject> collection = null;
            InfoBase infoBase = metadataService.LoadInfoBase();
            if (typeName == "Справочник") collection = infoBase.Catalogs;
            else if (typeName == "Документ") collection = infoBase.Documents;
            else if (typeName == "ПланОбмена") collection = infoBase.Publications;
            else if (typeName == "РегистрСведений") collection = infoBase.InformationRegisters;
            else if (typeName == "РегистрНакопления") collection = infoBase.AccumulationRegisters;
            else if (typeName == "ПланВидовХарактеристик") collection = infoBase.Characteristics;
            if (collection == null) return;

            metaObject = collection.Values.Where(o => o.Name == objectName).FirstOrDefault();
            if (metaObject == null) return;

            byte[] fileData = metadataService.ReadConfigFile(metaObject.FileName.ToString());
            if (fileData == null) return;

            using (StreamReader reader = metadataService.CreateReader(fileData))
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.Write(reader.ReadToEnd());
            }
        }
        private static void ShowApplicationObjectSchema(InfoBase infoBase, string metadataName)
        {
            string[] names = metadataName.Split('.');
            
            string typeName = names[0];
            string objectName = names[1];
            string tablePartName = string.Empty;
            if (names.Length == 3)
            {
                tablePartName = names[2];
            }

            ApplicationObject metaObject = null;
            Dictionary<Guid, ApplicationObject> collection = null;
            
            if (typeName == "Справочник") collection = infoBase.Catalogs;
            else if (typeName == "Документ") collection = infoBase.Documents;
            else if (typeName == "ПланОбмена") collection = infoBase.Publications;
            else if (typeName == "РегистрСведений") collection = infoBase.InformationRegisters;
            else if (typeName == "РегистрНакопления") collection = infoBase.AccumulationRegisters;
            else if (typeName == "ПланВидовХарактеристик") collection = infoBase.Characteristics;
            if (collection == null)
            {
                ShowErrorMessage($"Collection \"{typeName}\" is not found.");
                return;
            }

            metaObject = collection.Values.Where(o => o.Name == objectName).FirstOrDefault();
            if (metaObject == null)
            {
                ShowErrorMessage($"Object \"{objectName}\" is not found.");
                return;
            }

            if (!string.IsNullOrEmpty(tablePartName))
            {
                metaObject = metaObject.TableParts.Where(t => t.Name == tablePartName).FirstOrDefault();
                if (metaObject == null)
                {
                    ShowErrorMessage($"Table part \"{tablePartName}\" is not found.");
                    return;
                }
            }

            Console.WriteLine("".PadLeft(10, '-'));
            Console.WriteLine($"Object name: {metadataName}");
            Console.WriteLine($"Table  name: {metaObject.TableName}");
            Console.WriteLine();

            foreach (MetadataProperty property in metaObject.Properties)
            {
                Console.WriteLine($"+ {property.Name} ({property.Purpose})");
                
                foreach (DatabaseField field in property.Fields)
                {
                    Console.WriteLine($"  - {field.Name} ({field.Purpose}) {field.TypeName}");
                }

                Console.WriteLine();
            }

            Console.WriteLine("Press any key to exit ...");
            Console.ReadKey(false);
        }
        private static string BuildConnectionString(string server, string database, string userName, string password)
        {
            SqlConnectionStringBuilder connectionString = new()
            {
                DataSource = server,
                InitialCatalog = database
            };
            if (!string.IsNullOrWhiteSpace(userName))
            {
                connectionString.UserID = userName;
                connectionString.Password = password;
            }
            connectionString.IntegratedSecurity = string.IsNullOrWhiteSpace(userName);
            connectionString.Encrypt = false;

            return connectionString.ToString();
        }
    }
}