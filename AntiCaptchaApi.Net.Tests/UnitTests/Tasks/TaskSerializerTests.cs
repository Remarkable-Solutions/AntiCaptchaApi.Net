using System;
using AntiCaptchaApi.Net.Internal.Json;
using AntiCaptchaApi.Net.Tasks;
using AntiCaptchaApi.Net.Tests.UnitTests.Infrastructure;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Tasks;

public class TaskSerializerTests
{
    private const string Url = "https://example.com/login";
    private const string Key = "SITE_KEY";

    [Fact]
    public void ToJsonObject_WithoutProxy_WritesTypeProxyless_AndNoProxyFields()
    {
        var task = new TurnstileTask(Url, Key);

        var json = TaskSerializer.ToJsonObject(task).ToJsonString();

        SnapshotAssert.Equal("{\"type\":\"TurnstileTaskProxyless\",\"websiteURL\":\"https://example.com/login\",\"websiteKey\":\"SITE_KEY\"}", json);
    }

    [Fact]
    public void ToJsonObject_WithProxy_FlattensAllFiveProxyFields()
    {
        var task = new TurnstileTask(Url, Key)
        {
            UserAgent = "Mozilla/5.0 (probe)",
            Proxy = new Proxy(ProxyType.Socks5, "203.0.113.10", 1080) { Login = "proxyUser", Password = "proxyPass" },
        };

        var json = TaskSerializer.ToJsonObject(task).ToJsonString();

        SnapshotAssert.Equal(
            "{\"type\":\"TurnstileTask\",\"websiteURL\":\"https://example.com/login\",\"websiteKey\":\"SITE_KEY\",\"userAgent\":\"Mozilla/5.0 (probe)\"," +
            "\"proxyType\":\"socks5\",\"proxyAddress\":\"203.0.113.10\",\"proxyPort\":1080,\"proxyLogin\":\"proxyUser\",\"proxyPassword\":\"proxyPass\"}",
            json);
    }

    [Fact]
    public void ToJsonObject_ProxyWithoutCredentials_OmitsLoginAndPassword()
    {
        var task = new TurnstileTask(Url, Key) { UserAgent = "UA", Proxy = new Proxy(ProxyType.Http, "203.0.113.10", 8080) };

        var node = TaskSerializer.ToJsonObject(task);

        Assert.Equal("http", (string?)node["proxyType"]);
        Assert.Equal(8080, (int)node["proxyPort"]!);
        Assert.False(node.ContainsKey("proxyLogin"));
        Assert.False(node.ContainsKey("proxyPassword"));
    }

    [Fact]
    public void ToJsonObject_DoesNotEmitProxyObjectOrInternalMembers()
    {
        var task = new TurnstileTask(Url, Key) { UserAgent = "UA", Proxy = new Proxy(ProxyType.Http, "203.0.113.10", 8080) };

        var node = TaskSerializer.ToJsonObject(task);

        foreach (var key in new[] { "proxy", "Proxy", "typeBase", "proxyMode", "firstPollDelay", "resolvedFirstPollDelay", "equalityContract" })
        {
            Assert.False(node.ContainsKey(key), $"unexpected key '{key}'");
        }
    }

    [Fact]
    public void ToJsonObject_TypeIsFirstKey()
    {
        var node = TaskSerializer.ToJsonObject(new TurnstileTask(Url, Key));

        Assert.Equal("{\"type\":\"TurnstileTaskProxyless\",", node.ToJsonString().Substring(0, 33));
    }

    [Fact]
    public void ToJsonObject_UnregisteredType_ThrowsInvalidOperationWithTypeName()
    {
        var task = new OptionalTestTask(Key);

        var exception = Assert.Throws<InvalidOperationException>(() => TaskSerializer.ToJsonObject(task));

        Assert.Contains(nameof(OptionalTestTask), exception.Message);
        Assert.Contains("not registered in AntiCaptchaJsonContext", exception.Message);
    }
}
