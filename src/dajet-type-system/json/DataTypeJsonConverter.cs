using DaJet.TypeSystem;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DaJet.Json
{
    public sealed class DataTypeJsonConverter : JsonConverter<DataType>
    {
        public override void Write(Utf8JsonWriter writer, DataType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
        public override DataType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new FormatException();
            }

            string value = reader.GetString();

            return DataType.Parse(in value, out _);
        }
    }
}