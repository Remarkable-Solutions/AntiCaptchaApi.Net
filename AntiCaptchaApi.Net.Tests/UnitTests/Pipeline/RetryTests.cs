using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Tasks;
using AntiCaptchaApi.Net.Tests.UnitTests.Client;
using AntiCaptchaApi.Net.Tests.UnitTests.Infrastructure;
using AntiCaptchaApi.Net.Tests.UnitTests.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Pipeline;

public class RetryTests
{
    private static readonly TimeSpan TurnstileFirstPoll = TimeSpan.FromSeconds(10);

    private readonly ClientTestContext _context = new();

    private static TurnstileTask Turnstile() => new(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey);

    private int Creates => _context.Handler.RequestsFor("createTask").Count;

    private FakeHttpMessageHandler Handler => _context.Handler;

    [Fact]
    public async Task NoSlotOnCreate_TwiceThenSuccess_Attempts3_WithGrowingDelays()
    {
        Handler.Create(PipelineResponses.Error(2, "ERROR_NO_SLOT_AVAILABLE"))
            .Create(PipelineResponses.Error(2, "ERROR_NO_SLOT_AVAILABLE"))
            .Create(PipelineResponses.Created(30))
            .Poll(PipelineResponses.Ready("THIRD"));

        var result = await _context.Client.TrySolveAsync(Turnstile());

        Assert.True(result.IsSuccess);
        Assert.Equal("THIRD", result.Solution!.Token);
        Assert.Equal(3, result.Attempts);
        Assert.Equal(30, result.TaskId);
        Assert.Equal(new[] { TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TurnstileFirstPoll }, _context.Clock.Delays);
    }

    [Fact]
    public async Task CaptchaUnsolvableWhilePolling_RetriesWithNewTask()
    {
        Handler.Create(PipelineResponses.Created(1)).Poll(PipelineResponses.Error(12, "ERROR_CAPTCHA_UNSOLVABLE"))
            .Create(PipelineResponses.Created(2)).Poll(PipelineResponses.Ready("SECOND"));

        var result = await _context.Client.TrySolveAsync(Turnstile());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.TaskId);
        Assert.Equal(2, result.Attempts);
        Assert.Equal(new[] { 1, 2 }, Handler.RequestsFor("getTaskResult").Select(r => (int)r.Json!["taskId"]!));
        Assert.Equal(new[] { TurnstileFirstPoll, TimeSpan.FromSeconds(2), TurnstileFirstPoll }, _context.Clock.Delays);
    }

    [Fact]
    public async Task ProxyConnectionRefused_Retries()
    {
        Handler.Create(PipelineResponses.Created(1)).Poll(PipelineResponses.Error(25, "ERROR_PROXY_CONNECT_REFUSED"))
            .Create(PipelineResponses.Error(25, "ERROR_PROXY_CONNECT_REFUSED"))
            .Create(PipelineResponses.Created(3)).Poll(PipelineResponses.Ready());

        var result = await _context.Client.TrySolveAsync(Turnstile());

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Attempts);
        Assert.Equal(3, result.TaskId);
    }

    [Fact]
    public async Task TransientHttpOnCreate_Retries()
    {
        Handler.Throw("createTask", new HttpRequestException("connection reset"))
            .RespondStatus("createTask", HttpStatusCode.BadGateway)
            .Create(PipelineResponses.Created(3)).Poll(PipelineResponses.Ready());

        var result = await _context.Client.TrySolveAsync(Turnstile());

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Attempts);
    }

    [Fact]
    public async Task ZeroBalance_NoRetry_Attempts1()
    {
        Handler.Create(PipelineResponses.Error(10, "ERROR_ZERO_BALANCE"));

        var result = await _context.Client.TrySolveAsync(Turnstile());

        Assert.Equal(AntiCaptchaErrorCode.ZeroBalance, result.Error!.Code);
        Assert.Equal(1, result.Attempts);
        Assert.Null(result.TaskId);
        Assert.Equal(1, Creates);
        Assert.Empty(_context.Clock.Delays);
    }

    [Fact]
    public async Task KeyDoesNotExist_NoRetry()
    {
        Handler.Create(PipelineResponses.Error(1, "ERROR_KEY_DOES_NOT_EXIST"));

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.SolveAsync(Turnstile()));

        Assert.Equal(AntiCaptchaErrorCode.KeyDoesNotExist, exception.Error.Code);
        Assert.Equal(1, exception.Attempts);
        Assert.Equal(1, Creates);
    }

    [Fact]
    public async Task Status400OnCreate_NoRetry()
    {
        Handler.RespondStatus("createTask", HttpStatusCode.BadRequest);

        var result = await _context.Client.TrySolveAsync(Turnstile());

        Assert.Equal(AntiCaptchaErrorCode.HttpError, result.Error!.Code);
        Assert.Equal(1, result.Attempts);
    }

    [Fact]
    public async Task SolveTimeout_NoRetry()
    {
        Handler.Create(PipelineResponses.Created(9));
        for (var i = 0; i < 5; i++)
        {
            Handler.Poll(PipelineResponses.Processing);
        }
        var options = new SolveOptions { Timeout = TimeSpan.FromSeconds(20) };

        var result = await _context.Client.TrySolveAsync(Turnstile(), options);

        Assert.Equal(AntiCaptchaErrorCode.SolveTimeout, result.Error!.Code);
        Assert.Equal(1, result.Attempts);
        Assert.Equal(9, result.TaskId);
        Assert.Equal(1, Creates);
        Assert.Equal(new[] { 10, 3, 3, 3, 1 }, _context.Clock.Delays.Select(d => (int)d.TotalSeconds));
    }

    [Fact]
    public async Task PollErrorLimitExceeded_NoRetry()
    {
        Handler.Create(PipelineResponses.Created(9)).PollNetworkFailure().PollNetworkFailure();

        var result = await _context.Client.TrySolveAsync(Turnstile(), new SolveOptions { MaxConsecutivePollErrors = 1 });

        Assert.Equal(AntiCaptchaErrorCode.HttpError, result.Error!.Code);
        Assert.Equal(1, result.Attempts);
        Assert.Equal(9, result.TaskId);
        Assert.Equal(1, Creates);
    }

    [Fact]
    public async Task RetryableUntilMaxAttempts_FailsWithLastError()
    {
        Handler.Create(PipelineResponses.Error(2, "ERROR_NO_SLOT_AVAILABLE"))
            .Create(PipelineResponses.Created(2)).Poll(PipelineResponses.Error(12, "ERROR_CAPTCHA_UNSOLVABLE"))
            .Create(PipelineResponses.Error(2, "ERROR_NO_SLOT_AVAILABLE"));

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.SolveAsync(Turnstile()));

        Assert.Equal(AntiCaptchaErrorCode.NoSlotAvailable, exception.Error.Code);
        Assert.Equal(3, exception.Attempts);
        Assert.Equal(2, exception.TaskId); // the last *created* task
        Assert.Equal(new[] { TimeSpan.FromSeconds(2), TurnstileFirstPoll, TimeSpan.FromSeconds(4) }, _context.Clock.Delays);
    }

    [Fact]
    public async Task MaxAttempts1_DoesNotRetry()
    {
        Handler.Create(PipelineResponses.Error(2, "ERROR_NO_SLOT_AVAILABLE"));

        var result = await _context.Client.TrySolveAsync(Turnstile(), new SolveOptions { MaxAttempts = 1 });

        Assert.Equal(AntiCaptchaErrorCode.NoSlotAvailable, result.Error!.Code);
        Assert.Equal(1, result.Attempts);
        Assert.Empty(_context.Clock.Delays);
    }

    [Fact]
    public async Task RetryDelay_UsesOptionsAndClientRandom()
    {
        var handler = new FakeHttpMessageHandler();
        var clock = new FakeClock();
        var client = new AntiCaptchaClient("TEST_KEY", new HttpClient(handler), null, clock, () => 0.0);
        handler.Create(PipelineResponses.Error(2, "ERROR_NO_SLOT_AVAILABLE"))
            .Create(PipelineResponses.Error(2, "ERROR_NO_SLOT_AVAILABLE"))
            .Create(PipelineResponses.Error(2, "ERROR_NO_SLOT_AVAILABLE"));
        var options = new SolveOptions { RetryBaseDelay = TimeSpan.FromSeconds(10), RetryMaxDelay = TimeSpan.FromSeconds(15) };

        await client.TrySolveAsync(Turnstile(), options);

        Assert.Equal(new[] { TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(12) }, clock.Delays);
    }

    [Fact]
    public async Task CancelledDuringBackoff_ThrowsOperationCanceled()
    {
        using var tryCts = new CancellationTokenSource();
        using var solveCts = new CancellationTokenSource();
        var current = tryCts;
        Handler.Create(PipelineResponses.Error(2, "ERROR_NO_SLOT_AVAILABLE")).Create(PipelineResponses.Error(2, "ERROR_NO_SLOT_AVAILABLE"));
        _context.Clock.OnDelay = _ => current.Cancel();

        var tryException = await Record.ExceptionAsync(() => _context.Client.TrySolveAsync(Turnstile(), null, tryCts.Token));
        current = solveCts;
        var solveException = await Record.ExceptionAsync(() => _context.Client.SolveAsync(Turnstile(), null, solveCts.Token));

        Assert.IsAssignableFrom<OperationCanceledException>(tryException);
        Assert.IsAssignableFrom<OperationCanceledException>(solveException);
        Assert.Equal(2, Creates);
    }

    [Fact]
    public async Task TrySolve_ApiFailure_ReturnsErrorResult_NotThrow()
    {
        Handler.Create(PipelineResponses.Error(2, "ERROR_NO_SLOT_AVAILABLE"))
            .Create(PipelineResponses.Error(2, "ERROR_NO_SLOT_AVAILABLE"))
            .Create(PipelineResponses.Error(2, "ERROR_NO_SLOT_AVAILABLE"));

        var result = await _context.Client.TrySolveAsync(Turnstile());

        Assert.False(result.IsSuccess);
        Assert.Equal(AntiCaptchaErrorCode.NoSlotAvailable, result.Error!.Code);
        Assert.Equal(3, result.Attempts);
        Assert.Null(result.TaskId);
    }
}
