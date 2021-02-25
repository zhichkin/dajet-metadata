using DaJet.Metadata.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DaJet.Metadata
{
    public sealed class DBNamesCash
    {
        ///<summary>Соответствие имён файлов значимым типам метаданных</summary>
        public Dictionary<Guid, MetaObject> ValueTypes { get; } = new Dictionary<Guid, MetaObject>();
        ///<summary>Соответствие имён файлов ссылочным типам метаданных</summary>
        public Dictionary<Guid, MetaObject> ReferenceTypes { get; } = new Dictionary<Guid, MetaObject>();
        ///<summary>Соответствие идентификаторов объектов метаданных типа "Табличная часть"</summary>
        public Dictionary<Guid, MetaObject> TableParts { get; } = new Dictionary<Guid, MetaObject>();
        ///<summary>Соответствие идентификаторов объектов метаданных типа "Реквизит", "Измерение", "Ресурс"</summary>
        public Dictionary<Guid, MetaProperty> Properties { get; } = new Dictionary<Guid, MetaProperty>();
        ///<summary>Соответствие идентификаторов объектов метаданных ссылочного типа</summary>
        public ConcurrentDictionary<Guid, MetaObject> MetaReferenceTypes { get; } = new ConcurrentDictionary<Guid, MetaObject>();
    }
}