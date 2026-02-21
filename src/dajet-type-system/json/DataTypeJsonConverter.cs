using DaJet.TypeSystem;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DaJet.Json
{
    public sealed class DataTypeJsonConverter : JsonConverter<DataType>
    {
        public override void Write(Utf8JsonWriter writer, DataType value, JsonSerializerOptions options)
        {
            if (value.IsUndefined) { writer.WriteStringValue("undefined"); }
            else if (value.IsUuid) { writer.WriteStringValue("uuid"); }
            else if (value.IsBinary)
            {
                if (value.Size == 0)
                {
                    writer.WriteStringValue("binary");
                }
                else if (value.IsFixed)
                {
                    writer.WriteStringValue($"binary({value.Size},fixed)");
                }
                else
                {
                    writer.WriteStringValueSegment("binary", false);
                    writer.WriteStringValueSegment("(", false);
                    writer.WriteStringValueSegment(value.Size.ToString(), false);
                    writer.WriteStringValueSegment(")", true);
                }
            }
            else if (value.IsArray) { writer.WriteStringValue("array"); }
            else if (value.IsObject) { writer.WriteStringValue("object"); }
            else if (value.IsUnion)
            {
                bool first = true;

                if (value.IsReferenceOnlyUnion)
                {
                    writer.WriteStringValue("entity");
                }
                else
                {
                    writer.WriteStringValueSegment("union(", false);

                    if (value.IsBoolean)
                    {
                        if (!first) { writer.WriteStringValueSegment("|", false); }

                        writer.WriteStringValueSegment("boolean", false);

                        first = false;
                    }

                    if (value.IsDecimal)
                    {
                        if (!first) { writer.WriteStringValueSegment("|", false); }

                        writer.WriteStringValueSegment($"decimal({value.Precision},{value.Scale})", false);

                        first = false;
                    }

                    if (value.IsDateTime)
                    {
                        if (!first) { writer.WriteStringValueSegment("|", false); }

                        if (value.IsDateOnly) { writer.WriteStringValueSegment("date", false); }
                        else if (value.IsTimeOnly) { writer.WriteStringValueSegment("time", false); }
                        else { writer.WriteStringValueSegment("datetime", false); }

                        first = false;
                    }

                    if (value.IsString)
                    {
                        if (!first) { writer.WriteStringValueSegment("|", false); }

                        if (value.Size == 0)
                        {
                            writer.WriteStringValueSegment("string", false);
                        }
                        else if (value.IsFixed)
                        {
                            writer.WriteStringValueSegment($"string({value.Size},fixed)", false);
                        }
                        else
                        {
                            writer.WriteStringValueSegment($"string({value.Size})", false);
                        }

                        first = false;
                    }

                    if (value.IsEntity)
                    {
                        if (!first) { writer.WriteStringValueSegment("|", false); }

                        if (value.TypeCode > 0)
                        {
                            writer.WriteStringValueSegment($"entity({value.TypeCode})", false);
                        }
                        else
                        {
                            writer.WriteStringValueSegment("entity", false);
                        }
                    }

                    writer.WriteStringValueSegment(")", true);
                }
            }
            else if (value.IsBoolean) { writer.WriteStringValue("boolean"); }
            else if (value.IsInteger) { writer.WriteStringValue("integer"); }
            else if (value.IsDecimal) { writer.WriteStringValue($"decimal({value.Precision},{value.Scale})"); }
            else if (value.IsDateTime)
            {
                if (value.IsDateOnly) { writer.WriteStringValue("date"); }
                else if (value.IsTimeOnly) { writer.WriteStringValue("time"); }
                else { writer.WriteStringValue("datetime"); }
            }
            else if (value.IsString)
            {
                if (value.Size == 0)
                {
                    writer.WriteStringValue("string");
                }
                else if (value.IsFixed)
                {
                    writer.WriteStringValue($"string({value.Size},fixed)");
                }
                else
                {
                    writer.WriteStringValue($"string({value.Size})");
                }
            }
            else if (value.IsEntity)
            {
                if (value.TypeCode > 0)
                {
                    writer.WriteStringValue($"entity({value.TypeCode})");
                }
                else
                {
                    writer.WriteStringValue("entity");
                }
            }
        }
        public override DataType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new FormatException();
            }

            string value = reader.GetString();

            if (value == "undefined") { return DataType.Undefined; }
            if (value == "boolean") { return DataType.Boolean; }
            if (value == "integer") { return DataType.Integer(); }
            if (value == "date") { return DataType.Date; }
            if (value == "time") { return DataType.Time; }
            if (value == "datetime") { return DataType.DateTime; }
            if (value == "uuid") { return DataType.Uuid(); }
            if (value == "array") { return DataType.Array; }
            if (value == "object") { return DataType.Object; }

            ReadOnlySpan<char> source;
            Span<Range> qualifiers = stackalloc Range[5];

            if (value.StartsWith("decimal"))
            {
                source = value.AsSpan(8..(value.Length - 1));
                
                int count = source.Split(qualifiers, ',');

                if (!byte.TryParse(source[qualifiers[0]], out byte precision))
                {
                    throw new FormatException();
                }

                if (!byte.TryParse(source[qualifiers[1]], out byte scale))
                {
                    throw new FormatException();
                }

                return DataType.Decimal(precision, scale);
            }

            if (value.StartsWith("string"))
            {
                if (!value.Contains('(', StringComparison.Ordinal))
                {
                    return DataType.String();
                }

                source = value.AsSpan(7..(value.Length - 1));

                int count = source.Split(qualifiers, ',');

                if (!ushort.TryParse(source[qualifiers[0]], out ushort size))
                {
                    throw new FormatException();
                }

                if (count > 1 && source[qualifiers[1]].SequenceEqual("fixed"))
                {
                    return DataType.String(size, false);
                }

                return DataType.String(size);
            }

            if (value.StartsWith("binary"))
            {
                if (!value.Contains('(', StringComparison.Ordinal))
                {
                    return DataType.Binary();
                }

                source = value.AsSpan(7..(value.Length - 1));

                int count = source.Split(qualifiers, ',');

                if (!ushort.TryParse(source[qualifiers[0]], out ushort size))
                {
                    throw new FormatException();
                }

                if (count > 1 && source[qualifiers[1]].SequenceEqual("fixed"))
                {
                    return DataType.Binary(size, false);
                }

                return DataType.Binary(size);
            }

            if (value.StartsWith("entity"))
            {
                if (!value.Contains('(', StringComparison.Ordinal))
                {
                    return DataType.Entity(); // reference only union
                }

                source = value.AsSpan(7..(value.Length - 1));

                if (!int.TryParse(source, out int code))
                {
                    throw new FormatException();
                }

                return DataType.Entity(code);
            }

            if (value.StartsWith("union"))
            {
                // Структура DataType в "разобранном" виде
                DataTypeFlags types = DataTypeFlags.Undefined;
                QualifierFlags flags = QualifierFlags.None;
                ushort size = 0;
                byte precision = 0;
                byte scale = 0;
                int typeCode = 0;

                source = value.AsSpan(6..(value.Length - 1));

                int count = source.Split(qualifiers, '|');

                for (int i = 0; i < count; i++)
                {
                    if (source[qualifiers[i]].SequenceEqual("boolean"))
                    {
                        types |= DataTypeFlags.Boolean;
                    }
                    else if (source[qualifiers[i]].StartsWith("decimal"))
                    {
                        ParseDecimal(source[qualifiers[i]], ref types, ref precision, ref scale);
                    }
                    else if (source[qualifiers[i]].SequenceEqual("date"))
                    {
                        types |= DataTypeFlags.DateTime;
                        flags |= QualifierFlags.Date;
                    }
                    else if (source[qualifiers[i]].SequenceEqual("time"))
                    {
                        types |= DataTypeFlags.DateTime;
                        flags |= QualifierFlags.Time;
                    }
                    else if (source[qualifiers[i]].SequenceEqual("datetime"))
                    {
                        types |= DataTypeFlags.DateTime;
                        flags |= QualifierFlags.DateTime;
                    }
                    else if (source[qualifiers[i]].StartsWith("string"))
                    {
                        ParseString(source[qualifiers[i]], ref types, ref flags, ref size);
                    }
                    else if (source[qualifiers[i]].StartsWith("entity"))
                    {
                        ParseEntity(source[qualifiers[i]], ref types, ref typeCode);
                    }    
                }

                return new DataType(types, flags, size, precision, scale, typeCode);
            }

            return DataType.Undefined;
        }
        private static void ParseDecimal(ReadOnlySpan<char> source, ref DataTypeFlags type, ref byte precision, ref byte scale)
        {
            type |= DataTypeFlags.Decimal;

            Span<Range> qualifiers = stackalloc Range[2];

            source = source[8..(source.Length - 1)]; // Отсекаем 'decimal(' в начале и ')' на конце

            int count = source.Split(qualifiers, ',');

            if (!byte.TryParse(source[qualifiers[0]], out precision))
            {
                throw new FormatException();
            }

            if (!byte.TryParse(source[qualifiers[1]], out scale))
            {
                throw new FormatException();
            }
        }
        private static void ParseString(ReadOnlySpan<char> source, ref DataTypeFlags type, ref QualifierFlags flags, ref ushort size)
        {
            type |= DataTypeFlags.String;

            if (!source.Contains('('))
            {
                return;
            }

            Span<Range> qualifiers = stackalloc Range[2];

            source = source[7..(source.Length - 1)]; // Отсекаем 'string(' в начале и ')' на конце

            int count = source.Split(qualifiers, ',');

            if (!ushort.TryParse(source[qualifiers[0]], out size))
            {
                throw new FormatException();
            }

            if (count > 1 && source[qualifiers[1]].SequenceEqual("fixed"))
            {
                flags |= QualifierFlags.Fixed;
            }
        }
        private static void ParseEntity(ReadOnlySpan<char> source, ref DataTypeFlags type, ref int typeCode)
        {
            type |= DataTypeFlags.Entity;

            if (!source.Contains('('))
            {
                return;
            }

            source = source[7..(source.Length - 1)]; // Отсекаем 'entity(' в начале и ')' на конце

            if (!int.TryParse(source, out typeCode))
            {
                throw new FormatException();
            }
        }
    }
}