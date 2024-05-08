using System;
using Newtonsoft.Json;
using Rust.SignLogger.Ids;

namespace Rust.SignLogger.Json;

public class ButtonIdConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        ButtonId id = (ButtonId)value;
        writer.WriteValue(id.Id);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
        {
            return new ButtonId(reader.Value.ToString());
        }
                
        throw new JsonException($"Unexpected token {reader.TokenType} when parsing ButtonID.");
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(ButtonId) == objectType;
    }
}