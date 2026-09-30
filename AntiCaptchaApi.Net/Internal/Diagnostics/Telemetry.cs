using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AntiCaptchaApi.Net.Internal.Diagnostics;

/// <summary>
/// The process-wide activity source, meter and instruments of spec FR-10. With no listener attached they cost a flag check:
/// <see cref="ActivitySource.StartActivity(string, ActivityKind)"/> returns <see langword="null"/> and instruments record
/// nothing. Tags hold only task type names, error code names, task ids and attempt counts, never user data.
/// </summary>
internal static class Telemetry
{
    private static readonly string? Version = typeof(Telemetry).Assembly.GetName().Version?.ToString();

    public static readonly ActivitySource Source = new(AntiCaptchaDiagnostics.ActivitySourceName, Version);

    private static readonly Meter Meter = new(AntiCaptchaDiagnostics.MeterName, Version);

    public static readonly Counter<long> TasksCreated =
        Meter.CreateCounter<long>("anticaptcha.tasks.created", "{task}", "Tasks created with createTask.");

    public static readonly Counter<long> Solves =
        Meter.CreateCounter<long>("anticaptcha.solves", "{solve}", "Finished solves, by outcome (success or failure).");

    public static readonly Counter<long> Failures =
        Meter.CreateCounter<long>("anticaptcha.failures", "{failure}", "Failed solves, by error code.");

    public static readonly Histogram<double> SolveDuration =
        Meter.CreateHistogram<double>("anticaptcha.solve.duration", "s", "Duration of a solve, including retries.");

    public static readonly Counter<double> Cost =
        Meter.CreateCounter<double>("anticaptcha.cost", "USD", "Cost reported by the API for solved tasks.");
}
