using DaJet.CodeGenerator;
using DaJet.CodeGenerator.PostgreSql;
using DaJet.Metadata;
using DaJet.Metadata.Model;
using System.Diagnostics;

namespace test_code_generator
{
    [TestClass] public class Tests_PostgreSql
    {
        private readonly ISqlGenerator _generator;
        private readonly SqlGeneratorOptions _options;
        private readonly IMetadataCache _metadata = new MetadataCache();

        private const string SCHEMA_NAME = "dajet";
        private const string INFO_BASE_NAME = "dajet-metadata-pg";
        private const string OUTPUT_FILE_PATH = "C:\\temp\\pg-sql-views.sql";
        private const string PG_CONNECTION_STRING =
            "Host=127.0.0.1;Port=5432;Database=dajet-metadata-pg;Username=postgres;Password=postgres;";
        
        public Tests_PostgreSql()
        {
            _metadata.Add(INFO_BASE_NAME, new MetadataCacheOptions()
            {
                ConnectionString = PG_CONNECTION_STRING,
                DatabaseProvider = DatabaseProvider.PostgreSQL
            });

            _options = new SqlGeneratorOptions()
            {
                Schema = SCHEMA_NAME,
                OutputFile = OUTPUT_FILE_PATH,
                DatabaseProvider = "PostgreSql",
                ConnectionString = PG_CONNECTION_STRING
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
        [TestMethod] public void ScriptViews()
        {
            InfoBase infoBase = _metadata.TryGet(INFO_BASE_NAME, out _);

            Stopwatch watch = new();

            watch.Start();

            if (!_generator.TryScriptViews(in infoBase, out int result, out List<string> errors))
            {
                foreach (string error in errors)
                {
                    Console.WriteLine(error);
                }
            }

            watch.Stop();

            Console.WriteLine($"Scripted {result} views in {watch.ElapsedMilliseconds} ms");
        }
        [TestMethod] public void DropSchema()
        {
            _generator.DropSchema(SCHEMA_NAME);
        }
    }
}