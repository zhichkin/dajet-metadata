using DaJet.Data;
using DaJet.Json;
using DaJet.Metadata;
using DaJet.TypeSystem;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace DaJet
{
    public static class Program
    {
        private static readonly string MS_METADATA = "Data Source=ZHICHKIN;Initial Catalog=dajet-metadata;Integrated Security=True;Encrypt=False;";
        private static readonly string PG_METADATA = "Host=localhost;Port=5432;Database=dajet-metadata;Username=postgres;Password=postgres;";
        private static readonly string MS_UNF = "Data Source=ZHICHKIN;Initial Catalog=unf;Integrated Security=True;Encrypt=False;";
        private static readonly string PG_UNF = "Host=localhost;Port=5432;Database=unf;Username=postgres;Password=postgres;";
        private static readonly string MS_ERP = "Data Source=ZHICHKIN;Initial Catalog=erp_uh;Integrated Security=True;Encrypt=False;";
        private static readonly string PG_ERP = "Host=localhost;Port=5432;Database=erp_uh;Username=postgres;Password=postgres;";

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

            //TestDataTypeJsonConverter(); return;

            //TestResetFromAnotherThread(); return;

            //TestMetadataCache(); return;

            //GetMetadataObject("Справочник.Номенклатура"); return;

            //IterateMetadataObjects(MetadataNames.Catalog); return;

            //GetMetadataNames(); return;

            //GetEnumerationNames(); return;
            //GetEnumerationValues("Перечисление.ЭлементыСтруктурыОтчета"); return;

            //ShowChangeTrackingTable();

            //DumpFile(); return;
            //DumpRawFile(); return;

            CompareMetadataToDatabase();

            //GetEnumerationNames();
            //GetEnumerationValues("Перечисление.ВидыОбъектовМаркетплейсов");
            //GetEnumerationSingleValue("Перечисление.ВидыОбъектовМаркетплейсов.КодАктивации");

            //ShowExtensions();

            //Console.WriteLine("SQL Server");
            //OneDbMetadataProvider provider = new(DataSourceType.SqlServer, in MS_CONNECTION);

            //Console.WriteLine("PostgreSQL");
            //OneDbMetadataProvider provider = new(DataSourceType.PostgreSql, in PG_CONNECTION);

            //Stopwatch watch = new();
            //watch.Start();

            //int yearOffset = provider.GetYearOffset();
            //Configuration infoBase = provider.GetConfiguration();

            //provider.Initialize();

            //watch.Stop();

            //Console.WriteLine($"Finished in {watch.ElapsedMilliseconds} ms");

            //provider.Dump("Params", "DBNames", "C:\\temp\\1c-dump\\DBNames.txt");
        }

        private static void DumpFile()
        {
            string fileName = "fee259e3-7102-4591-abe1-95ae49703a4b";
            MetadataProvider provider = MetadataProvider.Create(DataSourceType.SqlServer, in MS_METADATA);
            provider.Dump("Config", fileName, $"C:\\temp\\1c-dump\\{fileName}.txt");

            //string fileName = "DBSchema";
            //MetadataProvider provider = new(DataSourceType.SqlServer, in MS_METADATA);
            //provider.Dump("DBSchema", string.Empty, $"C:\\temp\\1c-dump\\{fileName}.txt");

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
            //Configuration infoBase = provider.GetConfiguration();
            //string fileName = infoBase.Uuid.ToString().ToLowerInvariant();
            //provider.Dump(ConfigTables.Config, fileName, $"C:\\temp\\1c-dump\\config_unf.txt");

            //OneDbMetadataProvider provider = new(DataSourceType.SqlServer, in MS_CONNECTION);
            //provider.Dump(ConfigTables.Params, "DBNames", $"C:\\temp\\1c-dump\\DBNames.txt");
        }

        private static void DumpRawFile()
        {
            string fileName = "2b870ec1-271d-450b-abe0-ae78dd11fa23";
            MetadataProvider provider = MetadataProvider.Create(DataSourceType.SqlServer, in MS_METADATA);
            provider.DumpRaw("Config", fileName, $"C:\\temp\\1c-dump\\{fileName}_raw.txt");
            return;
        }

        private static void ShowEhtityDefinition(in EntityDefinition metadata, in MetadataProvider provider)
        {
            Console.WriteLine($"Name: {metadata.Name}");
            Console.WriteLine($"DbName: {metadata.DbName}");

            foreach (PropertyDefinition property in metadata.Properties)
            {
                Console.WriteLine("**************************");
                Console.WriteLine($"Name: {property.Name} [{property.Purpose}]");
                Console.WriteLine($"Type: {property.Type}");

                if (property.Type.IsEntity)
                {
                    List<string> types = provider.ResolveReferences(property.References);
                    Console.WriteLine($"References: {string.Join(',', types)}");
                }

                foreach (ColumnDefinition column in property.Columns)
                {
                    Console.WriteLine($"    - Column: {column.Name} : {column.Type} [{column.Purpose}]");
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
                    Console.WriteLine($"Name: {property.Name} [{property.Purpose}]");
                    Console.WriteLine($"Type: {property.Type}");

                    if (property.Type.IsEntity)
                    {
                        List<string> types = provider.ResolveReferences(property.References);
                        Console.WriteLine($"References: {string.Join(',', types)}");
                    }

                    foreach (ColumnDefinition column in property.Columns)
                    {
                        Console.WriteLine($"    - Column: {column.Name} : {column.Type} [{column.Purpose}]");
                    }
                }
            }
        }
        private static void GetMetadataObject(in string metadataFullName)
        {
            long start = Stopwatch.GetTimestamp();

            MetadataProvider provider = MetadataProvider.Create(DataSourceType.SqlServer, in MS_ERP);
            //MetadataProvider provider = new(DataSourceType.PostgreSql, in PG_ERP);

            //EntityDefinition metadata = provider.GetMetadataObject(63);
            EntityDefinition metadata = provider.GetMetadataObject(in metadataFullName);

            ShowEhtityDefinition(in metadata, in provider);

            long end = Stopwatch.GetTimestamp();

            TimeSpan elapsed = Stopwatch.GetElapsedTime(start, end);

            Console.WriteLine();
            Console.WriteLine($"[{metadata.DbName}] {metadata.Name} loaded in {elapsed.TotalMilliseconds} ms");
        }

        private static void IterateMetadataObjects(string type)
        {
            MetadataProvider provider = MetadataProvider.Create(DataSourceType.SqlServer, in MS_ERP);

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

        private static void CompareMetadataToDatabase()
        {
            long start = Stopwatch.GetTimestamp();

            MetadataProvider provider = MetadataProvider.Create(DataSourceType.SqlServer, in MS_METADATA);
            //MetadataProvider provider = MetadataProvider.Create(DataSourceType.PostgreSql, in PG_METADATA);
            //MetadataProvider provider = MetadataProvider.Create(DataSourceType.SqlServer, in MS_UNF);
            //MetadataProvider provider = MetadataProvider.Create(DataSourceType.PostgreSql, in PG_UNF);
            //MetadataProvider provider = MetadataProvider.Create(DataSourceType.SqlServer, in MS_ERP);
            //MetadataProvider provider = MetadataProvider.Create(DataSourceType.PostgreSql, in PG_ERP);

            List<string> metadataNames = new()
            {
                MetadataNames.Constant,
                MetadataNames.Publication,
                MetadataNames.Catalog,
                MetadataNames.Document,
                MetadataNames.Characteristic,
                MetadataNames.InformationRegister,
                MetadataNames.AccumulationRegister,
                MetadataNames.BusinessTask,
                MetadataNames.BusinessProcess,
                MetadataNames.Account,
                MetadataNames.AccountingRegister
            };

            string report = provider.CompareMetadataToDatabase(metadataNames);

            Console.WriteLine(report);

            long end = Stopwatch.GetTimestamp();

            TimeSpan elapsed = Stopwatch.GetElapsedTime(start, end);

            Console.WriteLine();
            Console.WriteLine($"Done in {elapsed.TotalMilliseconds} ms");
        }

        private static void GetMetadataNames()
        {
            MetadataProvider provider = MetadataProvider.Create(DataSourceType.SqlServer, in MS_METADATA);

            List<string> names = provider.GetMetadataNames(MetadataNames.Catalog);
            //List<string> names = provider.GetMetadataNames("Расширение1", MetadataNames.Catalog);

            foreach (string name in names)
            {
                Console.WriteLine(name);
            }
        }
        private static void GetEnumerationNames()
        {
            MetadataProvider provider = MetadataProvider.Create(DataSourceType.SqlServer, in MS_ERP);

            List<string> names = provider.GetMetadataNames(null, MetadataNames.Enumeration);

            foreach (string name in names)
            {
                Console.WriteLine(name);
            }
        }
        private static void GetEnumerationValues(in string fullName)
        {
            MetadataProvider provider = MetadataProvider.Create(DataSourceType.SqlServer, in MS_ERP);

            Dictionary<string, Guid> values = provider.GetEnumerationValues(in fullName);

            foreach (var value in values)
            {
                Console.WriteLine(string.Format("{0} = {1}", value.Value, value.Key));
            }
        }
        private static void GetEnumerationSingleValue(in string fullName)
        {
            MetadataProvider provider = MetadataProvider.Create(DataSourceType.SqlServer, in MS_ERP);

            long start = Stopwatch.GetTimestamp();

            Guid value = provider.GetEnumerationValue(in fullName);

            long end = Stopwatch.GetTimestamp();

            TimeSpan elapsed = Stopwatch.GetElapsedTime(start, end);

            Console.WriteLine(value.ToString());
            Console.WriteLine();
            Console.WriteLine($"Executed in {elapsed.TotalMilliseconds} ms");
        }

        private static void ShowChangeTrackingTable()
        {
            MetadataProvider provider = MetadataProvider.Create(DataSourceType.SqlServer, in MS_METADATA);
            //MetadataProvider provider = MetadataProvider.Create(DataSourceType.PostgreSql, in PG_METADATA);

            // Справочник.Номенклатура.Изменения
            // Справочник.Заимствованный.Изменения
            // Справочник.Расш1_Справочник1.Изменения

            string metadataFullName = "Справочник.Заимствованный.Изменения";

            EntityDefinition metadata = provider.GetMetadataObject(in metadataFullName);

            ShowEhtityDefinition(in metadata, in provider);
        }

        private static void ShowExtensions()
        {
            //MetadataProvider provider = MetadataProvider.Create(DataSourceType.SqlServer, MS_METADATA);
            MetadataProvider provider = MetadataProvider.Create(DataSourceType.SqlServer, MS_METADATA);

            foreach (ExtensionInfo extension in provider.GetExtensions())
            {
                Console.WriteLine($"{extension.Name} [{extension.Identity}]");
                Console.WriteLine($"- Order: [{extension.Order}]");
                Console.WriteLine($"- Active: {extension.IsActive}");
                Console.WriteLine($"- Scope: {extension.Scope}");
                Console.WriteLine($"- Purpose: {extension.Purpose}");
                Console.WriteLine($"- Version: {extension.Version}");
                Console.WriteLine($"- Updated: {extension.Updated:dd-MM-yyyy HH:mm:ss}");
                Console.WriteLine($"- Root file: {extension.RootFile}");
                Console.WriteLine($"- File name: {extension.FileName}");
                Console.WriteLine("------------------------------");
            }
        }

        private static void TestMetadataCache()
        {
            string cacheKey = "MS_METADATA";

            MetadataProvider.Add(in cacheKey, DataSourceType.SqlServer, MS_METADATA);

            MetadataProvider.Reset(in cacheKey);

            MetadataProvider provider = MetadataProvider.GetOrCreate(in cacheKey, DataSourceType.SqlServer, MS_METADATA);

            //MetadataProvider provider = MetadataProvider.Get(in cacheKey);

            provider = MetadataProvider.Get(in cacheKey);

            Console.WriteLine(provider.ElapsedSinceLastUpdate);

            EntityDefinition entity = provider.GetMetadataObject("Справочник.Номенклатура");

            if (entity is null)
            {
                Console.WriteLine("Metadata object not found [Справочник.Номенклатура]");
            }
            else
            {
                Console.WriteLine(entity.ToString());
            }

            Console.WriteLine($"Elapsed before Reset {provider.ElapsedSinceLastUpdate}");

            MetadataProvider.Reset(in cacheKey);

            Console.WriteLine($"Elapsed after Reset {provider.ElapsedSinceLastUpdate}");

            Console.WriteLine($"Elapsed before sleep {provider.ElapsedSinceLastUpdate}");

            Thread.Sleep(TimeSpan.FromSeconds(5));

            Console.WriteLine($"Elapsed after sleep {provider.ElapsedSinceLastUpdate}");

            Console.WriteLine($"Elapsed before Reset {provider.ElapsedSinceLastUpdate}");

            MetadataProvider.Reset(in cacheKey);

            Console.WriteLine($"Elapsed after Reset {provider.ElapsedSinceLastUpdate}");

            bool success = false;

            while (!success)
            {
                try
                {
                    entity = provider.GetMetadataObject("Справочник.Номенклатура");

                    if (entity is null)
                    {
                        throw new Exception("Таблица не найдена");
                    }

                    success = true;
                }
                catch (Exception error)
                {
                    Console.WriteLine($"[ERROR] {error.Message}");

                    Task.Delay(TimeSpan.FromSeconds(3)).Wait();

                    MetadataProvider.Reset(in cacheKey);
                }
            }

            Console.WriteLine(entity.ToString());
        }
        private static void TestResetFromAnotherThread()
        {
            string cacheKey = "MS_METADATA";

            MetadataProvider.Add(in cacheKey, DataSourceType.SqlServer, MS_METADATA);

            Task[] tasks = new Task[2];

            tasks[0] = Task.Run(ThreadOne);
            tasks[1] = Task.Run(ThreadTwo);

            Task.WaitAll(tasks);

            MetadataProvider provider = MetadataProvider.Get(in cacheKey);

            Console.WriteLine($"[{Environment.CurrentManagedThreadId}] {provider.ElapsedSinceLastUpdate}");
        }
        private static void ThreadOne()
        {
            string cacheKey = "MS_METADATA";

            MetadataProvider provider = MetadataProvider.Get(in cacheKey);

            EntityDefinition entity;
            do
            {
                entity = provider.GetMetadataObject("Справочник.Номенклатура");

                if (entity is null)
                {
                    Console.WriteLine($"[{Environment.CurrentManagedThreadId}] Metadata object not found");

                    Console.WriteLine($"[{Environment.CurrentManagedThreadId}] {provider.ElapsedSinceLastUpdate}");

                    Task.Delay(TimeSpan.FromSeconds(3)).Wait();
                }
            }
            while (entity is null);

            Console.WriteLine($"[{Environment.CurrentManagedThreadId}] {entity}");
        }
        private static void ThreadTwo()
        {
            string cacheKey = "MS_METADATA";

            MetadataProvider provider = MetadataProvider.Get(in cacheKey);

            while (provider.ElapsedSinceLastUpdate < 10000L)
            {
                Task.Delay(TimeSpan.FromSeconds(1)).Wait();

                Console.WriteLine($"[{Environment.CurrentManagedThreadId}] {provider.ElapsedSinceLastUpdate}");
            }

            MetadataProvider.Reset(in cacheKey);

            Console.WriteLine($"[{Environment.CurrentManagedThreadId}] Reset");
        }

        private static void TestDataTypeJsonConverter()
        {
            JsonSerializerOptions JsonOptions = new()
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            };

            JsonOptions.Converters.Add(new DataTypeJsonConverter());

            DataType type = DataType.Entity();

            string json = JsonSerializer.Serialize(type, JsonOptions);

            json = "\"entity\""; //"\"union(boolean|datetime|string(20)|decimal(10,0)|entity(23))\"";

            DataType test = JsonSerializer.Deserialize<DataType>(json, JsonOptions);

            Console.WriteLine(type.ToString());
            Console.WriteLine(test.ToString());
        }
    }
}