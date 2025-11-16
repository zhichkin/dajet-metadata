namespace DaJet
{
    internal sealed class SharedPropertyParser : ConfigFileParser
    {
        internal override void Parse(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
        {
            if (!registry.TryGetEntry(uuid, out SharedProperty metadata))
            {
                return; //NOTE: сюда не предполагается попадать!
            }

            ConfigFileReader reader = new(file);

            // Идентификатор объекта метаданных - значение поля FileName в таблице Config
            //if (reader[2][2][2][2][2][3].Seek()) { metadata.Uuid = reader.ValueAsUuid; }

            // Имя объекта метаданных конфигурации
            if (reader[2][2][2][2][3].Seek())
            {
                string name = reader.ValueAsString;
                metadata.Name = name;
                registry.AddMetadataName(MetadataName.SharedProperty, in name, uuid);
            }

            //_converter[1][2][1] += UsageSettings; // количество объектов метаданных, у которых значение использования общего реквизита не равно "Автоматически"
            
            if (reader[2][2][2][3][ConfigFileToken.StartObject].Seek())
            {
                uint[] root = [2, 2, 2, 3];

                metadata.Type = DataTypeParser.Parse(ref reader, root, in registry, out List<Guid> references);
            }

            metadata.DataSeparationUsage = (DataSeparationUsage)reader[2][6].SeekNumber();
            metadata.AutomaticUsage = (AutomaticUsage)reader[2][7].SeekNumber();
            metadata.DataSeparationMode = (DataSeparationMode)reader[2][13].SeekNumber();

            //if (options.IsExtension) // 1.1.1.1.14 = 0 если заимствование отстутствует
            //{
            //    _converter[1][1][1][1][15] += Parent; // uuid расширяемого объекта метаданных
            //}
        }
        internal override TableDefinition Load(Guid uuid, ReadOnlySpan<byte> file, in MetadataRegistry registry)
        {
            throw new NotImplementedException();
        }

        //private void UsageSettings(in ConfigFileReader source, in CancelEventArgs args)
        //{
        //    _count = source.GetInt32(); // количество настроек использования общего реквизита

        //    Guid uuid; // file name объекта метаданных, для которого используется настройка
        //    int usage; // значение настройки использования общего реквизита объектом метаданных

        //    while (_count > 0)
        //    {
        //        _ = source.Read(); // [2] (1.2.2) 0221aa25-8e8c-433b-8f5b-2d7fead34f7a
        //        uuid = source.GetUuid(); // file name объекта метаданных
        //        if (uuid == Guid.Empty) { throw new FormatException(); }

        //        _ = source.Read(); // [2] (1.2.3) { Начало объекта настройки
        //        _ = source.Read(); // [3] (1.2.3.0) 2
        //        _ = source.Read(); // [3] (1.2.3.1) 1
        //        usage = source.GetInt32(); // настройка использования общего реквизита
        //        if (usage == -1) { throw new FormatException(); }
        //        _ = source.Read(); // [3] (1.2.3.2) 00000000-0000-0000-0000-000000000000
        //        _ = source.Read(); // [2] (1.2.3) } Конец объекта настройки

        //        _target.UsageSettings.Add(uuid, (SharedPropertyUsage)usage);

        //        _count--; // Конец чтения настройки для объекта метаданных
        //    }
        //}
    }
}