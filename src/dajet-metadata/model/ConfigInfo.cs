namespace DaJet.Metadata.Model
{
    public sealed class ConfigInfo
    {
        /// <summary>
        /// Имя конфигурации
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Синоним конфигурации
        /// </summary>
        public string Alias { get; set; }
        /// <summary>
        /// Комментарий
        /// </summary>
        public string Comment { get; set; }
        /// <summary>
        /// Режим совместимости (версия платформы)
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// Версия конфигурации
        /// </summary>
        public string ConfigVersion { get; set; }
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
    }
}