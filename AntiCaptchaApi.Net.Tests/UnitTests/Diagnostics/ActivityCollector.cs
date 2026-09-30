using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Diagnostics;

/// <summary>
/// Records every stopped activity of the <c>AntiCaptcha</c> source while it is alive. Use it only in the
/// <see cref="TelemetryCollection"/>, where no other test emits telemetry at the same time.
/// </summary>
internal sealed class ActivityCollector : IDisposable
{
    private readonly ConcurrentQueue<Activity> _stopped = new();
    private readonly ActivityListener _listener;

    public ActivityCollector()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == AntiCaptchaDiagnostics.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = _stopped.Enqueue,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>The stopped activities in stop order.</summary>
    public IReadOnlyList<Activity> Stopped => _stopped.ToList();

    /// <summary>The only stopped activity named <paramref name="name"/>; fails the test if there are none or several.</summary>
    public Activity Single(string name) => Assert.Single(Stopped, a => a.OperationName == name);

    public void Dispose() => _listener.Dispose();
}
