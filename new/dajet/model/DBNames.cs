namespace DaJet
{
    ///<summary>
    ///Идентификатор объекта СУБД:
    ///<br>Code - Уникальный числовой код объекта СУБД (binary(4) - integer)</br>
    ///<br>Name - Буквенный идентификатор объекта СУБД для формирования имён</br>
    ///<br>Например: VT + LineNo или Reference + ReferenceChngR</br>
    ///</summary>
    internal readonly struct DbName
    {
        internal DbName(int code, string name)
        {
            Code = code;
            Name = name;
        }
        internal readonly int Code { get; } = 0;
        internal readonly string Name { get; } = string.Empty;
        public override string ToString()
        {
            return string.Format("{0}{1}", Name, Code);
        }
    }
    internal static class DBNames
    {
        private sealed class MissedDbName
        {
            internal int Code;
            internal Guid Uuid;
            internal string Name;
            internal MissedDbName(Guid uuid, int code, string name)
            {
                Uuid = uuid; Code = code; Name = name;
            }
        }
        internal static void Parse(ReadOnlySpan<byte> fileData, in MetadataRegistry registry)
        {
            ConfigFileReader reader = new(fileData);

            int count = 0;

            // Количество элементов файла DbNames из таблицы Params
            if (reader[2][1].Seek()) { count = reader.ValueAsNumber; }

            int code;
            Guid uuid;
            string name;

            List<MissedDbName> missed = new();

            while (reader.Read() && count > 0)
            {
                if (reader.Token == ConfigFileToken.StartObject)
                {
                    if (reader.Read() && reader.Token == ConfigFileToken.Value)
                    {
                        uuid = reader.ValueAsUuid;
                    }
                    else // [2][n][1] - уникальный идентификатор объекта метаданных
                    {
                        uuid = Guid.Empty;
                    }

                    if (reader.Read() && reader.Token == ConfigFileToken.String)
                    {
                        name = reader.ValueAsString;
                    }
                    else // [2][n][2] - имя объекта СУБД (как правило префикс)
                    {
                        name = null;
                    }

                    if (reader.Read() && reader.Token == ConfigFileToken.Value)
                    {
                        code = reader.ValueAsNumber;
                    }
                    else // [2][n][3] - уникальный числовой код объекта метаданных
                    {
                        code = -1;
                    }

                    if (uuid != Guid.Empty && name is not null && code > 0)
                    {
                        if (!registry.TryAddDbName(uuid, code, name))
                        {
                            missed.Add(new MissedDbName(uuid, code, name));
                        }
                    }
                }
                else if (reader.Token == ConfigFileToken.EndObject)
                {
                    count--;
                }
            }

            if (missed.Count > 0)
            {
                foreach (MissedDbName item in missed)
                {
                    registry.AddMissedDbName(item.Uuid, item.Code, item.Name);
                }
            }
        }
    }
}