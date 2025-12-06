using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    // Запрещена комбинация основного режима запуска "Управляемое приложение" и режима совместимости с версией 8.1
    // Включение режима совместимости с версией 8.3.9 и ниже не совместимо с выключенным разделением расширений у общего реквизита
    // Включение режима совместимости с версией 8.2.13 и ниже несовместимо с наличием в конфигурации общих реквизитов
    // Использование определяемых типов в режиме совместимости 8.3.2 и ниже недопустимо
    public sealed class InfoBase
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
        /// Префикс имен собственных объектов расширения конфигурации
        /// </summary>
        public string NamePrefix { get; set; }
        /// <summary>
        /// Поддерживать соответствие объектам расширяемой конфигурации по внутренним идентификаторам
        /// </summary>
        public bool MapMetadataByUuid { get; set; } = true;
        /// <summary>
        /// Режим совместимости расширения конфигурации
        /// </summary>
        public int ExtensionCompatibility { get; set; }

        #endregion

        #region "Парсер корневого файла конфигурации"

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
        internal static InfoBase Parse(Guid uuid, ReadOnlySpan<byte> fileData)
        {
            InfoBase metadata = new()
            {
                Uuid = uuid
            };

            ConfigFileReader reader = new(fileData);

            // Идентификатор объекта метаданных - значение поля FileName в таблице Config
            //if (reader[2][1].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

            // Свойства конфигурации

            ParseInfoBaseProperties(ref reader, in metadata);

            return metadata;
        }
        internal static int Parse(ReadOnlySpan<byte> fileData, out Dictionary<Guid, Guid[]> registry)
        {
            ConfigFileReader reader = new(fileData);

            registry = CreateMetadataRegistry();

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
                ParsePlatformComponent(ref reader, node, in registry);
            }

            return version;
        }
        internal static InfoBase Parse(Guid uuid, ReadOnlySpan<byte> fileData, out Dictionary<Guid, Guid[]> registry)
        {
            InfoBase metadata = new()
            {
                Uuid = uuid
            };

            ConfigFileReader reader = new(fileData);

            // Идентификатор объекта метаданных - значение поля FileName в таблице Config
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

            ParseInfoBaseProperties(ref reader, in metadata);

            // Реестр объектов конфигурации

            registry = CreateMetadataRegistry();

            components += 4; // Добавим смещение от первого узла компоненты

            for (uint node = 4; node < components; node++)
            {
                ParsePlatformComponent(ref reader, node, in registry);
            }

            return metadata;
        }
        private static void ParseInfoBaseProperties(ref ConfigFileReader reader, in InfoBase metadata)
        {
            // Наименование конфигурации
            if (reader[4][2][2][2][2][3].Seek()) { metadata.Name = reader.ValueAsString; }

            // Синоним
            if (reader[4][2][2][2][2][4][3].Seek()) { metadata.Alias = reader.ValueAsString; }

            // Комментарий
            if (reader[4][2][2][2][2][5].Seek()) { metadata.Comment = reader.ValueAsString; }

            // Подробная информация
            if (reader[4][2][2][5][3].Seek()) { metadata.DetailedDescription = reader.ValueAsString; }

            // Краткая информация
            if (reader[4][2][2][6][3].Seek()) { metadata.Description = reader.ValueAsString; }

            // Поставщик конфигурации
            if (reader[4][2][2][15].Seek()) { metadata.Provider = reader.ValueAsString; }

            // Версия конфигурации
            if (reader[4][2][2][16].Seek()) { metadata.AppConfigVersion = reader.ValueAsString; }

            // Режим управления блокировкой данных в транзакции по умолчанию
            if (reader[4][2][2][18].Seek()) { metadata.DataLockingMode = (DataLockingMode)reader.ValueAsNumber; }

            // Режим автонумерации объектов
            if (reader[4][2][2][20].Seek()) { metadata.AutoNumberingMode = (AutoNumberingMode)reader.ValueAsNumber; }

            // Режим совместимости
            if (reader[4][2][2][27].Seek())
            {
                int version = reader.ValueAsNumber;
                if (version == 0) { metadata.CompatibilityVersion = 80216; }
                else if (version == 1) { metadata.CompatibilityVersion = 80100; }
                else if (version == 2) { metadata.CompatibilityVersion = 80213; }
                else { metadata.CompatibilityVersion = version; }
            }

            //_converter[3][1][1][36] += ModalWindowMode; // Режим использования модальности
            if (reader[4][2][2][37].Seek()) { metadata.ModalWindowMode = (ModalWindowMode)reader.ValueAsNumber; }

            //_converter[3][1][1][38] += UICompatibilityMode; // Режим совместимости интерфейса
            if (reader[4][2][2][39].Seek()) { metadata.UICompatibilityMode = (UICompatibilityMode)reader.ValueAsNumber; }

            // Режим использования синхронных вызовов расширений платформы и внешних компонент
            if (reader[4][2][2][42].Seek()) { metadata.SyncCallsMode = (SyncCallsMode)reader.ValueAsNumber; }

            // Свойства расширения конфигурации
            //if (_cache != null && _cache.Extension != null)
            //{
            //    _converter[3][1][1][42] += NamePrefix;
            //    _converter[3][1][1][43] += ExtensionCompatibility;
            //    _converter[3][1][1][49] += MapMetadataByUuid;
            //}
        }
        private static void ParsePlatformComponent(ref ConfigFileReader reader, uint component, in Dictionary<Guid, Guid[]> registry)
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
                ParseComponentMetadataObjects(ref reader, component, in registry);
            }
            else if (uuid == Components.Operations) // [5][1] Идентификатор компоненты "Оперативный учёт"
            {
                ParseOperationsMetadataObjects(ref reader, component, in registry);
            }
            else if (uuid == Components.Accounting) // [6][1] Идентификатор компоненты "Бухгалтерский учёт"
            {
                ParseComponentMetadataObjects(ref reader, component, in registry);
            }
            else if (uuid == Components.BusinessProcess) // [7][1] Идентификатор компоненты "Бизнес-процессы"
            {
                ParseComponentMetadataObjects(ref reader, component, in registry);
            }
        }
        private static void ParseComponentMetadataObjects(ref ConfigFileReader reader, uint component, in Dictionary<Guid, Guid[]> registry)
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

                if (!registry.TryGetValue(uuid, out Guid[] objects))
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
                    
                    registry[uuid] = objects;
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
        private static void ParseOperationsMetadataObjects(ref ConfigFileReader reader, uint component, in Dictionary<Guid, Guid[]> registry)
        {
            if (!reader[component][2][2][3].Seek())
            {
                return;
            }

            int number_of_types = reader.ValueAsNumber; // Количество типов объектов метаданных компоненты

            number_of_types += 4; // С учётом смещения от первого узла типа

            for (uint type = 4; type < number_of_types; type++)
            {
                if (!reader[component][2][2][type][1].Seek()) { continue; }

                Guid uuid = reader.ValueAsUuid; // Идентификатор типа объекта метаданных

                if (!registry.TryGetValue(uuid, out Guid[] objects))
                {
                    continue; // Неподдерживаемый тип объекта метаданных
                }

                if (!reader[component][2][2][type][2].Seek()) { continue; }

                int number_of_objects = reader.ValueAsNumber; // Количество объектов данного типа

                if (objects is null)
                {
                    objects = new Guid[number_of_objects];
                    
                    registry[uuid] = objects;
                }

                for (int item = 0; item < objects.Length; item++) // [5][2][2][type][item]
                {
                    if (reader.Read() && reader.Token == ConfigFileToken.Value)
                    {
                        objects[item] = reader.ValueAsUuid; // Идентификатор объекта метаданных
                    }
                }
            }
        }

        #endregion
    }
}