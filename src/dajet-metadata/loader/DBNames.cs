namespace DaJet.Metadata
{
    internal sealed class DbName
    {
        internal int Code;
        internal Guid Uuid;
        internal string Name;
        internal DbName(Guid uuid, int code, string name)
        {
            Uuid = uuid; Code = code; Name = name;
        }
    }

    ///<summary>
    ///Идентификатор объекта СУБД:
    ///<br>Uuid - Идентификатор объекта метаданных (binary(16) - UUID)</br>
    ///<br>Code - Уникальный числовой код объекта СУБД (binary(4) - integer)</br>
    ///<br>Name - Буквенный идентификатор объекта СУБД для формирования имён</br>
    ///<br>Например: VT + LineNo или Reference + ReferenceChngR</br>
    ///</summary>
    internal static class DBNames
    {
        internal static void Parse(ReadOnlySpan<byte> fileData, in MetadataRegistry registry, in List<DbName> missed)
        {
            if (fileData.Length == 0)
            {
                return; // Пустой файл
            }

            ConfigFileReader reader = new(fileData);

            int count = 0;

            // Количество элементов файла DbNames из таблицы Params
            if (reader[2][1].Seek()) { count = reader.ValueAsNumber; }

            int code;
            Guid uuid;
            string name;
            bool supported = false;
            int length;
            Span<char> buffer = stackalloc char[32];

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
                        length = reader.GetChars(buffer);

                        supported = Configurator.IsSupportedToken(buffer[..length], out name);
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

                    if (supported && uuid != Guid.Empty && name is not null && code > 0)
                    {
                        if (!registry.TryRegisterDbName(uuid, code, in name))
                        {
                            missed.Add(new DbName(uuid, code, name));
                        }
                    }
                }
                else if (reader.Token == ConfigFileToken.EndObject)
                {
                    count--;
                }
            }
        }
    }
}