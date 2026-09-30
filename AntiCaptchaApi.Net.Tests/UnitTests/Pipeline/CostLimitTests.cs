using System;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Tasks;
using AntiCaptchaApi.Net.Tests.UnitTests.Client;
using AntiCaptchaApi.Net.Tests.UnitTests.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Pipeline;

public class CostLimitTests
{
    private readonly ClientTestContext _context = new();

    private static TurnstileTask Turnstile() => new(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey);

    private int Creates => _context.Handler.RequestsFor("createTask").Count;

    [Fact]
    public async Task CostLimitReachedBeforeRetry_FailsWithCostLimitReached()
    {
        _context.Handler.Create(PipelineResponses.Created(41)).Poll(PipelineResponses.Error(12, "ERROR_CAPTCHA_UNSOLVABLE"));

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(
            () => _context.Client.SolveAsync(Turnstile(), new SolveOptions { MaxCostPerSolve = 0m }));

        Assert.Equal(AntiCaptchaErrorCode.CostLimitReached, exception.Error.Code);
        Assert.False(exception.Error.IsRetryable);
        Assert.Contains("CaptchaUnsolvable", exception.Error.Description);
        Assert.Equal(41, exception.TaskId);
        Assert.Equal(1, exception.Attempts);
        Assert.Equal(AntiCaptchaErrorCode.CaptchaUnsolvable, Assert.IsType<AntiCaptchaException>(exception.InnerException).Error.Code);
        Assert.Equal(1, Creates);
        Assert.Equal(new[] { TimeSpan.FromSeconds(10) }, _context.Clock.Delays); // the first poll only, no backoff
    }

    [Fact]
    public async Task CostLimitReached_TrySolve_ReturnsErrorResult()
    {
        _context.Handler.Create(PipelineResponses.Error(2, "ERROR_NO_SLOT_AVAILABLE"));

        var result = await _context.Client.TrySolveAsync(Turnstile(), new SolveOptions { MaxCostPerSolve = 0m });

        Assert.Equal(AntiCaptchaErrorCode.CostLimitReached, result.Error!.Code);
        Assert.Null(result.TaskId);
        Assert.Equal(1, result.Attempts);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.001")]
    public async Task SolutionReceived_AlwaysReturned_EvenAboveLimit(string limit)
    {
        _context.Handler.Create(PipelineResponses.Created(42)).Poll(PipelineResponses.Ready("PAID", 0.002m));

        var result = await _context.Client.TrySolveAsync(Turnstile(), new SolveOptions { MaxCostPerSolve = decimal.Parse(limit, System.Globalization.CultureInfo.InvariantCulture) });

        Assert.True(result.IsSuccess);
        Assert.Equal("PAID", result.Solution!.Token);
        Assert.Equal(0.002m, result.Cost);
    }

    [Fact]
    public async Task NoLimit_DoesNotCheck()
    {
        _context.Handler.Create(PipelineResponses.Error(2, "ERROR_NO_SLOT_AVAILABLE"))
            .Create(PipelineResponses.Error(2, "ERROR_NO_SLOT_AVAILABLE"))
            .Create(PipelineResponses.Created(43)).Poll(PipelineResponses.Ready());

        var result = await _context.Client.TrySolveAsync(Turnstile(), new SolveOptions { MaxCostPerSolve = null });

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Attempts);
    }

    [Fact]
    public async Task PositiveLimit_NoCostReportedYet_AllowsRetry()
    {
        // The API reports a cost only with a ready result, which ends the solve; failed attempts report none.
        _context.Handler.Create(PipelineResponses.Created(44)).Poll(PipelineResponses.Error(12, "ERROR_CAPTCHA_UNSOLVABLE"))
            .Create(PipelineResponses.Created(45)).Poll(PipelineResponses.Ready());

        var result = await _context.Client.TrySolveAsync(Turnstile(), new SolveOptions { MaxCostPerSolve = 0.001m });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Attempts);
        Assert.Equal(45, result.TaskId);
    }
}
