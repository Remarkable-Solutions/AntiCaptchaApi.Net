using AntiCaptchaApi.Net.Responses.Abstractions;

namespace AntiCaptchaApi.Net.Internal.Models;

internal class ClientPayload<TResponse>(string clientKey) : Payload<TResponse>
    where TResponse : BaseResponse, new()
{
    public string ClientKey { get; set; } = clientKey;
}