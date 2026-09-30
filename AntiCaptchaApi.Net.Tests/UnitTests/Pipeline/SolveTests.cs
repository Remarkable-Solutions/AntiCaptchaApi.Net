using System;
using System.Threading;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Tasks;
using AntiCaptchaApi.Net.Tests.UnitTests.Client;
using AntiCaptchaApi.Net.Tests.UnitTests.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Pipeline;

public class SolveTests
{
    private const int TaskId = 555;

    private readonly ClientTestContext _context = new();

    private static TurnstileTask Turnstile() => new(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey);

    private int Creates => _context.Handler.RequestsFor("createTask").Count;

    private int Polls => _context.Handler.RequestsFor("getTaskResult").Count;

    [Fact]
    public async Task TrySolve_Success_ReturnsTaskIdSolutionCostSolveTimeWorkerIpAttempts1()
    {
        _context.Handler.Create(PipelineResponses.Created(TaskId))
            .Poll(PipelineResponses.Processing).Poll(PipelineResponses.Processing).Poll(PipelineResponses.Ready("TOK", 0.0025m));

        var result = await _context.Client.TrySolveAsync(Turnstile());

        Assert.True(result.IsSuccess);
        Assert.Equal(new SolveResult<TurnstileSolution>(TaskId, new TurnstileSolution("TOK", null), null, 0.0025m, TimeSpan.FromSeconds(12), "198.51.100.7", 1), result);
        Assert.Equal(1, Creates);
        Assert.Equal(3, Polls);
    }

    [Fact]
    public async Task Solve_Success_ReturnsSolution()
    {
        _context.Handler.Create(PipelineResponses.Created(TaskId)).Poll(PipelineResponses.Ready("SOLVED"));

        var solution = await _context.Client.SolveAsync(Turnstile());

        Assert.Equal("SOLVED", solution.Token);
    }

    [Theory]
    [InlineData("turnstile", 10)]
    [InlineData("image", 3)]
    public async Task TrySolve_FirstDelay_UsesTaskFirstPollDelay(string type, int expectedSeconds)
    {
        _context.Handler.Create(PipelineResponses.Created(TaskId)).Poll(PipelineResponses.Processing).Poll(PipelineResponses.Ready());

        var success = type == "image"
            ? (await _context.Client.TrySolveAsync(new ImageToTextTask(TaskSamples.ImageBody))).IsSuccess
            : (await _context.Client.TrySolveAsync(Turnstile())).IsSuccess;

        Assert.True(success);
        Assert.Equal(new[] { TimeSpan.FromSeconds(expectedSeconds), TimeSpan.FromSeconds(3) }, _context.Clock.Delays);
    }

    [Fact]
    public async Task TrySolve_FirstPollDelayOverride_Wins()
    {
        _context.Handler.Create(PipelineResponses.Created(TaskId)).Poll(PipelineResponses.Ready());

        await _context.Client.TrySolveAsync(Turnstile(), new SolveOptions { FirstPollDelay = TimeSpan.FromSeconds(1) });

        Assert.Equal(new[] { TimeSpan.FromSeconds(1) }, _context.Clock.Delays);
    }

    [Fact]
    public async Task TrySolve_UsesClientDefaultSolveOptions_WhenNullPassed()
    {
        var context = new ClientTestContext(options: new AntiCaptchaClientOptions
        {
            DefaultSolveOptions = new SolveOptions { FirstPollDelay = TimeSpan.FromSeconds(2), PollInterval = TimeSpan.FromSeconds(7), LanguagePool = "rn" },
        });
        context.Handler.Create(PipelineResponses.Created(TaskId)).Poll(PipelineResponses.Processing).Poll(PipelineResponses.Ready());

        var result = await context.Client.TrySolveAsync(Turnstile(), null);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(7) }, context.Clock.Delays);
        Assert.Equal("rn", (string?)Assert.Single(context.Handler.RequestsFor("createTask")).Json!["languagePool"]);
    }

    [Fact]
    public async Task TrySolve_PassesLanguagePoolAndCallbackUrl()
    {
        _context.Handler.Create(PipelineResponses.Created(TaskId)).Poll(PipelineResponses.Ready());
        var options = new SolveOptions { LanguagePool = "en", CallbackUrl = new Uri("https://callback.example.com/done") };

        await _context.Client.TrySolveAsync(Turnstile(), options);

        var body = Assert.Single(_context.Handler.RequestsFor("createTask")).Json!;
        Assert.Equal("en", (string?)body["languagePool"]);
        Assert.Equal("https://callback.example.com/done", (string?)body["callbackUrl"]);
    }

    [Fact]
    public async Task TrySolve_InvalidTask_ThrowsValidation_NoRequests()
    {
        var tryException = await Assert.ThrowsAsync<AntiCaptchaValidationException>(() => _context.Client.TrySolveAsync(new TurnstileTask("not-a-url", "")));
        var solveException = await Assert.ThrowsAsync<AntiCaptchaValidationException>(() => _context.Client.SolveAsync(new TurnstileTask("not-a-url", "")));

        Assert.Contains(tryException.Errors, e => e.Field == "WebsiteUrl");
        Assert.Contains(solveException.Errors, e => e.Field == "WebsiteKey");
        Assert.Empty(_context.Handler.Requests);
    }

    [Fact]
    public async Task TrySolve_InvalidOptions_ThrowsArgumentOutOfRange_NoRequests()
    {
        var options = new SolveOptions { MaxAttempts = 0 };

        var tryException = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _context.Client.TrySolveAsync(Turnstile(), options));
        var solveException = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _context.Client.SolveAsync(Turnstile(), options));

        Assert.Equal("o", tryException.ParamName);
        Assert.Equal("o", solveException.ParamName);
        Assert.Empty(_context.Handler.Requests);
    }

    [Fact]
    public async Task TrySolve_RelativeCallbackUrl_ThrowsArgument_NoRequests()
    {
        var options = new SolveOptions { CallbackUrl = new Uri("/done", UriKind.Relative) };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _context.Client.TrySolveAsync(Turnstile(), options));

        Assert.Equal("o", exception.ParamName);
        Assert.Empty(_context.Handler.Requests);
    }

    [Fact]
    public async Task TrySolve_NullTask_ThrowsArgumentNull()
    {
        var tryException = await Assert.ThrowsAsync<ArgumentNullException>(() => _context.Client.TrySolveAsync<TurnstileSolution>(null!));
        var solveException = await Assert.ThrowsAsync<ArgumentNullException>(() => _context.Client.SolveAsync<TurnstileSolution>(null!));

        Assert.Equal("task", tryException.ParamName);
        Assert.Equal("task", solveException.ParamName);
    }

    [Fact]
    public async Task Solve_Failure_ThrowsAntiCaptchaExceptionWithTaskIdAndAttempts()
    {
        _context.Handler.Create(PipelineResponses.Created(TaskId)).Poll(PipelineResponses.Error(16, "ERROR_NO_SUCH_CAPCHA_ID"));

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.SolveAsync(Turnstile()));

        Assert.Equal(AntiCaptchaErrorCode.NoSuchCaptchaId, exception.Error.Code);
        Assert.Equal(TaskId, exception.TaskId);
        Assert.Equal(1, exception.Attempts);
    }

    [Fact]
    public async Task Solve_CreateTaskFails_ThrowsWithoutTaskId_Attempts1()
    {
        _context.Handler.Create(PipelineResponses.Error(10, "ERROR_ZERO_BALANCE"));

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.SolveAsync(Turnstile()));

        Assert.Equal(AntiCaptchaErrorCode.ZeroBalance, exception.Error.Code);
        Assert.Null(exception.TaskId);
        Assert.Equal(1, exception.Attempts);
    }

    [Fact]
    public async Task TrySolve_Failure_ReturnsErrorWithTaskIdAndAttempts()
    {
        _context.Handler.Create(PipelineResponses.Created(TaskId)).Poll(PipelineResponses.Error(16, "ERROR_NO_SUCH_CAPCHA_ID"));

        var result = await _context.Client.TrySolveAsync(Turnstile());

        Assert.False(result.IsSuccess);
        Assert.Equal(AntiCaptchaErrorCode.NoSuchCaptchaId, result.Error!.Code);
        Assert.Equal(TaskId, result.TaskId);
        Assert.Equal(1, result.Attempts);
        Assert.Null(result.Solution);
        Assert.Null(result.Cost);
    }

    [Fact]
    public async Task TrySolve_ReadyWithoutTimes_HasNoSolveTime()
    {
        _context.Handler.Create(PipelineResponses.Created(TaskId)).Poll("{\"errorId\":0,\"status\":\"ready\",\"solution\":{\"token\":\"T\"}}");

        var result = await _context.Client.TrySolveAsync(Turnstile());

        Assert.True(result.IsSuccess);
        Assert.Null(result.SolveTime);
        Assert.Null(result.Cost);
        Assert.Null(result.WorkerIp);
    }

    [Fact]
    public async Task TrySolveAndSolve_CancelledDuringPollDelay_ThrowOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        _context.Handler.Create(PipelineResponses.Created(TaskId)).Create(PipelineResponses.Created(TaskId));
        _context.Clock.OnDelay = _ => cts.Cancel();

        var tryException = await Record.ExceptionAsync(() => _context.Client.TrySolveAsync(Turnstile(), null, cts.Token));
        var solveException = await Record.ExceptionAsync(() => _context.Client.SolveAsync(Turnstile(), null, cts.Token));

        Assert.IsAssignableFrom<OperationCanceledException>(tryException);
        Assert.IsAssignableFrom<OperationCanceledException>(solveException);
    }

    [Fact]
    public async Task SolveMethods_ThroughInterface_AreAvailable()
    {
        _context.Handler.Create(PipelineResponses.Created(1)).Poll(PipelineResponses.Ready("A"))
            .Create(PipelineResponses.Created(2)).Poll(PipelineResponses.Ready("B"));
        IAntiCaptchaClient client = _context.Client;

        var solution = await client.SolveAsync(Turnstile());
        var result = await client.TrySolveAsync(Turnstile());

        Assert.Equal("A", solution.Token);
        Assert.Equal("B", result.Solution!.Token);
    }

    [Fact]
    public void SolveResult_IsSuccess_Semantics()
    {
        var solution = new TurnstileSolution("T", null);
        var error = new AntiCaptchaError(AntiCaptchaErrorCode.ZeroBalance, "ERROR_ZERO_BALANCE", null, 10);

        Assert.True(new SolveResult<TurnstileSolution>(1, solution, null, null, null, null, 1).IsSuccess);
        Assert.False(new SolveResult<TurnstileSolution>(1, null, error, null, null, null, 1).IsSuccess);
        Assert.False(new SolveResult<TurnstileSolution>(1, solution, error, null, null, null, 1).IsSuccess);
        Assert.False(new SolveResult<TurnstileSolution>(null, null, null, null, null, null, 0).IsSuccess);
    }
}
