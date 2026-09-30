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
[JsonSerializable(typeof(GetTaskResultRequest))]
[JsonSerializable(typeof(GetTaskResultResponse))]
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
[JsonSerializable(typeof(RecaptchaV3Task))]
[JsonSerializable(typeof(RecaptchaSolution))]
[JsonSerializable(typeof(FunCaptchaTask))]
[JsonSerializable(typeof(FunCaptchaSolution))]
[JsonSerializable(typeof(GeeTestV3Task))]
[JsonSerializable(typeof(GeeTestV3Solution))]
[JsonSerializable(typeof(GeeTestV4Task))]
[JsonSerializable(typeof(GeeTestV4Solution))]
[JsonSerializable(typeof(AmazonWafTask))]
[JsonSerializable(typeof(AmazonWafSolution))]
[JsonSerializable(typeof(ProsopoTask))]
[JsonSerializable(typeof(ProsopoSolution))]
[JsonSerializable(typeof(FriendlyCaptchaTask))]
[JsonSerializable(typeof(FriendlyCaptchaSolution))]
[JsonSerializable(typeof(AntiGateTask))]
[JsonSerializable(typeof(AntiGateSolution))]
internal sealed partial class AntiCaptchaJsonContext : JsonSerializerContext
{
}
