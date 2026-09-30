using System;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Errors;

public class AntiCaptchaExceptionTests
{
    [Fact]
    public void Message_ContainsCodeAndDescription()
    {
        var error = new AntiCaptchaError(AntiCaptchaErrorCode.ZeroBalance, "ERROR_ZERO_BALANCE", "Account has zero balance", 10);

        var exception = new AntiCaptchaException(error, 42, 1);

        Assert.Equal("ZeroBalance: Account has zero balance", exception.Message);
    }

    [Fact]
    public void Message_WithoutDescription_IsCode()
    {
        var exception = new AntiCaptchaException(new AntiCaptchaError(AntiCaptchaErrorCode.HttpTimeout, null, null, null), null, 0);

        Assert.Equal("HttpTimeout", exception.Message);
    }

    [Fact]
    public void Properties_ExposeErrorTaskIdAttempts()
    {
        var error = new AntiCaptchaError(AntiCaptchaErrorCode.CaptchaUnsolvable, "ERROR_CAPTCHA_UNSOLVABLE", "unsolvable", 12);
        var inner = new InvalidOperationException("inner");

        var exception = new AntiCaptchaException(error, 42, 3, inner);

        Assert.Same(error, exception.Error);
        Assert.Equal(42, exception.TaskId);
        Assert.Equal(3, exception.Attempts);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void Ctor_NullError_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => new AntiCaptchaException(null!, null, 0));
    }
}
