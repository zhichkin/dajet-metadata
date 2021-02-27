using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DaJet.Metadata.Model
{
    public sealed class InfoBase
    {
        public List<Dictionary<Guid, MetaObject>> ValueTypes { get; private set; }
        public List<Dictionary<Guid, MetaObject>> ReferenceTypes { get; private set; }
        public InfoBase()
        {
            ValueTypes = new List<Dictionary<Guid, MetaObject>>()
            {
                Constants,
                AccountingRegisters,
                InformationRegisters,
                AccumulationRegisters
            };
            ReferenceTypes = new List<Dictionary<Guid, MetaObject>>()
            {
                Accounts,
                Catalogs,
                Documents,
                Enumerations,
                Publications,
                Characteristics
            };
        }
        public ConfigInfo ConfigInfo { get; set; }
        public Dictionary<Guid, MetaObject> TableParts { get; } = new Dictionary<Guid, MetaObject>();
        ///<summary>Соответствие идентификаторов объектов метаданных типа "Реквизит", "Измерение", "Ресурс"</summary>
        public Dictionary<Guid, MetaProperty> Properties { get; } = new Dictionary<Guid, MetaProperty>();
        ///<summary>Соответствие идентификаторов объектов метаданных ссылочного типа</summary>
        public ConcurrentDictionary<Guid, MetaObject> MetaReferenceTypes { get; } = new ConcurrentDictionary<Guid, MetaObject>();

        #region "Коллекции ссылочных типов данных (Guid - имя файла объекта метаданных в таблице Config)"

        ///<summary>Коллекция планов счетов (ссылочный тип данных)</summary>
        public Dictionary<Guid, MetaObject> Accounts { get; } = new Dictionary<Guid, MetaObject>();
        ///<summary>Коллекция справочников (ссылочный тип данных)</summary>
        public Dictionary<Guid, MetaObject> Catalogs { get; } = new Dictionary<Guid, MetaObject>();
        ///<summary>Коллекция документов (ссылочный тип данных)</summary>
        public Dictionary<Guid, MetaObject> Documents { get; } = new Dictionary<Guid, MetaObject>();
        ///<summary>Коллекция перечислений (ссылочный тип данных)</summary>
        public Dictionary<Guid, MetaObject> Enumerations { get; } = new Dictionary<Guid, MetaObject>();
        ///<summary>Коллекция планов обмена (ссылочный тип данных)</summary>
        public Dictionary<Guid, MetaObject> Publications { get; } = new Dictionary<Guid, MetaObject>();
        ///<summary>Коллекция планов видов характеристик (ссылочный тип данных)</summary>
        public Dictionary<Guid, MetaObject> Characteristics { get; } = new Dictionary<Guid, MetaObject>();

        #endregion

        #region "Коллекции значимых типов данных (Guid - имя файла объекта метаданных в таблице Config)"

        ///<summary>Коллекция констант (значимый тип данных)</summary>
        public Dictionary<Guid, MetaObject> Constants { get; } = new Dictionary<Guid, MetaObject>();
        ///<summary>Коллекция регистров бухгалтерии (значимый тип данных)</summary>
        public Dictionary<Guid, MetaObject> AccountingRegisters { get; } = new Dictionary<Guid, MetaObject>();
        ///<summary>Коллекция регистров сведений (значимый тип данных)</summary>
        public Dictionary<Guid, MetaObject> InformationRegisters { get; } = new Dictionary<Guid, MetaObject>();
        ///<summary>Коллекция регистров накопления (значимый тип данных)</summary>
        public Dictionary<Guid, MetaObject> AccumulationRegisters { get; } = new Dictionary<Guid, MetaObject>();

        #endregion
    }
}