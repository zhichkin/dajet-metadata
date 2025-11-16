using System.Diagnostics;

namespace DaJet
{
    public static class Program
    {
        private static readonly string MS_METADATA = "Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True;Encrypt=False;";
        private static readonly string MS_CONNECTION = "Data Source=ZHICHKIN;Initial Catalog=unf;Integrated Security=True;Encrypt=False;";
        private static readonly string PG_CONNECTION = "Host=localhost;Port=5432;Database=unf;Username=postgres;Password=postgres;";

        public static void Main(string[] args)
        {
            TestDataType(); return;

            //GetMetadataObject(); return;

            //DumpFile(); return;

            //Console.WriteLine("SQL Server");
            //OneDbMetadataProvider provider = new(DataSourceType.SqlServer, in MS_CONNECTION);

            Console.WriteLine("PostgreSQL");
            OneDbMetadataProvider provider = new(DataSourceType.PostgreSql, in PG_CONNECTION);

            Stopwatch watch = new();
            watch.Start();

            int yearOffset = provider.GetYearOffset();
            InfoBase infoBase = provider.GetInfoBase();

            provider.Initialize();

            watch.Stop();

            Console.WriteLine($"Finished in {watch.ElapsedMilliseconds} ms");

            //provider.Dump("Params", "DBNames", "C:\\temp\\1c-dump\\DBNames.txt");
        }

        private static void DumpFile()
        {
            // d11b89e1-90a2-47e7-b43f-7f231ec64b2f
            //OneDbMetadataProvider provider = new(DataSourceType.SqlServer, in MS_CONNECTION);
            //provider.Dump(ConfigTables.Config, fileName, $"C:\\temp\\1c-dump\\{fileName}.txt");

            //MetadataRegistry registry = new();
            //MsMetadataLoader loader = new(in MS_CONNECTION);
            //using (ConfigFileBuffer file = loader.Load("d11b89e1-90a2-47e7-b43f-7f231ec64b2f"))
            //{
            //    Guid uuid = new(file.FileName);
            //    //registry.AddEntry(uuid, null);
            //    CatalogParser parser = new();
            //    parser.Parse(uuid, file.AsReadOnlySpan(), in registry);
            //}

            OneDbMetadataProvider provider = new(DataSourceType.SqlServer, in MS_CONNECTION);

            InfoBase infoBase = provider.GetInfoBase();

            string fileName = infoBase.Uuid.ToString().ToLowerInvariant();

            provider.Dump(ConfigTables.Config, fileName, $"C:\\temp\\1c-dump\\config_unf.txt");

            //OneDbMetadataProvider provider = new(DataSourceType.SqlServer, in MS_CONNECTION);
            //provider.Dump(ConfigTables.Params, "DBNames", $"C:\\temp\\1c-dump\\DBNames.txt");
        }

        private static void TestDataType()
        {
            OneDbMetadataProvider provider = new(DataSourceType.SqlServer, in MS_METADATA);

            provider.Initialize();

            TableDefinition metadata = provider.GetMetadataObject("Справочник.Тестовый");

            Console.WriteLine($"Name: {metadata.Name}");
            Console.WriteLine($"DbName: {metadata.DbName}");
            
            foreach (PropertyDefinition property in metadata.Properties)
            {
                Console.WriteLine("**************************");
                Console.WriteLine($"Name: {property.Name}");
                Console.WriteLine($"Type: {property.Type}");
            }

            foreach (TableDefinition table in metadata.Tables)
            {
                Console.WriteLine();
                Console.WriteLine("-------------------------");
                Console.WriteLine($"Name: {table.Name}");
                Console.WriteLine($"DbName: {table.DbName}");

                foreach (PropertyDefinition property in table.Properties)
                {
                    Console.WriteLine("**************************");
                    Console.WriteLine($"Name: {property.Name}");
                    Console.WriteLine($"Type: {property.Type}");
                }
            }
        }

        private static void GetMetadataObject()
        {
            //OneDbMetadataProvider provider = new(DataSourceType.SqlServer, in MS_CONNECTION);
            OneDbMetadataProvider provider = new(DataSourceType.PostgreSql, in PG_CONNECTION);

            provider.Initialize();

            long start = Stopwatch.GetTimestamp();

            TableDefinition metadata = provider.GetMetadataObject("Справочник.Номенклатура");

            long end = Stopwatch.GetTimestamp();

            TimeSpan elapsed = Stopwatch.GetElapsedTime(start, end);

            Console.WriteLine($"[{metadata.DbName}] {metadata.Name} loaded in {elapsed.TotalMilliseconds} ms");
        }
    }
}