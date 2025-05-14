using AntiCaptchaApi.Net.Models;

namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces.Args
{
    public interface IProxyConfigArg : IRequestArg
    {
        public ProxyConfig ProxyConfig { get; set; }
    }
}