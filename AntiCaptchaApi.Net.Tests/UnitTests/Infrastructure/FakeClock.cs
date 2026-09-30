using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Internal.Time;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Infrastructure;

/// <summary>
/// Deterministic clock for pipeline tests: <see cref="Delay"/> returns immediately, advances <see cref="UtcNow"/>
/// and records the requested delay. A cancelled token yields a cancelled task, as <see cref="Task.Delay(TimeSpan, CancellationToken)"/> does.
/// </summary>
internal sealed class FakeClock : ISystemClock
{
    private readonly List<TimeSpan> _delays = new();

    public FakeClock(DateTimeOffset? start = null)
    {
        UtcNow = start ?? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }

    public DateTimeOffset UtcNow { get; private set; }

    /// <summary>Every delay that elapsed, in call order.</summary>
    public IReadOnlyList<TimeSpan> Delays => _delays;

    /// <summary>Invoked at the start of every <see cref="Delay"/> call, before the token is checked (e.g. to cancel mid-wait).</summary>
    public Action<TimeSpan>? OnDelay { get; set; }

    public void Advance(TimeSpan by) => UtcNow += by;

    public Task Delay(TimeSpan delay, CancellationToken ct)
    {
        OnDelay?.Invoke(delay);
        if (ct.IsCancellationRequested)
        {
            return Task.FromCanceled(ct);
        }

        _delays.Add(delay);
        UtcNow += delay;
        return Task.CompletedTask;
    }
}
