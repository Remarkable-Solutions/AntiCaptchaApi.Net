using AntiCaptchaApi.Net.Responses.Abstractions;

namespace AntiCaptchaApi.Net.Internal.Models;

internal abstract class SoftAndClientPayload<TResponse>(string clientKey, int softId = Payload<TResponse>.DefaultSoftId)
    : ClientPayload<TResponse>(clientKey)
    where TResponse : BaseResponse, new()
{
    public int SoftId { get;  set; } = softId;
}