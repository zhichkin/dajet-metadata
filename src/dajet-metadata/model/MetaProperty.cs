using System;
using System.Collections.Generic;
using System.Linq;

namespace DaJet.Metadata.Model
{
    public sealed class MetaProperty
    {
        public Guid UUID { get; set; }
        public string Name { get; set; }
        public string Field { get; set; }
        public PropertyPurpose Purpose { get; set; } = PropertyPurpose.Property;
        public int PropertyType { get; set; } = (int)DataTypes.NULL;
        public List<MetaField> Fields { get; set; } = new List<MetaField>();
        public bool IsPrimaryKey()
        {
            return (Fields != null
                && Fields.Count > 0
                && Fields.Where(f => f.IsPrimaryKey).FirstOrDefault() != null);
        }
        public bool IsReferenceType
        {
            get
            {
                return (PropertyType > 0 || PropertyType == (int)DataTypes.Multiple);
            }
        }
        public override string ToString() { return Name; }
    }
}