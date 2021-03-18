using System;
using System.Collections.Generic;

namespace DaJet.Metadata.Model
{
    ///<summary>Класс для описания объектов метаданных (справочников, документов, регистров и т.п.)</summary>
    public class MetadataObject
    {
        ///<summary>Идентификатор объекта метаданных из файла объекта метаданных</summary>
        public Guid Uuid { get; set; }
        ///<summary>Имя файла объекта метаданных в таблице Config и DBNames</summary>
        public Guid FileName { get; set; }
        ///<summary>Целочисленный идентификатор объекта метаданных из файла DBNames</summary>
        public int TypeCode { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; } = string.Empty;
        public string TableName { get; set; }
        public List<MetadataProperty> Properties { get; set; } = new List<MetadataProperty>();
        public List<MetadataObject> MetadataObjects { get; set; } = new List<MetadataObject>();
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