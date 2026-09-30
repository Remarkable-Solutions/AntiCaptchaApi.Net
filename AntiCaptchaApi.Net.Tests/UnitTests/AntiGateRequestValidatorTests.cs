using System.Linq;
using AntiCaptchaApi.Net.Enums;
using AntiCaptchaApi.Net.Internal;
using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Requests;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests;

public class AntiGateRequestValidatorTests
{
    private static AntiGateRequest CreateMinimalRequest() => new()
    {
        WebsiteUrl = "https://anti-captcha.com/tutorials/v2-textarea",
        TemplateName = "Wait for control appearance",
        Variables = JObject.FromObject(new { css_selector = "#x" }),
    };

    [Fact]
    public void Validate_Minimal_IsValid()
    {
        var request = CreateMinimalRequest();

        var result = CreateCaptchaRequestHelper.Validate(request);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingTemplateName_Error(string? templateName)
    {
        var request = CreateMinimalRequest();
        request.TemplateName = templateName!;

        var result = CreateCaptchaRequestHelper.Validate(request);

        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(AntiGateRequest.TemplateName), error.PropertyName);
    }

    [Fact]
    public void Validate_NullVariables_Error()
    {
        var request = CreateMinimalRequest();
        request.Variables = null!;

        var result = CreateCaptchaRequestHelper.Validate(request);

        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(AntiGateRequest.Variables), error.PropertyName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingWebsiteUrl_Error(string? websiteUrl)
    {
        var request = CreateMinimalRequest();
        request.WebsiteUrl = websiteUrl!;

        var result = CreateCaptchaRequestHelper.Validate(request);

        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(AntiGateRequest.WebsiteUrl), error.PropertyName);
    }

    [Fact]
    public void Validate_NullProxy_IsValid()
    {
        var request = CreateMinimalRequest();
        request.ProxyConfig = null!;

        var result = CreateCaptchaRequestHelper.Validate(request);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Validate_ValidProxy_IsValid()
    {
        var request = CreateMinimalRequest();
        request.ProxyConfig = new ProxyConfig { ProxyType = ProxyTypeOption.Http, ProxyAddress = "1.2.3.4", ProxyPort = 3128 };

        var result = CreateCaptchaRequestHelper.Validate(request);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(65536)]
    public void Validate_InvalidProxyPort_Error(int? port)
    {
        var request = CreateMinimalRequest();
        request.ProxyConfig = new ProxyConfig { ProxyAddress = "1.2.3.4", ProxyPort = port };

        var result = CreateCaptchaRequestHelper.Validate(request);

        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(ProxyConfig.ProxyPort), error.PropertyName);
    }

    [Fact]
    public void Validate_ProxyWithoutAddress_Error()
    {
        var request = CreateMinimalRequest();
        request.ProxyConfig = new ProxyConfig { ProxyPort = 3128 };

        var result = CreateCaptchaRequestHelper.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ProxyConfig.ProxyAddress));
    }
}
