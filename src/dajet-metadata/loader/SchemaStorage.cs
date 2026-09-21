namespace DaJet.Metadata
{
    internal static class SchemaStorage
    {
        internal static void Parse(ReadOnlySpan<byte> fileData, in MetadataRegistry registry, in List<int> codes)
        {
            if (fileData.Length == 0)
            {
                return; // Пустой файл
            }

            ConfigFileReader reader = new(fileData);

            int count = reader[2][1].SeekNumber(); // Количество таблиц файла SchemaStorage

            if (count == 0) { return; }

            count += 2;

            int code;
            string name;

            for (uint i = 2; i < count; i++)
            {
                name = reader[2][i][1].SeekString(); // Имя таблицы в терминах SDBL
                code = reader[2][i][3].SeekNumber(); // Код таблицы (объекта метаданных)

                codes.Add(code);
            }
        }
    }
}