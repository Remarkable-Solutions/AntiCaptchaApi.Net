using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Tasks;
using AntiCaptchaApi.Net.Tests.UnitTests.Client;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Pipeline;

public class WaitForResultTests
{
    private const int TaskId = 777;

    private readonly ClientTestContext _context = new();

    private int Polls => _context.Handler.RequestsFor("getTaskResult").Count;

    [Fact]
    public async Task Wait_ProcessingTwiceThenReady_ReturnsSolution_AndPollsThreeTimes()
    {
        _context.Handler.Poll(PipelineResponses.Processing).Poll(PipelineResponses.Processing).Poll(PipelineResponses.Ready("T1"));

        var solution = await _context.Client.WaitForResultAsync<TurnstileSolution>(TaskId);

        Assert.Equal("T1", solution.Token);
        Assert.Equal(3, Polls);
        Assert.All(_context.Handler.Requests, r => Assert.Equal(TaskId, (int)r.Json!["taskId"]!));
        Assert.Empty(_context.Handler.RequestsFor("createTask"));
    }

    [Fact]
    public async Task Wait_DelaysArePollInterval()
    {
        _context.Handler.Poll(PipelineResponses.Processing).Poll(PipelineResponses.Processing).Poll(PipelineResponses.Ready());
        var options = new SolveOptions { FirstPollDelay = TimeSpan.FromSeconds(7), PollInterval = TimeSpan.FromSeconds(5) };

        await _context.Client.WaitForResultAsync<TurnstileSolution>(TaskId, options);

        Assert.Equal(new[] { TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5) }, _context.Clock.Delays);
    }

    [Fact]
    public async Task Wait_DefaultFirstPollDelay_IsZeroForExistingTask()
    {
        _context.Handler.Poll(PipelineResponses.Processing).Poll(PipelineResponses.Ready());

        await _context.Client.WaitForResultAsync<TurnstileSolution>(TaskId, new SolveOptions { PollInterval = TimeSpan.FromSeconds(4) });

        Assert.Equal(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(4) }, _context.Clock.Delays);
    }

    [Fact]
    public async Task Wait_NullOptions_UsesClientDefaultSolveOptions()
    {
        var context = new ClientTestContext(options: new AntiCaptchaClientOptions
        {
            DefaultSolveOptions = new SolveOptions { FirstPollDelay = TimeSpan.FromSeconds(1), PollInterval = TimeSpan.FromSeconds(9) },
        });
        context.Handler.Poll(PipelineResponses.Processing).Poll(PipelineResponses.Ready());

        await context.Client.WaitForResultAsync<TurnstileSolution>(TaskId);

        Assert.Equal(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(9) }, context.Clock.Delays);
    }

    [Fact]
    public async Task Wait_TransientErrorsBelowLimit_KeepsPolling()
    {
        _context.Handler.PollNetworkFailure()
            .RespondStatus("getTaskResult", HttpStatusCode.ServiceUnavailable)
            .RespondStatus("getTaskResult", HttpStatusCode.TooManyRequests)
            .Poll("<html>bad gateway</html>")
            .PollNetworkFailure()
            .Poll(PipelineResponses.Ready("AFTER_ERRORS"));

        var solution = await _context.Client.WaitForResultAsync<TurnstileSolution>(TaskId);

        Assert.Equal("AFTER_ERRORS", solution.Token);
        Assert.Equal(6, Polls);
    }

    [Fact]
    public async Task Wait_ConsecutiveTransientErrorsExceedLimit_ThrowsWithTaskId()
    {
        for (var i = 0; i < 3; i++)
        {
            _context.Handler.PollNetworkFailure();
        }

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(
            () => _context.Client.WaitForResultAsync<TurnstileSolution>(TaskId, new SolveOptions { MaxConsecutivePollErrors = 2 }));

        Assert.Equal(AntiCaptchaErrorCode.HttpError, exception.Error.Code);
        Assert.Equal(TaskId, exception.TaskId);
        Assert.Equal(0, exception.Attempts);
        Assert.Equal(3, Polls);
    }

    [Fact]
    public async Task Wait_TransientCounterResetsAfterSuccessfulPoll()
    {
        _context.Handler.PollNetworkFailure().PollNetworkFailure()
            .Poll(PipelineResponses.Processing)
            .PollNetworkFailure().PollNetworkFailure()
            .Poll(PipelineResponses.Ready("RESET"));

        var solution = await _context.Client.WaitForResultAsync<TurnstileSolution>(TaskId, new SolveOptions { MaxConsecutivePollErrors = 2 });

        Assert.Equal("RESET", solution.Token);
        Assert.Equal(6, Polls);
    }

    [Fact]
    public async Task Wait_TimeoutElapsed_ThrowsSolveTimeout()
    {
        for (var i = 0; i < 5; i++)
        {
            _context.Handler.Poll(PipelineResponses.Processing);
        }
        var start = _context.Clock.UtcNow;
        var options = new SolveOptions { Timeout = TimeSpan.FromSeconds(10), PollInterval = TimeSpan.FromSeconds(3) };

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.WaitForResultAsync<TurnstileSolution>(TaskId, options));

        Assert.Equal(AntiCaptchaErrorCode.SolveTimeout, exception.Error.Code);
        Assert.Equal(TaskId, exception.TaskId);
        Assert.False(exception.Error.IsRetryable);
        Assert.Equal(5, Polls);
        Assert.Equal(new[] { 0, 3, 3, 3, 1 }, _context.Clock.Delays.Select(d => (int)d.TotalSeconds));
        Assert.Equal(TimeSpan.FromSeconds(10), _context.Clock.UtcNow - start);
    }

    [Fact]
    public async Task Wait_TransientErrorAtTimeout_ThrowsSolveTimeout()
    {
        _context.Handler.Poll(PipelineResponses.Processing).PollNetworkFailure();
        var options = new SolveOptions { Timeout = TimeSpan.FromSeconds(3), PollInterval = TimeSpan.FromSeconds(3) };

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.WaitForResultAsync<TurnstileSolution>(TaskId, options));

        Assert.Equal(AntiCaptchaErrorCode.SolveTimeout, exception.Error.Code);
        Assert.Equal(2, Polls);
    }

    [Theory]
    [InlineData(16, "ERROR_NO_SUCH_CAPCHA_ID", AntiCaptchaErrorCode.NoSuchCaptchaId)]
    [InlineData(1, "ERROR_KEY_DOES_NOT_EXIST", AntiCaptchaErrorCode.KeyDoesNotExist)]
    [InlineData(12, "ERROR_CAPTCHA_UNSOLVABLE", AntiCaptchaErrorCode.CaptchaUnsolvable)]
    public async Task Wait_FatalApiError_ThrowsImmediately(int errorId, string code, AntiCaptchaErrorCode expected)
    {
        _context.Handler.Poll(PipelineResponses.Processing).Poll(PipelineResponses.Error(errorId, code));

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.WaitForResultAsync<TurnstileSolution>(TaskId));

        Assert.Equal(expected, exception.Error.Code);
        Assert.Equal(TaskId, exception.TaskId);
        Assert.Equal(2, Polls);
    }

    [Fact]
    public async Task Wait_Status400_ThrowsImmediately()
    {
        _context.Handler.RespondStatus("getTaskResult", HttpStatusCode.BadRequest);

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.WaitForResultAsync<TurnstileSolution>(TaskId));

        Assert.Equal(AntiCaptchaErrorCode.HttpError, exception.Error.Code);
        Assert.Equal(1, Polls);
    }

    [Fact]
    public async Task Wait_ReadyWithoutSolution_ThrowsInvalidResponse()
    {
        // Decision 2026-09-30 (plan Task 4.2): InvalidResponse while polling is transient, so it is re-asked up to the limit.
        for (var i = 0; i < 6; i++)
        {
            _context.Handler.Poll(PipelineResponses.ReadyWithoutSolution);
        }

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.WaitForResultAsync<TurnstileSolution>(TaskId));

        Assert.Equal(AntiCaptchaErrorCode.InvalidResponse, exception.Error.Code);
        Assert.Equal(TaskId, exception.TaskId);
        Assert.Equal(6, Polls);
    }

    [Fact]
    public async Task Wait_ReadyWithoutSolutionThenReady_ReturnsSolution()
    {
        _context.Handler.Poll(PipelineResponses.ReadyWithoutSolution).Poll(PipelineResponses.Ready("SECOND_TIME"));

        var solution = await _context.Client.WaitForResultAsync<TurnstileSolution>(TaskId);

        Assert.Equal("SECOND_TIME", solution.Token);
    }

    [Fact]
    public async Task Wait_CancelledDuringDelay_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        _context.Handler.Poll(PipelineResponses.Processing);
        _context.Clock.OnDelay = _ =>
        {
            if (_context.Clock.Delays.Count == 1)
            {
                cts.Cancel();
            }
        };

        var exception = await Record.ExceptionAsync(() => _context.Client.WaitForResultAsync<TurnstileSolution>(TaskId, null, cts.Token));

        Assert.IsAssignableFrom<OperationCanceledException>(exception);
        Assert.Equal(1, Polls);
    }

    [Fact]
    public async Task Wait_InvalidOptions_ThrowsArgumentOutOfRange_AndSendsNoRequest()
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _context.Client.WaitForResultAsync<TurnstileSolution>(TaskId, new SolveOptions { PollInterval = TimeSpan.Zero }));

        Assert.Equal("o", exception.ParamName);
        Assert.Empty(_context.Handler.Requests);
    }

    [Fact]
    public async Task Wait_ThroughInterface_IsAvailable()
    {
        _context.Handler.Poll(PipelineResponses.Ready("IFACE"));
        IAntiCaptchaClient client = _context.Client;

        var solution = await client.WaitForResultAsync<TurnstileSolution>(TaskId);

        Assert.Equal("IFACE", solution.Token);
    }
}
