using System;
using System.Text.Json.Nodes;

namespace AntiCaptchaApi.Net.Internal.Json;

/// <summary>
/// Converts a <c>pushAntiGateVariable</c> value to JSON. Source generation cannot serialize an arbitrary
/// <see cref="object"/>, so only strings, booleans, numbers and <see cref="JsonNode"/> are accepted (spec FR-8).
/// Error messages name the type, never the value, which may be a secret (for example a password for the template).
/// </summary>
internal static class AntiGateVariableValue
{
    public static JsonNode ToJsonNode(object value, string paramName) => value switch
    {
        null => throw new ArgumentNullException(paramName),
        JsonNode node => node,
        string text => JsonValue.Create(text),
        bool flag => JsonValue.Create(flag),
        sbyte number => JsonValue.Create(number),
        byte number => JsonValue.Create(number),
        short number => JsonValue.Create(number),
        ushort number => JsonValue.Create(number),
        int number => JsonValue.Create(number),
        uint number => JsonValue.Create(number),
        long number => JsonValue.Create(number),
        ulong number => JsonValue.Create(number),
        decimal number => JsonValue.Create(number),
        float number when IsFinite(number) => JsonValue.Create(number),
        double number when IsFinite(number) => JsonValue.Create(number),
        float or double => throw new ArgumentException("The value must be a finite number (JSON has no NaN or Infinity).", paramName),
        _ => throw new ArgumentException(
            $"Unsupported value type {value.GetType().FullName}; use string, bool, an integral or floating-point number, decimal, or JsonNode.",
            paramName),
    };

    private static bool IsFinite(double number) => !double.IsNaN(number) && !double.IsInfinity(number);
}
