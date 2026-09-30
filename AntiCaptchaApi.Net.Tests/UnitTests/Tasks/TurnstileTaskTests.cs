using System.Linq;
using AntiCaptchaApi.Net.Internal.Json;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Tasks;

public class TurnstileTaskTests
{
    [Fact]
    public void Validate_InvalidUrlAndEmptyKey_ReportsBoth()
    {
        var task = new TurnstileTask("not-a-url", "");

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal(new[] { "WebsiteUrl", "WebsiteKey" }, exception.Errors.Select(e => e.Field));
    }

    [Fact]
    public void Validate_ProxyWithoutUserAgent_Fails()
    {
        var task = new TurnstileTask("https://example.com/login", "SITE_KEY") { Proxy = new Proxy(ProxyType.Http, "203.0.113.10", 8080) };

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal(new ValidationError("UserAgent", "required when Proxy is set"), Assert.Single(exception.Errors));
    }

    [Fact]
    public void Validate_ProxyWithUserAgent_Passes()
    {
        var task = new TurnstileTask("https://example.com/login", "SITE_KEY")
        {
            Proxy = new Proxy(ProxyType.Http, "203.0.113.10", 8080),
            UserAgent = "Mozilla/5.0 (probe)",
        };

        task.RunValidation();
    }

    [Fact]
    public void Serialize_OptionalFields_UseActionCDataChlPageDataNames()
    {
        var task = new TurnstileTask("https://example.com/login", "SITE_KEY") { Action = "login", CData = "cdata", ChlPageData = "page" };

        var node = TaskSerializer.ToJsonObject(task);

        Assert.Equal("login", (string?)node["action"]);
        Assert.Equal("cdata", (string?)node["cData"]);
        Assert.Equal("page", (string?)node["chlPageData"]);
    }

    [Fact]
    public void TypeName_WithoutProxy_IsTurnstileTaskProxyless()
    {
        Assert.Equal("TurnstileTaskProxyless", new TurnstileTask("https://example.com", "K").ResolveTypeName());
    }
}
