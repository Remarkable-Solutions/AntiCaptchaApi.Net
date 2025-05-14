using AntiCaptchaApi.Net.Responses;

namespace AntiCaptchaApi.Net.Internal.Models
{
    internal class GetAppStatsPayload : SoftAndClientPayload<GetAppStatsResponse>
    {
        public GetAppStatsPayload(string clientKey, int softId, string mode = null) : base(clientKey, softId)
        {
            Mode = mode;
        }

        public string Mode { get; }
    }
}