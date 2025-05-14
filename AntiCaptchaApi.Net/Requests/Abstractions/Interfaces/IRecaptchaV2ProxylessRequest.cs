using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces.Args;

namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces
{
    public interface IRecaptchaV2ProxylessRequest : IWebCaptchaRequest<RecaptchaSolution>, IIsInvisibleArg
    {
        public string RecaptchaDataSValue { get; set; }
    }
}