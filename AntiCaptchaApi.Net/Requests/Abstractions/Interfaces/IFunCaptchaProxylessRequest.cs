using AntiCaptchaApi.Net.Models.Solutions;

namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces
{
    public interface IFunCaptchaProxylessRequest : ICaptchaRequest<FunCaptchaSolution>
    {
        public string WebsitePublicKey { get; set; }
        public string FunCaptchaApiJsSubdomain { get; set; }
        public string Data { get; set; }
    }
}