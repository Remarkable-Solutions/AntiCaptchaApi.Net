using AntiCaptchaApi.Net.Responses;

namespace AntiCaptchaApi.Net.Internal.Models
{
    internal class GetBalancePayload : ClientPayload<BalanceResponse>
    {
        public GetBalancePayload(string clientKey) : base(clientKey)
        {
        }
    }
}