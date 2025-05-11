using AntiCaptchaApi.Net.Responses;

namespace AntiCaptchaApi.Net.Internal.Models;

internal class GetAppStatsPayload(string clientKey, int softId, string mode = null)
    : SoftAndClientPayload<GetAppStatsResponse>(clientKey, softId)
{
    public string Mode { get; } = mode;
}