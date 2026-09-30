using System;
using AntiCaptchaApi.Net.Internal.Pipeline;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Pipeline;

public class BackoffTests
{
    private static readonly TimeSpan Base = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan Max = TimeSpan.FromSeconds(30);

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(3, 8)]
    [InlineData(4, 16)]
    public void Delay_GrowsExponentially(int attempt, int expectedSeconds)
    {
        var delay = Backoff.Delay(attempt, Base, Max, () => 0.5);

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), delay);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(64)]
    [InlineData(int.MaxValue)]
    public void Delay_CappedAtMax(int attempt)
    {
        var delay = Backoff.Delay(attempt, Base, Max, () => 0.5);

        Assert.Equal(Max, delay);
    }

    [Theory]
    [InlineData(0.0, 0.8)]
    [InlineData(1.0, 1.2)]
    [InlineData(0.25, 0.9)]
    public void Delay_JitterWithinPlusMinus20Percent(double random, double factor)
    {
        var delay = Backoff.Delay(3, Base, Max, () => random);

        Assert.Equal(TimeSpan.FromSeconds(8 * factor).TotalMilliseconds, delay.TotalMilliseconds, precision: 3);
    }

    [Fact]
    public void Delay_JitterAppliesAfterTheCap()
    {
        var delay = Backoff.Delay(10, Base, Max, () => 1.0);

        Assert.Equal(TimeSpan.FromSeconds(36).TotalMilliseconds, delay.TotalMilliseconds, precision: 3);
    }
}
