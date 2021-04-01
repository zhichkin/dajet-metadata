using DaJet.Metadata.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DaJet.Metadata
{
    public interface IApplicationObjectFileParser
    {
        void UseInfoBase(InfoBase infoBase);
        void ParseMetaUuid(StreamReader reader, ApplicationObject metaObject);
        void ParseApplicationObject(StreamReader reader, ApplicationObject metaObject);
        DatabaseProvider DatabaseProvider { get; }
        void UseDatabaseProvider(DatabaseProvider databaseProvider);

        void SkipLines(StreamReader reader, int count);
        void ParseName(StreamReader reader, ApplicationObject metaObject);
        void ParseAlias(StreamReader reader, ApplicationObject metaObject);
        void ParsePropertiesAndTableParts(StreamReader reader, ApplicationObject metaObject);
    }
    public sealed class ApplicationObjectFileParser : IApplicationObjectFileParser
    {
        #region "Regular expressions"
        
        private readonly Regex rxUUID = new Regex("[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}"); // Example: eb3dfdc7-58b8-4b1f-b079-368c262364c9
        private readonly Regex rxSpecialUUID = new Regex("^{[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12},\\d+(?:})?,$"); // Example: {3daea016-69b7-4ed4-9453-127911372fe6,0}, | {cf4abea7-37b2-11d4-940f-008048da11f9,5,
        private readonly Regex rxDbName = new Regex("^{\\d,\\d,[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}},\"\\w+\",$"); // Example: {0,0,3df19dbf-efe7-4e31-99ad-fafb59ec1329},"Размещение",
        private readonly Regex rxDbType = new Regex("^{\"[#BSDN]\""); // Example: {"#",1aaea747-a4ba-4fb2-9473-075b1ced620c}, | {"B"}, | {"S",10,0}, | {"D","T"}, | {"N",10,0,1}
        private readonly Regex rxNestedProperties = new Regex("^{888744e1-b616-11d4-9436-004095e12fc7,\\d+[},]$"); // Коллекция реквизитов табличной части любого объекта метаданных look rxSpecialUUID

        // Структура блока описания ссылки на объект метаданных
        // {"#",157fa490-4ce9-11d4-9415-008048da11f9, - идентификатор класса типа "ОбъектМетаданных"
        // {1,fd8fe814-97e6-42d3-a042-b1e429cfb067}   - идентификатор объекта метаданных из файла таблицы Config
        // }

        #endregion

        private InfoBase InfoBase;
        private readonly IMetadataManager MetadataManager = new MetadataManager();
        private readonly Dictionary<Type, IFileParser> FileParsers = new Dictionary<Type, IFileParser>();
        private delegate void SpecialParser(StreamReader reader, string line, ApplicationObject metaObject);
        private readonly Dictionary<string, SpecialParser> _SpecialParsers = new Dictionary<string, SpecialParser>();

        public ApplicationObjectFileParser()
        {
            ConfigureParsers();
            ConfigureFileParsers();
        }
        public DatabaseProvider DatabaseProvider { get; private set; } = DatabaseProvider.SQLServer;
        public void UseDatabaseProvider(DatabaseProvider databaseProvider)
        {
            DatabaseProvider = databaseProvider;
            MetadataManager.UseDatabaseProvider(DatabaseProvider);
        }
        private void ConfigureParsers()
        {
            _SpecialParsers.Add("cf4abea7-37b2-11d4-940f-008048da11f9", ParseApplicationObjectProperties); // Catalogs properties collection
            _SpecialParsers.Add("932159f9-95b2-4e76-a8dd-8849fe5c5ded", ParseNestedObjects); // Catalogs nested objects collection

            _SpecialParsers.Add("45e46cbc-3e24-4165-8b7b-cc98a6f80211", ParseApplicationObjectProperties); // Documents properties collection
            _SpecialParsers.Add("21c53e09-8950-4b5e-a6a0-1054f1bbc274", ParseNestedObjects); // Documents nested objects collection

            _SpecialParsers.Add("31182525-9346-4595-81f8-6f91a72ebe06", ParseApplicationObjectProperties); // Коллекция реквизитов плана видов характеристик
            _SpecialParsers.Add("54e36536-7863-42fd-bea3-c5edd3122fdc", ParseNestedObjects); // Коллекция табличных частей плана видов характеристик

            _SpecialParsers.Add("1a1b4fea-e093-470d-94ff-1d2f16cda2ab", ParseApplicationObjectProperties); // Коллекция реквизитов плана обмена
            _SpecialParsers.Add("52293f4b-f98c-43ea-a80f-41047ae7ab58", ParseNestedObjects); // Коллекция табличных частей плана обмена

            _SpecialParsers.Add("13134203-f60b-11d5-a3c7-0050bae0a776", ParseApplicationObjectDimensions); // Коллекция измерений регистра сведений
            _SpecialParsers.Add("13134202-f60b-11d5-a3c7-0050bae0a776", ParseApplicationObjectMeasures); // Коллекция ресурсов регистра сведений
            _SpecialParsers.Add("a2207540-1400-11d6-a3c7-0050bae0a776", ParseApplicationObjectProperties); // Коллекция реквизитов регистра сведений

            _SpecialParsers.Add("b64d9a43-1642-11d6-a3c7-0050bae0a776", ParseApplicationObjectDimensions); // Коллекция измерений регистра накопления
            _SpecialParsers.Add("b64d9a41-1642-11d6-a3c7-0050bae0a776", ParseApplicationObjectMeasures); // Коллекция ресурсов регистра накопления
            _SpecialParsers.Add("b64d9a42-1642-11d6-a3c7-0050bae0a776", ParseApplicationObjectProperties); // Коллекция реквизитов регистра накопления

            _SpecialParsers.Add("6e65cbf5-daa8-4d8d-bef8-59723f4e5777", ParseApplicationObjectProperties); // Коллекция реквизитов плана счетов
            _SpecialParsers.Add("78bd1243-c4df-46c3-8138-e147465cb9a4", ParseApplicationObjectProperties); // Коллекция признаков учёта плана счетов
            // Коллекция признаков учёта субконто плана счетов - не имеет полей в таблице базы данных
            //_SpecialParsers.Add("c70ca527-5042-4cad-a315-dcb4007e32a3", ParseApplicationObjectProperties);

            _SpecialParsers.Add("35b63b9d-0adf-4625-a047-10ae874c19a3", ParseApplicationObjectDimensions); // Коллекция измерений регистра бухгалтерского учёта
            _SpecialParsers.Add("63405499-7491-4ce3-ac72-43433cbe4112", ParseApplicationObjectMeasures); // Коллекция ресурсов регистра бухгалтерского учёта
            _SpecialParsers.Add("9d28ee33-9c7e-4a1b-8f13-50aa9b36607b", ParseApplicationObjectProperties); // Коллекция реквизитов регистра бухгалтерского учёта
        }
        private void ConfigureFileParsers()
        {
            FileParsers.Add(typeof(Account), null);
            FileParsers.Add(typeof(AccountingRegister), null);
            FileParsers.Add(typeof(AccumulationRegister), null);
            FileParsers.Add(typeof(Catalog), null);
            FileParsers.Add(typeof(Characteristic), null);
            FileParsers.Add(typeof(Constant), null);
            FileParsers.Add(typeof(Document), new DocumentFileParser(MetadataManager, this));
            FileParsers.Add(typeof(Enumeration), null);
            FileParsers.Add(typeof(InformationRegister), null);
            FileParsers.Add(typeof(Publication), null);
        }

        public void UseInfoBase(InfoBase infoBase)
        {
            InfoBase = infoBase;
            foreach (var kvp in FileParsers)
            {
                if (kvp.Value == null) continue;
                kvp.Value.UseInfoBase(InfoBase);
            }
        }
        public void ParseMetaUuid(StreamReader reader, ApplicationObject metaObject)
        {
            _ = reader.ReadLine(); // 1. line
            string line = reader.ReadLine(); // 2. line

            string[] items = line.Split(',');

            string value = (metaObject.GetType() == typeof(Enumeration) ? items[1] : items[3]);

            metaObject.Uuid = new Guid(value);
        }
        public void ParseApplicationObject(StreamReader reader, ApplicationObject metaObject)
        {
            if (FileParsers.TryGetValue(metaObject.GetType(), out IFileParser parser))
            {
                if (parser != null)
                {
                    parser.Parse(reader, metaObject);
                    return;
                }
            }

            if (metaObject.GetType() == typeof(Constant))
            {
                ParseConstant(reader, metaObject);
                return;
            }

            SkipLines(reader, 3); // 1-3 lines
            
            if (metaObject.GetType() == typeof(Publication))
            {
                ParseName(reader, metaObject); // 4. line - metaobject's UUID and Name
                ParseAlias(reader, metaObject); // 5. line - metaobject's alias
                ParseIsDistributed(reader.ReadLine(), metaObject); // 6. line
            }
            else
            {
                SkipLines(reader, 1); // 4. line
                ParseName(reader, metaObject); // 5. line - metaobject's UUID and Name
                ParseAlias(reader, metaObject); // 6. line - metaobject's alias
            }

            SkipLines(reader, 1); // 7. line

            if (metaObject.GetType() == typeof(Catalog))
            {
                ParseReferenceOwner(reader, metaObject); // 8. line - свойство справочника "Владелец"
            }

            ParsePropertiesAndTableParts(reader, metaObject);
        }

        #region "Basic properties"

        private void ParseIsDistributed(string line, ApplicationObject metaObject)
        {
            if (!(metaObject is Publication publication)) return;
            int value = 0;
            if (line.Length > 1)
            {
                _ = int.TryParse(line.Substring(line.Length - 2, 1), out value);
            }
            publication.IsDistributed = (value == 1);
        }

        private void ParseReferenceOwner(StreamReader reader, ApplicationObject metaObject)
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
                    Guid uuid = new Guid(match.Value);
                    foreach (var collection in InfoBase.ReferenceTypes)
                    {
                        if (collection.TryGetValue(uuid, out ApplicationObject owner))
                        {
                            owners.Add(owner.TypeCode);
                            break;
                        }
                    }
                }
                _ = reader.ReadLine();
            }

            if (owners.Count > 0)
            {
                MetadataProperty property = new MetadataProperty
                {
                    Name = "Владелец",
                    FileName = Guid.Empty,
                    DbName = (DatabaseProvider == DatabaseProvider.SQLServer ? "_OwnerID" : "_OwnerID".ToLowerInvariant()),
                    Purpose = PropertyPurpose.System, // PropertyPurpose.Hierarchy - !?
                };
                property.PropertyType.CanBeReference = true;
                property.PropertyType.ReferenceTypeCode = (owners.Count == 1) ? owners[0] : 0; // single or multiple type
                if (property.PropertyType.IsMultipleType)
                {
                    // Multiple value type 

                    property.Fields.Add(new DatabaseField()
                    {
                        Name = (DatabaseProvider == DatabaseProvider.SQLServer ? "_OwnerID_TYPE" : "_OwnerID_TYPE".ToLowerInvariant()),
                        Length = 1,
                        TypeName = "binary",
                        Scale = 0,
                        Precision = 0,
                        IsNullable = false,
                        KeyOrdinal = 0,
                        IsPrimaryKey = false,
                        Purpose = FieldPurpose.Discriminator
                    });
                    property.Fields.Add(new DatabaseField()
                    {
                        Name = (DatabaseProvider == DatabaseProvider.SQLServer ? "_OwnerID_RTRef" : "_OwnerID_RTRef".ToLowerInvariant()),
                        Length = 4,
                        TypeName = "binary",
                        Scale = 0,
                        Precision = 0,
                        IsNullable = false,
                        KeyOrdinal = 0,
                        IsPrimaryKey = false,
                        Purpose = FieldPurpose.TypeCode
                    });
                    property.Fields.Add(new DatabaseField()
                    {
                        Name = (DatabaseProvider == DatabaseProvider.SQLServer ? "_OwnerID_RRRef" : "_OwnerID_RRRef".ToLowerInvariant()),
                        Length = 16,
                        TypeName = "binary",
                        Scale = 0,
                        Precision = 0,
                        IsNullable = false,
                        KeyOrdinal = 0,
                        IsPrimaryKey = false,
                        Purpose = FieldPurpose.Object
                    });
                }
                else // Single value type
                {
                    property.Fields.Add(new DatabaseField()
                    {
                        Name = (DatabaseProvider == DatabaseProvider.SQLServer ? "_OwnerIDRRef" : "_OwnerIDRRef".ToLowerInvariant()),
                        Length = 16,
                        TypeName = "binary",
                        Scale = 0,
                        Precision = 0,
                        IsNullable = false,
                        KeyOrdinal = 0,
                        IsPrimaryKey = false,
                        Purpose = FieldPurpose.Value
                    });
                }
                metaObject.Properties.Add(property);
            }
        }

        #endregion

        #region "User defined properties"

        private void ParseApplicationObjectMeasures(StreamReader reader, string line, ApplicationObject metaObject)
        {
            ParseMetaProperties(reader, line, metaObject, PropertyPurpose.Measure);
        }
        private void ParseApplicationObjectDimensions(StreamReader reader, string line, ApplicationObject metaObject)
        {
            ParseMetaProperties(reader, line, metaObject, PropertyPurpose.Dimension);
        }
        private void ParseApplicationObjectProperties(StreamReader reader, string line, ApplicationObject metaObject)
        {
            ParseMetaProperties(reader, line, metaObject, PropertyPurpose.Property);
        }
        private void ParseMetaProperties(StreamReader reader, string line, ApplicationObject metaObject, PropertyPurpose purpose)
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
                        ParseMetadataProperty(reader, nextLine, metaObject, purpose);
                        break;
                    }
                }
            }
        }
        private void ParseMetadataProperty(StreamReader reader, string line, ApplicationObject metaObject, PropertyPurpose purpose)
        {
            string[] lines = line.Split(',');
            string fileName = lines[2].Replace("}", string.Empty);
            string objectName = lines[3].Replace("\"", string.Empty);

            if (!InfoBase.Properties.TryGetValue(new Guid(fileName), out MetadataProperty property))
            {
                return;
            }

            property.Name = objectName;
            property.Purpose = purpose;
            metaObject.Properties.Add(property);

            ParseMetadataPropertyTypes(reader, property);
            CreateDatabaseFields(property);
        }
        private void ParseMetadataPropertyTypes(StreamReader reader, MetadataProperty property)
        {
            string line = reader.ReadLine();
            if (line == null) return;

            // Ищем начало описания типа данных свойства
            while (line != "{\"Pattern\",")
            {
                line = reader.ReadLine();
                if (line == null) return;
            }
            Match match;
            List<int> typeCodes = new List<int>();
            DataTypeInfo typeInfo = new DataTypeInfo();
            // Читаем все допустимые для значения данного свойства типы данных
            // до тех пор, пока не закончатся описания типов данных
            while ((line = reader.ReadLine()) != null)
            {
                match = rxDbType.Match(line);
                if (!match.Success) break;

                string token = match.Value.Replace("{", string.Empty).Replace("\"", string.Empty);
                if (token == MetadataTokens.S) typeInfo.CanBeString = true;
                else if (token == MetadataTokens.B) typeInfo.CanBeBoolean = true;
                else if (token == MetadataTokens.N) typeInfo.CanBeNumeric = true;
                else if (token == MetadataTokens.D) typeInfo.CanBeDateTime = true;
                else if (token == MetadataTokens.R)
                {
                    string[] lines = line.Split(',');
                    string uuid = lines[1].Replace("}", string.Empty);
                    if (uuid == "e199ca70-93cf-46ce-a54b-6edc88c3a296") // ХранилищеЗначения - varbinary(max)
                    {
                        typeInfo.IsValueStorage = true;
                    }
                    else if (uuid == "fc01b5df-97fe-449b-83d4-218a090e681e") // УникальныйИдентификатор - binary(16)
                    {
                        typeInfo.IsUuid = true;
                    }
                    else if (InfoBase.MetaReferenceTypes.TryGetValue(new Guid(uuid), out ApplicationObject type))
                    {
                        typeInfo.CanBeReference = true;
                        typeCodes.Add(type.TypeCode); // требуется для определения многозначности типа данных (см. комментарий ниже)
                    }
                    else
                    {
                        //TODO: log warning - unexpected token
                    }
                }
                else
                {
                    //TODO: log warning - unexpected token
                }
            }
            if (typeCodes.Count == 1)
            {
                // В целях оптимизации в DataTypeInfo не хранятся все допустимые для данного свойства коды ссылочных типов.
                // В случае составного типа код типа конкретного значения можно получить в базе данных в поле {имя поля}_TRef.
                // В случае же сохранения кода типа в базу данных код типа можно получить из свойства ApplicationObject.TypeCode.
                // У не составных типов данных такого поля в базе данных нет, поэтому необходимо сохранить код типа в DataTypeInfo.
                typeInfo.ReferenceTypeCode = typeCodes[0];
            }
            property.PropertyType = typeInfo;
        }
        private void CreateDatabaseFields(MetadataProperty property)
        {
            // TODO: убрать в MetadataPropertyFactory
            if (property.PropertyType.IsMultipleType)
            {
                CreateDatabaseFieldsForMultipleType(property);
            }
            else
            {
                CreateDatabaseFieldsForSingleType(property);
            }
        }
        private void CreateDatabaseFieldsForSingleType(MetadataProperty property)
        {
            if (property.PropertyType.IsUuid)
            {
                property.Fields.Add(new DatabaseField(property.DbName, "binary", 16));
            }
            else if (property.PropertyType.IsBinary)
            {
                // is used only for system properties of system types
                // TODO: log if it eventually happens
            }
            else if (property.PropertyType.IsValueStorage)
            {
                property.Fields.Add(new DatabaseField(property.DbName, "varbinary", -1));
            }
            else if (property.PropertyType.CanBeString)
            {
                // should be updated from database
                property.Fields.Add(new DatabaseField(property.DbName, "nvarchar", 10));
            }
            else if (property.PropertyType.CanBeNumeric)
            {
                // should be updated from database
                property.Fields.Add(new DatabaseField(property.DbName, "numeric", 9, 10, 0));
            }
            else if (property.PropertyType.CanBeBoolean)
            {
                property.Fields.Add(new DatabaseField(property.DbName, "binary", 1));
            }
            else if (property.PropertyType.CanBeDateTime)
            {
                // can be updated from database
                property.Fields.Add(new DatabaseField(property.DbName, "datetime2", 6, 19, 0));
            }
            else if (property.PropertyType.CanBeReference)
            {
                property.Fields.Add(new DatabaseField(property.DbName + MetadataTokens.RRef, "binary", 16));
            }
        }
        private void CreateDatabaseFieldsForMultipleType(MetadataProperty property)
        {
            property.Fields.Add(new DatabaseField(property.DbName + "_" + MetadataTokens.TYPE, "binary", 1));
            if (property.PropertyType.CanBeString)
            {
                // should be updated from database
                property.Fields.Add(new DatabaseField(property.DbName + "_" + MetadataTokens.S, "nvarchar", 10));
            }
            if (property.PropertyType.CanBeNumeric)
            {
                // should be updated from database
                property.Fields.Add(new DatabaseField(property.DbName + "_" + MetadataTokens.N, "numeric", 9, 10, 0));
            }
            if (property.PropertyType.CanBeBoolean)
            {
                property.Fields.Add(new DatabaseField(property.DbName + "_" + MetadataTokens.L, "binary", 1));
            }
            if (property.PropertyType.CanBeDateTime)
            {
                // can be updated from database
                property.Fields.Add(new DatabaseField(property.DbName + "_" + MetadataTokens.T, "datetime2", 6, 19, 0));
            }
            if (property.PropertyType.CanBeReference)
            {
                if (property.PropertyType.ReferenceTypeCode == 0) // miltiple refrence type
                {
                    property.Fields.Add(new DatabaseField(property.DbName + "_" + MetadataTokens.RTRef, "binary", 4));
                }
                property.Fields.Add(new DatabaseField(property.DbName + "_" + MetadataTokens.RRRef, "binary", 16));
            }
        }
        
        // Описание типов данных свойств объектов метаданных (реквизитов, измерений или ресурсов)
        // {"Pattern", начало блока описания типов данных свойства, если описаний несколько, то это составной тип данных
        // {"B"} Булево - binary(1)
        // {"D","D"} Дата(Дата) - datetime2
        // {"D","T"} Дата(Время) - datetime2
        // {"D"} Дата(ДатаВремя) - datetime2
        // {"S",10,0} Строка(10) фиксированная - nchar(10)
        // {"S",10,1} Строка(10) переменная - nvarchar(10)
        // {"S"} Строка(неограниченная) всегда переменная - nvarchar(max)
        // {"N",10,2,0} Число(10,2) Отрицательное или положительное - numeric(10,2)
        // {"N",10,2,1} Число(10,2) Неотрицательное - numeric(10, 2)
        // {"#",e199ca70-93cf-46ce-a54b-6edc88c3a296} ХранилищеЗначения - varbinary(max)
        // {"#",fc01b5df-97fe-449b-83d4-218a090e681e} УникальныйИдентификатор - binary(16)
        // {"#",70497451-981e-43b8-af46-fae8d65d16f2} Ссылка (идентификатор объекта метаданных) - binary(16)

        #endregion

        #region "Table parts"

        private void ParseNestedObjects(StreamReader reader, string line, ApplicationObject dbo)
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
        private void ParseNestedObject(StreamReader reader, string line, ApplicationObject owner)
        {
            string[] lines = line.Split(',');
            string fileName = lines[2].Replace("}", string.Empty);
            string objectName = lines[3].Replace("\"", string.Empty);

            if (InfoBase.TableParts.TryGetValue(new Guid(fileName), out ApplicationObject nested))
            {
                if (nested is TablePart)
                {
                    ((TablePart)nested).Owner = owner;
                }
                nested.Name = objectName;
                nested.TableName = owner.TableName + nested.TableName;
                owner.TableParts.Add((TablePart)nested);
            }
            ParseNestedMetaProperties(reader, nested);
        }
        private void ParseNestedMetaProperties(StreamReader reader, ApplicationObject dbo)
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

        private void ParseConstant(StreamReader reader, ApplicationObject metaObject)
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

            if (InfoBase.Properties.TryGetValue(new Guid(uuid), out MetadataProperty property))
            {
                property.Name = "Value";
                property.Fields.Add(new DatabaseField()
                {
                    Name = property.DbName
                });
                metaObject.Properties.Add(property);
            }
            ParseMetadataPropertyTypes(reader, property);
        }

        #endregion

        public void SkipLines(StreamReader reader, int count)
        {
            for (int i = 0; i < count; i++)
            {
                _ = reader.ReadLine();
            }
        }
        public void ParseName(StreamReader reader, ApplicationObject metaObject)
        {
            string line = reader.ReadLine();
            if (line == null) return;

            string[] lines = line.Split(',');
            //string uuid = lines[2].Replace("}", string.Empty); // ParseApplicationObjectUuid

            metaObject.Name = lines[3].Replace("\"", string.Empty);
        }
        public void ParseAlias(StreamReader reader, ApplicationObject metaObject)
        {
            string line = reader.ReadLine();
            if (line == null) return;

            string[] lines = line.Split(',');
            string alias = lines[2].Replace("}", string.Empty);

            metaObject.Alias = alias.Replace("\"", string.Empty);
        }
        public void ParsePropertiesAndTableParts(StreamReader reader, ApplicationObject metaObject)
        {
            string line;
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
    }
}