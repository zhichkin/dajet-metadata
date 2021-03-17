using System;
using System.Collections.Generic;
using System.Linq;

namespace DaJet.Metadata.Model
{
    ///<summary>Класс для описания свойств объекта метаданных <see cref="MetadataObject"> (реквизитов, измерений и ресурсов)</summary>
    public sealed class MetadataProperty
    {
        public Guid FileName { get; set; }
        public string Name { get; set; }
        public string Field { get; set; }
        public List<DatabaseField> Fields { get; set; } = new List<DatabaseField>();
        ///<summary>Логический смысл свойства. Подробнее смотри перечисление <see cref="PropertyPurpose"/>.</summary>
        public PropertyPurpose Purpose { get; set; } = PropertyPurpose.Property;
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