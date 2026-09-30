using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Internal.Json.Envelopes;

/// <summary>Body of the account calls that need only the key, for example <c>POST getBalance</c>.</summary>
internal sealed class ClientKeyRequest
{
    public ClientKeyRequest(string clientKey)
    {
        ClientKey = clientKey;
    }

    [JsonPropertyName("clientKey")]
    public string ClientKey { get; }
}
