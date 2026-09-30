using System;
using System.Threading;
using AntiCaptchaApi.Net.Internal.Time;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Time;

public class SystemClockTests
{
    [Fact]
    public void Delay_Zero_CompletesImmediately()
    {
        ISystemClock clock = SystemClock.Instance;

        var task = clock.Delay(TimeSpan.Zero, CancellationToken.None);

        Assert.True(task.IsCompletedSuccessfully);
    }
}
