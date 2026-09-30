using System.Linq;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Tasks;

public class ValidatorTests
{
    private string WebsiteKey { get; } = "  ";

    [Fact]
    public void Url_Relative_AddsError()
    {
        var v = new Validator();

        v.Url("/login", "WebsiteUrl");

        var error = Assert.Single(v.Errors);
        Assert.Equal("WebsiteUrl", error.Field);
    }

    [Fact]
    public void Url_FtpScheme_AddsError()
    {
        var v = new Validator();

        v.Url("ftp://example.com/file", "WebsiteUrl");

        Assert.Single(v.Errors);
    }

    [Theory]
    [InlineData("https://example.com/login")]
    [InlineData("http://example.com")]
    public void Url_Https_NoError(string url)
    {
        var v = new Validator();

        v.Url(url);

        Assert.Empty(v.Errors);
    }

    [Fact]
    public void NotEmpty_Whitespace_AddsErrorNamedByCallerExpression()
    {
        var v = new Validator();

        v.NotEmpty(WebsiteKey);

        var error = Assert.Single(v.Errors);
        Assert.Equal("WebsiteKey", error.Field);
        Assert.Equal("must not be empty", error.Message);
    }

    [Fact]
    public void Range_OutOfRange_AddsError()
    {
        var port = 65536;
        var v = new Validator();

        v.Range(port, 1, 65535).Range(1, 1, 65535, "Other");

        var error = Assert.Single(v.Errors);
        Assert.Equal("port", error.Field);
        Assert.Equal("must be between 1 and 65535", error.Message);
    }

    [Fact]
    public void OneOf_NotInSet_AddsError()
    {
        var apiDomain = "evil.example.com";
        var v = new Validator();

        v.OneOf(apiDomain, new[] { "www.google.com", "www.recaptcha.net" })
            .OneOf("www.google.com", new[] { "www.google.com", "www.recaptcha.net" }, "Allowed");

        var error = Assert.Single(v.Errors);
        Assert.Equal("apiDomain", error.Field);
        Assert.Equal("must be one of: www.google.com, www.recaptcha.net", error.Message);
    }

    [Fact]
    public void Chaining_CollectsAllErrors()
    {
        object? variables = null;
        var v = new Validator();

        v.Url("not-a-url", "WebsiteUrl")
            .NotEmpty("", "WebsiteKey")
            .NotNull(variables)
            .Must(false, "MinLength", "must not exceed MaxLength")
            .Must(true, "Ignored", "never reported");

        Assert.Equal(new[] { "WebsiteUrl", "WebsiteKey", "variables", "MinLength" }, v.Errors.Select(e => e.Field));
    }

    [Fact]
    public void NotEmpty_ThisQualifiedExpression_StripsThisPrefix()
    {
        var v = new Validator();

        v.NotEmpty(this.WebsiteKey);

        Assert.Equal("WebsiteKey", Assert.Single(v.Errors).Field);
    }

    [Fact]
    public void ThrowIfInvalid_WithErrors_ThrowsWithAllErrors()
    {
        var v = new Validator().Url("x", "WebsiteUrl").NotEmpty("", "WebsiteKey");

        var exception = Assert.Throws<AntiCaptchaValidationException>(() => v.ThrowIfInvalid());

        Assert.Equal(2, exception.Errors.Count);
    }

    [Fact]
    public void ThrowIfInvalid_NoErrors_DoesNotThrow()
    {
        new Validator().Url("https://example.com").ThrowIfInvalid();
    }
}
