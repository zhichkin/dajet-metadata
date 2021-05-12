using DaJet.Metadata.Model;
using DaJet.Metadata.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;

namespace DaJet.Metadata.ConfigDiff
{
    [TestClass] public sealed class ConfigDiff
    {
        [TestMethod] public void TestDiff()
        {
            IMetadataService metadata = new MetadataService();
            IMetaDiffService metadiff = new MetaDiffService();

            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=cerberus;Integrated Security=True");
            InfoBase ibOld = metadata.LoadInfoBase();

            metadata
                .UseDatabaseProvider(DatabaseProvider.SQLServer)
                .UseConnectionString("Data Source=ZHICHKIN;Initial Catalog=cerberus_new;Integrated Security=True");
            InfoBase ibNew = metadata.LoadInfoBase();

            MetaDiff diff = metadiff.Compare(ibOld, ibNew);

            WriteDiffToFile(diff);
        }

        private void WriteDiffToFile(MetaDiff diff)
        {
            using (StreamWriter stream = new StreamWriter(@"C:\temp\diff.txt", false, Encoding.UTF8))
            {
                WriteToFile(stream, diff, 0, string.Empty);
            }
        }
        private void WriteToFile(StreamWriter stream, MetaDiff diff, int level, string path)
        {
            string indent = level == 0 ? string.Empty : "-".PadLeft(level * 4, '-');
            //string thisPath = path + (string.IsNullOrEmpty(path) ? string.Empty : ".") + i.ToString();
            stream.WriteLine(indent + "[" + level.ToString() + "] (" + diff.Difference.ToString() + ") "
                + diff.Target.ToString()
                + (diff.Target is MetadataProperty property ? " (" + property.PropertyType.ToString() + ")" : string.Empty));

            foreach (var entry in diff.NewValues)
            {
                indent = "-".PadLeft((level + 1) * 4, '-');
                stream.WriteLine(indent + "[*] " + entry.Key + " = " + entry.Value.ToString());
            }

            for (int i = 0; i < diff.Children.Count; i++)
            {
                //thisPath = path + (string.IsNullOrEmpty(path) ? string.Empty : ".") + i.ToString();
                WriteToFile(stream, diff.Children[i], level + 1, path);
            }
        }
    }
}