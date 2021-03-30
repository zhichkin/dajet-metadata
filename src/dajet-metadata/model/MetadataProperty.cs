using System;
using System.Collections.Generic;
using System.Linq;

namespace DaJet.Metadata.Model
{
    ///<summary>Класс для описания свойств объекта метаданных <see cref="MetadataObject"> (реквизитов, измерений и ресурсов)</summary>
    public class MetadataProperty
    {
        ///<summary>
        /// Идентификатор свойства объекта метаданных из файла DBNames таблицы Params.
        /// Используется для того, чтобы ссылаться на свойство в файле объекта метаданных из таблицы Config.
        ///</summary>
        public Guid FileName { get; set; } = Guid.Empty;
        ///<summary>Имя свойства объекта метаданных</summary>
        public string Name { get; set; } = string.Empty;
        ///<summary>Синоним свойства объекта метаданных</summary>
        public string Alias { get; set; } = string.Empty;
        ///<summary>Основа имени поля в таблице СУБД (может быть дополнено постфиксами в зависимости от типа данных свойства)</summary>
        public string DbName { get; set; } = string.Empty;
        ///<summary>Коллекция для описания полей таблицы СУБД свойства объекта метаданных</summary>
        public List<DatabaseField> Fields { get; set; } = new List<DatabaseField>();
        ///<summary>Логический смысл свойства. Подробнее смотри перечисление <see cref="PropertyPurpose"/>.</summary>
        public PropertyPurpose Purpose { get; set; } = PropertyPurpose.Property;
        ///<summary>Описание типов данных <see cref="DataTypeInfo"/>, которые могут использоваться для значений свойства.</summary>
        public DataTypeInfo PropertyType { get; set; } = new DataTypeInfo();
        public bool IsPrimaryKey()
        {
            return (Fields != null
                && Fields.Count > 0
                && Fields.Where(f => f.IsPrimaryKey).FirstOrDefault() != null);
        }
        public override string ToString() { return Name; }
    }
}