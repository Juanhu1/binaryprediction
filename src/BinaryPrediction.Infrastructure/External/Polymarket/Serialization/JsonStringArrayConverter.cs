using System.Text.Json;
using System.Text.Json.Serialization;

namespace BinaryPrediction.Infrastructure.External.Polymarket.Serialization;

public class JsonStringArrayConverter : JsonConverter<IReadOnlyList<string>?>
{
    public override IReadOnlyList<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var rawValue = reader.GetString();
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return [];
            }

            return JsonSerializer.Deserialize<IReadOnlyList<string>>(rawValue, options);
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            return JsonSerializer.Deserialize<IReadOnlyList<string>>(ref reader, options);
        }

        throw new JsonException($"Unexpected token {reader.TokenType} for string array.");
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlyList<string>? value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
