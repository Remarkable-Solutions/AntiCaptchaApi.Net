using System.Text.Json;
using AntiCaptchaApi.Net.Internal.Json;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Tasks;

public class ProxyTests
{
    [Fact]
    public void ToString_WithPassword_RedactsPassword()
    {
        var proxy = new Proxy(ProxyType.Socks5, "203.0.113.10", 1080) { Login = "proxyUser", Password = "proxyPass" };

        var text = proxy.ToString();

        Assert.DoesNotContain("proxyPass", text);
        Assert.Contains("Password = ***", text);
        Assert.Contains("Login = proxyUser", text);
        Assert.Contains("Address = 203.0.113.10", text);
        Assert.Contains("Port = 1080", text);
        Assert.Contains("Type = Socks5", text);
    }

    [Fact]
    public void ToString_WithoutPassword_DoesNotPrintPlaceholder()
    {
        var proxy = new Proxy(ProxyType.Http, "203.0.113.10", 8080);

        var text = proxy.ToString();

        Assert.DoesNotContain("***", text);
        Assert.Contains("Password = ", text);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var first = new Proxy(ProxyType.Socks4, "203.0.113.10", 1080) { Login = "u", Password = "p" };
        var second = new Proxy(ProxyType.Socks4, "203.0.113.10", 1080) { Login = "u", Password = "p" };

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, second with { Password = "other" });
    }

    [Theory]
    [InlineData(ProxyType.Http, "\"http\"")]
    [InlineData(ProxyType.Socks4, "\"socks4\"")]
    [InlineData(ProxyType.Socks5, "\"socks5\"")]
    public void ProxyType_Serializes_Lowercase(ProxyType type, string expected)
    {
        var json = JsonSerializer.Serialize(type, AntiCaptchaJsonContext.Default.ProxyType);

        Assert.Equal(expected, json);
    }
}
