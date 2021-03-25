using DaJet.Metadata.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;

namespace DaJet.Metadata.Tests
{
    [TestClass] public sealed class NewParser
    {
        [TestMethod] public void TestNewParser()
        {
            MDObject mdObject;
            string filePath = @"C:\temp\Контрагенты.txt"; //@"C:\temp\my_exchange._Enum89.txt";
            using (StreamReader stream = new StreamReader(filePath, Encoding.UTF8))
            {
                mdObject = MDObjectParser.Parse(stream);
            }
            using (StreamWriter stream = new StreamWriter(@"C:\temp\output.txt", false, Encoding.UTF8))
            {
                ShowMDObject(stream, mdObject, 0);
            }
        }
        private void ShowMDObject(StreamWriter stream, MDObject mdObject, int level)
        {
            string indent = level == 0 ? string.Empty : "-".PadLeft(level * 4, '-');
            foreach (object value in mdObject.Values)
            {
                if (value is MDObject child)
                {
                    stream.WriteLine(indent + "[" + level.ToString() + "]" + " " + value.ToString());
                    ShowMDObject(stream, child, level + 1);
                }
                else if (value is string text)
                {
                    stream.WriteLine(indent + "[" + level.ToString() + "]" + " \"" + text.ToString() + "\"");
                }
                else
                {
                    stream.WriteLine(indent + "[" + level.ToString() + "]" + " " + value.ToString());
                }
            }
        }
    }
}