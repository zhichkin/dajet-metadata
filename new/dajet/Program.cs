using System.Diagnostics;

namespace DaJet
{
    public static class Program
    {
        private static readonly string MS_CONNECTION = "Data Source=ZHICHKIN;Initial Catalog=erp_uh;Integrated Security=True;Encrypt=False;";
        private static readonly string PG_CONNECTION = "Host=localhost;Port=5432;Database=erp_uh;Username=postgres;Password=postgres;";

        public static void Main(string[] args)
        {
            GetMetadataObject(); return;

            //DumpFile("d11b89e1-90a2-47e7-b43f-7f231ec64b2f"); return;

            Console.WriteLine("SQL Server");
            OneDbMetadataProvider provider = new(DataSourceType.SqlServer, in MS_CONNECTION);

            //Console.WriteLine("PostgreSQL");
            //OneDbMetadataProvider provider = new(DataSourceType.PostgreSql, in PG_CONNECTION);

            Stopwatch watch = new();
            watch.Start();

            int yearOffset = provider.GetYearOffset();
            InfoBase infoBase = provider.GetInfoBase();

            provider.Initialize();

            watch.Stop();

            Console.WriteLine($"Finished in {watch.ElapsedMilliseconds} ms");

            //provider.Dump("Params", "DBNames", "C:\\temp\\1c-dump\\DBNames.txt");
        }

        private static void DumpFile(string fileName)
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
            provider.Dump(ConfigTables.Params, "DBNames", $"C:\\temp\\1c-dump\\DBNames.txt");
        }

        private static void GetMetadataObject()
        {
            OneDbMetadataProvider provider = new(DataSourceType.SqlServer, in MS_CONNECTION);

            provider.Initialize();

            Catalog metadata = provider.GetMetadataObject<Catalog>("Справочник.Номенклатура");

            Console.WriteLine($"[{metadata.Uuid}] {metadata.Name} {{{metadata.GetType()}}}");
        }
    }
}