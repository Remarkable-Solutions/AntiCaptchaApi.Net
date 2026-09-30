using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Infrastructure;

public class FakeClockTests
{
    [Fact]
    public async Task Delay_AdvancesUtcNow_AndRecordsDelay()
    {
        var clock = new FakeClock();
        var start = clock.UtcNow;

        await clock.Delay(TimeSpan.FromSeconds(3), CancellationToken.None);
        await clock.Delay(TimeSpan.FromSeconds(10), CancellationToken.None);

        Assert.Equal(start + TimeSpan.FromSeconds(13), clock.UtcNow);
        Assert.Equal(new[] { TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(10) }, clock.Delays);
    }

    [Fact]
    public async Task Delay_CancelledToken_ThrowsOperationCanceled()
    {
        var clock = new FakeClock();
        var start = clock.UtcNow;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => clock.Delay(TimeSpan.FromSeconds(3), cts.Token));

        Assert.Equal(start, clock.UtcNow);
        Assert.Empty(clock.Delays);
    }
}
