using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using AntiCaptchaApi.Net.Internal.Diagnostics;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Diagnostics;

[Collection(TelemetryCollection.Name)]
public class TelemetryTests
{
    [Fact]
    public void Telemetry_Instruments_HaveSpecNamesAndUnits()
    {
        var published = new List<string>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, _) =>
            {
                if (instrument.Meter.Name == AntiCaptchaDiagnostics.MeterName)
                {
                    published.Add($"{instrument.Name} {instrument.GetType().Name} {instrument.Unit}");
                }
            },
        };

        _ = Telemetry.Source; // Initializes the static instruments before the listener starts.
        listener.Start();

        Assert.Equal(
            new[]
            {
                "anticaptcha.cost Counter`1 USD",
                "anticaptcha.failures Counter`1 {failure}",
                "anticaptcha.solve.duration Histogram`1 s",
                "anticaptcha.solves Counter`1 {solve}",
                "anticaptcha.tasks.created Counter`1 {task}",
            },
            published.OrderBy(p => p, System.StringComparer.Ordinal));
        Assert.Equal(AntiCaptchaDiagnostics.ActivitySourceName, Telemetry.Source.Name);
        Assert.IsType<Counter<long>>(Telemetry.TasksCreated);
        Assert.IsType<Counter<long>>(Telemetry.Solves);
        Assert.IsType<Counter<long>>(Telemetry.Failures);
        Assert.IsType<Histogram<double>>(Telemetry.SolveDuration);
        Assert.IsType<Counter<double>>(Telemetry.Cost);
    }
}
