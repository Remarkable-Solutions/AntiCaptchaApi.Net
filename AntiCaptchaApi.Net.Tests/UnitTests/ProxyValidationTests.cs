using System;
using System.Collections.Generic;
using System.Linq;
using AntiCaptchaApi.Net.Enums;
using AntiCaptchaApi.Net.Internal;
using AntiCaptchaApi.Net.Internal.Extensions;
using AntiCaptchaApi.Net.Internal.Validation;
using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Requests;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests;

public class ProxyValidationTests
{
    private const string WebsiteUrl = "https://example.com/login";
    private const string WebsiteKey = "0x4AAAAAAAB-test-site-key";
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/128.0 Safari/537.36";

    /// <summary>
    /// Every request type whose anti-captcha task requires a proxy, built with all other required fields set
    /// and <c>ProxyConfig = null</c>.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, Func<ValidationResult>> ProxyRequiredRequestsWithoutProxy =
        new Dictionary<string, Func<ValidationResult>>
        {
            [nameof(RecaptchaV2Request)] = () => CreateCaptchaRequestHelper.Validate(new RecaptchaV2Request
            {
                WebsiteUrl = WebsiteUrl, WebsiteKey = WebsiteKey, UserAgent = UserAgent,
            }),
            [nameof(RecaptchaV2EnterpriseRequest)] = () => CreateCaptchaRequestHelper.Validate(new RecaptchaV2EnterpriseRequest
            {
                WebsiteUrl = WebsiteUrl, WebsiteKey = WebsiteKey, UserAgent = UserAgent,
            }),
            [nameof(GeeTestV3Request)] = () => CreateCaptchaRequestHelper.Validate(new GeeTestV3Request
            {
                WebsiteUrl = WebsiteUrl, Gt = "gt-value", Challenge = "challenge-value", UserAgent = UserAgent,
            }),
            [nameof(GeeTestV4Request)] = () => CreateCaptchaRequestHelper.Validate(new GeeTestV4Request
            {
                WebsiteUrl = WebsiteUrl, Gt = "gt-value", UserAgent = UserAgent,
            }),
            [nameof(TurnstileCaptchaRequest)] = () => CreateCaptchaRequestHelper.Validate(new TurnstileCaptchaRequest
            {
                WebsiteUrl = WebsiteUrl, WebsiteKey = WebsiteKey,
            }),
            [nameof(ProsopoRequest)] = () => CreateCaptchaRequestHelper.Validate(new ProsopoRequest
            {
                WebsiteUrl = WebsiteUrl, WebsiteKey = WebsiteKey, UserAgent = UserAgent,
            }),
            [nameof(FriendlyCaptchaRequest)] = () => CreateCaptchaRequestHelper.Validate(new FriendlyCaptchaRequest
            {
                WebsiteUrl = WebsiteUrl, WebsiteKey = WebsiteKey, UserAgent = UserAgent,
            }),
        };

    public static IEnumerable<object[]> ProxyRequiredRequestTypes =>
        ProxyRequiredRequestsWithoutProxy.Keys.Select(name => new object[] { name });

    [Fact]
    public void Validate_TurnstileRequestWithoutProxy_ReturnsProxyConfigError()
    {
        var request = new TurnstileCaptchaRequest { WebsiteUrl = WebsiteUrl, WebsiteKey = WebsiteKey };

        var result = CreateCaptchaRequestHelper.Validate(request);

        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(ProxyConfig), error.PropertyName);
    }

    [Fact]
    public void Validate_TurnstileRequestWithProxyNoUserAgent_IsValid()
    {
        var request = new TurnstileCaptchaRequest
        {
            WebsiteUrl = WebsiteUrl,
            WebsiteKey = WebsiteKey,
            ProxyConfig = CreateProxy(8080),
        };

        var result = CreateCaptchaRequestHelper.Validate(request);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Validate_TurnstileRequestWithProxyPortZero_ReturnsProxyPortError()
    {
        var request = new TurnstileCaptchaRequest
        {
            WebsiteUrl = WebsiteUrl,
            WebsiteKey = WebsiteKey,
            ProxyConfig = CreateProxy(0),
        };

        var result = CreateCaptchaRequestHelper.Validate(request);

        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(ProxyConfig.ProxyPort), error.PropertyName);
    }

    [Theory]
    [MemberData(nameof(ProxyRequiredRequestTypes))]
    public void Validate_ProxyRequiredRequestWithoutProxy_ReturnsProxyConfigError(string requestType)
    {
        var validate = ProxyRequiredRequestsWithoutProxy[requestType];

        var result = validate();

        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(ProxyConfig), error.PropertyName);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(65535, true)]
    [InlineData(0, false)]
    [InlineData(65536, false)]
    [InlineData(null, false)]
    public void ValidateProxy_Port(int? port, bool expectedValid)
    {
        var result = new ValidationResult().ValidateProxy(CreateProxy(port));

        AssertPortValidation(result, expectedValid);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(65535, true)]
    [InlineData(0, false)]
    [InlineData(65536, false)]
    [InlineData(null, false)]
    public void ValidateOptionalProxy_Port(int? port, bool expectedValid)
    {
        var result = new ValidationResult().ValidateOptionalProxy(CreateProxy(port));

        AssertPortValidation(result, expectedValid);
    }

    private static void AssertPortValidation(ValidationResult result, bool expectedValid)
    {
        if (expectedValid)
        {
            Assert.True(result.IsValid, string.Join("; ", result.Errors));
            return;
        }

        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(ProxyConfig.ProxyPort), error.PropertyName);
        Assert.Equal("must have value between 1 and 65535", error.ErrorMessage);
    }

    private static ProxyConfig CreateProxy(int? port) => new()
    {
        ProxyType = ProxyTypeOption.Http,
        ProxyAddress = "10.0.0.1",
        ProxyPort = port,
    };
}
