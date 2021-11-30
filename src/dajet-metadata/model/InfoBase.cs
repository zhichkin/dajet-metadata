using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DaJet.Metadata.Model
{
    public sealed class InfoBase : MetadataObject
    {
        public ConfigInfo ConfigInfo { get; set; }
        public int YearOffset { get; set; }
        public int PlatformRequiredVersion { get; set; }
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
        
        ///<summary>Соответствие идентификаторов объектов метаданных типа "ТабличнаяЧасть"</summary>
        public Dictionary<Guid, ApplicationObject> TableParts { get; } = new Dictionary<Guid, ApplicationObject>();
        ///<summary>Соответствие идентификаторов объектов метаданных типа "Реквизит", "Измерение", "Ресурс"</summary>
        public Dictionary<Guid, MetadataProperty> Properties { get; } = new Dictionary<Guid, MetadataProperty>();
        ///<summary>Коллекция общих свойств конфигурации</summary>
        public Dictionary<Guid, SharedProperty> SharedProperties { get; set; } = new Dictionary<Guid, SharedProperty>();
        ///<summary>Коллекция определяемых типов конфигурации</summary>
        public Dictionary<Guid, CompoundType> CompoundTypes { get; set; } = new Dictionary<Guid, CompoundType>();
        ///<summary>Коллекция типов определяемых характеристиками</summary>
        public Dictionary<Guid, Characteristic> CharacteristicTypes { get; set; } = new Dictionary<Guid, Characteristic>();
        ///<summary>Соответствие идентификаторов объектов метаданных ссылочного типа</summary>
        public ConcurrentDictionary<Guid, ApplicationObject> ReferenceTypeUuids { get; } = new ConcurrentDictionary<Guid, ApplicationObject>();
        ///<summary>Соответствие кодов типов объектов метаданных ссылочного типа</summary>
        public ConcurrentDictionary<int, ApplicationObject> ReferenceTypeCodes { get; } = new ConcurrentDictionary<int, ApplicationObject>();

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

        public void ApplyCompoundType(DataTypeInfo typeInfo, CompoundType compound)
        {
            // TODO: add internal flags field to the DataTypeInfo class so as to use bitwise operations
            if (!typeInfo.CanBeString && compound.TypeInfo.CanBeString) typeInfo.CanBeString = true;
            if (!typeInfo.CanBeBoolean && compound.TypeInfo.CanBeBoolean) typeInfo.CanBeBoolean = true;
            if (!typeInfo.CanBeNumeric && compound.TypeInfo.CanBeNumeric) typeInfo.CanBeNumeric = true;
            if (!typeInfo.CanBeDateTime && compound.TypeInfo.CanBeDateTime) typeInfo.CanBeDateTime = true;
            if (!typeInfo.CanBeReference && compound.TypeInfo.CanBeReference) typeInfo.CanBeReference = true;
            if (!typeInfo.IsUuid && compound.TypeInfo.IsUuid) typeInfo.IsUuid = true;
            if (!typeInfo.IsValueStorage && compound.TypeInfo.IsValueStorage) typeInfo.IsValueStorage = true;
            if (!typeInfo.IsBinary && compound.TypeInfo.IsBinary) typeInfo.IsBinary = true;
        }
        public void ApplyCharacteristic(DataTypeInfo typeInfo, Characteristic characteristic)
        {
            // TODO: add internal flags field to the DataTypeInfo class so as to use bitwise operations
            if (!typeInfo.CanBeString && characteristic.TypeInfo.CanBeString) typeInfo.CanBeString = true;
            if (!typeInfo.CanBeBoolean && characteristic.TypeInfo.CanBeBoolean) typeInfo.CanBeBoolean = true;
            if (!typeInfo.CanBeNumeric && characteristic.TypeInfo.CanBeNumeric) typeInfo.CanBeNumeric = true;
            if (!typeInfo.CanBeDateTime && characteristic.TypeInfo.CanBeDateTime) typeInfo.CanBeDateTime = true;
            if (!typeInfo.CanBeReference && characteristic.TypeInfo.CanBeReference) typeInfo.CanBeReference = true;
            if (!typeInfo.IsUuid && characteristic.TypeInfo.IsUuid) typeInfo.IsUuid = true;
            if (!typeInfo.IsValueStorage && characteristic.TypeInfo.IsValueStorage) typeInfo.IsValueStorage = true;
            if (!typeInfo.IsBinary && characteristic.TypeInfo.IsBinary) typeInfo.IsBinary = true;
        }

        ///<summary>Функция возвращает объект метаданных по его полному имени или null, если не найден.</summary>
        ///<param name="metadataName">Полное имя объекта метаданных, например, "Справочник.Номенклатура" или "Документ.ЗаказКлиента.Товары".</param>
        public ApplicationObject GetApplicationObjectByName(string metadataName)
        {
            string[] names = metadataName.Split('.');

            string typeName = names[0];
            string objectName = names[1];
            string tablePartName = null;
            if (names.Length == 3)
            {
                tablePartName = names[2];
            }

            ApplicationObject metaObject = null;
            Dictionary<Guid, ApplicationObject> collection = null;

            if (typeName == "Справочник") collection = Catalogs;
            else if (typeName == "Документ") collection = Documents;
            else if (typeName == "Константа") collection = Constants;
            else if (typeName == "ПланСчетов") collection = Accounts;
            else if (typeName == "ПланОбмена") collection = Publications;
            else if (typeName == "Перечисление") collection = Enumerations;
            else if (typeName == "РегистрСведений") collection = InformationRegisters;
            else if (typeName == "РегистрНакопления") collection = AccumulationRegisters;
            else if (typeName == "РегистрБухгалтерии") collection = AccountingRegisters;
            else if (typeName == "ПланВидовХарактеристик") collection = Characteristics;
            if (collection == null)
            {
                return null;
            }

            metaObject = collection.Values.Where(o => o.Name == objectName).FirstOrDefault();
            if (metaObject == null)
            {
                return null;
            }

            if (tablePartName != null)
            {
                return metaObject.TableParts.Where(t => t.Name == tablePartName).FirstOrDefault();
            }

            return metaObject;
        }
    }
}