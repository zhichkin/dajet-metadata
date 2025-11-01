using System.Text;

namespace DaJet
{
    internal abstract class MetadataLoader
    {
        internal static MetadataLoader Create(DataSourceType dataSource, in string connectionString)
        {
            if (dataSource == DataSourceType.SqlServer)
            {
                return new MsMetadataLoader(in connectionString);
            }
            else if (dataSource == DataSourceType.PostgreSql)
            {
                return new PgMetadataLoader(in connectionString);
            }

            throw new InvalidOperationException($"Unsupported data source [{dataSource}]");
        }
        private static readonly Dictionary<Guid, ConfigFileParser> _parsers = new(14)
        {
            [MetadataType.SharedProperty] = new SharedPropertyParser(),
            [MetadataType.DefinedType] = new DefinedTypeParser(),
            [MetadataType.Catalog] = new CatalogParser(),
            [MetadataType.Constant] = new ConstantParser(),
            [MetadataType.Document] = new DocumentParser(),
            [MetadataType.Enumeration] = new EnumerationParser(),
            [MetadataType.Publication] = new PublicationParser(),
            [MetadataType.Characteristic] = new CharacteristicParser(),
            [MetadataType.InformationRegister] = new InformationRegisterParser(),
            [MetadataType.AccumulationRegister] = new AccumulationRegisterParser(),
            [MetadataType.Account] = new AccountParser(),
            [MetadataType.AccountingRegister] = new AccountingRegisterParser(),
            [MetadataType.BusinessTask] = new BusinessTaskParser(),
            [MetadataType.BusinessProcess] = new BusinessProcessParser()
        };
        private static ConfigFileParser GetConfigFileParser(Guid type)
        {
            if (!_parsers.TryGetValue(type, out ConfigFileParser parser))
            {
                return null;
            }

            return parser;
        }

        internal abstract int GetYearOffset();
        internal abstract ConfigFileBuffer Load(in string fileName);
        internal abstract ConfigFileBuffer Load(in string tableName, in string fileName);
        internal abstract IEnumerable<ConfigFileBuffer> Stream(Guid[] files);

        internal void Dump(in string tableName, in string fileName, in string outputPath)
        {
            using (StreamWriter writer = new(outputPath, false, Encoding.UTF8))
            {
                using (ConfigFileBuffer file = Load(in tableName, in fileName))
                {
                    ConfigFileReader reader = new(file.AsReadOnlySpan());

                    reader.Dump(in writer);
                }
            }
        }

        internal ConfigFileBuffer Load(Guid fileUuid)
        {
            return Load(fileUuid.ToString().ToLowerInvariant());
        }
        internal Guid GetRoot()
        {
            Guid root = Guid.Empty;

            using (ConfigFileBuffer file = Load("root"))
            {
                ConfigFileReader reader = new(file.AsReadOnlySpan());

                if (reader[2].Seek()) { root = reader.ValueAsUuid; }
            }

            return root;
        }
        internal InfoBase GetInfoBase()
        {
            Guid root = GetRoot();

            InfoBase infoBase;

            using (ConfigFileBuffer file = Load(root))
            {
                infoBase = InfoBase.Parse(file.AsReadOnlySpan());
            }

            return infoBase;
        }
        internal Dictionary<Guid, Guid[]> GetMetadataItems()
        {
            Guid root = GetRoot();

            Dictionary<Guid, Guid[]> metadata;

            using (ConfigFileBuffer file = Load(root))
            {
                metadata = InfoBase.ParseRegistry(file.AsReadOnlySpan());
            }

            return metadata;
        }
        internal MetadataRegistry GetMetadataRegistry(bool UseExtensions = false)
        {
            Dictionary<Guid, Guid[]> metadata = GetMetadataItems();

            MetadataRegistry registry = new();

            Guid[] items;

            // Подготавливаем реестр метаданных для многопоточной обработки
            if (metadata.TryGetValue(MetadataType.SharedProperty, out items))
            {
                foreach (Guid uuid in items)
                {
                    registry.AddEntry(uuid, new SharedProperty(uuid));
                }
            }

            // Подготавливаем реестр метаданных для многопоточной обработки
            if (metadata.TryGetValue(MetadataType.DefinedType, out items))
            {
                foreach (Guid uuid in items)
                {
                    registry.AddEntry(uuid, new DefinedType(uuid));
                }
            }

            //TODO: calculate capacity + registry.EnsureCapacity(metadata + dbnames)

            InitializeDBNames(in registry, UseExtensions);

            InitializeMetadataRegistry(in metadata, in registry);

            return registry;
        }
        private sealed class ConfigFileBatchWork
        {
            internal Guid Type;
            internal Guid[] Entries;
            internal MetadataRegistry Registry;
        }
        private void InitializeDBNames(in MetadataRegistry registry, bool UseExtensions = false)
        {
            using (ConfigFileBuffer file = Load(ConfigTables.Params, "DBNames"))
            {
                DBNames.Parse(file.AsReadOnlySpan(), in registry);
            }

            if (UseExtensions)
            {
                using (ConfigFileBuffer file = Load(ConfigTables.Params, "DBNames-Ext-1"))
                {
                    DBNames.Parse(file.AsReadOnlySpan(), in registry);
                }

                //using (ConfigFileBuffer file = Load(ConfigTables.Params, "DBNames-Ext-%"))
                //{
                //    DbNamesParser.Parse(file.AsReadOnlySpan(), in registry);
                //}
            }
        }
        private void InitializeMetadataRegistry(in Dictionary<Guid, Guid[]> metadata, in MetadataRegistry registry)
        {
            Task[] tasks = new Task[metadata.Keys.Count];

            int index = 0;

            foreach (var item in metadata)
            {
                Task task = Task.Factory.StartNew(
                    InitializeMetadataRegistryEntries,
                    new ConfigFileBatchWork()
                    {
                        Type = item.Key,
                        Entries = item.Value,
                        Registry = registry
                    },
                    CancellationToken.None,
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);

                tasks[index] = task;

                index++;
            }

            try
            {
                Task.WaitAll(tasks);
            }
            catch (Exception exception)
            {
                if (exception is AggregateException errors)
                {
                    foreach (Exception error in errors.InnerExceptions)
                    {
                        //TODO: log and report errors
                    }
                }
            }
        }
        private void InitializeMetadataRegistryEntries(object parameters)
        {
            if (parameters is not ConfigFileBatchWork work)
            {
                return;
            }

            MetadataRegistry registry = work.Registry;

            ConfigFileParser parser = GetConfigFileParser(work.Type);

            try
            {
                foreach (ConfigFileBuffer file in Stream(work.Entries))
                {
                    ConfigFileReader reader = new(file.AsReadOnlySpan());

                    if (parser is not null)
                    {
                        Guid uuid = new(file.FileName);

                        parser.Parse(uuid, file.AsReadOnlySpan(), in registry);
                    }
                }
            }
            catch (Exception error)
            {
                throw;
            }
        }
    }
}