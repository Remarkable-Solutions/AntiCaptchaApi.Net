using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces;

namespace AntiCaptchaApi.Net.Requests;

/// <summary>
/// Represents a request for solving Prosopo captcha with proxies.
/// </summary>
public class ProsopoRequest : ProsopoProxylessRequest, IProsopoRequest
{
    /// <summary>
    /// [Required]
    /// Browser's User-Agent used in emulation.
    /// You must use a modern-browser signature.
    /// </summary>
    public string UserAgent { get; set; }

    /// <summary>
    /// [Required] ProxyConfig.ProxyType : Type of proxy http/socks4/socks5.
    /// [Required] ProxyConfig.proxyAddress : Proxy IP address ipv4/ipv6. No host names or IP addresses from local networks.
    /// [Required] ProxyConfig.proxyPort : Proxy port.
    /// [Optional] ProxyConfig.proxyLogin : Login for proxy which requires authorization (basic)
    /// [Optional] ProxyConfig.proxyPassword : Proxy password
    /// </summary>
    public ProxyConfig ProxyConfig { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProsopoRequest"/> class.
    /// </summary>
    public ProsopoRequest()
    {
            
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProsopoRequest"/> class.
    /// </summary>
    /// <param name="websiteUrl">Address of a target web page.</param>
    /// <param name="websiteKey">Prosopo sitekey.</param>
    /// <param name="userAgent">Browser's User-Agent.</param>
    /// <param name="proxyConfig">Proxy configuration.</param>
    public ProsopoRequest(string websiteUrl, string websiteKey, string userAgent, ProxyConfig proxyConfig) 
        : base(websiteUrl, websiteKey)
    {
        UserAgent = userAgent;
        ProxyConfig = proxyConfig;
    }
}