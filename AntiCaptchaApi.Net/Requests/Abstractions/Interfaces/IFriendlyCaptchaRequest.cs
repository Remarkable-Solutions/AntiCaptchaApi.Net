using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces.Args;

namespace AntiCaptchaApi.Net.Requests.Abstractions.Interfaces
{
    /// <summary>
    /// Defines the contract for a FriendlyCaptchaTask request (with proxy).
    /// </summary>
    public interface IFriendlyCaptchaRequest : IFriendlyCaptchaProxylessRequest, IProxyConfigWithUserAgentArgs
    {
    
    }
}