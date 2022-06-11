using DaJet.CodeGenerator.SqlServer;
using DaJet.Metadata;
using DaJet.Metadata.Model;

namespace test_code_generator
{
    [TestClass] public class UnitTests
    {
        private const string MS_CONNECTION_STRING = "Data Source=ZHICHKIN;Initial Catalog=dajet-metadata-ms;Integrated Security=True;Encrypt=False;";

        [TestMethod] public void GenerateCatalogView()
        {
            SqlGenerator generator = new SqlGenerator();
            generator.Configure(MS_CONNECTION_STRING);

            IMetadataCache metadata = new MetadataCache();

            metadata.Add("dajet-metadata-ms", new MetadataCacheOptions()
            {
                DatabaseProvider = DatabaseProvider.SQLServer,
                ConnectionString = MS_CONNECTION_STRING
            });

            InfoBase infoBase = metadata.TryGet("dajet-metadata-ms", out string error);

            ApplicationObject metaObject = infoBase.GetApplicationObjectByName("Перечисление.Перечисление1");

            if (!(generator.TryCreateView(metaObject, out error)))
            {
                Console.WriteLine(error);
            }
            else
            {
                Console.WriteLine($"View [{metaObject.Name}] created successfully.");
            }

            //string script = generator.GenerateEnumViewScript(metaObject as Enumeration);

            //string script = generator.GenerateViewScript(catalog);
            //Console.WriteLine(script);

            //foreach (TablePart table in catalog.TableParts)
            //{
            //    string script = generator.GenerateViewScript(table);
            //    Console.WriteLine(script);
            //}
        }
    }
}