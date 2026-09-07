using System.Text;

namespace DaJet.Metadata
{
    //NOTE: Разбор описания схемы хранения из таблицы SchemaStorage.
    //NOTE: Формат: {0,{N,{"ИмяТаблицы","N",код,"",{поля},{индексы}}, ...}}
    //NOTE: Имя таблицы — первый элемент записи третьего уровня вложенности и
    //NOTE: записано без ведущего подчёркивания, которое есть у имени таблицы СУБД.
    internal static class StorageSchemaReader
    {
        internal static void Parse(ReadOnlySpan<byte> file, int schemaId, in MetadataRegistry registry)
        {
            int depth = 0;
            bool expectName = false;

            for (int i = 0; i < file.Length; i++)
            {
                byte current = file[i];

                if (current == (byte)'{')
                {
                    depth++; expectName = (depth == 3); continue;
                }

                if (current == (byte)'}')
                {
                    depth--; expectName = false; continue;
                }

                if (current == (byte)'"')
                {
                    int start = i + 1;
                    int end = start;

                    while (end < file.Length && file[end] != (byte)'"') { end++; }

                    if (expectName && end > start)
                    {
                        string name = Encoding.UTF8.GetString(file.Slice(start, end - start));

                        registry.RegisterStorageSchema("_" + name, schemaId);
                    }

                    expectName = false; i = end; continue;
                }

                if (current == (byte)',') { continue; }

                if (current > (byte)' ') { expectName = false; }
            }
        }
    }
}
