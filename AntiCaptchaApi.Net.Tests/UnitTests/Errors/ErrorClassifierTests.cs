using AntiCaptchaApi.Net.Internal.Errors;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Errors;

public class ErrorClassifierTests
{
    private static AntiCaptchaError Api(AntiCaptchaErrorCode code) => new(code, null, "description", 1);

    private static AntiCaptchaError Http(int? status) => ErrorClassifier.HttpError(status, "HTTP failure");

    public static TheoryData<AntiCaptchaError, bool> RetryableCases => new()
    {
        { Api(AntiCaptchaErrorCode.NoSlotAvailable), true },
        { Api(AntiCaptchaErrorCode.CaptchaUnsolvable), true },
        { Api(AntiCaptchaErrorCode.ProxyConnectionRefused), true },
        { Http(500), true },
        { Http(503), true },
        { Http(null), true },
        { Api(AntiCaptchaErrorCode.KeyDoesNotExist), false },
        { Api(AntiCaptchaErrorCode.ZeroBalance), false },
        { Api(AntiCaptchaErrorCode.IpBlocked), false },
        { Api(AntiCaptchaErrorCode.SolveTimeout), false },
        { Api(AntiCaptchaErrorCode.Unknown), false },
        { Api(AntiCaptchaErrorCode.AccessDenied), false },
        { Http(400), false },
    };

    public static TheoryData<AntiCaptchaError, bool> TransientCases => new()
    {
        { Http(null), true },
        { Http(408), true },
        { Http(429), true },
        { Http(500), true },
        { Http(504), true },
        { Api(AntiCaptchaErrorCode.HttpTimeout), true },
        { Api(AntiCaptchaErrorCode.InvalidResponse), true },
        { Http(400), false },
        { Http(404), false },
        { Api(AntiCaptchaErrorCode.NoSlotAvailable), false },
        { Api(AntiCaptchaErrorCode.ZeroBalance), false },
        { Api(AntiCaptchaErrorCode.SolveTimeout), false },
        { Api(AntiCaptchaErrorCode.Unknown), false },
        { Api(AntiCaptchaErrorCode.AccessDenied), false },
    };

    [Theory]
    [MemberData(nameof(RetryableCases))]
    public void IsRetryable(AntiCaptchaError error, bool expected)
    {
        Assert.Equal(expected, ErrorClassifier.IsRetryable(error));
        Assert.Equal(expected, error.IsRetryable);
    }

    [Theory]
    [MemberData(nameof(TransientCases))]
    public void IsTransient(AntiCaptchaError error, bool expected)
    {
        Assert.Equal(expected, ErrorClassifier.IsTransient(error));
        Assert.Equal(expected, error.IsTransient);
    }

    [Fact]
    public void HttpError_KeepsStatusAsRawCode()
    {
        var error = Http(503);

        Assert.Equal(AntiCaptchaErrorCode.HttpError, error.Code);
        Assert.Equal("503", error.RawCode);
        Assert.Null(error.ErrorId);
    }

    [Fact]
    public void HttpError_NetworkFailure_HasNoRawCode()
    {
        Assert.Null(Http(null).RawCode);
    }
}
