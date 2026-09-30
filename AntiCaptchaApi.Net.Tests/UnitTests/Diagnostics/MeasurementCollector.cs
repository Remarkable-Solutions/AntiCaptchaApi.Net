using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Diagnostics;

/// <summary>One recorded measurement; <see cref="Tags"/> is sorted by key and formatted as <c>key=value</c>.</summary>
internal sealed record Measurement(string Instrument, double Value, IReadOnlyList<string> Tags)
{
    public override string ToString() => $"{Instrument} {Value} [{string.Join(", ", Tags)}]";
}

/// <summary>
/// Records every measurement of the <c>AntiCaptcha</c> meter while it is alive. Use it only in the
/// <see cref="TelemetryCollection"/>, where no other test emits telemetry at the same time.
/// </summary>
internal sealed class MeasurementCollector : IDisposable
{
    private readonly ConcurrentQueue<Measurement> _measurements = new();
    private readonly MeterListener _listener = new();

    public MeasurementCollector()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == AntiCaptchaDiagnostics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Add(instrument, value, tags));
        _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Add(instrument, value, tags));
        _listener.Start();
    }

    public IReadOnlyList<Measurement> All => _measurements.ToList();

    public IReadOnlyList<Measurement> For(string instrument) => All.Where(m => m.Instrument == instrument).ToList();

    public void Dispose() => _listener.Dispose();

    private void Add(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var formatted = new List<string>(tags.Length);
        foreach (var tag in tags)
        {
            formatted.Add($"{tag.Key}={tag.Value}");
        }
        formatted.Sort(StringComparer.Ordinal);
        _measurements.Enqueue(new Measurement(instrument.Name, value, formatted));
    }
}
