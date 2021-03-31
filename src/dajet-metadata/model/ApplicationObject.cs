using System;
using System.Collections.Generic;

namespace DaJet.Metadata.Model
{
    ///<summary>Класс для описания объектов метаданных (справочников, документов, регистров и т.п.)</summary>
    public class ApplicationObject : MetadataObject
    {
        ///<summary>Целочисленный идентификатор объекта метаданных из файла DBNames</summary>
        public int TypeCode { get; set; }
        public string TableName { get; set; }
        public List<MetadataProperty> Properties { get; set; } = new List<MetadataProperty>();
        public List<ApplicationObject> ApplicationObjects { get; set; } = new List<ApplicationObject>();
        public bool IsReferenceType
        {
            get
            {
                Type thisType = GetType();
                return thisType == typeof(Account)
                    || thisType == typeof(Catalog)
                    || thisType == typeof(Document)
                    || thisType == typeof(Enumeration)
                    || thisType == typeof(Publication)
                    || thisType == typeof(Characteristic);
            }
        }
        public override string ToString() { return string.Format("{0}.{1}", GetType().Name, Name); }
    }
}