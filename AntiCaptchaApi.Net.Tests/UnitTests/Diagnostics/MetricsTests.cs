using System;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Tasks;
using AntiCaptchaApi.Net.Tests.UnitTests.Client;
using AntiCaptchaApi.Net.Tests.UnitTests.Pipeline;
using AntiCaptchaApi.Net.Tests.UnitTests.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Diagnostics;

[Collection(TelemetryCollection.Name)]
public sealed class MetricsTests : IDisposable
{
    private const string Type = "task.type=TurnstileTaskProxyless";

    private readonly ClientTestContext _context = new();
    private readonly MeasurementCollector _metrics = new();

    private static TurnstileTask Turnstile() => new(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey);

    public void Dispose() => _metrics.Dispose();

    [Fact]
    public async Task CreateTask_IncrementsTasksCreated()
    {
        _context.Handler.Create(PipelineResponses.Created(7));

        await _context.Client.CreateTaskAsync(Turnstile());

        Assert.Equal(new[] { new Measurement("anticaptcha.tasks.created", 1, new[] { Type }) }, _metrics.All, MeasurementComparer.Instance);
    }

    [Fact]
    public async Task CreateTask_ApiError_DoesNotIncrementTasksCreated()
    {
        _context.Handler.Create(PipelineResponses.Error(10, "ERROR_ZERO_BALANCE"));

        await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.CreateTaskAsync(Turnstile()));

        Assert.Empty(_metrics.All);
    }

    [Fact]
    public async Task Solve_Success_RecordsSolvesDurationAndCost()
    {
        _context.Handler.Create(PipelineResponses.Created(7)).Poll(PipelineResponses.Processing).Poll(PipelineResponses.Ready(cost: 0.0025m));

        var result = await _context.Client.TrySolveAsync(Turnstile());

        Assert.True(result.IsSuccess);
        Assert.Equal(
            new[]
            {
                new Measurement("anticaptcha.tasks.created", 1, new[] { Type }),
                new Measurement("anticaptcha.cost", 0.0025, new[] { Type }),
                new Measurement("anticaptcha.solves", 1, new[] { "outcome=success", Type }),
                new Measurement("anticaptcha.solve.duration", 13, new[] { "outcome=success", Type }), // 10 s first poll + 3 s interval.
            },
            _metrics.All,
            MeasurementComparer.Instance);
    }

    [Fact]
    public async Task Solve_Failure_IncrementsFailuresByErrorCode()
    {
        _context.Handler.Create(PipelineResponses.Error(10, "ERROR_ZERO_BALANCE"));

        var result = await _context.Client.TrySolveAsync(Turnstile());

        Assert.False(result.IsSuccess);
        Assert.Equal(
            new[]
            {
                new Measurement("anticaptcha.failures", 1, new[] { "error.code=ZeroBalance", Type }),
                new Measurement("anticaptcha.solves", 1, new[] { "outcome=failure", Type }),
                new Measurement("anticaptcha.solve.duration", 0, new[] { "outcome=failure", Type }),
            },
            _metrics.All,
            MeasurementComparer.Instance);
    }

    [Fact]
    public async Task Retry_CountsEachCreatedTask()
    {
        _context.Handler.Create(PipelineResponses.Created(1)).Poll(PipelineResponses.Error(12, "ERROR_CAPTCHA_UNSOLVABLE"))
            .Create(PipelineResponses.Created(2)).Poll(PipelineResponses.Ready());

        var result = await _context.Client.TrySolveAsync(Turnstile());

        Assert.Equal(2, result.Attempts);
        Assert.Equal(2, _metrics.For("anticaptcha.tasks.created").Count);
        Assert.All(_metrics.For("anticaptcha.tasks.created"), m => Assert.Equal(new[] { Type }, m.Tags));
        Assert.Single(_metrics.For("anticaptcha.solves"));
        Assert.Empty(_metrics.For("anticaptcha.failures")); // The solve succeeded; a retried attempt is not a failed solve.
    }

    private sealed class MeasurementComparer : System.Collections.Generic.IEqualityComparer<Measurement>
    {
        public static readonly MeasurementComparer Instance = new();

        public bool Equals(Measurement? x, Measurement? y) =>
            x is not null && y is not null && x.Instrument == y.Instrument && Math.Abs(x.Value - y.Value) < 1e-12
            && System.Linq.Enumerable.SequenceEqual(x.Tags, y.Tags);

        public int GetHashCode(Measurement obj) => obj.Instrument.GetHashCode();
    }
}
