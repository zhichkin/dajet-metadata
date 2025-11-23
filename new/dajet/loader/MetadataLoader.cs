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
        internal void DumpRaw(in string tableName, in string fileName, in string outputPath)
        {
            using (FileStream writer = new(outputPath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (ConfigFileBuffer file = Load(in tableName, in fileName))
                {
                    writer.Write(file.AsReadOnlySpan());
                }
            }
        }

        internal abstract int GetYearOffset();
        internal abstract ConfigFileBuffer Load(in string fileName);
        internal abstract ConfigFileBuffer Load(in string tableName, in string fileName);
        internal abstract IEnumerable<ConfigFileBuffer> Stream(Guid[] files);

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
                Guid uuid = new(file.FileName);

                infoBase = InfoBase.Parse(uuid, file.AsReadOnlySpan());
            }

            return infoBase;
        }
        internal ConfigFileBuffer Load(Guid fileUuid)
        {
            return Load(fileUuid.ToString().ToLowerInvariant());
        }
        internal EntityDefinition Load(in string type, Guid uuid, in MetadataRegistry registry)
        {
            if (!ConfigFileParser.TryGetParser(in type, out ConfigFileParser parser))
            {
                return null;
            }

            EntityDefinition definition;

            using (ConfigFileBuffer file = Load(uuid))
            {
                definition = parser.Load(uuid, file.AsReadOnlySpan(), in registry, false);
            }

            return definition;
        }
        internal EntityDefinition LoadWithRelations(in string type, Guid uuid, in MetadataRegistry registry)
        {
            if (!ConfigFileParser.TryGetParser(in type, out ConfigFileParser parser))
            {
                return null;
            }

            EntityDefinition definition;

            using (ConfigFileBuffer file = Load(uuid))
            {
                definition = parser.Load(uuid, file.AsReadOnlySpan(), in registry, true);
            }

            return definition;
        }
        internal MetadataRegistry GetMetadataRegistry()
        {
            Guid root = GetRoot();

            int version;

            Dictionary<Guid, Guid[]> metadata;

            using (ConfigFileBuffer file = Load(root))
            {
                version = InfoBase.Parse(file.AsReadOnlySpan(), out metadata);
            }

            MetadataRegistry registry = new()
            {
                CompatibilityVersion = version
            };

            //TODO: registry.EnsureCapacity(in metadata); // add DBNames into count

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

            // Инициализация объектов реестра метаданных,
            // загрузка кодов ссылочных типов и имён СУБД

            InitializeDBNames(in registry);

            // Инициализация объектов реестра метаданных зависит
            // от предварительной загрузки кодов ссылочных типов

            InitializeMetadataRegistry(in metadata, in registry);

            return registry;
        }
        private sealed class ConfigFileBatchWork
        {
            internal Guid Type;
            internal Guid[] Entries;
            internal MetadataRegistry Registry;
        }
        private void InitializeDBNames(in MetadataRegistry registry)
        {
            using (ConfigFileBuffer file = Load(ConfigTables.Params, "DBNames"))
            {
                DBNames.Parse(file.AsReadOnlySpan(), in registry);
            }

            using (ConfigFileBuffer file = Load(ConfigTables.Params, "DBNames-Ext-1"))
            {
                DBNames.Parse(file.AsReadOnlySpan(), in registry);
            }

            //using (ConfigFileBuffer file = Load(ConfigTables.Params, "DBNames-Ext-%"))
            //{
            //    DbNamesParser.Parse(file.AsReadOnlySpan(), in registry);
            //}
        }
        private void InitializeMetadataRegistry(in Dictionary<Guid, Guid[]> metadata, in MetadataRegistry registry)
        {
            Task[] tasks = new Task[metadata.Keys.Count - 2]; // Всего 14 объектов метаданных

            int index = 0;

            foreach (var item in metadata)
            {
                if (item.Key == MetadataType.DefinedType || item.Key == MetadataType.SharedProperty)
                {
                    continue; // Отложенная инициализация (смотри комментарии ниже)
                }

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
                throw; //TODO: log and report errors

                //if (exception is AggregateException errors)
                //{
                //    foreach (Exception error in errors.InnerExceptions)
                //    {
                //
                //    }
                //}
            }

            // Инициализация свойства "Type" характеристик зависит от предварительной
            // инициализации коллекции _references, так как тип данных характеристики
            // может ссылаться сам на себя как на ссылочный тип.

            if (metadata.TryGetValue(MetadataType.Characteristic, out Guid[] entries))
            {
                try
                {
                    foreach (ConfigFileBuffer file in Stream(entries))
                    {
                        ConfigFileReader reader = new(file.AsReadOnlySpan());

                        Guid uuid = new(file.FileName);

                        Characteristic.InitializeDataType(uuid, file.AsReadOnlySpan(), registry);
                    }
                }
                catch (Exception error)
                {
                    throw;
                }
            }

            // Инициализация свойства "Type" определяемых типов зависит от предварительной
            // инициализации коллекции _references, которая заполняется многопоточно выше.

            if (metadata.TryGetValue(MetadataType.DefinedType, out entries))
            {
                InitializeMetadataRegistryEntries(new ConfigFileBatchWork()
                {
                    Type = MetadataType.DefinedType,
                    Entries = entries,
                    Registry = registry
                });
            }

            // Инициализация свойства "Type" общих реквизитов зависит от предварительной
            // инициализации коллекций _references, _characteristics и _defined_types.

            if (metadata.TryGetValue(MetadataType.SharedProperty, out entries))
            {
                InitializeMetadataRegistryEntries(new ConfigFileBatchWork()
                {
                    Type = MetadataType.SharedProperty,
                    Entries = entries,
                    Registry = registry
                });
            }
        }
        private void InitializeMetadataRegistryEntries(object parameters)
        {
            if (parameters is not ConfigFileBatchWork work)
            {
                return;
            }

            MetadataRegistry registry = work.Registry;

            if (!ConfigFileParser.TryGetParser(work.Type, out ConfigFileParser parser))
            {
                return;
            }

            try
            {
                foreach (ConfigFileBuffer file in Stream(work.Entries))
                {
                    ConfigFileReader reader = new(file.AsReadOnlySpan());

                    Guid uuid = new(file.FileName);

                    parser.Initialize(uuid, file.AsReadOnlySpan(), in registry);
                }
            }
            catch (Exception error)
            {
                throw;
            }
        }
    }
}