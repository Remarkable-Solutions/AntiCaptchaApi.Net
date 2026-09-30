namespace AntiCaptchaApi.Net;

/// <summary>
/// Names for subscribing to the library's telemetry, for example with OpenTelemetry's
/// <c>AddSource(AntiCaptchaDiagnostics.ActivitySourceName)</c> and <c>AddMeter(AntiCaptchaDiagnostics.MeterName)</c>.
/// Telemetry never carries the client key, proxy details, website data or solution content.
/// </summary>
public static class AntiCaptchaDiagnostics
{
    /// <summary>
    /// The name of the <c>System.Diagnostics.ActivitySource</c>: one <c>AntiCaptcha.Solve</c> activity per solve and one
    /// child <c>AntiCaptcha.&lt;apiMethod&gt;</c> activity per API call.
    /// </summary>
    public const string ActivitySourceName = "AntiCaptcha";

    /// <summary>
    /// The name of the <c>System.Diagnostics.Metrics.Meter</c> with the instruments <c>anticaptcha.tasks.created</c>,
    /// <c>anticaptcha.solves</c>, <c>anticaptcha.failures</c>, <c>anticaptcha.solve.duration</c> (s) and
    /// <c>anticaptcha.cost</c> (USD).
    /// </summary>
    public const string MeterName = "AntiCaptcha";
}
