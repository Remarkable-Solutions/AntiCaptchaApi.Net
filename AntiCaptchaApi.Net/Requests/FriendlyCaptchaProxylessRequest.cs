using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces;

namespace AntiCaptchaApi.Net.Requests;

/// <summary>
/// Represents a request for solving Friendly Captcha without proxies.
/// </summary>
public class FriendlyCaptchaProxylessRequest : WebsiteCaptchaRequest<ProsopoSolution>, IFriendlyCaptchaProxylessRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FriendlyCaptchaProxylessRequest"/> class.
    /// </summary>
    public FriendlyCaptchaProxylessRequest()
    {
            
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FriendlyCaptchaProxylessRequest"/> class.
    /// </summary>
    /// <param name="websiteUrl">Address of a target web page.</param>
    /// <param name="websiteKey">Friendly Captcha sitekey.</param>
    public FriendlyCaptchaProxylessRequest(string websiteUrl, string websiteKey)
    {
        WebsiteUrl = websiteUrl;
        WebsiteKey = websiteKey;
    }
}