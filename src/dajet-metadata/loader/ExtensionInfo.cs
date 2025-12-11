using System.Text.Json.Serialization;

namespace DaJet.Metadata
{
    /// <summary>
    /// Варианты возможных областей действия расширения конфигурации (доступно, начиная с версиии 8.3.12)
    /// </summary>
    public enum ExtensionScope
    {
        /// <summary>
        /// <b>ИнформационнаяБаза</b>
        /// <br>Расширение конфигурации действует для всей информационной базы.</br>
        /// </summary>
        InfoBase = 1,
        /// <summary>
        /// <b>РазделениеДанных</b>
        /// <br>Расширение конфигурации действует только в области, в которой оно подключено.</br>
        /// </summary>
        DataSeparation = 2
    }

    /// <summary>
    /// Варианты назначения расширения конфигурации (доступно, начиная с версиии 8.3.10)
    /// </summary>
    public enum ExtensionPurpose
    {
        /// <summary>
        /// <b>Исправление</b>
        /// <br>Расширение для корректирования ошибок и неточностей.</br>
        /// </summary>
        Patch,
        /// <summary>
        /// <b>Адаптация</b>
        /// <br>Расширение для настройки существующих решений</br>
        /// <br>с учетом специфики конкретного внедрения.</br>
        /// </summary>
        Customization,
        /// <summary>
        /// <b>Дополнение</b>
        /// <br>Расширение для внесения нового функционала.</br>
        /// </summary>
        AddOn
    }

    /// <summary>
    /// Расширение конфигурации (доступно, начиная с версии 8.3.6)
    /// </summary>
    public sealed class ExtensionInfo
    {
        /// <summary>
        /// Идентификатор расширения _IDRRef в таблице _ExtensionsInfo
        /// <br>Используется для поиска соответствующего файла DbNames расширения</br>
        /// </summary>
        public Guid Identity { get; set; } = Guid.Empty;
        /// <summary>Идентификатор расширения как объекта метаданных</summary>
        public Guid Uuid { get; set; }
        /// <summary>Имя расширения как объекта метаданных
        /// <br>Не должно превышать 80 символов</br></summary>
        public string Name { get; set; }
        /// <summary>
        /// Если данное свойство установлено в значение "Ложь", то при следующем запуске
        /// <br>расширение не будет подключено. (Доступно, начиная с версии 8.3.12)</br>
        /// </summary>
        public bool IsActive { get; set; }
        /// <summary>Версия расширения (доступно, начиная с версии 8.3.6)</summary>
        public string Version { get; set; }
        /// <summary>Порядковый номер расширения</summary>
        public int Order { get; set; }
        /// <summary>Область действия расширения (доступно, начиная с версиии 8.3.12)</summary>
        public ExtensionScope Scope { get; set; } = ExtensionScope.InfoBase;
        /// <summary>Назначение расширения (доступно, начиная с версиии 8.3.10)</summary>
        public ExtensionPurpose Purpose { get; set; } = ExtensionPurpose.Customization;
        /// <summary>
        /// Это значение является значением поля "FileName" таблицы "ConfigCAS".
        /// <br>Вычисляется по алгоритму SHA-1 по значению поля "BinaryData" таблицы "ConfigCAS".</br>
        /// </summary>
        public string RootFile { get; set; }
        /// <summary>
        /// Это значение является значением поля "FileName" таблицы "ConfigCAS".
        /// <br>Имя файла описания объектов метаданных, входящих в состав расширения.</br>
        /// </summary>
        public string FileName { get; set; }
        /// <summary>Дата и время последнего обновления расширения</summary>
        public DateTime Updated { get; set; }
        /// <summary>
        /// Содержит узел распределенной информационной базы, в котором создано данное расширение конфигурации.
        /// <br>Если текущая информационная база не является узлом распределенной информационной базы или</br>
        /// <br>расширение создано локально, то содержит значение "Неопределено". (Доступно, начиная с версии 8.3.12)</br>
        /// </summary>
        public string MasterNode { get; set; } = "0:00000000000000000000000000000000";
        /// <summary>
        /// Если это свойство установлено в значение "Истина", то данное расширение конфигурации
        /// <br>будет передаваться в распределенных информационных базах, организованных планами обмена,</br>
        /// <br>у которых свойство "РаспределеннаяИнформационнаяБаза" установлено в значение "Истина".</br>
        /// </summary>
        public bool IsDistributed { get; set; } // Доступно, начиная с версии 8.3.12
        ///<summary>
        ///<b>Сопоставление имён файлов метаданных расширения:</b>
        ///<br><b>Ключ:</b> стандартное имя файла, образованное от UUID'а объекта метаданных.</br>
        ///<br><b>Значение:</b> реальное имя файла в таблице ConfigCAS (вероятно хэш данных).</br>
        ///</summary>
        [JsonIgnore] public Dictionary<string, string> FileMap { get; } = new();
    }
}