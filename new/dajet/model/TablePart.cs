namespace DaJet
{
    internal sealed class TablePart : DatabaseObject
    {
        internal static TablePart Create(Guid uuid, int code, string name)
        {
            return new TablePart(uuid, code, name);
        }
        internal TablePart(Guid uuid, int code, string name) : base(uuid, code, name) { }

        private int _LineNo;
        internal override void AddDbName(int code, string name)
        {
            if (name == MetadataToken.LineNo)
            {
                _LineNo = code;
            }
        }
        internal string GetColumnNameНомерСтроки()
        {
            return string.Format("_{0}{1}", MetadataToken.LineNo, _LineNo);
        }

        internal static EntityDefinition[] Parse(ref ConfigFileReader reader, uint root, in DatabaseObject owner, in MetadataRegistry registry, bool relations)
        {
            Guid type = reader[root][1].SeekUuid(); // идентификатор типа коллекции
            int count = reader[root][2].SeekNumber(); // количество элементов коллекции

            if (count == 0) { return null; }

            EntityDefinition[] array = new EntityDefinition[count];

            uint[] vector = [root, 0, 3]; // Адрес коллекции свойств табличной части

            for (uint i = 0; i < array.Length; i++)
            {
                EntityDefinition table = new(); array[i] = table;

                uint N = i + 3; // Добавляем смещение от корневого узла [root][0]

                if (reader[root][N][ConfigFileToken.StartObject].Seek())
                {
                    Guid uuid = reader[root][N][1][2][6][2][2][3].SeekUuid();

                    if (registry.TryGetEntry(uuid, out TablePart entry))
                    {
                        table.DbName = string.Format("{0}{1}", owner.GetMainDbName(), entry.GetMainDbName());
                    }
                    else
                    {
                        //TODO: Зафиксировать ошибку.
                        //TODO: Табличная часть не найдена в реестре объектов метаданных,
                        //TODO: а это значит, что у неё нет соответствующей таблицы в базе данных
                    }

                    table.Name = reader[root][N][1][2][6][2][3].SeekString();

                    //if (_cache != null && _cache.Extension != null) // [5][2] 0.1.5.1.8 = 0 если заимствование отстутствует
                    //{
                    //    _converter[0][1][5][1][9] += Parent; // uuid расширяемого объекта метаданных
                    //}

                    Configurator.ConfigureTablePart(in table, in entry, in owner);

                    vector[1] = N; // Коллекция свойств текущей табличной части

                    if (reader[vector][ConfigFileToken.StartObject].Seek())
                    {
                        PropertyDefinition[] properties = Property.Parse(ref reader, vector, in registry, relations);

                        if (properties is not null)
                        {
                            table.Properties.AddRange(properties);
                        }
                    }
                }
            }

            return array;
        }
    }
}