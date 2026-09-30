using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Internal.Json;

/// <summary>
/// Writes enum members as camelCase strings (<c>Socks5</c> → <c>"socks5"</c>, <c>Points</c> → <c>"points"</c>),
/// which is the form the anti-captcha API documents. Integer values are rejected when reading.
/// </summary>
internal sealed class LowerCaseEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
    where TEnum : struct, Enum
{
    public LowerCaseEnumConverter()
        : base(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
    {
    }
}
