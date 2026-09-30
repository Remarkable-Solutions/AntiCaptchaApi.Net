using System;
using AntiCaptchaApi.Net.Internal.Time;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Time;

public class UnixTimeTests
{
    [Theory]
    [InlineData(-62_135_596_800L)]
    [InlineData(0L)]
    [InlineData(253_402_300_799L)]
    public void TryFromSeconds_InRange_ReturnsUtcTime(long seconds)
    {
        var ok = UnixTime.TryFromSeconds(seconds, out var value);

        Assert.True(ok);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(seconds), value);
        Assert.Equal(TimeSpan.Zero, value.Offset);
    }

    [Theory]
    [InlineData(-62_135_596_801L)]
    [InlineData(253_402_300_800L)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void TryFromSeconds_OutOfRange_ReturnsFalse(long seconds)
    {
        var ok = UnixTime.TryFromSeconds(seconds, out var value);

        Assert.False(ok);
        Assert.Equal(default, value);
    }
}
