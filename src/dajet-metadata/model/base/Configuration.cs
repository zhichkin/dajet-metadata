using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    // Запрещена комбинация основного режима запуска "Управляемое приложение" и режима совместимости с версией 8.1
    // Включение режима совместимости с версией 8.3.9 и ниже не совместимо с выключенным разделением расширений у общего реквизита
    // Включение режима совместимости с версией 8.2.13 и ниже несовместимо с наличием в конфигурации общих реквизитов
    // Использование определяемых типов в режиме совместимости 8.3.2 и ниже недопустимо
    public sealed class Configuration
    {
        #region "Свойства конфигурации"
        public Guid Uuid { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public string Comment { get; set; }
        /// <summary>
        /// Версия среды выполнения платформы
        /// </summary>
        public int PlatformVersion { get; set; }
        /// <summary>
        /// Режим совместимости платформы
        /// </summary>
        public int CompatibilityVersion { get; set; }
        /// <summary>
        /// Смещение дат
        /// </summary>
        public int YearOffset { get; set; }
        /// <summary>
        /// Версия прикладной конфигурации
        /// </summary>
        public string AppConfigVersion { get; set; } = string.Empty;
        /// <summary>
        /// Краткая информация о конфигурации
        /// </summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>
        /// Подробная информация о конфигурации
        /// </summary>
        public string DetailedDescription { get; set; } = string.Empty;
        /// <summary>
        /// Поставщик конфигурации
        /// </summary>
        public string Provider { get; set; } = string.Empty;
        /// <summary>
        /// Режим использования синхронных вызовов расширений платформы и внешних компонент
        /// </summary>
        public SyncCallsMode SyncCallsMode { get; set; }
        /// <summary>
        /// Режим управления блокировкой данных
        /// </summary>
        public DataLockingMode DataLockingMode { get; set; }
        /// <summary>
        /// Режим использования модальности
        /// </summary>
        public ModalWindowMode ModalWindowMode { get; set; }
        /// <summary>
        /// Режим автонумерации объектов
        /// </summary>
        public AutoNumberingMode AutoNumberingMode { get; set; }
        /// <summary>
        /// Режим совместимости интерфейса
        /// </summary>
        public UICompatibilityMode UICompatibilityMode { get; set; }
        /// <summary>
        /// Префикс имён собственных объектов расширения конфигурации
        /// </summary>
        public string NamePrefix { get; set; } = string.Empty;
        /// <summary>
        /// Поддерживать соответствие объектам расширяемой конфигурации по внутренним идентификаторам
        /// <br>Доступен, начиная с версии 8.3.14</br>
        /// </summary>
        public bool MapMetadataByUuid { get; set; } = true;
        /// <summary>
        /// Режим совместимости расширения конфигурации
        /// </summary>
        public int ExtensionCompatibility { get; set; } = 80306;

        #endregion

        private readonly Dictionary<Guid, Guid[]> _metadata = CreateMetadataRegistry();
        private static Dictionary<Guid, Guid[]> CreateMetadataRegistry()
        {
            return new(14)
            {
                { MetadataTypes.SharedProperty,       null }, // Общие реквизиты
                { MetadataTypes.Publication,          null }, // Планы обмена
                { MetadataTypes.DefinedType,          null }, // Определяемые типы
                { MetadataTypes.Constant,             null }, // Константы
                { MetadataTypes.Catalog,              null }, // Справочники
                { MetadataTypes.Document,             null }, // Документы
                { MetadataTypes.Enumeration,          null }, // Перечисления
                { MetadataTypes.Characteristic,       null }, // Планы видов характеристик
                { MetadataTypes.Account,              null }, // Планы счетов
                { MetadataTypes.InformationRegister,  null }, // Регистры сведений
                { MetadataTypes.AccumulationRegister, null }, // Регистры накопления
                { MetadataTypes.AccountingRegister,   null }, // Регистры бухгалтерии
                { MetadataTypes.BusinessProcess,      null }, // Бизнес-процесс
                { MetadataTypes.BusinessTask,         null }  // Задача
            };
        }
        public Dictionary<Guid, Guid[]> Metadata { get { return _metadata; } }

        public override string ToString() { return Name; }
        
        #region "Парсер корневого файла конфигурации"

        internal static Configuration Parse(Guid uuid, ReadOnlySpan<byte> fileData, byte cfid = 0)
        {
            Configuration configuration = new()
            {
                Uuid = uuid
            };

            ConfigFileReader reader = new(fileData);

            // Идентификатор объекта метаданных - значение поля FileName в таблице Config или ConfigCAS
            //if (reader[2][1].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

            uint components = 0;

            // Количество компонент платформы:
            // [4] Компонента платформы "Общие объекты"
            // [5] Компонента платформы "Оперативный учёт"
            // [6] Компонента платформы "Бухгалтерский учёт"
            // [7] Компонента платформы "Расчёт"
            // [8] Компонента платформы "Бизнес-процессы"
            if (reader[3].Seek()) { components = (uint)reader.ValueAsNumber; }

            // Свойства конфигурации

            ParseConfigurationProperties(ref reader, in configuration, cfid);

            // Реестр объектов конфигурации

            components += 4; // Добавим смещение от первого узла компоненты

            for (uint node = 4; node < components; node++)
            {
                ParsePlatformComponent(ref reader, node, configuration.Metadata);
            }

            return configuration;
        }
        private static void ParseConfigurationProperties(ref ConfigFileReader reader, in Configuration configuration, byte cfid)
        {
            // Наименование конфигурации
            if (reader[4][2][2][2][2][3].Seek()) { configuration.Name = reader.ValueAsString; }

            // Синоним
            if (reader[4][2][2][2][2][4][3].Seek()) { configuration.Alias = reader.ValueAsString; }

            // Комментарий
            if (reader[4][2][2][2][2][5].Seek()) { configuration.Comment = reader.ValueAsString; }

            // Подробная информация
            if (reader[4][2][2][5][3].Seek()) { configuration.DetailedDescription = reader.ValueAsString; }

            // Краткая информация
            if (reader[4][2][2][6][3].Seek()) { configuration.Description = reader.ValueAsString; }

            // Поставщик конфигурации
            if (reader[4][2][2][15].Seek()) { configuration.Provider = reader.ValueAsString; }

            // Версия конфигурации
            if (reader[4][2][2][16].Seek()) { configuration.AppConfigVersion = reader.ValueAsString; }

            // Режим управления блокировкой данных в транзакции по умолчанию
            if (reader[4][2][2][18].Seek()) { configuration.DataLockingMode = (DataLockingMode)reader.ValueAsNumber; }

            // Режим автонумерации объектов
            if (reader[4][2][2][20].Seek()) { configuration.AutoNumberingMode = (AutoNumberingMode)reader.ValueAsNumber; }

            // Режим совместимости
            if (reader[4][2][2][27].Seek())
            {
                int version = reader.ValueAsNumber;
                if (version == 0) { configuration.CompatibilityVersion = 80216; }
                else if (version == 1) { configuration.CompatibilityVersion = 80100; }
                else if (version == 2) { configuration.CompatibilityVersion = 80213; }
                else { configuration.CompatibilityVersion = version; }
            }

            //_converter[3][1][1][36] += ModalWindowMode; // Режим использования модальности
            if (reader[4][2][2][37].Seek()) { configuration.ModalWindowMode = (ModalWindowMode)reader.ValueAsNumber; }

            //_converter[3][1][1][38] += UICompatibilityMode; // Режим совместимости интерфейса
            if (reader[4][2][2][39].Seek()) { configuration.UICompatibilityMode = (UICompatibilityMode)reader.ValueAsNumber; }

            // Режим использования синхронных вызовов расширений платформы и внешних компонент
            if (reader[4][2][2][42].Seek()) { configuration.SyncCallsMode = (SyncCallsMode)reader.ValueAsNumber; }

            // Свойства расширения конфигурации
            if (cfid > 0)
            {
                if (reader[4][2][2][43].Seek()) { configuration.NamePrefix = reader.ValueAsString; }
                if (reader[4][2][2][44].Seek()) { configuration.ExtensionCompatibility = reader.ValueAsNumber; }
                if (reader[4][2][2][50].Seek()) { configuration.MapMetadataByUuid = (reader.ValueAsNumber == 1); }
            }
        }
        private static void ParsePlatformComponent(ref ConfigFileReader reader, uint component, in Dictionary<Guid, Guid[]> metadata)
        {
            Guid uuid;

            if (component == 4)
            {
                uuid = Components.Common; // Исключение, так как файл читается последовательно, вектора [4][2][2] уже обработаны, а списки объектов начинаются с [4][2][3]
            }
            else if (reader[component][1].Seek())
            {
                uuid = reader.ValueAsUuid;
            }
            else
            {
                return; // Этого не должно быть
            }

            if (uuid == Components.Common) // [4][1] Идентификатор компоненты "Общие объекты"
            {
                ParseComponentMetadataObjects(ref reader, component, in metadata);
            }
            else if (uuid == Components.Operations) // [5][1] Идентификатор компоненты "Оперативный учёт"
            {
                ParseOperationsMetadataObjects(ref reader, component, in metadata);
            }
            else if (uuid == Components.Accounting) // [6][1] Идентификатор компоненты "Бухгалтерский учёт"
            {
                ParseComponentMetadataObjects(ref reader, component, in metadata);
            }
            else if (uuid == Components.BusinessProcess) // [7][1] Идентификатор компоненты "Бизнес-процессы"
            {
                ParseComponentMetadataObjects(ref reader, component, in metadata);
            }
        }
        private static void ParseComponentMetadataObjects(ref ConfigFileReader reader, uint component, in Dictionary<Guid, Guid[]> metadata)
        {
            // [6][2][3] 2  Количество типов объектов метаданных компоненты "Бухгалтерский учёт"
            // [6][2][4] {
            // [6][2][4][1] 238e7e88-3c5f-48b2-8a3b-81ebbecb20ed | Тип объекта "План счетов"
            // [6][2][4][2] 1                                    | Количество объектов данного типа
            // [6][2][4][3] 3b0c4744-6437-4ccc-bc96-9dd9bae7a7ae | Идентификатор объекта метаданных
            // [6][2][4] }
            // [6][2][5] {
            // [6][2][5][1] 2deed9b8-0056-4ffe-a473-c20a6c32a0bc | Тип объекта "Регистр бухгалтерии"
            // [6][2][5][2] 1                                    | Количество объектов данного типа
            // [6][2][5][3] 7cbb9946-2e19-4797-8ed4-d7a0ece334bb | Идентификатор объекта метаданных
            // [6][2][5] }
            // [6][2] }

            if (!reader[component][2][3].Seek())
            {
                return; // Такого не должно быть
            }

            int number_of_types = reader.ValueAsNumber; // Количество типов объектов метаданных компоненты

            number_of_types += 4; // С учётом смещения от первого узла типа

            for (uint type = 4; type < number_of_types; type++)
            {
                if (!reader[component][2][type][1].Seek()) { continue; }

                Guid uuid = reader.ValueAsUuid; // Идентификатор типа объекта метаданных

                if (!metadata.TryGetValue(uuid, out Guid[] objects))
                {
                    continue; // Неподдерживаемый тип объекта метаданных
                }

                if (!reader[component][2][type][2].Seek()) { continue; }

                int number_of_objects = reader.ValueAsNumber; // Количество объектов данного типа

                if (objects is null)
                {
                    if (number_of_objects == 0)
                    {
                        objects = Array.Empty<Guid>();
                    }
                    else
                    {
                        objects = new Guid[number_of_objects];
                    }

                    metadata[uuid] = objects;
                }

                for (int item = 0; item < objects.Length; item++) // [component][2][type][item]
                {
                    if (reader.Read() && reader.Token == ConfigFileToken.Value)
                    {
                        objects[item] = reader.ValueAsUuid; // Идентификатор объекта метаданных
                    }
                }
            }
        }
        private static void ParseOperationsMetadataObjects(ref ConfigFileReader reader, uint component, in Dictionary<Guid, Guid[]> metadata)
        {
            if (!reader[component][2][2][3].Seek())
            {
                return;
            }

            int number_of_types = reader.ValueAsNumber; // Количество типов объектов метаданных компоненты

            number_of_types += 4; // С учётом смещения от первого узла типа

            for (uint node = 4; node < number_of_types; node++)
            {
                if (!reader[component][2][2][node][1].Seek()) { continue; }

                Guid type = reader.ValueAsUuid; // Идентификатор типа объекта метаданных

                if (!metadata.TryGetValue(type, out Guid[] objects))
                {
                    continue; // Неподдерживаемый тип объекта метаданных
                }

                if (!reader[component][2][2][node][2].Seek()) { continue; }

                int number_of_objects = reader.ValueAsNumber; // Количество объектов данного типа

                if (objects is null)
                {
                    objects = new Guid[number_of_objects];

                    metadata[type] = objects;
                }

                for (int item = 0; item < objects.Length; item++) // [5][2][2][node][item]
                {
                    if (reader.Read() && reader.Token == ConfigFileToken.Value)
                    {
                        objects[item] = reader.ValueAsUuid; // Идентификатор объекта метаданных
                    }
                }
            }
        }

        #endregion

        internal static Configuration ParsePropertiesOnly(Guid uuid, ReadOnlySpan<byte> fileData, byte cfid)
        {
            Configuration metadata = new()
            {
                Uuid = uuid
            };

            ConfigFileReader reader = new(fileData);

            // Идентификатор объекта метаданных - значение поля FileName в таблице Config
            //if (reader[2][1].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

            // Свойства конфигурации

            ParseConfigurationProperties(ref reader, in metadata, cfid);

            return metadata;
        }
        internal static int ParseMetadataRegistryOnly(ReadOnlySpan<byte> fileData, out Dictionary<Guid, Guid[]> metadata)
        {
            ConfigFileReader reader = new(fileData);

            metadata = CreateMetadataRegistry();

            // Количество компонент платформы:
            // [4] Компонента платформы "Общие объекты"
            // [5] Компонента платформы "Оперативный учёт"
            // [6] Компонента платформы "Бухгалтерский учёт"
            // [7] Компонента платформы "Расчёт"
            // [8] Компонента платформы "Бизнес-процессы"
            // [9] ?
            // [10] ?

            uint components = (uint)reader[3].SeekNumber(); // = 7 компонент платформы

            // Режим совместимости

            int version = reader[4][2][2][27].SeekNumber();
            if (version == 0) { version = 80216; }
            else if (version == 1) { version = 80100; }
            else if (version == 2) { version = 80213; }

            // Реестр объектов конфигурации

            components += 4; // Добавим смещение для удобства

            for (uint node = 4; node < components; node++)
            {
                ParsePlatformComponent(ref reader, node, in metadata);
            }

            return version;
        }
    }
}