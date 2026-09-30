using System;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Pipeline;

public class SolveOptionsTests
{
    [Fact]
    public void Defaults_MatchSpec()
    {
        var options = new SolveOptions();

        Assert.Equal(TimeSpan.FromSeconds(180), options.Timeout);
        Assert.Equal(TimeSpan.FromSeconds(3), options.PollInterval);
        Assert.Null(options.FirstPollDelay);
        Assert.Equal(3, options.MaxAttempts);
        Assert.Equal(5, options.MaxConsecutivePollErrors);
        Assert.Equal(TimeSpan.FromSeconds(2), options.RetryBaseDelay);
        Assert.Equal(TimeSpan.FromSeconds(30), options.RetryMaxDelay);
        Assert.Null(options.MaxCostPerSolve);
        Assert.Null(options.LanguagePool);
        Assert.Null(options.CallbackUrl);
        Assert.IsAssignableFrom<CreateOptions>(options);
    }

    [Fact]
    public void Validate_Defaults_DoesNotThrow()
    {
        new SolveOptions().Validate("o");
    }

    public static TheoryData<string, SolveOptions> NonPositiveTimes => new()
    {
        { "Timeout", new SolveOptions { Timeout = TimeSpan.Zero } },
        { "Timeout", new SolveOptions { Timeout = TimeSpan.FromSeconds(-1) } },
        { "PollInterval", new SolveOptions { PollInterval = TimeSpan.Zero } },
        { "PollInterval", new SolveOptions { PollInterval = TimeSpan.FromMilliseconds(-1) } },
        { "RetryBaseDelay", new SolveOptions { RetryBaseDelay = TimeSpan.Zero } },
        { "RetryMaxDelay", new SolveOptions { RetryMaxDelay = TimeSpan.FromSeconds(-5) } },
        { "FirstPollDelay", new SolveOptions { FirstPollDelay = TimeSpan.FromSeconds(-1) } },
    };

    [Theory]
    [MemberData(nameof(NonPositiveTimes))]
    public void Validate_NonPositiveTimeoutOrPollInterval_Throws(string option, SolveOptions options)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate("o"));

        Assert.Equal("o", exception.ParamName);
        Assert.Contains(option, exception.Message);
    }

    [Fact]
    public void Validate_ZeroFirstPollDelay_IsAllowed()
    {
        new SolveOptions { FirstPollDelay = TimeSpan.Zero }.Validate("o");
    }

    [Fact]
    public void Validate_MaxAttemptsZero_Throws()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new SolveOptions { MaxAttempts = 0 }.Validate("o"));

        Assert.Contains("MaxAttempts", exception.Message);
    }

    [Fact]
    public void Validate_NegativeMaxConsecutivePollErrors_Throws()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new SolveOptions { MaxConsecutivePollErrors = -1 }.Validate("o"));

        Assert.Contains("MaxConsecutivePollErrors", exception.Message);
    }

    [Fact]
    public void Validate_NegativeMaxCost_Throws()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new SolveOptions { MaxCostPerSolve = -0.001m }.Validate("o"));

        Assert.Contains("MaxCostPerSolve", exception.Message);
    }

    [Fact]
    public void Validate_ZeroMaxCost_IsAllowed()
    {
        new SolveOptions { MaxCostPerSolve = 0m }.Validate("o");
    }
}
