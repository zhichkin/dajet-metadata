using System.Collections.Generic;

namespace DaJet
{
    internal static class DataTypeChars // 0x01 _TYPE
    {
        internal const byte B = (byte)'B'; // 0x02 _L
        internal const byte N = (byte)'N'; // 0x03 _N
        internal const byte D = (byte)'D'; // 0x04 _T
        internal const byte S = (byte)'S'; // 0x05 _S
        internal const byte R = (byte)'#'; // 0x08 [_TRef] _RRef
    }
    internal static class DataTypeParser
    {
        internal static DataType Parse(ref ConfigFileReader reader, ReadOnlySpan<uint> root, in MetadataRegistry registry, out List<Guid> references)
        {
            // [root][1][2][2][3][{] - Начало объекта описания типов (открывающая фигурная скобка)

            references = new List<Guid>();

            if (!reader[root][1].Seek()) // [1][2][2][3]
            {
                return new DataType(); // Null
            }

            ReadOnlySpan<byte> pattern = "Pattern"u8;

            ReadOnlySpan<byte> value = reader.GetBytes();

            if (!value.SequenceEqual(pattern))
            {
                return null; // Это не объект "ОписаниеТипов" !
            }

            DataType type = new(); // Неопределённый тип данных

            while (reader.Read())
            {
                if (reader.Token == ConfigFileToken.EndObject)
                {
                    break; // Конец объекта "ОписаниеТипов"
                }

                if (reader.Token == ConfigFileToken.StartObject && reader.Read())
                {
                    // Начинаем читать следующее описание типа

                    value = reader.GetBytes(); // Дискриминатор

                    if (value.IsEmpty) { break; } // Что-то пошло не так !

                    byte discriminator = value[0];

                    if (discriminator == DataTypeChars.B) // {"B"}
                    {
                        type.IsBoolean = true; // _Fld + code + _L
                    }
                    else if (discriminator == DataTypeChars.N) // {"N",10,2,0} | {"N",10,2,1}
                    {
                        type.IsDecimal = true; // _Fld + code + _N

                        if (reader.Read()) { type.Precision = (byte)reader.ValueAsNumber; }
                        if (reader.Read()) { type.Scale = (byte)reader.ValueAsNumber; }
                        if (reader.Read()) { type.NumericQualifier = (NumericKind)reader.ValueAsNumber; }
                    }
                    else if (discriminator == DataTypeChars.D) // {"D"} | {"D","D"} | {"D","T"}
                    {
                        type.IsDateTime = true; // _Fld + code + _T

                        if (reader.Read())
                        {
                            if (reader.Token == ConfigFileToken.EndObject)
                            {
                                type.DateTimeQualifier = DateTimePart.DateTime;
                                continue; // Переходим к описанию следующего типа
                            }
                            else
                            {
                                value = reader.GetBytes();

                                if (value.IsEmpty)
                                {
                                    type.DateTimeQualifier = DateTimePart.DateTime;
                                }
                                else
                                {
                                    discriminator = value[0];

                                    if (discriminator == DataTypeChars.D)
                                    {
                                        type.DateTimeQualifier = DateTimePart.Date;
                                    }
                                    else
                                    {
                                        type.DateTimeQualifier = DateTimePart.Time;
                                    }
                                }
                            }
                        }
                    }
                    else if (discriminator == DataTypeChars.S) // {"S"} | {"S",10,0} | {"S",10,1}
                    {
                        type.IsString = true; // _Fld + code + _S

                        if (reader.Read())
                        {
                            if (reader.Token == ConfigFileToken.EndObject)
                            {
                                type.Size = 0; // Строка неограниченной длины
                                type.StringQualifier = StringKind.Variable;
                                continue; // Переходим к описанию следующего типа
                            }
                            else
                            {
                                type.Size = (ushort)reader.ValueAsNumber;

                                if (reader.Read())
                                {
                                    type.StringQualifier = (StringKind)reader.ValueAsNumber;
                                }
                            }
                        }
                    }
                    else if (discriminator == DataTypeChars.R) // {"#",70497451-981e-43b8-af46-fae8d65d16f2}
                    {
                        if (reader.Read())
                        {
                            Guid uuid = reader.ValueAsUuid;

                            if (uuid == SingleType.ValueStorage)
                            {
                                type.IsBinary = true;
                            }
                            else if (uuid == SingleType.UniqueIdentifier)
                            {
                                type.IsUuid = true;
                            }
                            else
                            {
                                references.Add(uuid);
                            }
                        }
                    }

                    if (reader.Read() && reader.Token == ConfigFileToken.EndObject)
                    {
                        continue; // Переходим к описанию следующего типа
                    }
                }
            }

            if (references.Count > 0)
            {
                // Конфигурирование ссылочных типов данных объекта "ОписаниеТипов".
                // Внимание!
                // Если описание типов ссылается на определяемый тип или характеристику,
                // которые не являются или не содержат в своём составе ссылочные типы данных,
                // то в таком случае описание типов будет содержать только примитивные типы данных.
                // Выполняется конфигурирование следующих свойств:
                // - DataType.IsEntity (bool)
                // - DataType.TypeCode (int)

                Configurator.ConfigureDataTypeReferences(in registry, ref type, in references);
            }

            return type;
        }

        // RULES (правила разрешения ссылочных типов данных для объекта "ОписаниеТипов"):
        // 1. DataType (property type) can have only one reference to DefinedType or Characteristic.
        //    Additional references to another data types are not allowed in this case.
        // 2. DefinedType and Characteristic can not reference them self or each other.
        // 3. Если ссылочный тип имеет значение, например, "СправочникСсылка", то есть любой справочник,
        //    в таком случае необходимо вычислить количество справочников в составе конфигурации:
        //    если возможным справочником будет только один, то это будет single reference type.
        // 4. То же самое, что и для пункта #3, касается значения типа "ЛюбаяСсылка".
        // Внимание!
        // Значения общих ссылочных типов могут комбинироваться друг с другом
        // и конкретными ссылочными типами, в том числе это касается типа ЛюбаяСсылка.

        // NOTE: Lazy-load of DefinedType: recursion is avoided because of the rule #2.
        // NOTE: Lazy-load of Characteristic: recursion is avoided because of the rule #2.

        // Рекурсия возможна, например, при загрузке в кэш объекта метаданных "МойПланВидовХарактеристик",
        // который имеет реквизит с типом данных "Характеристика.МойПланВидовХарактеристик".
        // При попытке разрешить ссылку на характеристику попадаем сюда и уходим в рекурсию:
        // reference =         Характеристика.МойПланВидовХарактеристик
        // uuid      = ПланВидовХарактеристик.МойПланВидовХарактеристик
        // Решение: загрузка типов значений характеристик при первичном заполнении кэша метаданных.

        // Редкий, исключительный случай, но всё-таки надо учесть ¯\_(ツ)_/¯
        // Если reference это общий ссылочный тип данных, например, ПланОбменаСсылка <see cref="ReferenceTypes"/>,
        // и для этого типа данных пользователем в конфигруации создан только один конкретный тип, например,
        // ПланОбмена.МойПланОбмена, то свойства Reference и TypeCode переменной target заполняются пустыми значениями,
        // что соответствует множественному ссылочному типу данных, и это, как следствие, приводит к тому, что
        // свойство IsMultipleType класса <see cref="DataTypeDescriptor"/> возвращает некорректное значение true,
        // что в свою очередь приводит к некорректному формированию метаданных полей базы данных для такого свойства
        // объекта метаданных в процедуре <see cref="ConfigureDatabaseColumns"/>.
        // Важно!
        // Таким образом в конфигураторе типом свойства объекта метаданных (или табличной части) является общий тип данных,
        // но на самом деле на уровне базы данных он интерпретируется и используется как конкретный тип данных.
        // Другими словами там, где обычно генерируется три поля: _Fld123_TYPE, _Fld123_RTRef и _Fld123_RRRef,
        // создаётся только одно _Fld123RRef ...

        // Ещё один редкий случай: если тип данных определён как любой общий ссылочный тип, в том числе ЛюбаяСсылка,
        // или комбинация таких типов, и при этом в конфигурации не определено ни одного соответствующего конкретного
        // ссылочного типа, а также при этом типу данных не назначено ни одного простого типа данных, то есть возникает
        // ситуация, когда свойство объекта как-будто бы не имеет полей в таблице базы данных, тогда в таком исключительном
        // случае типу данных назначается составной ссылочный тип: _TYPE + _TRef + _RRef.
    }
}