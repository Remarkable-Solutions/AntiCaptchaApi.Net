using AntiCaptchaApi.Net.Internal.Errors;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Errors;

public class ErrorCodeParserTests
{
    // Every code documented at https://anti-captcha.com/apidoc/errors (checked 2026-09-30), with its errorId.
    public static TheoryData<string, AntiCaptchaErrorCode, int> DocumentedCodes => new()
    {
        { "ERROR_KEY_DOES_NOT_EXIST", AntiCaptchaErrorCode.KeyDoesNotExist, 1 },
        { "ERROR_NO_SLOT_AVAILABLE", AntiCaptchaErrorCode.NoSlotAvailable, 2 },
        { "ERROR_ZERO_CAPTCHA_FILESIZE", AntiCaptchaErrorCode.ZeroCaptchaFileSize, 3 },
        { "ERROR_TOO_BIG_CAPTCHA_FILESIZE", AntiCaptchaErrorCode.TooBigCaptchaFileSize, 4 },
        { "ERROR_ZERO_BALANCE", AntiCaptchaErrorCode.ZeroBalance, 10 },
        { "ERROR_IP_NOT_ALLOWED", AntiCaptchaErrorCode.IpNotAllowed, 11 },
        { "ERROR_CAPTCHA_UNSOLVABLE", AntiCaptchaErrorCode.CaptchaUnsolvable, 12 },
        { "ERROR_BAD_DUPLICATES", AntiCaptchaErrorCode.BadDuplicates, 13 },
        { "ERROR_NO_SUCH_METHOD", AntiCaptchaErrorCode.NoSuchMethod, 14 },
        { "ERROR_IMAGE_TYPE_NOT_SUPPORTED", AntiCaptchaErrorCode.ImageTypeNotSupported, 15 },
        { "ERROR_NO_SUCH_CAPCHA_ID", AntiCaptchaErrorCode.NoSuchCaptchaId, 16 },
        { "ERROR_IP_BLOCKED", AntiCaptchaErrorCode.IpBlocked, 21 },
        { "ERROR_TASK_ABSENT", AntiCaptchaErrorCode.TaskAbsent, 22 },
        { "ERROR_TASK_NOT_SUPPORTED", AntiCaptchaErrorCode.TaskNotSupported, 23 },
        { "ERROR_INCORRECT_SESSION_DATA", AntiCaptchaErrorCode.IncorrectSessionData, 24 },
        { "ERROR_PROXY_CONNECT_REFUSED", AntiCaptchaErrorCode.ProxyConnectionRefused, 25 },
        { "ERROR_PROXY_CONNECT_TIMEOUT", AntiCaptchaErrorCode.ProxyConnectionTimeout, 26 },
        { "ERROR_PROXY_READ_TIMEOUT", AntiCaptchaErrorCode.ProxyReadTimeout, 27 },
        { "ERROR_PROXY_BANNED", AntiCaptchaErrorCode.ProxyBanned, 28 },
        { "ERROR_PROXY_TRANSPARENT", AntiCaptchaErrorCode.ProxyTransparent, 29 },
        { "ERROR_RECAPTCHA_TIMEOUT", AntiCaptchaErrorCode.RecaptchaTimeout, 30 },
        { "ERROR_RECAPTCHA_INVALID_SITEKEY", AntiCaptchaErrorCode.RecaptchaInvalidSiteKey, 31 },
        { "ERROR_RECAPTCHA_INVALID_DOMAIN", AntiCaptchaErrorCode.RecaptchaInvalidDomain, 32 },
        { "ERROR_RECAPTCHA_OLD_BROWSER", AntiCaptchaErrorCode.RecaptchaOldBrowser, 33 },
        { "ERROR_TOKEN_EXPIRED", AntiCaptchaErrorCode.TokenExpired, 34 },
        { "ERROR_PROXY_HAS_NO_IMAGE_SUPPORT", AntiCaptchaErrorCode.ProxyHasNoImageSupport, 35 },
        { "ERROR_PROXY_INCOMPATIBLE_HTTP_VERSION", AntiCaptchaErrorCode.ProxyIncompatibleHttpVersion, 36 },
        { "ERROR_PROXY_NOT_AUTHORISED", AntiCaptchaErrorCode.ProxyNotAuthorised, 49 },
        { "ERROR_INVALID_KEY_TYPE", AntiCaptchaErrorCode.InvalidKeyType, 51 },
        { "ERROR_FAILED_LOADING_WIDGET", AntiCaptchaErrorCode.FailedLoadingWidget, 52 },
        { "ERROR_VISIBLE_RECAPTCHA", AntiCaptchaErrorCode.VisibleRecaptcha, 53 },
        { "ERROR_ALL_WORKERS_FILTERED", AntiCaptchaErrorCode.AllWorkersFiltered, 54 },
        { "ERROR_ACCOUNT_SUSPENDED", AntiCaptchaErrorCode.AccountSuspended, 55 },
        { "ERROR_TEMPLATE_NOT_FOUND", AntiCaptchaErrorCode.TemplateNotFound, 56 },
        { "ERROR_TASK_CANCELED", AntiCaptchaErrorCode.TaskCanceled, 57 },
    };

    [Theory]
    [MemberData(nameof(DocumentedCodes))]
    public void Parse_KnownCode_MapsEnum(string raw, AntiCaptchaErrorCode expected, int errorId)
    {
        var code = ErrorCodeParser.Parse(raw);

        Assert.Equal(expected, code);
        Assert.Equal(errorId, (int)code);
    }

    [Fact]
    public void Parse_UnknownCode_ReturnsUnknownAndKeepsRaw()
    {
        var error = ErrorCodeParser.FromApi(99, "ERROR_SOMETHING_NEW", "A code this library does not know");

        Assert.Equal(AntiCaptchaErrorCode.Unknown, error.Code);
        Assert.Equal("ERROR_SOMETHING_NEW", error.RawCode);
        Assert.Equal("A code this library does not know", error.Description);
        Assert.Equal(99, error.ErrorId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Parse_Null_ReturnsUnknown(string? raw)
    {
        Assert.Equal(AntiCaptchaErrorCode.Unknown, ErrorCodeParser.Parse(raw));
    }

    [Fact]
    public void Parse_IsCaseSensitive_LowercaseIsUnknown()
    {
        Assert.Equal(AntiCaptchaErrorCode.Unknown, ErrorCodeParser.Parse("error_zero_balance"));
    }

    [Fact]
    public void FromApi_KnownCode_KeepsRawAndErrorId()
    {
        var error = ErrorCodeParser.FromApi(10, "ERROR_ZERO_BALANCE", "Account has zero balance");

        Assert.Equal(new AntiCaptchaError(AntiCaptchaErrorCode.ZeroBalance, "ERROR_ZERO_BALANCE", "Account has zero balance", 10), error);
    }
}
