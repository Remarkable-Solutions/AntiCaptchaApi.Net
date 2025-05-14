using AntiCaptchaApi.Net.Responses.Abstractions;

namespace AntiCaptchaApi.Net.Internal.Models
{
    internal abstract class SoftAndClientPayload<TResponse> : ClientPayload<TResponse>
        where TResponse : BaseResponse, new()
    {
        protected SoftAndClientPayload(string clientKey, int softId = Payload<TResponse>.DefaultSoftId) : base(clientKey)
        {
            SoftId = softId;
        }

        public int SoftId { get;  set; }
    }
}