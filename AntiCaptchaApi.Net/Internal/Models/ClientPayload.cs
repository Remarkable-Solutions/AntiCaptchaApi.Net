using AntiCaptchaApi.Net.Responses.Abstractions;

namespace AntiCaptchaApi.Net.Internal.Models
{
    internal class ClientPayload<TResponse> : Payload<TResponse>
        where TResponse : BaseResponse, new()
    {
        public ClientPayload(string clientKey)
        {
            ClientKey = clientKey;
        }

        public string ClientKey { get; set; }
    }
}