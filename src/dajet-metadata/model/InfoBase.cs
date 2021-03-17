using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DaJet.Metadata.Model
{
    public sealed class InfoBase
    {
        public List<Dictionary<Guid, MetadataObject>> ValueTypes { get; private set; }
        public List<Dictionary<Guid, MetadataObject>> ReferenceTypes { get; private set; }
        public InfoBase()
        {
            ValueTypes = new List<Dictionary<Guid, MetadataObject>>()
            {
                Constants,
                AccountingRegisters,
                InformationRegisters,
                AccumulationRegisters
            };
            ReferenceTypes = new List<Dictionary<Guid, MetadataObject>>()
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
        public Dictionary<Guid, MetadataObject> TableParts { get; } = new Dictionary<Guid, MetadataObject>();
        ///<summary>Соответствие идентификаторов объектов метаданных типа "Реквизит", "Измерение", "Ресурс"</summary>
        public Dictionary<Guid, MetadataProperty> Properties { get; } = new Dictionary<Guid, MetadataProperty>();
        ///<summary>Соответствие идентификаторов объектов метаданных ссылочного типа</summary>
        public ConcurrentDictionary<Guid, MetadataObject> MetaReferenceTypes { get; } = new ConcurrentDictionary<Guid, MetadataObject>();

        #region "Коллекции ссылочных типов данных (Guid - имя файла объекта метаданных в таблице Config)"

        ///<summary>Коллекция планов счетов (ссылочный тип данных)</summary>
        public Dictionary<Guid, MetadataObject> Accounts { get; } = new Dictionary<Guid, MetadataObject>();
        ///<summary>Коллекция справочников (ссылочный тип данных)</summary>
        public Dictionary<Guid, MetadataObject> Catalogs { get; } = new Dictionary<Guid, MetadataObject>();
        ///<summary>Коллекция документов (ссылочный тип данных)</summary>
        public Dictionary<Guid, MetadataObject> Documents { get; } = new Dictionary<Guid, MetadataObject>();
        ///<summary>Коллекция перечислений (ссылочный тип данных)</summary>
        public Dictionary<Guid, MetadataObject> Enumerations { get; } = new Dictionary<Guid, MetadataObject>();
        ///<summary>Коллекция планов обмена (ссылочный тип данных)</summary>
        public Dictionary<Guid, MetadataObject> Publications { get; } = new Dictionary<Guid, MetadataObject>();
        ///<summary>Коллекция планов видов характеристик (ссылочный тип данных)</summary>
        public Dictionary<Guid, MetadataObject> Characteristics { get; } = new Dictionary<Guid, MetadataObject>();

        #endregion

        #region "Коллекции значимых типов данных (Guid - имя файла объекта метаданных в таблице Config)"

        ///<summary>Коллекция констант (значимый тип данных)</summary>
        public Dictionary<Guid, MetadataObject> Constants { get; } = new Dictionary<Guid, MetadataObject>();
        ///<summary>Коллекция регистров бухгалтерии (значимый тип данных)</summary>
        public Dictionary<Guid, MetadataObject> AccountingRegisters { get; } = new Dictionary<Guid, MetadataObject>();
        ///<summary>Коллекция регистров сведений (значимый тип данных)</summary>
        public Dictionary<Guid, MetadataObject> InformationRegisters { get; } = new Dictionary<Guid, MetadataObject>();
        ///<summary>Коллекция регистров накопления (значимый тип данных)</summary>
        public Dictionary<Guid, MetadataObject> AccumulationRegisters { get; } = new Dictionary<Guid, MetadataObject>();

        #endregion
    }
}