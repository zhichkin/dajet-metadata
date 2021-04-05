using DaJet.Metadata.Model;
using DaJet.Metadata.Parsers;
using DaJet.Metadata.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace DaJet.Metadata.NewParser
{
    [TestClass]
    public sealed class NewParser
    {
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
            FileReader.UseDatabaseProvider(DatabaseProvider.SQLServer);
            FileReader.UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True");

            Configurator configurator = new Configurator(FileReader);
            InfoBase infoBase = configurator.OpenInfoBase();
            Catalog catalog = (Catalog)infoBase.Catalogs.Values.Where(c => c.Name == "ПростойСправочник").FirstOrDefault();
            if (catalog == null)
            {
                Console.WriteLine("Catalog is not found!");
                return;
            }

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

            ApplicationObject tablePart = catalog.TableParts.Where(t => t.Name == "ТабличнаяЧасть3").FirstOrDefault();
            if (tablePart == null)
            {
                Console.WriteLine("Table part is not found!");
                return;
            }
            result = metadata.CompareWithDatabase(tablePart, out delete, out insert);
            Console.WriteLine("Compare table part with database = " + result.ToString());
        }

        private void LogResult(StreamWriter stream, ApplicationObject model, List<string> delete, List<string> insert)
        {
            stream.WriteLine("\"" + model.Name + "\" (" + model.TableName + "):");
            if (delete.Count > 0)
            {
                stream.WriteLine("  Delete fields:");
                foreach (string field in delete)
                {
                    stream.WriteLine("   - " + field);
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
        [TestMethod] public void TestCatalogs()
        {
            //FileReader.UseDatabaseProvider(DatabaseProvider.SQLServer);
            //FileReader.UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=accounting_3_0_72_72_demo;Integrated Security=True"); // trade_11_2_3_159_demo
            //Configurator configurator = new Configurator(FileReader);
            //InfoBase infoBase = configurator.OpenInfoBase();

            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=accounting_3_0_72_72_demo;Integrated Security=True"); // accounting_3_0_72_72_demo

            InfoBase infoBase = metadata.LoadInfoBase();

            List<string> delete;
            List<string> insert;
            using (StreamWriter stream = new StreamWriter(@"C:\temp\TestCatalogs.txt", false, Encoding.UTF8))
            {
                int count = 0;
                foreach (var kvp in infoBase.Catalogs)
                {
                    //if (kvp.Value.Name != "ВнешниеПользователи") continue;

                    count++;
                    Catalog model = kvp.Value as Catalog;
                    if (model == null)
                    {
                        stream.WriteLine("Catalog {" + kvp.Key.ToString() + "} is not found!");
                        continue;
                    }

                    bool result = metadata.CompareWithDatabase(model, out delete, out insert);
                    if (!result)
                    {
                        LogResult(stream, model, delete, insert);
                    }

                    foreach (TablePart tablePart in model.TableParts)
                    {
                        result = metadata.CompareWithDatabase(model, out delete, out insert);
                        if (!result)
                        {
                            LogResult(stream, tablePart, delete, insert);
                        }
                    }
                }
                stream.WriteLine("*******************************");
                stream.WriteLine(count.ToString() + " objects processed.");
            }
        }
        [TestMethod] public void TestCharacteristics()
        {
            //FileReader.UseDatabaseProvider(DatabaseProvider.SQLServer);
            //FileReader.UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=accounting_3_0_72_72_demo;Integrated Security=True"); // trade_11_2_3_159_demo
            //Configurator configurator = new Configurator(FileReader);
            //InfoBase infoBase = configurator.OpenInfoBase();

            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=accounting_3_0_72_72_demo;Integrated Security=True"); // accounting_3_0_72_72_demo

            InfoBase infoBase = metadata.LoadInfoBase();

            List<string> delete;
            List<string> insert;
            using (StreamWriter stream = new StreamWriter(@"C:\temp\TestCharacteristics.txt", false, Encoding.UTF8))
            {
                int count = 0;
                foreach (var kvp in infoBase.Characteristics)
                {
                    count++;
                    Characteristic model = kvp.Value as Characteristic;
                    if (model == null)
                    {
                        stream.WriteLine("{" + kvp.Key.ToString() + "} is not found!");
                        continue;
                    }

                    bool result = metadata.CompareWithDatabase(model, out delete, out insert);
                    if (!result)
                    {
                        LogResult(stream, model, delete, insert);
                    }

                    foreach (TablePart tablePart in model.TableParts)
                    {
                        result = metadata.CompareWithDatabase(model, out delete, out insert);
                        if (!result)
                        {
                            LogResult(stream, tablePart, delete, insert);
                        }
                    }
                }
                stream.WriteLine("*******************************");
                stream.WriteLine(count.ToString() + " objects processed.");
            }
        }
        [TestMethod] public void TestDocuments()
        {
            //FileReader.UseDatabaseProvider(DatabaseProvider.SQLServer);
            //FileReader.UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True"); // trade_11_2_3_159_demo
            //Configurator configurator = new Configurator(FileReader);
            //InfoBase infoBase = configurator.OpenInfoBase();

            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True"); // accounting_3_0_72_72_demo

            InfoBase infoBase = metadata.LoadInfoBase();

            List<string> delete;
            List<string> insert;
            using (StreamWriter stream = new StreamWriter(@"C:\temp\TestDocuments.txt", false, Encoding.UTF8))
            {
                int count = 0;
                foreach (var kvp in infoBase.Documents)
                {
                    count++;
                    Document model = kvp.Value as Document;
                    if (model == null)
                    {
                        stream.WriteLine("{" + kvp.Key.ToString() + "} is not found!");
                        continue;
                    }

                    bool result = metadata.CompareWithDatabase(model, out delete, out insert);
                    if (!result)
                    {
                        LogResult(stream, model, delete, insert);
                    }

                    foreach (TablePart tablePart in model.TableParts)
                    {
                        result = metadata.CompareWithDatabase(model, out delete, out insert);
                        if (!result)
                        {
                            LogResult(stream, tablePart, delete, insert);
                        }
                    }
                }
                stream.WriteLine("*******************************");
                stream.WriteLine(count.ToString() + " objects processed.");
            }
        }
        [TestMethod] public void TestPublications()
        {
            //FileReader.UseDatabaseProvider(DatabaseProvider.SQLServer);
            //FileReader.UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True"); // trade_11_2_3_159_demo
            //Configurator configurator = new Configurator(FileReader);
            //InfoBase infoBase = configurator.OpenInfoBase();

            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True"); // accounting_3_0_72_72_demo

            InfoBase infoBase = metadata.LoadInfoBase();

            List<string> delete;
            List<string> insert;
            using (StreamWriter stream = new StreamWriter(@"C:\temp\TestPublications.txt", false, Encoding.UTF8))
            {
                int count = 0;
                foreach (var kvp in infoBase.Publications)
                {
                    count++;
                    Publication model = kvp.Value as Publication;
                    if (model == null)
                    {
                        stream.WriteLine("{" + kvp.Key.ToString() + "} is not found!");
                        continue;
                    }

                    bool result = metadata.CompareWithDatabase(model, out delete, out insert);
                    if (!result)
                    {
                        LogResult(stream, model, delete, insert);
                    }

                    foreach (TablePart tablePart in model.TableParts)
                    {
                        result = metadata.CompareWithDatabase(model, out delete, out insert);
                        if (!result)
                        {
                            LogResult(stream, tablePart, delete, insert);
                        }
                    }
                }
                stream.WriteLine("*******************************");
                stream.WriteLine(count.ToString() + " objects processed.");
            }
        }
        [TestMethod] public void TestEnumerations()
        {
            //FileReader.UseDatabaseProvider(DatabaseProvider.SQLServer);
            //FileReader.UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=accounting_3_0_72_72_demo;Integrated Security=True"); // trade_11_2_3_159_demo
            //Configurator configurator = new Configurator(FileReader);
            //InfoBase infoBase = configurator.OpenInfoBase();

            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=accounting_3_0_72_72_demo;Integrated Security=True"); // accounting_3_0_72_72_demo

            InfoBase infoBase = metadata.LoadInfoBase();

            List<string> delete;
            List<string> insert;
            using (StreamWriter stream = new StreamWriter(@"C:\temp\TestEnumerations.txt", false, Encoding.UTF8))
            {
                int count = 0;
                foreach (var kvp in infoBase.Enumerations)
                {
                    count++;
                    Enumeration model = kvp.Value as Enumeration;
                    if (model == null)
                    {
                        stream.WriteLine("{" + kvp.Key.ToString() + "} is not found!");
                        continue;
                    }

                    bool result = metadata.CompareWithDatabase(model, out delete, out insert);
                    if (!result)
                    {
                        LogResult(stream, model, delete, insert);
                    }
                }
                stream.WriteLine("*******************************");
                stream.WriteLine(count.ToString() + " objects processed.");
            }
        }
        [TestMethod] public void TestInformationRegisters()
        {
            //FileReader.UseDatabaseProvider(DatabaseProvider.SQLServer);
            //FileReader.UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=trade_11_2_3_159_demo;Integrated Security=True"); // trade_11_2_3_159_demo
            //Configurator configurator = new Configurator(FileReader);
            //InfoBase infoBase = configurator.OpenInfoBase();

            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=trade_11_2_3_159_demo;Integrated Security=True"); // accounting_3_0_72_72_demo

            InfoBase infoBase = metadata.LoadInfoBase();

            List<string> delete;
            List<string> insert;
            using (StreamWriter stream = new StreamWriter(@"C:\temp\TestInformationRegisters.txt", false, Encoding.UTF8))
            {
                int count = 0;
                foreach (var kvp in infoBase.InformationRegisters)
                {
                    count++;
                    InformationRegister model = kvp.Value as InformationRegister;
                    if (model == null)
                    {
                        stream.WriteLine("{" + kvp.Key.ToString() + "} is not found!");
                        continue;
                    }

                    bool result = metadata.CompareWithDatabase(model, out delete, out insert);
                    if (!result)
                    {
                        LogResult(stream, model, delete, insert);
                    }
                }
                stream.WriteLine("*******************************");
                stream.WriteLine(count.ToString() + " objects processed.");
            }
        }
        [TestMethod] public void TestAccumulationRegisters()
        {
            //FileReader.UseDatabaseProvider(DatabaseProvider.SQLServer);
            //FileReader.UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=trade_11_2_3_159_demo;Integrated Security=True"); // trade_11_2_3_159_demo
            //Configurator configurator = new Configurator(FileReader);
            //InfoBase infoBase = configurator.OpenInfoBase();

            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=trade_11_2_3_159_demo;Integrated Security=True"); // accounting_3_0_72_72_demo

            InfoBase infoBase = metadata.LoadInfoBase();

            List<string> delete;
            List<string> insert;
            using (StreamWriter stream = new StreamWriter(@"C:\temp\TestAccumulationRegisters.txt", false, Encoding.UTF8))
            {
                int count = 0;
                foreach (var kvp in infoBase.AccumulationRegisters)
                {
                    count++;
                    AccumulationRegister model = kvp.Value as AccumulationRegister;
                    if (model == null)
                    {
                        stream.WriteLine("{" + kvp.Key.ToString() + "} is not found!");
                        continue;
                    }

                    bool result = metadata.CompareWithDatabase(model, out delete, out insert);
                    if (!result)
                    {
                        LogResult(stream, model, delete, insert);
                    }
                }
                stream.WriteLine("*******************************");
                stream.WriteLine(count.ToString() + " objects processed.");
            }
        }

        [TestMethod] public void TestPerformanceSingleThead()
        {
            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=accounting_3_0_72_72_demo;Integrated Security=True");

            Stopwatch stopwatch = new Stopwatch();

            int samples = 10;
            InfoBase infoBase;
            List<long> stats = new List<long>(samples);

            for (int i = 0; i < samples; i++)
            {
                stopwatch.Start();
                infoBase = metadata.LoadInfoBase();
                stopwatch.Stop();
                stats.Add(stopwatch.ElapsedMilliseconds);
                stopwatch.Reset();
            }

            long sum = 0;
            for (int i = 0; i < samples; i++)
            {
                sum += stats[i];
                Console.WriteLine((i + 1).ToString() + " : " + stats[i].ToString() + " ms");
            }
            Console.WriteLine("Avg: " + (sum / samples).ToString() + " ms");
        }
        [TestMethod] public void TestPerformanceParallel()
        {
            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=accounting_3_0_72_72_demo;Integrated Security=True");

            Stopwatch stopwatch = new Stopwatch();

            int samples = 10;
            InfoBase infoBase;
            List<long> stats = new List<long>(samples);

            for (int i = 0; i < samples; i++)
            {
                stopwatch.Start();
                infoBase = metadata.OpenInfoBase();
                stopwatch.Stop();
                stats.Add(stopwatch.ElapsedMilliseconds);
                stopwatch.Reset();
            }

            long sum = 0;
            for (int i = 0; i < samples; i++)
            {
                sum += stats[i];
                Console.WriteLine((i + 1).ToString() + " : " + stats[i].ToString() + " ms");
            }
            Console.WriteLine("Avg: " + (sum / samples).ToString() + " ms");
        }
    }
}