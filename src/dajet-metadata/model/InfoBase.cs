using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DaJet.Metadata.Model
{
    public sealed class InfoBase : MetadataObject
    {
        public List<Dictionary<Guid, ApplicationObject>> Registers { get; private set; }
        public List<Dictionary<Guid, ApplicationObject>> ValueTypes { get; private set; }
        public List<Dictionary<Guid, ApplicationObject>> ReferenceTypes { get; private set; }
        public Dictionary<Type, Dictionary<Guid, ApplicationObject>> AllTypes { get; private set; }
        public InfoBase()
        {
            Registers = new List<Dictionary<Guid, ApplicationObject>>()
            {
                AccountingRegisters,
                InformationRegisters,
                AccumulationRegisters
            };
            ValueTypes = new List<Dictionary<Guid, ApplicationObject>>()
            {
                Constants,
                AccountingRegisters,
                InformationRegisters,
                AccumulationRegisters
            };
            ReferenceTypes = new List<Dictionary<Guid, ApplicationObject>>()
            {
                Accounts,
                Catalogs,
                Documents,
                Enumerations,
                Publications,
                Characteristics
            };
            AllTypes = new Dictionary<Type, Dictionary<Guid, ApplicationObject>>()
            {
                { typeof(Account), Accounts },
                { typeof(AccountingRegister), AccountingRegisters },
                { typeof(AccumulationRegister), AccumulationRegisters },
                { typeof(Catalog), Catalogs },
                { typeof(Characteristic), Characteristics },
                { typeof(Constant), Constants },
                { typeof(Document), Documents },
                { typeof(Enumeration), Enumerations },
                { typeof(InformationRegister), InformationRegisters },
                { typeof(Publication), Publications }
            };
        }
        public ConfigInfo ConfigInfo { get; set; }
        ///<summary>Соответствие идентификаторов объектов метаданных типа "ТабличнаяЧасть"</summary>
        public Dictionary<Guid, ApplicationObject> TableParts { get; } = new Dictionary<Guid, ApplicationObject>();
        ///<summary>Соответствие идентификаторов объектов метаданных типа "Реквизит", "Измерение", "Ресурс"</summary>
        public Dictionary<Guid, MetadataProperty> Properties { get; } = new Dictionary<Guid, MetadataProperty>();
        ///<summary>Коллекция общих свойств конфигурации</summary>
        public Dictionary<Guid, SharedProperty> SharedProperties { get; set; } = new Dictionary<Guid, SharedProperty>();
        ///<summary>Соответствие идентификаторов объектов метаданных ссылочного типа</summary>
        public ConcurrentDictionary<Guid, ApplicationObject> MetaReferenceTypes { get; } = new ConcurrentDictionary<Guid, ApplicationObject>();

        #region "Коллекции ссылочных типов данных (Guid - имя файла объекта метаданных в таблице Config)"

        ///<summary>Коллекция планов счетов (ссылочный тип данных)</summary>
        public Dictionary<Guid, ApplicationObject> Accounts { get; } = new Dictionary<Guid, ApplicationObject>();
        ///<summary>Коллекция справочников (ссылочный тип данных)</summary>
        public Dictionary<Guid, ApplicationObject> Catalogs { get; } = new Dictionary<Guid, ApplicationObject>();
        ///<summary>Коллекция документов (ссылочный тип данных)</summary>
        public Dictionary<Guid, ApplicationObject> Documents { get; } = new Dictionary<Guid, ApplicationObject>();
        ///<summary>Коллекция перечислений (ссылочный тип данных)</summary>
        public Dictionary<Guid, ApplicationObject> Enumerations { get; } = new Dictionary<Guid, ApplicationObject>();
        ///<summary>Коллекция планов обмена (ссылочный тип данных)</summary>
        public Dictionary<Guid, ApplicationObject> Publications { get; } = new Dictionary<Guid, ApplicationObject>();
        ///<summary>Коллекция планов видов характеристик (ссылочный тип данных)</summary>
        public Dictionary<Guid, ApplicationObject> Characteristics { get; } = new Dictionary<Guid, ApplicationObject>();

        #endregion

        #region "Коллекции значимых типов данных (Guid - имя файла объекта метаданных в таблице Config)"

        ///<summary>Коллекция констант (значимый тип данных)</summary>
        public Dictionary<Guid, ApplicationObject> Constants { get; } = new Dictionary<Guid, ApplicationObject>();
        ///<summary>Коллекция регистров бухгалтерии (значимый тип данных)</summary>
        public Dictionary<Guid, ApplicationObject> AccountingRegisters { get; } = new Dictionary<Guid, ApplicationObject>();
        ///<summary>Коллекция регистров сведений (значимый тип данных)</summary>
        public Dictionary<Guid, ApplicationObject> InformationRegisters { get; } = new Dictionary<Guid, ApplicationObject>();
        ///<summary>Коллекция регистров накопления (значимый тип данных)</summary>
        public Dictionary<Guid, ApplicationObject> AccumulationRegisters { get; } = new Dictionary<Guid, ApplicationObject>();

        #endregion
    }
}