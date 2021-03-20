using DaJet.Metadata.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DaJet.Metadata
{
    public interface IFileParser
    {
        void UseInfoBase(InfoBase infoBase);
        void Parse(StreamReader stream, MetadataObject metaObject);
    }
    public sealed class DocumentFileParser : IFileParser
    {
        private readonly Regex rxUUID = new Regex("[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}"); // Example: eb3dfdc7-58b8-4b1f-b079-368c262364c9

        private InfoBase InfoBase { get; set; }
        private readonly IMetadataManager MetadataManager;
        private readonly IMetadataObjectFileParser BasicParser;
        public DocumentFileParser(IMetadataManager manager, IMetadataObjectFileParser parser)
        {
            BasicParser = parser;
            MetadataManager = manager;
        }
        public void UseInfoBase(InfoBase infoBase)
        {
            InfoBase = infoBase;
        }
        public void Parse(StreamReader stream, MetadataObject document)
        {
            ParseBasicProperties(stream, document); // Чтение общих для всех объектов метаданных свойств
            ParseBasisForFillOut(stream, document); // Чтение объектов, на основании которых может быть заполнен данный документ
            ParseRegistersToPost(stream, document); // Чтение регистров, для которых данный документ является регистратором
            BasicParser.ParsePropertiesAndTableParts(stream, document);
        }
        private void ParseBasicProperties(StreamReader stream, MetadataObject document)
        {
            BasicParser.SkipLines(stream, 4); // 1-4 lines
            BasicParser.ParseName(stream, document); // 5. line - metaobject's UUID and Name
            BasicParser.ParseAlias(stream, document); // 6. line - metaobject's alias
            BasicParser.SkipLines(stream, 1); // 7. line
        }
        private void ParseBasisForFillOut(StreamReader stream, MetadataObject document)
        {
            string line = stream.ReadLine(); // 8. line - объекты-основания для заполнения документа
            if (line == null) return;

            string[] lines = line.Split(','); // {0,0},1, или {0,3,
            if (lines.Length < 2) return;
            // Вычисляем количество блоков описания объектов метаданных
            if (!int.TryParse(lines[1].TrimEnd('}'), out int count))
            {
                return;
            }
            if (count == 0) return;

            // Читаем блоки описания объектов метаданных (объектов-оснований)
            // {"#",157fa490-4ce9-11d4-9415-008048da11f9,
            // {1,e1f1df1a-5f4b-4269-9f67-4a5fa61df942}
            // },
            BasicParser.SkipLines(stream, (count * 3) + 1);
            // },1, завершающая блок строка
        }
        public void ParseRegistersToPost(StreamReader stream, MetadataObject document)
        {
            string line = stream.ReadLine(); // начало блока описания регистров
            if (line == null) return;
            
            string[] lines = line.Split(','); // {0,0},1, или {0,3,
            if (lines.Length < 2) return;

            // Вычисляем количество блоков описания регистров
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
                foreach (var collection in InfoBase.Registers)
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

            // Добавляем свойство "Регистратор" в найденные регистры
            foreach (MetadataObject register in registers)
            {
                // Получаем ссылку на фабрику регистра
                IMetadataObjectFactory factory = MetadataManager.GetFactory(register.GetType());
                if (factory == null) break; // что-то очень сильно пошло не так ...

                factory.PropertyFactory.AddPropertyРегистратор(register, document, MetadataManager.DatabaseProvider);
            }
        }
    }
}