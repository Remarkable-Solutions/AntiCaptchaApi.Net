using System;
using System.Linq;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Tasks;

public class CaptchaTaskTests
{
    private static readonly Proxy ValidProxy = new(ProxyType.Socks5, "203.0.113.10", 1080) { Login = "proxyUser", Password = "proxyPass" };

    [Fact]
    public void TypeName_None_IsTypeBase()
    {
        var task = new NoneTestTask("SITE_KEY");

        Assert.Equal("NoneTestTask", task.ResolveTypeName());
    }

    [Fact]
    public void TypeName_OptionalWithoutProxy_AppendsProxyless()
    {
        var task = new OptionalTestTask("SITE_KEY");

        Assert.Equal("OptionalTestTaskProxyless", task.ResolveTypeName());
    }

    [Fact]
    public void TypeName_OptionalWithProxy_IsTypeBase()
    {
        var task = new OptionalTestTask("SITE_KEY") { Proxy = ValidProxy };

        Assert.Equal("OptionalTestTask", task.ResolveTypeName());
    }

    [Fact]
    public void TypeName_Required_IsTypeBase()
    {
        var task = new RequiredTestTask("SITE_KEY") { Proxy = ValidProxy };

        Assert.Equal("RequiredTestTask", task.ResolveTypeName());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TypeName_OptionalSameName_IgnoresProxy(bool withProxy)
    {
        var task = new SameNameTestTask("SITE_KEY") { Proxy = withProxy ? ValidProxy : null };

        Assert.Equal("SameNameTestTask", task.ResolveTypeName());
    }

    [Fact]
    public void Validate_ProxyOnNoneType_FailsWithProxyNotSupported()
    {
        var task = new NoneTestTask("SITE_KEY") { Proxy = ValidProxy };

        var error = Assert.Single(Validate(task).Errors);

        Assert.Equal(new ValidationError("Proxy", "not supported by NoneTestTask"), error);
    }

    [Fact]
    public void Validate_RequiredWithoutProxy_FailsWithProxyRequired()
    {
        var task = new RequiredTestTask("SITE_KEY");

        var error = Assert.Single(Validate(task).Errors);

        Assert.Equal(new ValidationError("Proxy", "required"), error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    public void Validate_ProxyPortOutOfRange_Fails(int port)
    {
        var task = new OptionalTestTask("SITE_KEY") { Proxy = ValidProxy with { Port = port } };

        var error = Assert.Single(Validate(task).Errors);

        Assert.Equal("Proxy.Port", error.Field);
    }

    [Fact]
    public void Validate_ProxyPort65535_Passes()
    {
        var task = new OptionalTestTask("SITE_KEY") { Proxy = ValidProxy with { Port = 65535 } };

        task.RunValidation();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ProxyEmptyAddress_Fails(string address)
    {
        var task = new OptionalTestTask("SITE_KEY") { Proxy = ValidProxy with { Address = address } };

        var error = Assert.Single(Validate(task).Errors);

        Assert.Equal("Proxy.Address", error.Field);
    }

    [Fact]
    public void Validate_ProxyUndefinedType_Fails()
    {
        var task = new OptionalTestTask("SITE_KEY") { Proxy = ValidProxy with { Type = (ProxyType)42 } };

        var error = Assert.Single(Validate(task).Errors);

        Assert.Equal("Proxy.Type", error.Field);
    }

    [Fact]
    public void Validate_PerTypeRulesAndProxyRules_AreAllReported()
    {
        var task = new OptionalTestTask("") { Proxy = ValidProxy with { Address = "", Port = 0 } };

        var fields = Validate(task).Errors.Select(e => e.Field);

        Assert.Equal(new[] { "Proxy.Address", "Proxy.Port", "WebsiteKey" }, fields);
    }

    [Fact]
    public void Validate_ValidTask_DoesNotThrow()
    {
        new OptionalTestTask("SITE_KEY").RunValidation();
    }

    [Fact]
    public void Validate_ErrorMessage_DoesNotContainProxyPassword()
    {
        var task = new NoneTestTask("") { Proxy = ValidProxy with { Port = 0 } };

        var exception = Validate(task);

        Assert.DoesNotContain("proxyPass", exception.Message);
    }

    [Fact]
    public void FirstPollDelay_Default_Is10Seconds()
    {
        var task = new OptionalTestTask("SITE_KEY");

        Assert.Equal(TimeSpan.FromSeconds(10), task.ResolvedFirstPollDelay);
    }

    [Fact]
    public void ToString_WithProxy_RedactsPassword()
    {
        var task = new OptionalTestTask("SITE_KEY") { Proxy = ValidProxy };

        var text = task.ToString();

        Assert.Contains("Password = ***", text);
        Assert.DoesNotContain("proxyPass", text);
    }

    private static AntiCaptchaValidationException Validate(CaptchaTask task) =>
        Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);
}
