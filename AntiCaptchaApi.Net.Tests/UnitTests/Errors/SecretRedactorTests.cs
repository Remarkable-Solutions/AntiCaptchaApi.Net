using AntiCaptchaApi.Net.Internal.Errors;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Errors;

public class SecretRedactorTests
{
    [Fact]
    public void Redact_ReplacesKeyAndPasswords()
    {
        var redactor = new SecretRedactor("SECRET_KEY", new[] { "PX_PASS", "OTHER_PASS" });

        var redacted = redactor.Redact("key SECRET_KEY, proxy user:PX_PASS@host, again SECRET_KEY and OTHER_PASS");

        Assert.Equal("key ***, proxy user:***@host, again *** and ***", redacted);
    }

    [Fact]
    public void Redact_Null_ReturnsNull()
    {
        var redactor = new SecretRedactor("SECRET_KEY", new[] { "PX_PASS" });

        Assert.Null(redactor.Redact(null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Redact_EmptySecret_IsIgnored(string? emptySecret)
    {
        var redactor = new SecretRedactor("SECRET_KEY", new[] { emptySecret, "PX_PASS" });

        Assert.Equal("plain text", redactor.Redact("plain text"));
        Assert.Equal("*** ***", redactor.Redact("SECRET_KEY PX_PASS"));
    }

    [Fact]
    public void Redact_OverlappingSecrets_LongerIsReplacedFirst()
    {
        var redactor = new SecretRedactor("ABC", new[] { "ABCDEF" });

        Assert.Equal("*** and ***", redactor.Redact("ABCDEF and ABC"));
    }

    [Fact]
    public void With_AddsPasswords_WithoutChangingOriginal()
    {
        var redactor = new SecretRedactor("SECRET_KEY", new string?[0]);

        var extended = redactor.With("PX_PASS");

        Assert.Equal("PX_PASS", redactor.Redact("PX_PASS"));
        Assert.Equal("***", extended.Redact("PX_PASS"));
        Assert.Equal("***", extended.Redact("SECRET_KEY"));
    }
}
