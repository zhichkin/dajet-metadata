using DaJet.Metadata.Enrichers;
using DaJet.Metadata.Model;
using DaJet.Metadata.Parsers;
using DaJet.Metadata.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DaJet.Metadata.NewParser
{
    [TestClass] public sealed class NewParser
    {
        private const string ROOT_FILE_NAME = "root";
        private const string DBNAMES_FILE_NAME = "DBNames";
        private const string DBSCHEMA_FILE_NAME = "DBSchema";

        private readonly ConfigFileParser FileParser = new ConfigFileParser();
        private readonly IConfigFileReader FileReader = new ConfigFileReader();

        [TestMethod] public void MS_ReadDBSchema()
        {
            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=accounting_3_0_72_72_demo;Integrated Security=True"); // trade_11_2_3_159_demo

            byte[] fileData = metadata.ReadBytes(DBSCHEMA_FILE_NAME);
            using (MemoryStream memory = new MemoryStream(fileData))
            using (StreamReader reader = new StreamReader(memory, Encoding.UTF8, false))
            using (StreamWriter stream = new StreamWriter(@"C:\temp\DbSchema.txt", false, Encoding.UTF8))
            {
                stream.Write(reader.ReadToEnd());
            }

            ConfigObject schema;
            using (MemoryStream memory = new MemoryStream(fileData))
            using (StreamReader reader = new StreamReader(memory, Encoding.UTF8, false))
            {
                schema = FileParser.Parse(reader);
            }
            using (StreamWriter stream = new StreamWriter(@"C:\temp\DbSchema_parsed.txt", false, Encoding.UTF8))
            {
                WriteToFile(stream, schema, 0, string.Empty);
            }
        }
        [TestMethod] public void PG_ReadDBSchema()
        {
            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.PostgreSQL)
                .UseConnectionString("Host=127.0.0.1;Port=5432;Database=trade_11_2_3_159_demo;Username=postgres;Password=postgres;");

            byte[] fileData = metadata.ReadBytes(DBSCHEMA_FILE_NAME);
            using (MemoryStream memory = new MemoryStream(fileData))
            using (StreamReader reader = new StreamReader(memory, Encoding.UTF8, false))
            using (StreamWriter stream = new StreamWriter(@"C:\temp\DbSchema-pg.txt", false, Encoding.UTF8))
            {
                stream.Write(reader.ReadToEnd());
            }

            ConfigObject schema;
            using (MemoryStream memory = new MemoryStream(fileData))
            using (StreamReader reader = new StreamReader(memory, Encoding.UTF8, false))
            {
                schema = FileParser.Parse(reader);
            }
            using (StreamWriter stream = new StreamWriter(@"C:\temp\DbSchema-pg-parsed.txt", false, Encoding.UTF8))
            {
                WriteToFile(stream, schema, 0, string.Empty);
            }
        }

        [TestMethod] public void ParseDbNames()
        {
            ConfigObject mdObject;
            using (StreamReader stream = new StreamReader(@"C:\temp\DbNames.txt", Encoding.UTF8))
            {
                mdObject = FileParser.Parse(stream);
            }
            using (StreamWriter stream = new StreamWriter(@"C:\temp\DbNames_parsed.txt", false, Encoding.UTF8))
            {
                WriteToFile(stream, mdObject, 0, string.Empty);
            }
        }
        [TestMethod] public void ParseConfigObject()
        {
            ConfigObject mdObject;
            string filePath = @"C:\temp\original.txt";
            using (StreamReader stream = new StreamReader(filePath, Encoding.UTF8))
            {
                mdObject = FileParser.Parse(stream);
            }
            using (StreamWriter stream = new StreamWriter(@"C:\temp\original_parsed.txt", false, Encoding.UTF8))
            {
                WriteToFile(stream, mdObject, 0, string.Empty);
            }
            filePath = @"C:\temp\changed.txt";
            using (StreamReader stream = new StreamReader(filePath, Encoding.UTF8))
            {
                mdObject = FileParser.Parse(stream);
            }
            using (StreamWriter stream = new StreamWriter(@"C:\temp\changed_parsed.txt", false, Encoding.UTF8))
            {
                WriteToFile(stream, mdObject, 0, string.Empty);
            }
        }
        private void WriteToFile(StreamWriter stream, ConfigObject mdObject, int level, string path)
        {
            string indent = level == 0 ? string.Empty : "-".PadLeft(level * 4, '-');
            for (int i = 0; i < mdObject.Values.Count; i++)
            {
                object value = mdObject.Values[i];

                string thisPath = path + (string.IsNullOrEmpty(path) ? string.Empty : ".") + i.ToString();

                if (value is ConfigObject child)
                {
                    stream.WriteLine(indent + "[" + level.ToString() + "] (" + thisPath + ") " + value.ToString());
                    WriteToFile(stream, child, level + 1, thisPath);
                }
                else if (value is string text)
                {
                    stream.WriteLine(indent + "[" + level.ToString() + "] (" + thisPath + ") \"" + text.ToString() + "\"");
                }
                else
                {
                    stream.WriteLine(indent + "[" + level.ToString() + "] (" + thisPath + ") " + value.ToString());
                }
            }
        }

        [TestMethod] public void TestCompareObjects()
        {
            ConfigObject mdSource;
            ConfigObject mdTarget;
            DiffObject diff;
            string sourceFile = @"C:\temp\original.txt";
            string targetFile = @"C:\temp\changed.txt";
            using (StreamReader stream = new StreamReader(sourceFile, Encoding.UTF8))
            {
                mdSource = FileParser.Parse(stream);
            }
            using (StreamReader stream = new StreamReader(targetFile, Encoding.UTF8))
            {
                mdTarget = FileParser.Parse(stream);
            }

            diff = mdSource.CompareTo(mdTarget);

            using (StreamWriter stream = new StreamWriter(@"C:\temp\DiffObject.txt", false, Encoding.UTF8))
            {
                ShowDiffObject(stream, diff, 0);
            }
        }
        private void ShowDiffObject(StreamWriter stream, DiffObject parent, int level)
        {
            string indent = level == 0 ? string.Empty : "-".PadLeft(level * 4, '-');

            ConfigObject mdSource = parent.SourceValue as ConfigObject;
            ConfigObject mdTarget = parent.TargetValue as ConfigObject;

            if (mdSource != null && mdTarget != null)
            {
                stream.WriteLine(CreateObjectPresentation(parent, level, indent));
            }
            else if (mdSource != null)
            {
                stream.WriteLine(CreateObjectPresentation(parent, level, indent));
            }
            else if (mdTarget != null)
            {
                stream.WriteLine(CreateObjectPresentation(parent, level, indent));
            }
            else
            {
                stream.WriteLine(CreateDiffPresentation(parent, level, indent));
            }

            foreach (DiffObject diff in parent.DiffObjects)
            {
                ShowDiffObject(stream, diff, level + 1);
            }
        }
        private string CreateDiffPresentation(DiffObject diff, int level, string indent)
        {
            string token = " "; // None
            if (diff.DiffKind == DiffKind.Update)
            {
                token = "*";
            }
            else if (diff.DiffKind == DiffKind.Insert)
            {
                token = "+";
            }
            else if (diff.DiffKind == DiffKind.Delete)
            {
                token = "-";
            }
            string presentation = indent + "[" + level.ToString() + "] " + token + " (" + diff.Path + ") ";
            if (diff.SourceValue == null)
            {
                presentation += "NULL";
            }
            else
            {
                presentation += "\"" + diff.SourceValue.ToString() + "\"";
            }
            presentation += " : ";
            if (diff.TargetValue == null)
            {
                presentation += "NULL";
            }
            else
            {
                presentation += "\"" + diff.TargetValue?.ToString() + "\"";
            }
            return presentation;
        }
        private string CreateObjectPresentation(DiffObject diff, int level, string indent)
        {
            string presentation = indent + "[" + level.ToString() + "] (" + diff.Path + ") ";
            if (diff.SourceValue == null)
            {
                presentation += "NULL";
            }
            else
            {
                presentation += "\"" + diff.SourceValue.ToString() + "\"";
            }
            presentation += " : ";
            if (diff.TargetValue == null)
            {
                presentation += "NULL";
            }
            else
            {
                presentation += "\"" + diff.TargetValue?.ToString() + "\"";
            }
            return presentation;
        }

        [TestMethod] public void TestLoadingObject()
        {
            InfoBase infoBase = new InfoBase();
            DbNamesParser dbNames = new DbNamesParser();
            IMetadataManager manager = new MetadataManager();

            string filePath = @"C:\temp\DbNames.txt";
            using (StreamReader stream = new StreamReader(filePath, Encoding.UTF8))
            {
                dbNames.Parse(stream, infoBase, manager);
            }

            if (!infoBase.Catalogs.TryGetValue(new Guid("e1f1df1a-5f4b-4269-9f67-4a5fa61df942"), out ApplicationObject metaObject))
            {
                Console.WriteLine("Catalog is not found!");
                return;
            }

            Catalog catalog = (Catalog)metaObject;
            Configurator configurator = new Configurator(FileReader);
            IContentEnricher enricher = configurator.GetEnricher<Catalog>();
            IConfigFileReader fileReader = new ConfigFileReader();
            fileReader.UseDatabaseProvider(DatabaseProvider.SQLServer);
            fileReader.UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True");
            byte[] bytes = fileReader.ReadBytes("e1f1df1a-5f4b-4269-9f67-4a5fa61df942");
            using (StreamReader reader = fileReader.CreateReader(bytes))
            {
                ConfigObject cfo = FileParser.Parse(reader);
                enricher.Enrich(catalog, cfo);
            }

            //filePath = @"C:\temp\e1f1df1a-5f4b-4269-9f67-4a5fa61df942.txt";
            //using (StreamReader stream = new StreamReader(filePath, Encoding.UTF8))
            //{
            //    parser.Parse(stream, catalog, infoBase, DatabaseProvider.SQLServer);
            //}

            Console.WriteLine("Uuid : " + catalog.Uuid.ToString());
            Console.WriteLine("Name : " + catalog.Name);
            Console.WriteLine("Alias : " + catalog.Alias);
            Console.WriteLine("TypeCode : " + catalog.TypeCode.ToString());
            Console.WriteLine("FileName : " + catalog.FileName.ToString());
            Console.WriteLine("TableName : " + catalog.TableName);
            Console.WriteLine("Owners : " + catalog.Owners.ToString());
            Console.WriteLine("CodeType : " + catalog.CodeType.ToString());
            Console.WriteLine("CodeLength : " + catalog.CodeLength);
            Console.WriteLine("DescriptionLength : " + catalog.DescriptionLength);
            Console.WriteLine("HierarchyType : " + catalog.HierarchyType.ToString());
            Console.WriteLine("IsHierarchical : " + catalog.IsHierarchical.ToString());

            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True");
            List<string> delete;
            List<string> insert;
            bool result = metadata.CompareWithDatabase(catalog, out delete, out insert);
            Console.WriteLine("Compare catalog with database = " + result.ToString());

            ApplicationObject tablePart = catalog.ApplicationObjects.Where(t => t.Name == "ТабличнаяЧасть3").FirstOrDefault();
            if (tablePart == null)
            {
                Console.WriteLine("Table part is not found!");
                return;
            }
            result = metadata.CompareWithDatabase(tablePart, out delete, out insert);
            Console.WriteLine("Compare table part with database = " + result.ToString());
        }

        [TestMethod] public void TestSharedProperties()
        {
            IConfigFileReader fileReader = new ConfigFileReader();
            fileReader.UseDatabaseProvider(DatabaseProvider.SQLServer);
            fileReader.UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=accounting_3_0_72_72_demo;Integrated Security=True"); // dajet-metadata

            Configurator configurator = new Configurator(fileReader);
            InfoBase infoBase = configurator.OpenInfoBase();

            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=accounting_3_0_72_72_demo;Integrated Security=True"); // trade_11_2_3_159_demo

            IContentEnricher enricher = configurator.GetEnricher<Catalog>();
            using (StreamWriter stream = new StreamWriter(@"C:\temp\TestCatalogs.txt", false, Encoding.UTF8))
            {
                foreach (var kvp in infoBase.Catalogs)
                {
                    Catalog catalog = kvp.Value as Catalog;
                    if (catalog == null)
                    {
                        stream.WriteLine("Catalog {" + kvp.Key.ToString() + "} is not found!");
                        continue;
                    }
                    
                    ConfigObject cfo = fileReader.ReadConfigObject(catalog.FileName.ToString());
                    enricher.Enrich(catalog, cfo);

                    List<string> delete;
                    List<string> insert;
                    bool result = metadata.CompareWithDatabase(catalog, out delete, out insert);
                    if (!result)
                    {
                        stream.WriteLine("Catalog \"" + catalog.Name + "\" (" + catalog.TableName + "):");
                        if (delete.Count > 0)
                        {
                            stream.WriteLine("  Delete fields:");
                            foreach (string field in delete)
                            {
                                stream.WriteLine("   - " + field); // d896353a-b506-4161-9eba-376a6ccd9671 - default string ?
                            }
                        }
                        if (insert.Count > 0)
                        {
                            stream.WriteLine("  Insert fields:");
                            foreach (string field in insert)
                            {
                                stream.WriteLine("   - " + field);
                            }
                        }
                    }
                }
            }
        }
    }
}