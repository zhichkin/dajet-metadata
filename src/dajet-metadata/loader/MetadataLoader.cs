using DaJet.Data;
using DaJet.TypeSystem;
using System.Text;

namespace DaJet.Metadata
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
        internal abstract ConfigFileBuffer Load(in string tableName, in string fileName);
        internal abstract IEnumerable<ConfigFileBuffer> Stream(string tableName, string[] fileNames);
        internal abstract EntityDefinition GetDbTableSchema(in string tableName);
        internal abstract List<ExtensionInfo> GetExtensions();
        internal abstract T ExecuteScalar<T>(in string script, int timeout);

        internal Guid GetRoot()
        {
            Guid root = Guid.Empty;

            using (ConfigFileBuffer file = Load(ConfigTables.Config, "root"))
            {
                ConfigFileReader reader = new(file.AsReadOnlySpan());

                if (reader[2].Seek()) { root = reader.ValueAsUuid; }
            }

            return root;
        }
        internal InfoBase GetInfoBase()
        {
            Guid root = GetRoot();

            string fileName = root.ToString().ToLowerInvariant();

            InfoBase infoBase;

            using (ConfigFileBuffer file = Load(ConfigTables.Config, fileName))
            {
                Guid uuid = new(file.FileName);

                infoBase = InfoBase.Parse(uuid, file.AsReadOnlySpan());
            }

            return infoBase;
        }
        internal EntityDefinition Load(in string type, Guid uuid, in MetadataRegistry registry)
        {
            if (!ConfigFileParser.TryGetParser(in type, out ConfigFileParser parser))
            {
                return null;
            }

            EntityDefinition definition;

            string tableName;
            string identifier = uuid.ToString().ToLowerInvariant();

            if (!registry.TryGetFileName(identifier, out string fileName))
            {
                fileName = identifier;
                tableName = ConfigTables.Config;
            }
            else
            {
                tableName = ConfigTables.ConfigCAS;
            }

            using (ConfigFileBuffer file = Load(in tableName, in fileName))
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

            string tableName;
            string identifier = uuid.ToString().ToLowerInvariant();

            if (!registry.TryGetFileName(identifier, out string fileName))
            {
                fileName = identifier;
                tableName = ConfigTables.Config;
            }
            else
            {
                tableName = ConfigTables.ConfigCAS;
            }

            using (ConfigFileBuffer file = Load(in tableName, in fileName))
            {
                definition = parser.Load(uuid, file.AsReadOnlySpan(), in registry, true);
            }

            return definition;
        }
        
        internal MetadataRegistry GetMetadataRegistry()
        {
            Guid root = GetRoot();

            string rootFile = root.ToString().ToLowerInvariant();

            int version;

            Dictionary<Guid, string[]> metadata;

            using (ConfigFileBuffer file = Load(ConfigTables.Config, rootFile))
            {
                version = InfoBase.Parse(file.AsReadOnlySpan(), out metadata);
            }

            MetadataRegistry registry = new()
            {
                CompatibilityVersion = version
            };

            // Подготавливаем реестр метаданных для многопоточной обработки
            if (metadata.TryGetValue(MetadataTypes.SharedProperty, out string[] fileNames))
            {
                foreach (string fileName in fileNames)
                {
                    Guid uuid = new(fileName);

                    registry.AddEntry(uuid, new SharedProperty(uuid));
                }
            }

            // Подготавливаем реестр метаданных для многопоточной обработки
            if (metadata.TryGetValue(MetadataTypes.DefinedType, out fileNames))
            {
                foreach (string fileName in fileNames)
                {
                    Guid uuid = new(fileName);

                    registry.AddEntry(uuid, new DefinedType(uuid));
                }
            }

            // Инициализация объектов реестра метаданных,
            // загрузка кодов ссылочных типов и имён СУБД

            InitializeDBNames(in registry);

            // Инициализация объектов реестра метаданных зависит
            // от предварительной загрузки кодов ссылочных типов

            InitializeMetadataRegistry(in registry, in metadata, ConfigTables.Config);

            ApplyExtensions(in registry);

            return registry;
        }
        private sealed class ConfigFileBatchWork
        {
            internal Guid Type;
            internal string TableName;
            internal string[] FileNames;
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
        }
        private void InitializeMetadataRegistry(in MetadataRegistry registry, in Dictionary<Guid, string[]> metadata, in string tableName)
        {
            Task[] tasks = new Task[metadata.Keys.Count - 2]; // Всего 14 объектов метаданных

            int index = 0;

            foreach (var item in metadata)
            {
                if (item.Key == MetadataTypes.DefinedType || item.Key == MetadataTypes.SharedProperty)
                {
                    continue; // Отложенная инициализация (смотри комментарии ниже)
                }

                Task task = Task.Factory.StartNew(
                    InitializeMetadataRegistryEntries,
                    new ConfigFileBatchWork()
                    {
                        Type = item.Key,
                        Registry = registry,
                        TableName = tableName,
                        FileNames = item.Value
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

            if (metadata.TryGetValue(MetadataTypes.Characteristic, out string[] fileNames))
            {
                try
                {
                    foreach (ConfigFileBuffer file in Stream(tableName, fileNames))
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

            if (metadata.TryGetValue(MetadataTypes.DefinedType, out fileNames))
            {
                InitializeMetadataRegistryEntries(new ConfigFileBatchWork()
                {
                    Type = MetadataTypes.DefinedType,
                    Registry = registry,
                    TableName = tableName,
                    FileNames = fileNames
                });
            }

            // Инициализация свойства "Type" общих реквизитов зависит от предварительной
            // инициализации коллекций _references, _characteristics и _defined_types.

            if (metadata.TryGetValue(MetadataTypes.SharedProperty, out fileNames))
            {
                InitializeMetadataRegistryEntries(new ConfigFileBatchWork()
                {
                    Type = MetadataTypes.SharedProperty,
                    Registry = registry,
                    TableName = tableName,
                    FileNames = fileNames
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
                foreach (ConfigFileBuffer file in Stream(work.TableName, work.FileNames))
                {
                    ConfigFileReader reader = new(file.AsReadOnlySpan());
                    
                    parser.Initialize(file.AsReadOnlySpan(), in registry);
                }
            }
            catch (Exception error)
            {
                throw;
            }
        }

        protected static void DecodeZippedInfo(in byte[] zippedInfo, in ExtensionInfo extension)
        {
            // Начальные байты 0x43 0xC2 - похоже, что просто константы.
            // Далее идёт байт 0x9A, который определяет кодировку ASCII для последующего значения.
            // Длина значения 0x14 (20 байт)
            // Далее идёт 20 байт - контрольная сумма расширения (SHA-1).
            // Это значение является значением поля "FileName" таблицы "ConfigCAS".
            // Контрольная сумма вычисляется по алгоритму SHA-1 по значению поля "BinaryData"
            // таблицы "ConfigCAS". Данный файл является корневым файлом расширения(root file)
            // по аналогии с корневым файлом основной конфигурации.
            // Флаг "Защита от опасных действий" 0xA1 = false 0xA2 = true
            // Далее: 0x9A - тип данных char (ASCII)
            // Затем: 0x08 - количество символов = 8 байт
            // Далее идёт 8 байт - версия изменения расширения, которая соответствует
            // значению поля _Version в таблице _ExtensionsInfo, при этом почему-то
            // на -1, то есть в СУБД значение больше на 1.
            // Флаг "Безопасный режим, имя профиля безопасности" 0x81 = false 0x82 = true
            // 0x81 - неизвестный флаг (?)
            // Индекс байта = [37] 0x97 - тип данных nchar (может быть 0x9A, если далее только латиница ASCII)
            // Индекс байта = [38] (см. ниже) 0x3A - количество символов (без учёта '\0' в конце).
            // Далее следует описание расширения, в том числе его синоним. Формат такой же,
            // который используется для описания объектов метаданных в файле config.
            // Флаг наличия значения версии 0x81 или значение кодировки (см. ниже).
            // 0x9A или 0x97 - кодировка строкового значения
            // 0x0A - длина строкового значения (без '\0' в конце, как до этого)
            // Далее идёт значение версии расширения, как задано в конфигураторе в поле "Версия".
            // Флаг "Активно" 0x81 = false 0x82 = true
            // Флаг "Использовать основные роли для всех пользователей" 0x81 = false 0x82 = true
            // 0x20 - завершение описания (константа)

            extension.RootFile = Convert.ToHexString(zippedInfo, 4, 20).ToLower();

            Encoding encoding = (zippedInfo[37] == 0x9A) ? Encoding.ASCII : Encoding.Unicode;
            
            int chars = zippedInfo[38]; // Длина описания метаданных в скобочном формате 1С (в символах)

            int index = 39; // Первый символ описания метаданных - открывающая фигурная скобка '{'

            // 56 символов + 199 = 255
            // Длина синонима должна быть 199 символов. Общее количество 255.
            // Если больше, то происходит что-то непонятное - ломается кодировка описания.

            // {"#",87024738-fc2a-4436-ada1-df79d395c424,
            // {1,"ru","Расширение 1"}
            // }

            //NOTE: Читаем полученное количество символов в соответствующей кодировке
            //NOTE: и вычисляем длину в байтах, чтобы получить смещение до следующего значения

            char[] buffer = new char[chars];

            using (MemoryStream stream = new(zippedInfo, index, zippedInfo.Length - index))
            {
                using (StreamReader reader = new(stream, encoding))
                {
                    for (int i = 0; i < chars; i++)
                    {
                        buffer[i] = (char)reader.Read();
                    }
                }
            }

            string config = new(buffer);

            int size = encoding.GetByteCount(config);

            //ReadOnlySpan<byte> alias = Encoding.UTF8.GetBytes(config);

            //ConfigFileReader parser = new(alias);

            //if (parser[3][3].Seek())
            //{
            //    extension.Alias = parser.ValueAsString;
            //}

            if (zippedInfo[size + 39] == 0x81) // Значение версии отсутствует
            {
                extension.IsActive = (zippedInfo[size + 40] == 0x82);
            }
            else
            {
                //NOTE: нужно прочитать значение версии (строковое значение), используя
                //NOTE: соответствующую кодировку и вычислить количество байт смещения
                //NOTE: до следующего значения, а, именно, флага активности расширения

                encoding = (zippedInfo[size + 39] == 0x9A) ? Encoding.ASCII : Encoding.Unicode;
                
                chars = zippedInfo[size + 40]; // Длина версии расширения в символах (строковое значение)

                buffer = new char[chars];

                int offset = size + 41;

                using (MemoryStream stream = new(zippedInfo, offset, zippedInfo.Length - offset))
                {
                    using (StreamReader reader = new(stream, encoding))
                    {
                        for (int i = 0; i < chars; i++)
                        {
                            buffer[i] = (char)reader.Read();
                        }
                    }
                }

                config = new(buffer);

                size = encoding.GetByteCount(config);

                extension.Version = config;

                extension.IsActive = (zippedInfo[offset + size] == 0x82);
            }
        }
        private void ApplyExtensions(in MetadataRegistry registry)
        {
            List<ExtensionInfo> extensions = GetExtensions();

            foreach (ExtensionInfo extension in extensions)
            {
                if (!extension.IsActive) { continue; }

                ParseRootFile(in extension, in registry);

                ApplyExtension(in extension, in registry);
            }
        }
        private void ParseRootFile(in ExtensionInfo extension, in MetadataRegistry registry)
        {
            using (ConfigFileBuffer file = Load(ConfigTables.ConfigCAS, extension.RootFile))
            {
                ReadOnlySpan<byte> buffer = file.AsReadOnlySpan();

                ConfigFileReader reader = new(buffer);

                string root = "";
                int consumed = 0;

                // Версия платформы 1С:Предприятие 8
                int version = reader[2][3][1].SeekNumber();

                //NOTE: Ищем конец первого файла
                if (reader[ConfigFileToken.EndObject].Seek())
                {
                    consumed = reader.Consumed;

                    byte current = buffer[consumed];

                    if (current != CharBytes.Comma)
                    {
                        throw new FormatException($"Ожидался символ \"запятая\", а не [{buffer[consumed]}].");
                    }

                    //NOTE: Ищем начало второго файла
                    while (consumed < buffer.Length && current != CharBytes.OpenBrace)
                    {
                        current = buffer[++consumed];
                    }

                    reader = new ConfigFileReader(buffer[consumed..]);

                    root = reader[2].SeekString();
                    extension.Uuid = new Guid(root);
                    extension.FileName = root;

                    //NOTE: Ищем конец второго файла
                    if (reader[ConfigFileToken.EndObject].Seek())
                    {
                        consumed += reader.Consumed;

                        current = buffer[consumed];

                        //NOTE: Ищем начало третьего файла
                        while (consumed < buffer.Length && current != CharBytes.OpenBrace)
                        {
                            current = buffer[++consumed];
                        }

                        reader = new ConfigFileReader(buffer[consumed..]);

                        int count = reader[1].SeekNumber(); // количество файлов описания метаданных расширения

                        if (count == 0) { return; }

                        uint next = 2;

                        for (uint i = 0; i < count; i++)
                        {
                            string key = reader[next + i].SeekString();
                            string value = reader[next + i + 1].SeekString();

                            next++;

                            byte[] hex = Convert.FromBase64String(value);
                            string fileName = Convert.ToHexString(hex).ToLowerInvariant();

                            registry.AddFileName(in key, in fileName);

                            //_ = extension.FileMap.TryAdd(key, fileName);

                            //if (key == root) // Корневой файл описания объектов расширения
                            //{
                            //    extension.FileName = fileName; // Реестр метаданных
                            //}
                        }
                    }
                }
            }
        }
        private void ApplyExtension(in ExtensionInfo extension, in MetadataRegistry registry)
        {
            if (!registry.TryGetFileName(extension.FileName, out string mainFile))
            {
                throw new InvalidOperationException();
            }

            Dictionary<Guid, string[]> metadata;

            using (ConfigFileBuffer file = Load(ConfigTables.ConfigCAS, mainFile))
            {
                int version = InfoBase.Parse(file.AsReadOnlySpan(), out metadata);
            }

            string[] items; // Заимствованные и собственные объекты метаданных расширения

            // Подготавливаем реестр метаданных для многопоточной обработки
            if (metadata.TryGetValue(MetadataTypes.SharedProperty, out items))
            {
                foreach (string item in items)
                {
                    Guid uuid = new(item);

                    registry.AddEntry(uuid, new SharedProperty(uuid));
                }
            }

            // Подготавливаем реестр метаданных для многопоточной обработки
            if (metadata.TryGetValue(MetadataTypes.DefinedType, out items))
            {
                foreach (string item in items)
                {
                    Guid uuid = new(item);

                    registry.AddEntry(uuid, new DefinedType(uuid));
                }
            }

            // Инициализация объектов реестра метаданных,
            // загрузка кодов ссылочных типов и имён СУБД
            string fileName = string.Format("DBNames-Ext-{0}", extension.Identity.ToString().ToLowerInvariant());

            using (ConfigFileBuffer file = Load(ConfigTables.Params, in fileName))
            {
                DBNames.Parse(file.AsReadOnlySpan(), in registry);
            }

            // Сопоставляем идентифкаторы объектов метаданных файлам в базе данных
            foreach (var item in metadata)
            {
                string[] identifiers = item.Value;

                for (int i = 0; i < identifiers.Length; i++)
                {
                    string identifier = identifiers[i];

                    //TODO: заимствованные объекты (их нет в DBNames-Ext) ???

                    Guid uuid = new(identifier);

                    if (item.Key == MetadataTypes.Catalog)
                    {
                        registry.AddEntry(uuid, Catalog.Create(uuid, 0, MetadataToken.Reference));
                    }
                    else if (item.Key == MetadataTypes.Constant)
                    {
                        registry.AddEntry(uuid, Constant.Create(uuid, 0, MetadataToken.Const));
                    }
                    else if (item.Key == MetadataTypes.Publication)
                    {
                        registry.AddEntry(uuid, Publication.Create(uuid, 0, MetadataToken.Node));
                    }

                    if (registry.TryGetFileName(identifier, out fileName))
                    {
                        identifiers[i] = fileName;
                    }
                }
            }

            // Инициализация объектов реестра метаданных зависит
            // от предварительной загрузки кодов ссылочных типов
            InitializeMetadataRegistry(in registry, in metadata, ConfigTables.ConfigCAS);
        }
    }
}