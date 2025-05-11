using AntiCaptchaApi.Net.Enums;
using Newtonsoft.Json;

namespace AntiCaptchaApi.Net.Models
{
    /// <summary>
    /// Represents the configuration for a proxy server to be used with certain captcha tasks (e.g., RecaptchaV2Request).
    /// </summary>
    public class ProxyConfig
    {
        /// <summary>
        /// Gets or sets the type of the proxy (e.g., Http, Https, Socks4, Socks5).
        /// Required.
        /// </summary>
        public ProxyTypeOption ProxyType {  get; set; }

        /// <summary>
        /// Gets or sets the login username for the proxy.
        /// Required if the proxy requires authentication.
        /// </summary>
        public string ProxyLogin {  get; set; }

        /// <summary>
        /// Gets or sets the login password for the proxy.
        /// Required if the proxy requires authentication.
        /// </summary>
        public string ProxyPassword {  get; set; }

        /// <summary>
        /// Gets or sets the port number of the proxy server.
        /// Required.
        /// </summary>
        public int? ProxyPort { get; set; } // Marked as nullable, but API likely requires it. Documentation reflects requirement.

        /// <summary>
        /// Gets or sets the IP address or hostname of the proxy server.
        /// Required.
        /// </summary>
        public string ProxyAddress { get; set; }
    }
}