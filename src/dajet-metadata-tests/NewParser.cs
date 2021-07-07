using DaJet.Metadata.Model;
using DaJet.Metadata.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DaJet.Metadata.NewParser
{
    [TestClass]
    public sealed class NewParser
    {
        private readonly ConfigFileParser FileParser = new ConfigFileParser();

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

        [TestMethod] public void WriteRootToFile()
        {
            IConfigFileReader fileReader = new ConfigFileReader();
            fileReader.UseDatabaseProvider(DatabaseProvider.SQLServer);
            fileReader.UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True");

            byte[] root = fileReader.ReadBytes("root");
            using (StreamReader reader = fileReader.CreateReader(root))
            {
                using (StreamWriter stream = new StreamWriter(@"C:\temp\root.txt", false, Encoding.UTF8))
                {
                    //WriteToFile(stream, root, 0, string.Empty);
                    stream.Write(reader.ReadToEnd());
                }
            }
        }
        [TestMethod] public void WriteConfigRootToFile()
        {
            IConfigFileReader fileReader = new ConfigFileReader();
            fileReader.UseDatabaseProvider(DatabaseProvider.SQLServer);
            fileReader.UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=cerberus;Integrated Security=True");

            ConfigObject root = fileReader.ReadConfigObject("root");
            ConfigObject config = fileReader.ReadConfigObject(root.GetString(new int[] { 1 }));

            using (StreamWriter stream = new StreamWriter(@"C:\temp\config.txt", false, Encoding.UTF8))
            {
                WriteToFile(stream, config, 0, string.Empty);
            }
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
            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True"); // accounting_3_0_72_72_demo

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
            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=cerberus;Integrated Security=True"); // accounting_3_0_72_72_demo

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
            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=cerberus;Integrated Security=True"); // accounting_3_0_72_72_demo

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
            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=cerberus;Integrated Security=True"); // accounting_3_0_72_72_demo

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
            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=cerberus;Integrated Security=True"); // accounting_3_0_72_72_demo

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
            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=cerberus;Integrated Security=True"); // accounting_3_0_72_72_demo

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
            IMetadataService metadata = new MetadataService();
            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=cerberus;Integrated Security=True"); // accounting_3_0_72_72_demo

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