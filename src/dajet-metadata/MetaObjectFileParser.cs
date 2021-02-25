using DaJet.Metadata.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DaJet.Metadata
{
    public interface IMetaObjectFileParser
    {
        void UseCash(DBNamesCash cash);
        void ParseMetaUuid(StreamReader reader, MetaObject metaObject);
        void ParseMetaObject(StreamReader reader, MetaObject metaObject);
    }
    public sealed class MetaObjectFileParser : IMetaObjectFileParser
    {
        #region "Regular expressions"
        
        private readonly Regex rxUUID = new Regex("[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}"); // Example: eb3dfdc7-58b8-4b1f-b079-368c262364c9
        private readonly Regex rxSpecialUUID = new Regex("^{[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12},\\d+(?:})?,$"); // Example: {3daea016-69b7-4ed4-9453-127911372fe6,0}, | {cf4abea7-37b2-11d4-940f-008048da11f9,5,
        private readonly Regex rxDbName = new Regex("^{\\d,\\d,[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}},\"\\w+\",$"); // Example: {0,0,3df19dbf-efe7-4e31-99ad-fafb59ec1329},"Размещение",
        private readonly Regex rxDbType = new Regex("^{\"[#BSDN]\""); // Example: {"#",1aaea747-a4ba-4fb2-9473-075b1ced620c}, | {"B"}, | {"S",10,0}, | {"D","T"}, | {"N",10,0,1}
        private readonly Regex rxNestedProperties = new Regex("^{888744e1-b616-11d4-9436-004095e12fc7,\\d+[},]$"); // Коллекция реквизитов табличной части любого объекта метаданных look rxSpecialUUID

        // Структура ссылки на объект метаданных
        // {"#",157fa490-4ce9-11d4-9415-008048da11f9, - идентификатор класса объекта метаданных
        // {1,fd8fe814-97e6-42d3-a042-b1e429cfb067}   - внутренний идентификатор объекта метаданных
        // }

        #endregion

        internal delegate void SpecialParser(StreamReader reader, string line, MetaObject metaObject);
        private readonly Dictionary<string, SpecialParser> _SpecialParsers = new Dictionary<string, SpecialParser>();

        private DBNamesCash Cash;

        public MetaObjectFileParser()
        {
            ConfigureParsers();
        }
        private void ConfigureParsers()
        {
            _SpecialParsers.Add("cf4abea7-37b2-11d4-940f-008048da11f9", ParseMetaObjectProperties); // Catalogs properties collection
            _SpecialParsers.Add("932159f9-95b2-4e76-a8dd-8849fe5c5ded", ParseNestedObjects); // Catalogs nested objects collection

            _SpecialParsers.Add("45e46cbc-3e24-4165-8b7b-cc98a6f80211", ParseMetaObjectProperties); // Documents properties collection
            _SpecialParsers.Add("21c53e09-8950-4b5e-a6a0-1054f1bbc274", ParseNestedObjects); // Documents nested objects collection

            _SpecialParsers.Add("31182525-9346-4595-81f8-6f91a72ebe06", ParseMetaObjectProperties); // Коллекция реквизитов плана видов характеристик
            _SpecialParsers.Add("54e36536-7863-42fd-bea3-c5edd3122fdc", ParseNestedObjects); // Коллекция табличных частей плана видов характеристик

            _SpecialParsers.Add("1a1b4fea-e093-470d-94ff-1d2f16cda2ab", ParseMetaObjectProperties); // Коллекция реквизитов плана обмена
            _SpecialParsers.Add("52293f4b-f98c-43ea-a80f-41047ae7ab58", ParseNestedObjects); // Коллекция табличных частей плана обмена

            _SpecialParsers.Add("13134203-f60b-11d5-a3c7-0050bae0a776", ParseMetaObjectDimensions); // Коллекция измерений регистра сведений
            _SpecialParsers.Add("13134202-f60b-11d5-a3c7-0050bae0a776", ParseMetaObjectMeasures); // Коллекция ресурсов регистра сведений
            _SpecialParsers.Add("a2207540-1400-11d6-a3c7-0050bae0a776", ParseMetaObjectProperties); // Коллекция реквизитов регистра сведений

            _SpecialParsers.Add("b64d9a43-1642-11d6-a3c7-0050bae0a776", ParseMetaObjectDimensions); // Коллекция измерений регистра накопления
            _SpecialParsers.Add("b64d9a41-1642-11d6-a3c7-0050bae0a776", ParseMetaObjectMeasures); // Коллекция ресурсов регистра накопления
            _SpecialParsers.Add("b64d9a42-1642-11d6-a3c7-0050bae0a776", ParseMetaObjectProperties); // Коллекция реквизитов регистра накопления

            _SpecialParsers.Add("6e65cbf5-daa8-4d8d-bef8-59723f4e5777", ParseMetaObjectProperties); // Коллекция реквизитов плана счетов
            _SpecialParsers.Add("78bd1243-c4df-46c3-8138-e147465cb9a4", ParseMetaObjectProperties); // Коллекция признаков учёта плана счетов
            // Коллекция признаков учёта субконто плана счетов - не имеет полей в таблице базы данных
            //_SpecialParsers.Add("c70ca527-5042-4cad-a315-dcb4007e32a3", ParseMetaObjectProperties);

            _SpecialParsers.Add("35b63b9d-0adf-4625-a047-10ae874c19a3", ParseMetaObjectDimensions); // Коллекция измерений регистра бухгалтерского учёта
            _SpecialParsers.Add("63405499-7491-4ce3-ac72-43433cbe4112", ParseMetaObjectMeasures); // Коллекция ресурсов регистра бухгалтерского учёта
            _SpecialParsers.Add("9d28ee33-9c7e-4a1b-8f13-50aa9b36607b", ParseMetaObjectProperties); // Коллекция реквизитов регистра бухгалтерского учёта
        }

        public void UseCash(DBNamesCash cash)
        {
            Cash = cash;
        }
        public void ParseMetaUuid(StreamReader reader, MetaObject metaObject)
        {
            _ = reader.ReadLine(); // 1. line
            string line = reader.ReadLine(); // 2. line

            string[] items = line.Split(',');

            string value = (metaObject.TypeName == MetaObjectTypes.Enumeration ? items[1] : items[3]);

            metaObject.MetaUuid = new Guid(value);
        }
        public void ParseMetaObject(StreamReader reader, MetaObject metaObject)
        {
            if (metaObject.TypeName == MetaObjectTypes.Constant)
            {
                ParseConstant(reader, metaObject); return;
            }

            string line = reader.ReadLine(); // 1. line

            line = reader.ReadLine(); // 2. line
            //string uuid = ParseMetaObjectUuid(line, metaObject); // идентификатор объекта метаданных ParseMetaUuid
            _ = reader.ReadLine(); // 3. line
            line = reader.ReadLine(); // 4. line
            if (metaObject.TypeName == MetaObjectTypes.Publication)
            {
                ParseMetaObjectName(line, metaObject); // metaobject's UUID and Name
            }

            line = reader.ReadLine(); // 5. line
            if (metaObject.TypeName == MetaObjectTypes.Publication)
            {
                ParseMetaObjectAlias(line, metaObject); // metaobject's alias
            }
            else
            {
                ParseMetaObjectName(line, metaObject); // metaobject's UUID and Name
            }

            line = reader.ReadLine(); // 6. line
            if (metaObject.TypeName != MetaObjectTypes.Publication)
            {
                ParseMetaObjectAlias(line, metaObject); // metaobject's alias
            }

            _ = reader.ReadLine(); // 7. line

            if (metaObject.TypeName == MetaObjectTypes.Catalog)
            {
                // starts from 8. line
                ParseReferenceOwner(reader, metaObject); // свойство справочника "Владелец"
            }
            else if (metaObject.TypeName == MetaObjectTypes.Document)
            {
                // starts from 8. line
                // TODO: Parse объекты метаданных, которые являются основанием для заполнения текущего
                // starts after count (количество объектов оснований) * 3 (размер ссылки на объект метаданных) + 1 (тэг закрытия блока объектов оснований)
                // TODO: Parse все регистры (информационные, накопления и бухгалтерские), по которым текущий документ выполняет движения.
            }

            int count = 0;
            string UUID = null;
            Match match = null;
            while ((line = reader.ReadLine()) != null)
            {
                match = rxSpecialUUID.Match(line);
                if (!match.Success) continue;

                string[] lines = line.Split(',');
                UUID = lines[0].Replace("{", string.Empty);
                count = int.Parse(lines[1].Replace("}", string.Empty));
                if (count == 0) continue;

                if (_SpecialParsers.ContainsKey(UUID))
                {
                    _SpecialParsers[UUID](reader, line, metaObject);
                }
            }
        }

        #region "Basic properties"

        private string ParseMetaObjectUuid(string line, MetaObject metaObject)
        {
            if (!metaObject.IsReferenceType) return string.Empty;

            string[] items = line.Split(',');

            return (metaObject.TypeName == MetaObjectTypes.Enumeration ? items[1] : items[3]);
        }
        private void ParseMetaObjectName(string line, MetaObject metaObject)
        {
            string[] lines = line.Split(',');
            //string uuid = lines[2].Replace("}", string.Empty); // ParseMetaObjectUuid
            metaObject.Name = lines[3].Replace("\"", string.Empty);
        }
        private void ParseMetaObjectAlias(string line, MetaObject metaObject)
        {
            string[] lines = line.Split(',');
            string alias = lines[2].Replace("}", string.Empty);
            metaObject.Alias = alias.Replace("\"", string.Empty);
        }
        private void ParseReferenceOwner(StreamReader reader, MetaObject metaObject)
        {
            int count = 0;
            string[] lines;

            string line = reader.ReadLine(); // 8. line
            if (line != null)
            {
                lines = line.Split(',');
                count = int.Parse(lines[1].Replace("}", string.Empty));
            }
            if (count == 0) return;

            Match match;
            List<int> owners = new List<int>();
            for (int i = 0; i < count; i++)
            {
                _ = reader.ReadLine();
                line = reader.ReadLine();
                if (line == null) return;

                match = rxUUID.Match(line);
                if (match.Success)
                {
                    if (Cash.ReferenceTypes.TryGetValue(new Guid(match.Value), out MetaObject owner))
                    {
                        owners.Add(owner.TypeCode);
                    }
                }
                _ = reader.ReadLine();
            }

            if (owners.Count > 0)
            {
                MetaProperty property = new MetaProperty
                {
                    PropertyType = (owners.Count == 1) ? owners[0] : (int)DataTypes.Multiple,
                    Name = "Владелец",
                    Field = "OwnerID" // [_OwnerIDRRef] | [_OwnerID_TYPE] + [_OwnerID_RTRef] + [_OwnerID_RRRef]
                    // TODO: add DbField[s] at once ?
                };
                metaObject.Properties.Add(property);
            }
        }

        #endregion

        #region "User defined properties"

        private void ParseMetaObjectMeasures(StreamReader reader, string line, MetaObject metaObject)
        {
            ParseMetaProperties(reader, line, metaObject, PropertyPurpose.Measure);
        }
        private void ParseMetaObjectDimensions(StreamReader reader, string line, MetaObject metaObject)
        {
            ParseMetaProperties(reader, line, metaObject, PropertyPurpose.Dimension);
        }
        private void ParseMetaObjectProperties(StreamReader reader, string line, MetaObject metaObject)
        {
            ParseMetaProperties(reader, line, metaObject, PropertyPurpose.Property);
        }
        private void ParseMetaProperties(StreamReader reader, string line, MetaObject metaObject, PropertyPurpose purpose)
        {
            string[] lines = line.Split(',');
            int count = int.Parse(lines[1].Replace("}", string.Empty));
            Match match;
            string nextLine;
            for (int i = 0; i < count; i++)
            {
                while ((nextLine = reader.ReadLine()) != null)
                {
                    match = rxDbName.Match(nextLine);
                    if (match.Success)
                    {
                        ParseMetaProperty(reader, nextLine, metaObject, purpose);
                        break;
                    }
                }
            }
        }
        private void ParseMetaProperty(StreamReader reader, string line, MetaObject metaObject, PropertyPurpose purpose)
        {
            string[] lines = line.Split(',');
            string fileName = lines[2].Replace("}", string.Empty);
            string objectName = lines[3].Replace("\"", string.Empty);

            if (Cash.Properties.TryGetValue(new Guid(fileName), out MetaProperty property))
            {
                property.Name = objectName;
                property.Purpose = purpose;
                metaObject.Properties.Add(property);
            }
            ParseMetaPropertyTypes(reader, property);
        }
        private void ParseMetaPropertyTypes(StreamReader reader, MetaProperty property)
        {
            string line = reader.ReadLine();
            if (line == null) return;

            while (line != "{\"Pattern\",")
            {
                line = reader.ReadLine();
                if (line == null) return;
            }

            Match match;
            List<int> typeCodes = new List<int>();
            while ((line = reader.ReadLine()) != null)
            {
                match = rxDbType.Match(line);
                if (!match.Success) break;

                int typeCode = (int)DataTypes.NULL;
                string typeName = string.Empty;
                string token = match.Value.Replace("{", string.Empty).Replace("\"", string.Empty);
                switch (token)
                {
                    case MetadataTokens.S: { typeCode = (int)DataTypes.String; break; }
                    case MetadataTokens.B: { typeCode = (int)DataTypes.Boolean; break; }
                    case MetadataTokens.N: { typeCode = (int)DataTypes.Numeric; break; }
                    case MetadataTokens.D: { typeCode = (int)DataTypes.DateTime; break; }
                }
                if (typeCode != (int)DataTypes.NULL)
                {
                    typeCodes.Add(typeCode);
                }
                else
                {
                    string[] lines = line.Split(',');
                    string uuid = lines[1].Replace("}", string.Empty);

                    if (uuid == "e199ca70-93cf-46ce-a54b-6edc88c3a296")
                    {
                        // ХранилищеЗначения - varbinary(max)
                        typeCodes.Add((int)DataTypes.Binary);
                    }
                    else if (uuid == "fc01b5df-97fe-449b-83d4-218a090e681e")
                    {
                        // УникальныйИдентификатор - binary(16)
                        typeCodes.Add((int)DataTypes.UUID);
                    }
                    else if (Cash.MetaReferenceTypes.TryGetValue(new Guid(uuid), out MetaObject type))
                    {
                        typeCodes.Add(type.TypeCode);
                    }
                    else // metaobject reference type file is not parsed yet ?
                    {
                        typeCodes.Add((int)DataTypes.Object);
                    }
                }

                if (typeCodes.Count > 1) break;
            }

            if (typeCodes.Count == 1) property.PropertyType = typeCodes[0];
            else if (typeCodes.Count > 1) property.PropertyType = (int)DataTypes.Multiple;
            else property.PropertyType = (int)DataTypes.NULL;
        }

        #endregion

        #region "Table parts"

        private void ParseNestedObjects(StreamReader reader, string line, MetaObject dbo)
        {
            string[] lines = line.Split(',');
            int count = int.Parse(lines[1]);
            Match match;
            string nextLine;
            for (int i = 0; i < count; i++)
            {
                while ((nextLine = reader.ReadLine()) != null)
                {
                    match = rxDbName.Match(nextLine);
                    if (match.Success)
                    {
                        ParseNestedObject(reader, nextLine, dbo);
                        break;
                    }
                }
            }
        }
        private void ParseNestedObject(StreamReader reader, string line, MetaObject owner)
        {
            string[] lines = line.Split(',');
            string fileName = lines[2].Replace("}", string.Empty);
            string objectName = lines[3].Replace("\"", string.Empty);

            if (Cash.TableParts.TryGetValue(new Guid(fileName), out MetaObject nested))
            {
                nested.Owner = owner;
                nested.Name = objectName;
                //TODO: check what to do here
                //nested.TypeName = dbname.Token;
                //nested.TypeCode = dbname.TypeCode;
                nested.TableName = owner.TableName + nested.TableName;
                owner.MetaObjects.Add(nested);
            }
            ParseNestedMetaProperties(reader, nested);
        }
        private void ParseNestedMetaProperties(StreamReader reader, MetaObject dbo)
        {
            string line;
            Match match;
            while ((line = reader.ReadLine()) != null)
            {
                match = rxNestedProperties.Match(line);
                if (match.Success)
                {
                    ParseMetaProperties(reader, line, dbo, PropertyPurpose.Property);
                    break;
                }
            }
        }

        #endregion

        #region "Constants"

        private void ParseConstant(StreamReader reader, MetaObject metaObject)
        {
            _ = reader.ReadLine(); // 1. line
            _ = reader.ReadLine(); // 2. line
            _ = reader.ReadLine(); // 3. line
            _ = reader.ReadLine(); // 4. line
            _ = reader.ReadLine(); // 5. line
            string line = reader.ReadLine(); // 6. line

            string[] lines = line.Split(',');
            string uuid = lines[2].TrimEnd('}');
            metaObject.Name = lines[3].Trim('"');

            if (Cash.Properties.TryGetValue(new Guid(uuid), out MetaProperty property))
            {
                metaObject.Properties.Add(property);
            }
            ParseMetaPropertyTypes(reader, property);
        }

        #endregion
    }
}