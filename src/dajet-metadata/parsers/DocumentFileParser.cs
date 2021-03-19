using DaJet.Metadata.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DaJet.Metadata
{
    public interface IFileParser
    {
        void Parse(StreamReader stream, MetadataObject metaObject);
    }
    public sealed class DocumentFileParser : IFileParser
    {
        private readonly Regex rxUUID = new Regex("[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}"); // Example: eb3dfdc7-58b8-4b1f-b079-368c262364c9

        private readonly IMetadataObjectsManager MetadataManager;
        private readonly InfoBase InfoBase;
        public DocumentFileParser(InfoBase infoBase, IMetadataObjectsManager manager)
        {
            InfoBase = infoBase;
            MetadataManager = manager;
        }
        public void Parse(StreamReader stream, MetadataObject document)
        {
            ParseBasisForFillOut(stream, document); // Чтение объектов, на основании которых может быть заполнен данный документ
            ParseRegistersToPost(stream, document); // Чтение регистров, для которых данный документ является регистратором
        }
        public void ParseBasisForFillOut(StreamReader stream, MetadataObject document)
        {
            string line = stream.ReadLine(); // 8. line - объекты-основания для заполнения документа
            if (line == null) return;

            string[] lines = line.Split(','); // {0,0},1, или {0,3,
            if (lines.Length < 2) return;
            if (!int.TryParse(lines[1].TrimEnd('}'), out int count))
            {
                return;
            }
            if (count == 0) return;

            // Читаем блоки описания объектов метаданных (объектов-оснований)
            for (int i = 0; i < count; i++) // цикл по количеству блоков
            {
                _ = stream.ReadLine(); // {"#",157fa490-4ce9-11d4-9415-008048da11f9,
                _ = stream.ReadLine(); // {1,e1f1df1a-5f4b-4269-9f67-4a5fa61df942}
                _ = stream.ReadLine(); // },
            }
            _ = stream.ReadLine(); // },1, завершающая блок строка 
        }
        public void ParseRegistersToPost(StreamReader stream, MetadataObject document)
        {
            string line = stream.ReadLine(); // начало блока описания регистров
            if (line == null) return;

            string[] lines = line.Split(','); // {0,0},1, или {0,3,
            if (lines.Length < 2) return;
            if (!int.TryParse(lines[1].TrimEnd('}'), out int count))
            {
                return;
            }
            if (count == 0) return;

            // Читаем блоки описания объектов метаданных (регистров)
            Match match;
            List<MetadataObject> registers = new List<MetadataObject>();
            for (int i = 0; i < count; i++) // цикл по количеству блоков
            {
                _ = stream.ReadLine();    // {"#",157fa490-4ce9-11d4-9415-008048da11f9,
                line = stream.ReadLine(); // {1,e1f1df1a-5f4b-4269-9f67-4a5fa61df942}
                if (line == null) return;

                match = rxUUID.Match(line); // ищем uuid
                if (!match.Success) break; // нарушение формата потока

                Guid uuid = new Guid(match.Value);
                foreach (var collection in InfoBase.ValueTypes)
                {
                    // Ищем регистр в соответствующем наборе коллекций
                    if (collection.TryGetValue(uuid, out MetadataObject register))
                    {
                        registers.Add(register);
                        break;
                    }
                }
                _ = stream.ReadLine(); // }, конец блока описания объекта метаданных
            }
            _ = stream.ReadLine(); // },0,... конец блока описания регистров

            if (registers.Count == 0) return;

            // Добавляем свойство "Регистратор" в найденные регистры ! зачем 1С так сделала ???

            IMetadataObjectFactory factory = MetadataManager.GetFactory(typeof(Document));
            if (factory == null) return; // что-то очень сильно пошло не так ...

            MetadataProperty property = factory.PropertyFactory.CreateProperty("Регистратор", PropertyPurpose.System);
            property.PropertyType.CanBeReference = true;
            property.PropertyType.ReferenceTypeCode = (registers.Count == 1) ? document.TypeCode : 0; // single or multiple type

            if (property.PropertyType.IsMultipleType)
            {
                // Multiple value type 

                property.Fields.Add(new DatabaseField()
                {
                    Name = (MetadataManager.DatabaseProvider == DatabaseProviders.SQLServer ? "_OwnerID_TYPE" : "_OwnerID_TYPE".ToLowerInvariant()),
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
                    Name = (MetadataManager.DatabaseProvider == DatabaseProviders.SQLServer ? "_OwnerID_RTRef" : "_OwnerID_RTRef".ToLowerInvariant()),
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
                    Name = (MetadataManager.DatabaseProvider == DatabaseProviders.SQLServer ? "_OwnerID_RRRef" : "_OwnerID_RRRef".ToLowerInvariant()),
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
                    Name = (MetadataManager.DatabaseProvider == DatabaseProviders.SQLServer ? "_OwnerIDRRef" : "_OwnerIDRRef".ToLowerInvariant()),
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
        }
    }
}