using System.Text.Json.Serialization;
using AntiCaptchaApi.Net.Internal.Json;

namespace AntiCaptchaApi.Net;

/// <summary>The protocol of a <see cref="Proxy"/>. Sent to the API in lowercase (<c>http</c>, <c>socks4</c>, <c>socks5</c>).</summary>
[JsonConverter(typeof(LowerCaseEnumConverter<ProxyType>))]
public enum ProxyType
{
    /// <summary>HTTP or HTTPS proxy (<c>http</c>).</summary>
    Http,

    /// <summary>SOCKS4 proxy (<c>socks4</c>).</summary>
    Socks4,

    /// <summary>SOCKS5 proxy (<c>socks5</c>).</summary>
    Socks5,
}
