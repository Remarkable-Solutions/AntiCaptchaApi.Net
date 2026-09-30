using System.Text;

namespace AntiCaptchaApi.Net;

/// <summary>
/// A proxy that anti-captcha workers use to solve a task from the proxy's IP address.
/// Its fields are sent flattened into the task (<c>proxyType</c>, <c>proxyAddress</c>, <c>proxyPort</c>,
/// <c>proxyLogin</c>, <c>proxyPassword</c>). <see cref="ToString"/> never prints the password.
/// </summary>
/// <param name="Type">The proxy protocol.</param>
/// <param name="Address">The proxy host name or IP address.</param>
/// <param name="Port">The proxy port, 1–65535.</param>
public sealed record Proxy(ProxyType Type, string Address, int Port)
{
    /// <summary>The proxy user name, or <see langword="null"/> when the proxy needs no authentication.</summary>
    public string? Login { get; init; }

    /// <summary>The proxy password, or <see langword="null"/>. It is redacted as <c>***</c> in <see cref="ToString"/>.</summary>
    public string? Password { get; init; }

    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Type = ").Append(Type)
            .Append(", Address = ").Append(Address)
            .Append(", Port = ").Append(Port)
            .Append(", Login = ").Append(Login)
            .Append(", Password = ").Append(Password is null ? null : "***");
        return true;
    }
}
