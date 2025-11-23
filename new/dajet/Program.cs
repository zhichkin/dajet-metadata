using System.Diagnostics;
using System.Reflection.Emit;

namespace DaJet
{
    public static class Program
    {
        private static readonly string MS_METADATA = "Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True;Encrypt=False;";
        private static readonly string MS_CONNECTION = "Data Source=ZHICHKIN;Initial Catalog=erp_uh;Integrated Security=True;Encrypt=False;";
        private static readonly string PG_CONNECTION = "Host=localhost;Port=5432;Database=unf;Username=postgres;Password=postgres;";

		private static int GetTypeSize(Type type)
		{
			//Type type = typeof(ConfigFileReader);
			var dm = new DynamicMethod("$", typeof(int), Type.EmptyTypes);
			ILGenerator il = dm.GetILGenerator();
			il.Emit(OpCodes.Sizeof, type);
			il.Emit(OpCodes.Ret);
			int size = (int)dm.Invoke(null, null);
			return size;
		}
		public static void Main(string[] args)
        {
            //Console.WriteLine(GetTypeSize(typeof(DataType))); return;

            //GetMetadataObject(); return;

            //IterateMetadataObjects(MetadataName.Catalog); return;

            //DumpFile(); return;
            //DumpRawFile(); return;

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

        private static void DumpFile()
        {
            string fileName = "2b870ec1-271d-450b-abe0-ae78dd11fa23";
            OneDbMetadataProvider provider = new(DataSourceType.SqlServer, in MS_METADATA);
            provider.Dump(ConfigTables.Config, fileName, $"C:\\temp\\1c-dump\\{fileName}.txt");
            return;

            //MetadataRegistry registry = new();
            //MsMetadataLoader loader = new(in MS_CONNECTION);
            //using (ConfigFileBuffer file = loader.Load("d11b89e1-90a2-47e7-b43f-7f231ec64b2f"))
            //{
            //    Guid uuid = new(file.FileName);
            //    //registry.AddEntry(uuid, null);
            //    CatalogParser parser = new();
            //    parser.Parse(uuid, file.AsReadOnlySpan(), in registry);
            //}

            //OneDbMetadataProvider provider = new(DataSourceType.SqlServer, in MS_CONNECTION);
            //InfoBase infoBase = provider.GetInfoBase();
            //string fileName = infoBase.Uuid.ToString().ToLowerInvariant();
            //provider.Dump(ConfigTables.Config, fileName, $"C:\\temp\\1c-dump\\config_unf.txt");

            //OneDbMetadataProvider provider = new(DataSourceType.SqlServer, in MS_CONNECTION);
            //provider.Dump(ConfigTables.Params, "DBNames", $"C:\\temp\\1c-dump\\DBNames.txt");
        }

        private static void DumpRawFile()
        {
            string fileName = "2b870ec1-271d-450b-abe0-ae78dd11fa23";
            OneDbMetadataProvider provider = new(DataSourceType.SqlServer, in MS_METADATA);
            provider.DumpRaw(ConfigTables.Config, fileName, $"C:\\temp\\1c-dump\\{fileName}_raw.txt");
            return;
        }

        private static void GetMetadataObject()
        {
            long start = Stopwatch.GetTimestamp();

            OneDbMetadataProvider provider = new(DataSourceType.PostgreSql, in PG_CONNECTION);

            provider.Initialize();

            EntityDefinition metadata = provider.GetMetadataObjectWithRelations("Справочник.Номенклатура");

            Console.WriteLine($"Name: {metadata.Name}");
            Console.WriteLine($"DbName: {metadata.DbName}");

            foreach (PropertyDefinition property in metadata.Properties)
            {
                Console.WriteLine("**************************");
                Console.WriteLine($"Name: {property.Name}");
                Console.WriteLine($"Type: {property.Type}");

                if (property.Type.IsEntity)
                {
                    List<string> types = provider.ResolveReferences(property.References);
                    Console.WriteLine($"Relations: {string.Join(',', types)}");
                }

                foreach (ColumnDefinition column in property.Columns)
                {
                    Console.WriteLine($"    - Column: {column.Name} : {column.Type}");
                }
            }

            foreach (EntityDefinition table in metadata.Entities)
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

                    if (property.Type.IsEntity)
                    {
                        List<string> types = provider.ResolveReferences(property.References);
                        Console.WriteLine($"Relations: {string.Join(',', types)}");
                    }

                    foreach (ColumnDefinition column in property.Columns)
                    {
                        Console.WriteLine($"    - Column: {column.Name} : {column.Type}");
                    }
                }
            }

            long end = Stopwatch.GetTimestamp();

            TimeSpan elapsed = Stopwatch.GetElapsedTime(start, end);

            Console.WriteLine();
            Console.WriteLine($"[{metadata.DbName}] {metadata.Name} loaded in {elapsed.TotalMilliseconds} ms");
        }

        private static void IterateMetadataObjects(string type)
        {
            OneDbMetadataProvider provider = new(DataSourceType.SqlServer, in MS_CONNECTION);

            provider.Initialize();

            int count = 0;

            long start = Stopwatch.GetTimestamp();

            foreach (EntityDefinition entity in provider.GetMetadataObjects(type))
            {
                count++;

                Console.WriteLine(string.Format("{0} [{1}]", entity.Name, entity.DbName));
            }

            long end = Stopwatch.GetTimestamp();

            TimeSpan elapsed = Stopwatch.GetElapsedTime(start, end);

            Console.WriteLine($"Loaded {count} entities in {elapsed.TotalMilliseconds} ms");
        }
    }
}