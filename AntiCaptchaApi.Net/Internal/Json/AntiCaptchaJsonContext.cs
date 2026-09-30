using System.Text.Json.Serialization;
using AntiCaptchaApi.Net.Internal.Json.Envelopes;
using AntiCaptchaApi.Net.Tasks;

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
// Tasks and their solutions (one pair per catalog type; guarded by TaskSnapshotTests).
[JsonSerializable(typeof(TurnstileTask))]
[JsonSerializable(typeof(TurnstileSolution))]
[JsonSerializable(typeof(ImageToTextTask))]
[JsonSerializable(typeof(ImageToTextSolution))]
[JsonSerializable(typeof(ImageToCoordinatesTask))]
[JsonSerializable(typeof(ImageToCoordinatesSolution))]
[JsonSerializable(typeof(RecaptchaV2Task))]
[JsonSerializable(typeof(RecaptchaV2EnterpriseTask))]
[JsonSerializable(typeof(RecaptchaSolution))]
internal sealed partial class AntiCaptchaJsonContext : JsonSerializerContext
{
}
