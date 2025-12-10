using DaJet.Data;
using DaJet.TypeSystem;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        internal abstract ConfigFileBuffer Load(in string fileName);
        internal abstract ConfigFileBuffer Load(in string tableName, in string fileName);
        internal abstract IEnumerable<ConfigFileBuffer> Stream(Guid[] files);
        internal abstract EntityDefinition GetDbTableSchema(in string tableName);
        internal abstract bool IsExtensionsSupported();
        internal abstract List<ExtensionInfo> GetExtensions();
        internal abstract T ExecuteScalar<T>(in string script, int timeout);

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
            if (metadata.TryGetValue(MetadataTypes.SharedProperty, out items))
            {
                foreach (Guid uuid in items)
                {
                    registry.AddEntry(uuid, new SharedProperty(uuid));
                }
            }

            // Подготавливаем реестр метаданных для многопоточной обработки
            if (metadata.TryGetValue(MetadataTypes.DefinedType, out items))
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
                if (item.Key == MetadataTypes.DefinedType || item.Key == MetadataTypes.SharedProperty)
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

            if (metadata.TryGetValue(MetadataTypes.Characteristic, out Guid[] entries))
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

            if (metadata.TryGetValue(MetadataTypes.DefinedType, out entries))
            {
                InitializeMetadataRegistryEntries(new ConfigFileBatchWork()
                {
                    Type = MetadataTypes.DefinedType,
                    Entries = entries,
                    Registry = registry
                });
            }

            // Инициализация свойства "Type" общих реквизитов зависит от предварительной
            // инициализации коллекций _references, _characteristics и _defined_types.

            if (metadata.TryGetValue(MetadataTypes.SharedProperty, out entries))
            {
                InitializeMetadataRegistryEntries(new ConfigFileBatchWork()
                {
                    Type = MetadataTypes.SharedProperty,
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

        private static ulong ReadVarInt(in byte[] data, ref int index)
        {
            int offset = 0;
            ulong value = 0UL;
            ulong chunk;

            do
            {
                chunk = data[index++];
                value |= (chunk & 0x7F) << offset;
                offset += 7;
            }
            while ((chunk & 0x80) == 0x80);

            return value;
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
    }
}