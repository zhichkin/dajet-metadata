using DaJet.TypeSystem;

namespace DaJet.Metadata
{
    internal enum NumericKind
    {
        Signed = 0, // Знаковое
        UnSigned = 1 // Беззнаковое
    }
    internal enum StringKind
    {
        Fixed = 0, // Фиксированная длина
        Variable = 1 // Переменная длина
    }
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
                return DataType.Undefined;
            }

            ReadOnlySpan<byte> pattern = "Pattern"u8;

            ReadOnlySpan<byte> value = reader.GetBytes();

            if (!value.SequenceEqual(pattern))
            {
                return DataType.Undefined; // Это не объект "ОписаниеТипов" !
            }

            // Структура DataType в "разобранном" виде
            DataTypeFlags types = DataTypeFlags.Undefined;
            QualifierFlags qualifiers = QualifierFlags.None;
            ushort size = 0;
            byte precision = 0;
            byte scale = 0;
            int typeCode = 0;
            
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
                        types |= DataTypeFlags.Boolean; // _Fld + code + _L
                    }
                    else if (discriminator == DataTypeChars.N) // {"N",10,2,0} | {"N",10,2,1}
                    {
                        types |= DataTypeFlags.Decimal; // _Fld + code + _N

                        if (reader.Read()) { precision = (byte)reader.ValueAsNumber; }
                        if (reader.Read()) { scale = (byte)reader.ValueAsNumber; }
                        if (reader.Read())
                        {
                            if ((NumericKind)reader.ValueAsNumber == NumericKind.UnSigned)
                            {
                                qualifiers |= QualifierFlags.UnSigned;
                            }
                        }
                    }
                    else if (discriminator == DataTypeChars.D) // {"D"} | {"D","D"} | {"D","T"}
                    {
                        types |= DataTypeFlags.DateTime; // _Fld + code + _T

                        if (reader.Read())
                        {
                            if (reader.Token == ConfigFileToken.EndObject)
                            {
                                qualifiers |= QualifierFlags.DateTime;
                                continue; // Переходим к описанию следующего типа
                            }
                            else
                            {
                                value = reader.GetBytes();

                                if (value.IsEmpty)
                                {
                                    qualifiers |= QualifierFlags.DateTime;
                                }
                                else
                                {
                                    discriminator = value[0];

                                    if (discriminator == DataTypeChars.D)
                                    {
                                        qualifiers |= QualifierFlags.Date;
                                    }
                                    else
                                    {
                                        qualifiers |= QualifierFlags.Time;
                                    }
                                }
                            }
                        }
                    }
                    else if (discriminator == DataTypeChars.S) // {"S"} | {"S",10,0} | {"S",10,1}
                    {
                        types |= DataTypeFlags.String; // _Fld + code + _S

                        if (reader.Read())
                        {
                            if (reader.Token == ConfigFileToken.EndObject)
                            {
                                // Строка неограниченной длины
                                continue; // Переходим к описанию следующего типа
                            }
                            else
                            {
                                size = (ushort)reader.ValueAsNumber;
                                
                                if (reader.Read())
                                {
                                    if ((StringKind)reader.ValueAsNumber == StringKind.Fixed)
                                    {
                                        qualifiers |= QualifierFlags.Fixed;
                                    }
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
                                types |= DataTypeFlags.Binary;
                            }
                            else if (uuid == SingleType.UniqueIdentifier)
                            {
                                types |= DataTypeFlags.Uuid;
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
                // то используется именно определённый этими объектами тип данных.

                //Configurator.ConfigureDataTypeReferences(in registry, ref type, in references);

                for (int i = 0; i < references.Count; i++)
                {
                    Guid reference = references[i];

                    if (i == 0) // Единственно допустимая ссылка данного типа
                    {
                        // Тип "Определяемый тип" (переопределяет входной тип данных)

                        if (registry.TryGetDefinedType(reference, out DefinedType defined))
                        {
                            return defined.Type; // Описание типа берётся из определяемого типа
                        }

                        // Тип "Характеристика" (переопределяет входной тип данных)

                        if (registry.TryGetCharacteristic(reference, out Characteristic characteristic))
                        {
                            return characteristic.Type; // Описание типа берётся из характеристики
                        }
                    }

                    // Конкретный ссылочный тип

                    if (registry.TryGetReference(reference, out MetadataObject entry))
                    {
                        if ((types & DataTypeFlags.Entity) == DataTypeFlags.Entity) // Ранее минимум одна ссылка уже была найдена
                        {
                            typeCode = 0; break; // Составной ссылочный тип (дальше не ищем)
                        }
                        else // Пока что единственный найденный ссылочный тип (ищем дальше)
                        {
                            types |= DataTypeFlags.Entity;
                            typeCode = entry.Code;
                        }
                    }
                    else // Общий ссылочный тип
                    {
                        int result; // Результат поиска конкретных ссылочных типов

                        if (reference == ReferenceType.AnyReference)
                        {
                            result = registry.GetGenericTypeCode(ReferenceType.AllReferenceTypes);
                        }
                        else
                        {
                            result = registry.GetGenericTypeCode(reference);
                        }

                        if (result == 0) // Составной ссылочный тип
                        {
                            types |= DataTypeFlags.Entity;
                            typeCode = 0;
                            break; // Дальше не ищем
                        }
                        else if (result > 0) // Единственный ссылочный тип данного общего типа
                        {
                            if ((types & DataTypeFlags.Entity) == DataTypeFlags.Entity) // Ранее минимум одна ссылка уже была найдена
                            {
                                types |= DataTypeFlags.Entity;
                                typeCode = 0; // Составной ссылочный тип
                                break; // Дальше не ищем
                            }
                            else // Пока что единственный найденный ссылочный тип
                            {
                                types |= DataTypeFlags.Entity;
                                typeCode = result; // Ищем дальше
                            }
                        }
                    }
                }

                // Если не удалось найти хотя бы один конкретный ссылочный тип,
                // а описание типа данных не содержит ни одного простого типа,
                // тогда применяем следующее правило - ссылка составного типа

                if ((types & DataTypeFlags.Entity) == 0 && types == DataTypeFlags.Undefined)
                {
                    types |= DataTypeFlags.Entity;
                    typeCode = 0;
                }
            }

            return new DataType(types, qualifiers, size, precision, scale, typeCode);
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