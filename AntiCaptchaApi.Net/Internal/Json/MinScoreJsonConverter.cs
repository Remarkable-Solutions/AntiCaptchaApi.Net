using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using AntiCaptchaApi.Net.Tasks;

namespace AntiCaptchaApi.Net.Internal.Json;

/// <summary>Writes <see cref="MinScore"/> as the JSON number the API expects (<c>0.3</c>, <c>0.7</c>, <c>0.9</c>) and reads it back.</summary>
internal sealed class MinScoreJsonConverter : JsonConverter<MinScore>
{
    public override MinScore Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetDecimal(out var value))
        {
            switch (value)
            {
                case 0.3m: return MinScore.Score03;
                case 0.7m: return MinScore.Score07;
                case 0.9m: return MinScore.Score09;
            }
        }
        throw new JsonException("minScore must be one of the numbers 0.3, 0.7 or 0.9.");
    }

    public override void Write(Utf8JsonWriter writer, MinScore value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(ToNumber(value));

    private static decimal ToNumber(MinScore value) => value switch
    {
        MinScore.Score03 => 0.3m,
        MinScore.Score07 => 0.7m,
        MinScore.Score09 => 0.9m,
        _ => throw new JsonException($"MinScore value {(int)value} is not defined."),
    };
}
