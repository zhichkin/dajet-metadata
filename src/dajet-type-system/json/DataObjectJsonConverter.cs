using DaJet.TypeSystem;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DaJet.Json
{
    public sealed class DataObjectJsonConverter : JsonConverter<DataObject>
    {
        public override void Write(Utf8JsonWriter writer, DataObject source, JsonSerializerOptions options)
        {
            string name;
            object value;

            writer.WriteStartObject();

            for (int ordinal = 0; ordinal < source.Count; ordinal++)
            {
                name = source.GetName(ordinal);
                value = source.GetValue(name);

                if (value is null) { writer.WriteNull(name); }
                else if (value is bool boolean) { writer.WriteBoolean(name, boolean); }
                else if (value is decimal dec16) { writer.WriteNumber(name, dec16); }
                else if (value is sbyte int1) { writer.WriteNumber(name, int1); }
                else if (value is short int2) { writer.WriteNumber(name, int2); }
                else if (value is int int4) { writer.WriteNumber(name, int4); }
                else if (value is long int8) { writer.WriteNumber(name, int8); }
                else if (value is byte uint1) { writer.WriteNumber(name, uint1); }
                else if (value is ushort uint2) { writer.WriteNumber(name, uint2); }
                else if (value is uint uint4) { writer.WriteNumber(name, uint4); }
                else if (value is ulong uint8) { writer.WriteNumber(name, uint8); }
                else if (value is DateTime dateTime) { writer.WriteString(name, dateTime.ToString("yyyy-MM-ddTHH:mm:ss")); }
                else if (value is string text) { writer.WriteString(name, text); }
                else if (value is byte[] binary) { writer.WriteString(name, Convert.ToBase64String(binary)); }
                else if (value is Guid uuid) { writer.WriteString(name, uuid); }
                else if (value is Entity entity) { writer.WriteString(name, entity.ToString()); }
                else if (value is DataType type) { writer.WriteString(name, type.ToString()); }
                else if (value is Union union)
                {
                    if (union.Tag == UnionTag.Undefined)
                    {
                        writer.WriteNull(name);
                    }
                    else if (union.Tag == UnionTag.Boolean)
                    {
                        writer.WriteBoolean(name, union.GetBoolean());
                    }
                    else if (union.Tag == UnionTag.Decimal)
                    {
                        writer.WriteNumber(name, union.GetDecimal());
                    }
                    else if (union.Tag == UnionTag.DateTime)
                    {
                        writer.WriteString(name, union.GetDateTime().ToString("yyyy-MM-ddTHH:mm:ss"));
                    }
                    else if (union.Tag == UnionTag.String)
                    {
                        writer.WriteString(name, union.GetString());
                    }
                    else if (union.Tag == UnionTag.Entity)
                    {
                        writer.WriteString(name, union.GetEntity().ToString());
                    }
                }
                else if (value is DataObject _object)
                {
                    writer.WritePropertyName(name);
                    Write(writer, _object, options);
                }
                else if (value is List<DataObject> _array)
                {
                    writer.WritePropertyName(name);
                    writer.WriteStartArray();
                    foreach (DataObject _item in _array)
                    {
                        Write(writer, _item, options);
                    }
                    writer.WriteEndArray();
                }
                else if (value is List<bool> array_boolean)
                {
                    writer.WritePropertyName(name);
                    writer.WriteStartArray();
                    foreach (bool item_boolean in array_boolean)
                    {
                        writer.WriteBooleanValue(item_boolean);
                    }
                    writer.WriteEndArray();
                }
                else if (value is List<decimal> array_dec16)
                {
                    writer.WritePropertyName(name);
                    writer.WriteStartArray();
                    foreach (decimal item_dec16 in array_dec16)
                    {
                        writer.WriteNumberValue(item_dec16);
                    }
                    writer.WriteEndArray();
                }
                else if (value is List<sbyte> array_int1)
                {
                    writer.WritePropertyName(name);
                    writer.WriteStartArray();
                    foreach (sbyte item_int1 in array_int1)
                    {
                        writer.WriteNumberValue(item_int1);
                    }
                    writer.WriteEndArray();

                }
                else if (value is List<short> array_int2)
                {
                    writer.WritePropertyName(name);
                    writer.WriteStartArray();
                    foreach (short item_int2 in array_int2)
                    {
                        writer.WriteNumberValue(item_int2);
                    }
                    writer.WriteEndArray();
                }
                else if (value is List<int> array_int4)
                {
                    writer.WritePropertyName(name);
                    writer.WriteStartArray();
                    foreach (int item_int4 in array_int4)
                    {
                        writer.WriteNumberValue(item_int4);
                    }
                    writer.WriteEndArray();
                }
                else if (value is List<long> array_int8)
                {
                    writer.WritePropertyName(name);
                    writer.WriteStartArray();
                    foreach (long item_int8 in array_int8)
                    {
                        writer.WriteNumberValue(item_int8);
                    }
                    writer.WriteEndArray();
                }
                else if (value is List<byte> array_uint1)
                {
                    writer.WritePropertyName(name);
                    writer.WriteStartArray();
                    foreach (byte item_uint1 in array_uint1)
                    {
                        writer.WriteNumberValue(item_uint1);
                    }
                    writer.WriteEndArray();
                }
                else if (value is List<ushort> array_uint2)
                {
                    writer.WritePropertyName(name);
                    writer.WriteStartArray();
                    foreach (ushort item_uint2 in array_uint2)
                    {
                        writer.WriteNumberValue(item_uint2);
                    }
                    writer.WriteEndArray();
                }
                else if (value is List<uint> array_uint4)
                {
                    writer.WritePropertyName(name);
                    writer.WriteStartArray();
                    foreach (uint item_uin4 in array_uint4)
                    {
                        writer.WriteNumberValue(item_uin4);
                    }
                    writer.WriteEndArray();
                }
                else if (value is List<ulong> array_uint8)
                {
                    writer.WritePropertyName(name);
                    writer.WriteStartArray();
                    foreach (ulong item_uin8 in array_uint8)
                    {
                        writer.WriteNumberValue(item_uin8);
                    }
                    writer.WriteEndArray();
                }
                else if (value is List<DateTime> array_dateTime)
                {
                    writer.WritePropertyName(name);
                    writer.WriteStartArray();
                    foreach (DateTime item_datetime in array_dateTime)
                    {
                        writer.WriteStringValue(item_datetime.ToString("yyyy-MM-ddTHH:mm:ss"));
                    }
                    writer.WriteEndArray();
                }
                else if (value is List<string> array_text)
                {
                    writer.WritePropertyName(name);
                    writer.WriteStartArray();
                    foreach (string item_text in array_text)
                    {
                        writer.WriteStringValue(item_text);
                    }
                    writer.WriteEndArray();
                }
                else if (value is List<byte[]> array_binary)
                {
                    writer.WritePropertyName(name);
                    writer.WriteStartArray();
                    foreach (byte[] item_binary in array_binary)
                    {
                        writer.WriteStringValue(Convert.ToBase64String(item_binary));
                    }
                    writer.WriteEndArray();
                }
                else if (value is List<Guid> array_uuid)
                {
                    writer.WritePropertyName(name);
                    writer.WriteStartArray();
                    foreach (Guid item_uuid in array_uuid)
                    {
                        writer.WriteStringValue(item_uuid);
                    }
                    writer.WriteEndArray();
                }
                else if (value is List<Entity> array_entity)
                {
                    writer.WritePropertyName(name);
                    writer.WriteStartArray();
                    foreach (Entity item_entity in array_entity)
                    {
                        writer.WriteStringValue(item_entity.ToString());
                    }
                    writer.WriteEndArray();
                }
                else if (value is List<DataType> array_type)
                {
                    writer.WritePropertyName(name);
                    writer.WriteStartArray();
                    foreach (DataType item_type in array_type)
                    {
                        writer.WriteStringValue(item_type.ToString());
                    }
                    writer.WriteEndArray();
                }
                else if (value is List<Union> array_union)
                {
                    writer.WritePropertyName(name);
                    writer.WriteStartArray();
                    foreach (Union item_union in array_union)
                    {
                        if (item_union.Tag == UnionTag.Undefined)
                        {
                            writer.WriteNull(name);
                        }
                        else if (item_union.Tag == UnionTag.Boolean)
                        {
                            writer.WriteBoolean(name, item_union.GetBoolean());
                        }
                        else if (item_union.Tag == UnionTag.Decimal)
                        {
                            writer.WriteNumber(name, item_union.GetDecimal());
                        }
                        else if (item_union.Tag == UnionTag.DateTime)
                        {
                            writer.WriteString(name, item_union.GetDateTime().ToString("yyyy-MM-ddTHH:mm:ss"));
                        }
                        else if (item_union.Tag == UnionTag.String)
                        {
                            writer.WriteString(name, item_union.GetString());
                        }
                        else if (item_union.Tag == UnionTag.Entity)
                        {
                            writer.WriteString(name, item_union.GetEntity().ToString());
                        }
                    }
                    writer.WriteEndArray();
                }
            }

            writer.WriteEndObject();
        }

        public override DataObject Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            return ParseObject(ref reader);
        }
        private DataObject ParseObject(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            DataObject tagret = new();

            string key = null;
            object value = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break; // end of target object - return result
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    key = reader.GetString(); continue;
                }

                if (reader.TokenType == JsonTokenType.Null)
                {
                    value = null;
                }
                else if (reader.TokenType == JsonTokenType.True)
                {
                    value = true;
                }
                else if (reader.TokenType == JsonTokenType.False)
                {
                    value = false;
                }
                else if (reader.TokenType == JsonTokenType.Number)
                {
                    decimal number = reader.GetDecimal();

                    if (number.Scale > 0)
                    {
                        value = number;
                    }
                    else
                    {
                        value = Convert.ToInt32(number);
                    }
                }
                else if (reader.TokenType == JsonTokenType.String)
                {
                    string input = reader.GetString();

                    if (Guid.TryParse(input, out Guid uuid))
                    {
                        value = uuid;
                    }
                    else if (DateTime.TryParse(input, out DateTime date))
                    {
                        value = date;
                    }
                    else if (Entity.TryParse(input, out Entity entity))
                    {
                        value = entity;
                    }
                    else if (DataType.TryParse(in input, out DataType type, out _))
                    {
                        value = type;
                    }
                    else
                    {
                        value = input;
                    }
                }
                else if (reader.TokenType == JsonTokenType.StartObject)
                {
                    value = ParseObject(ref reader);
                }
                else if (reader.TokenType == JsonTokenType.StartArray)
                {
                    value = ParseArray(ref reader);
                }
                else
                {
                    throw new JsonException();
                }

                tagret.SetValue(key, value);
            }

            return tagret;
        }
        private object ParseArray(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }

            IList array = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    break; // end of current array - return result
                }

                if (reader.TokenType == JsonTokenType.True)
                {
                    array ??= new List<bool>(); array.Add(true);
                }
                else if (reader.TokenType == JsonTokenType.False)
                {
                    array ??= new List<bool>(); array.Add(false);
                }
                else if (reader.TokenType == JsonTokenType.Number)
                {
                    decimal number = reader.GetDecimal();

                    if (number.Scale > 0)
                    {
                        array ??= new List<decimal>(); array.Add(number);
                    }
                    else
                    {
                        array ??= new List<int>(); array.Add(Convert.ToInt32(number));
                    }
                }
                else if (reader.TokenType == JsonTokenType.String)
                {
                    string text = reader.GetString();

                    if (Guid.TryParse(text, out Guid uuid))
                    {
                        array ??= new List<Guid>(); array.Add(uuid);
                    }
                    else if (DateTime.TryParse(text, out DateTime date))
                    {
                        array ??= new List<DateTime>(); array.Add(date);
                    }
                    else if (Entity.TryParse(text, out Entity entity))
                    {
                        array ??= new List<Entity>(); array.Add(entity);
                    }
                    else if (DataType.TryParse(in text, out DataType type, out _))
                    {
                        array ??= new List<DataType>(); array.Add(type);
                    }
                    else
                    {
                        array ??= new List<string>(); array.Add(text);
                    }
                }
                else if (reader.TokenType == JsonTokenType.StartObject)
                {
                    array ??= new List<DataObject>();

                    DataObject item = ParseObject(ref reader);

                    array.Add(item);
                }
                else // StartArray | Comment | None
                {
                    throw new JsonException();
                }
            }

            return array is not null ? array : new List<object>(); // empty array
        }
    }
}