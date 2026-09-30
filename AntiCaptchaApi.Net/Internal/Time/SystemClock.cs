using System;
using System.Threading;
using System.Threading.Tasks;

namespace AntiCaptchaApi.Net.Internal.Time;

/// <summary>The real clock: wall-clock UTC time and <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.</summary>
internal sealed class SystemClock : ISystemClock
{
    public static SystemClock Instance { get; } = new();

    private SystemClock()
    {
    }

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public Task Delay(TimeSpan delay, CancellationToken ct) => Task.Delay(delay, ct);
}
