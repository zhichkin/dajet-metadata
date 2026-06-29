using DaJet.TypeSystem;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DaJet.Json
{
    public sealed class EntityJsonConverter : JsonConverter<Entity>
    {
        public override void Write(Utf8JsonWriter writer, Entity value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
        public override Entity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new FormatException();
            }

            string value = reader.GetString();

            return Entity.Parse(value);
        }
    }
}