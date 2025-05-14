using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces.Args;

namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces
{
    public interface IWebCaptchaRequest<TSolution> : ICaptchaRequest<TSolution>, IWebsiteKeyArg
        where TSolution : BaseSolution  
    {
    
    }
}