using System.Linq;
using AntiCaptchaApi.Net.Internal;
using AntiCaptchaApi.Net.Requests;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests;

public class RecaptchaV3RequestValidatorTests
{
    private const string WebsiteUrl = "https://example.com/login";
    private const string WebsiteKey = "6LcR_okUAAAAAPYrPe-HK_0RULO1aZM15ENyM-Mf";

    [Fact]
    public void Validate_DefaultRequest_IsValid()
    {
        var request = new RecaptchaV3Request { WebsiteUrl = WebsiteUrl, WebsiteKey = WebsiteKey };

        var result = CreateCaptchaRequestHelper.Validate(request);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Validate_EnterpriseRequestWithoutIsEnterprise_IsValid()
    {
        var request = new RecaptchaV3EnterpriseRequest { WebsiteUrl = WebsiteUrl, WebsiteKey = WebsiteKey };

        var result = CreateCaptchaRequestHelper.Validate(request);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Theory]
    [InlineData(0.3)]
    [InlineData(0.5)]
    [InlineData(0.7)]
    [InlineData(0.9)]
    public void Validate_MinScore_Accepted(double minScore)
    {
        var request = new RecaptchaV3Request
        {
            WebsiteUrl = WebsiteUrl,
            WebsiteKey = WebsiteKey,
            MinScore = (decimal)minScore,
        };

        var result = CreateCaptchaRequestHelper.Validate(request);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Theory]
    [InlineData(0.4)]
    [InlineData(1.0)]
    [InlineData(0)]
    public void Validate_MinScore_Rejected(double minScore)
    {
        var request = new RecaptchaV3Request
        {
            WebsiteUrl = WebsiteUrl,
            WebsiteKey = WebsiteKey,
            MinScore = (decimal)minScore,
            IsEnterprise = false,
        };

        var result = CreateCaptchaRequestHelper.Validate(request);

        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(RecaptchaV3Request.MinScore), error.PropertyName);
        var message = error.ToString();
        Assert.All(new[] { "0.3", "0.5", "0.7", "0.9" }, value => Assert.Contains(value, message));
    }
}
