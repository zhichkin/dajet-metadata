using DaJet.Metadata.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;

namespace DaJet.Metadata.NewParser
{
    [TestClass] public sealed class NewParser
    {
        [TestMethod] public void TestNewParser()
        {
            MDObject mdObject;
            string filePath = @"C:\temp\Справочник_original.txt";
            using (StreamReader stream = new StreamReader(filePath, Encoding.UTF8))
            {
                mdObject = MDObjectParser.Parse(stream);
            }
            using (StreamWriter stream = new StreamWriter(@"C:\temp\Справочник_original_parsed.txt", false, Encoding.UTF8))
            {
                ShowMDObject(stream, mdObject, 0, string.Empty);
            }
            filePath = @"C:\temp\Справочник_changed.txt";
            using (StreamReader stream = new StreamReader(filePath, Encoding.UTF8))
            {
                mdObject = MDObjectParser.Parse(stream);
            }
            using (StreamWriter stream = new StreamWriter(@"C:\temp\Справочник_changed_parsed.txt", false, Encoding.UTF8))
            {
                ShowMDObject(stream, mdObject, 0, string.Empty);
            }
        }
        private void ShowMDObject(StreamWriter stream, MDObject mdObject, int level, string path)
        {
            string indent = level == 0 ? string.Empty : "-".PadLeft(level * 4, '-');
            for (int i = 0; i < mdObject.Values.Count; i++)
            {
                object value = mdObject.Values[i];

                string thisPath = path + (string.IsNullOrEmpty(path) ? string.Empty : ".") + i.ToString();

                if (value is MDObject child)
                {
                    stream.WriteLine(indent + "[" + level.ToString() + "] (" + thisPath + ") " + value.ToString());
                    ShowMDObject(stream, child, level + 1, thisPath);
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
    }
}