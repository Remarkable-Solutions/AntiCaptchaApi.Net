using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Tasks;
using AntiCaptchaApi.Net.Tests.UnitTests.Client;
using AntiCaptchaApi.Net.Tests.UnitTests.Pipeline;
using AntiCaptchaApi.Net.Tests.UnitTests.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Diagnostics;

[Collection(TelemetryCollection.Name)]
public sealed class ActivityTests : IDisposable
{
    private const int TaskId = 555;

    private readonly ClientTestContext _context = new();
    private readonly ActivityCollector _activities = new();

    private static TurnstileTask Turnstile() => new(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey);

    public void Dispose() => _activities.Dispose();

    [Fact]
    public async Task Solve_Success_EmitsSolveActivityWithTypeTaskIdAttempts()
    {
        _context.Handler.Create(PipelineResponses.Error(2, "ERROR_NO_SLOT_AVAILABLE"))
            .Create(PipelineResponses.Created(TaskId)).Poll(PipelineResponses.Processing).Poll(PipelineResponses.Ready());

        var result = await _context.Client.TrySolveAsync(Turnstile());

        Assert.True(result.IsSuccess);
        var solve = _activities.Single("AntiCaptcha.Solve");
        Assert.Equal(ActivityKind.Internal, solve.Kind);
        Assert.Equal("TurnstileTaskProxyless", solve.GetTagItem("anticaptcha.task.type"));
        Assert.Equal(TaskId, solve.GetTagItem("anticaptcha.task.id"));
        Assert.Equal(2, solve.GetTagItem("anticaptcha.attempts"));
        Assert.Null(solve.GetTagItem("anticaptcha.error.code"));
        Assert.Equal(ActivityStatusCode.Unset, solve.Status);
    }

    [Fact]
    public async Task Solve_Failure_SetsErrorStatusAndErrorCode()
    {
        _context.Handler.Create(PipelineResponses.Error(10, "ERROR_ZERO_BALANCE"));

        var ex = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.SolveAsync(Turnstile()));

        Assert.Equal(AntiCaptchaErrorCode.ZeroBalance, ex.Error.Code);
        var solve = _activities.Single("AntiCaptcha.Solve");
        Assert.Equal(ActivityStatusCode.Error, solve.Status);
        Assert.Equal("ZeroBalance", solve.StatusDescription);
        Assert.Equal("ZeroBalance", solve.GetTagItem("anticaptcha.error.code"));
        Assert.Equal(1, solve.GetTagItem("anticaptcha.attempts"));
        Assert.Null(solve.GetTagItem("anticaptcha.task.id")); // No task was created.
    }

    [Fact]
    public async Task EachHttpCall_EmitsChildActivityNamedByMethod()
    {
        _context.Handler.Create(PipelineResponses.Created(TaskId))
            .PollNetworkFailure().Poll(PipelineResponses.Processing).Poll(PipelineResponses.Ready());

        await _context.Client.TrySolveAsync(Turnstile());

        var solve = _activities.Single("AntiCaptcha.Solve");
        var children = _activities.Stopped.Where(a => a.ParentSpanId == solve.SpanId).ToList();
        Assert.Equal(
            new[] { "AntiCaptcha.createTask", "AntiCaptcha.getTaskResult", "AntiCaptcha.getTaskResult", "AntiCaptcha.getTaskResult" },
            children.Select(a => a.OperationName));
        Assert.All(children, a => Assert.Equal(ActivityKind.Client, a.Kind));
        Assert.All(children, a => Assert.Equal(solve.TraceId, a.TraceId));
        var failed = children[1];
        Assert.Equal(ActivityStatusCode.Error, failed.Status);
        Assert.Equal("HttpError", failed.GetTagItem("anticaptcha.error.code"));
        Assert.All(children.Where(a => a != failed), a => Assert.Equal(ActivityStatusCode.Unset, a.Status));
        Assert.Equal(4, _activities.Stopped.Count - 1); // Nothing else than the solve and its calls.
    }

    [Fact]
    public async Task StandaloneCall_EmitsRootActivityNamedByMethod()
    {
        _context.Handler.RespondJson("getBalance", "{\"errorId\":0,\"balance\":\"1.5\"}");

        await _context.Client.GetBalanceAsync();

        var call = _activities.Single("AntiCaptcha.getBalance");
        Assert.Null(call.Parent);
        Assert.Equal(default, call.ParentSpanId);
        Assert.Single(_activities.Stopped);
    }

    [Fact]
    public async Task NoListener_NoActivityCreated()
    {
        _activities.Dispose();
        using var otherSource = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "SomethingElse",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };
        ActivitySource.AddActivityListener(otherSource);
        Activity? seenByHandler = null;
        var seen = false;
        _context.Handler.Script("createTask", _ =>
        {
            seenByHandler = Activity.Current;
            seen = true;
            return Task.FromResult(Json(PipelineResponses.Created(TaskId)));
        }).Poll(PipelineResponses.Ready());

        var result = await _context.Client.TrySolveAsync(Turnstile());

        Assert.True(result.IsSuccess);
        Assert.True(seen);
        Assert.Null(seenByHandler);
        Assert.False(Internal.Diagnostics.Telemetry.Source.HasListeners());
    }

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}
