using DaJet.Metadata.Model;
using DaJet.Metadata.Parsers;
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
        [TestMethod] public void ParseDbNames()
        {
            MDObject mdObject;
            using (StreamReader stream = new StreamReader(@"C:\temp\DbNames.txt", Encoding.UTF8))
            {
                mdObject = MDObjectParser.Parse(stream);
            }
            using (StreamWriter stream = new StreamWriter(@"C:\temp\DbNames_parsed.txt", false, Encoding.UTF8))
            {
                WriteToFile(stream, mdObject, 0, string.Empty);
            }
        }
        [TestMethod] public void ParseMetadataFile()
        {
            MDObject mdObject;
            string filePath = @"C:\temp\Справочник_original.txt";
            using (StreamReader stream = new StreamReader(filePath, Encoding.UTF8))
            {
                mdObject = MDObjectParser.Parse(stream);
            }
            using (StreamWriter stream = new StreamWriter(@"C:\temp\Справочник_original_parsed.txt", false, Encoding.UTF8))
            {
                WriteToFile(stream, mdObject, 0, string.Empty);
            }
            filePath = @"C:\temp\Справочник_changed.txt";
            using (StreamReader stream = new StreamReader(filePath, Encoding.UTF8))
            {
                mdObject = MDObjectParser.Parse(stream);
            }
            using (StreamWriter stream = new StreamWriter(@"C:\temp\Справочник_changed_parsed.txt", false, Encoding.UTF8))
            {
                WriteToFile(stream, mdObject, 0, string.Empty);
            }
        }
        private void WriteToFile(StreamWriter stream, MDObject mdObject, int level, string path)
        {
            string indent = level == 0 ? string.Empty : "-".PadLeft(level * 4, '-');
            for (int i = 0; i < mdObject.Values.Count; i++)
            {
                object value = mdObject.Values[i];

                string thisPath = path + (string.IsNullOrEmpty(path) ? string.Empty : ".") + i.ToString();

                if (value is MDObject child)
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
            MDObject mdSource;
            MDObject mdTarget;
            DiffObject diff;
            string sourceFile = @"C:\temp\Справочник_original.txt";
            string targetFile = @"C:\temp\Справочник_changed.txt";
            using (StreamReader stream = new StreamReader(sourceFile, Encoding.UTF8))
            {
                mdSource = MDObjectParser.Parse(stream);
            }
            using (StreamReader stream = new StreamReader(targetFile, Encoding.UTF8))
            {
                mdTarget = MDObjectParser.Parse(stream);
            }

            diff = MDObjectParser.CompareObjects(mdSource, mdTarget);

            using (StreamWriter stream = new StreamWriter(@"C:\temp\DiffObject.txt", false, Encoding.UTF8))
            {
                ShowDiffObject(stream, diff, 0);
            }
        }
        private void ShowDiffObject(StreamWriter stream, DiffObject parent, int level)
        {
            string indent = level == 0 ? string.Empty : "-".PadLeft(level * 4, '-');

            MDObject mdSource = parent.SourceValue as MDObject;
            MDObject mdTarget = parent.TargetValue as MDObject;

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

            if (!infoBase.Catalogs.TryGetValue(new Guid("e1f1df1a-5f4b-4269-9f67-4a5fa61df942"), out MetadataObject metaObject))
            {
                Console.WriteLine("Catalog is not found!");
                return;
            }

            Catalog catalog = (Catalog)metaObject;
            CatalogFileParser parser = new CatalogFileParser();
            IMetadataFileReader fileReader = new MetadataFileReader();
            fileReader.UseDatabaseProvider(DatabaseProviders.SQLServer);
            fileReader.UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True");
            byte[] bytes = fileReader.ReadBytes("e1f1df1a-5f4b-4269-9f67-4a5fa61df942");
            using (StreamReader reader = fileReader.CreateReader(bytes))
            {
                parser.Parse(reader, catalog, infoBase, DatabaseProviders.SQLServer);
            }

            //filePath = @"C:\temp\e1f1df1a-5f4b-4269-9f67-4a5fa61df942.txt";
            //using (StreamReader stream = new StreamReader(filePath, Encoding.UTF8))
            //{
            //    parser.Parse(stream, catalog, infoBase, DatabaseProviders.SQLServer);
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
                .UseDatabaseProvider(DatabaseProviders.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True");
            List<string> delete;
            List<string> insert;
            bool result = metadata.CompareWithDatabase(catalog, out delete, out insert);
            Console.WriteLine("Compare catalog with database = " + result.ToString());

            MetadataObject tablePart = catalog.MetadataObjects.Where(t => t.Name == "ТабличнаяЧасть3").FirstOrDefault();
            if (tablePart == null)
            {
                Console.WriteLine("Table part is not found!");
                return;
            }
            result = metadata.CompareWithDatabase(tablePart, out delete, out insert);
            Console.WriteLine("Compare table part with database = " + result.ToString());
        }
    }
}