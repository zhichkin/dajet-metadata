using DaJet.CodeGenerator;
using DaJet.CodeGenerator.SqlServer;
using DaJet.Metadata;
using DaJet.Metadata.Model;
using System.Diagnostics;

namespace test_code_generator
{
    [TestClass] public class UnitTests
    {
        private readonly ISqlGenerator _generator;
        private readonly SqlGeneratorOptions _options;
        private readonly IMetadataCache _metadata = new MetadataCache();

        // trade_11_2_3_159_demo | accounting_3_0_72_72_demo | dajet-metadata-ms;
        private const string INFO_BASE_NAME = "dajet-metadata-ms";
        private const string MS_CONNECTION_STRING =
            "Data Source=ZHICHKIN;Initial Catalog=dajet-metadata-ms;Integrated Security=True;Encrypt=False;";

        public UnitTests()
        {
            _metadata.Add(INFO_BASE_NAME, new MetadataCacheOptions()
            {
                ConnectionString = MS_CONNECTION_STRING,
                DatabaseProvider = DatabaseProvider.SQLServer
            });

            _options = new SqlGeneratorOptions()
            {
                DatabaseProvider = "SqlServer",
                ConnectionString = MS_CONNECTION_STRING
            };

            _generator = new SqlGenerator(_options);
        }
        [TestMethod] public void GenerateViews()
        {
            InfoBase infoBase = _metadata.TryGet(INFO_BASE_NAME, out _);

            Stopwatch watch = new();
            
            watch.Start();

            if (!_generator.TryCreateViews(in infoBase, out int result, out List<string> errors))
            {
                foreach (string error in errors)
                {
                    Console.WriteLine(error);
                }
            }

            watch.Stop();

            Console.WriteLine($"Created {result} views in {watch.ElapsedMilliseconds} ms");
        }
        [TestMethod] public void DropViews()
        {
            Stopwatch watch = new();

            watch.Start();

            int result = _generator.DropViews();

            watch.Stop();

            Console.WriteLine($"Dropped {result} views in {watch.ElapsedMilliseconds} ms");
        }
        [TestMethod] public void DropCatalogView()
        {
            InfoBase infoBase = _metadata.TryGet(INFO_BASE_NAME, out _);

            ApplicationObject metaObject = infoBase.GetApplicationObjectByName("Справочник.Валюты");

            _generator.DropView(in metaObject);
        }
    }
}