using System.Text.Json.Serialization;
using AntiCaptchaApi.Net.Internal.Json.Envelopes;

namespace AntiCaptchaApi.Net.Internal.Json;

/// <summary>
/// Source-generated serialization metadata for every type that crosses the wire (AOT/trim-safe, no reflection).
/// Output is compact and omits nulls. Every wire property also carries an explicit <see cref="JsonPropertyNameAttribute"/>.
/// </summary>
[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ApiErrorEnvelope))]
[JsonSerializable(typeof(CreateTaskRequest))]
[JsonSerializable(typeof(CreateTaskResponse))]
[JsonSerializable(typeof(ProxyType))]
internal sealed partial class AntiCaptchaJsonContext : JsonSerializerContext
{
}
