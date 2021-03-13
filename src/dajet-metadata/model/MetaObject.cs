using System;
using System.Collections.Generic;

namespace DaJet.Metadata.Model
{
    public class MetaObject
    {
        ///<summary>Имя файла объекта метаданных в таблице Config и DBNames</summary>
        public Guid UUID { get; set; } // TODO: change property name to FileName
        ///<summary>Идентификатор объекта метаданных из файла объекта метаданных</summary>
        public Guid MetaUuid { get; set; } // TODO: change property name to Uuid ?
        public string TypeName { get; set; }
        public int TypeCode { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; } = string.Empty;
        public string Schema { get; set; } = "dbo";
        public string TableName { get; set; }
        public MetaObject Owner { get; set; } // owner of table part (this object is a table part)
        public List<MetaProperty> Properties { get; set; } = new List<MetaProperty>();
        public List<MetaObject> MetaObjects { get; set; } = new List<MetaObject>(); // table parts or other dependent objects (table of changes, totals, etc.)
        public bool IsReferenceType
        {
            get
            {
                return TypeName == MetaObjectTypes.Account
                    || TypeName == MetaObjectTypes.Catalog
                    || TypeName == MetaObjectTypes.Document
                    || TypeName == MetaObjectTypes.Enumeration
                    || TypeName == MetaObjectTypes.Publication
                    || TypeName == MetaObjectTypes.Characteristic;
            }
        }
        public override string ToString() { return string.Format("{0}.{1}", TypeName, Name); }
    }
}