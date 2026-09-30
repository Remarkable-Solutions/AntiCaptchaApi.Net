using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Enums;
using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Tests.Helpers;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests;

public class WaitForTaskResultTests
{
    private const int TaskId = 4242;
    private const string ProcessingJson = "{\"errorId\":0,\"status\":\"processing\"}";
    private const string ReadyJson =
        "{\"errorId\":0,\"status\":\"ready\",\"solution\":{\"text\":\"deditur\"},\"cost\":\"0.00070\",\"ip\":\"1.2.3.4\"," +
        "\"createTime\":1700000000,\"endTime\":1700000012,\"solveCount\":0}";
    private const string NoSlotJson =
        "{\"errorId\":16,\"errorCode\":\"ERROR_NO_SUCH_CAPCHA_ID\",\"errorDescription\":\"Task you are requesting does not exist\"}";

    [Fact]
    public async Task Wait_TransientErrorThenReady_ReturnsSolution()
    {
        var handler = new FakeHttpMessageHandler()
            .EnqueueException(new HttpRequestException("blip 1"))
            .EnqueueJson(ProcessingJson)
            .EnqueueException(new HttpRequestException("blip 2"))
            .EnqueueJson(ReadyJson);

        var result = await CreateClient(handler).WaitForTaskResultAsync<ImageToTextSolution>(TaskId);

        Assert.Equal(TaskStatusType.Ready, result.Status);
        Assert.Equal("deditur", result.Solution.Text);
        Assert.Equal(4, handler.RequestCount);
    }

    [Fact]
    public async Task Wait_ThreeConsecutiveTransientErrors_ReturnsLastErrorAfterThreePolls()
    {
        var handler = new FakeHttpMessageHandler()
            .EnqueueException(new HttpRequestException("blip 1"))
            .EnqueueException(new HttpRequestException("blip 2"))
            .EnqueueException(new HttpRequestException("blip 3"))
            .EnqueueJson(ReadyJson);

        var result = await CreateClient(handler).WaitForTaskResultAsync<ImageToTextSolution>(TaskId);

        Assert.Null(result.Status);
        Assert.Equal("HttpError", result.ErrorCode);
        Assert.Contains("blip 3", result.ErrorDescription);
        Assert.Equal(3, handler.RequestCount);
    }

    [Fact]
    public async Task Wait_TransientErrorsNotConsecutive_DoesNotGiveUp()
    {
        var handler = new FakeHttpMessageHandler()
            .EnqueueException(new HttpRequestException("blip 1"))
            .EnqueueException(new HttpRequestException("blip 2"))
            .EnqueueJson(ProcessingJson)
            .EnqueueException(new HttpRequestException("blip 3"))
            .EnqueueException(new HttpRequestException("blip 4"))
            .EnqueueJson(ReadyJson);

        var result = await CreateClient(handler).WaitForTaskResultAsync<ImageToTextSolution>(TaskId);

        Assert.Equal(TaskStatusType.Ready, result.Status);
        Assert.Equal(6, handler.RequestCount);
    }

    [Fact]
    public async Task Wait_InvalidJsonThenReady_ReturnsSolution()
    {
        var handler = new FakeHttpMessageHandler()
            .EnqueueJson("<html>502 Bad Gateway</html>")
            .EnqueueJson(ReadyJson);

        var result = await CreateClient(handler).WaitForTaskResultAsync<ImageToTextSolution>(TaskId);

        Assert.Equal(TaskStatusType.Ready, result.Status);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task Wait_ApiError_ReturnsImmediately()
    {
        var handler = new FakeHttpMessageHandler()
            .EnqueueJson(NoSlotJson)
            .EnqueueJson(ReadyJson);

        var result = await CreateClient(handler).WaitForTaskResultAsync<ImageToTextSolution>(TaskId);

        Assert.Equal(TaskStatusType.Error, result.Status);
        Assert.Equal(16, result.ErrorId);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task Wait_HttpTimeoutWithoutCallerCancellation_IsTransient()
    {
        var handler = new FakeHttpMessageHandler()
            .EnqueueException(new TaskCanceledException("HttpClient.Timeout elapsed"))
            .EnqueueJson(ReadyJson);

        var result = await CreateClient(handler).WaitForTaskResultAsync<ImageToTextSolution>(TaskId);

        Assert.Equal(TaskStatusType.Ready, result.Status);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task Wait_CallerCancelled_StopsPolling()
    {
        using var cts = new CancellationTokenSource();
        var handler = new FakeHttpMessageHandler()
            .EnqueueJson(ProcessingJson)
            .Enqueue(_ =>
            {
                cts.Cancel();
                throw new OperationCanceledException(cts.Token);
            })
            .EnqueueJson(ProcessingJson)
            .EnqueueJson(ReadyJson);
        var client = CreateClient(handler);

        try
        {
            var result = await client.WaitForTaskResultAsync<ImageToTextSolution>(TaskId, cts.Token);
            Assert.Equal("Cancelled", result.ErrorCode);
        }
        catch (OperationCanceledException)
        {
            // Ending the wait by throwing is also a valid way to honor caller cancellation.
        }

        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task Wait_MaxWaitExceeded_ReturnsTimeout()
    {
        var handler = new FakeHttpMessageHandler();
        for (var i = 0; i < 5000; i++)
            handler.EnqueueJson(ProcessingJson);
        var config = new ClientConfig { DelayTimeBetweenCheckingTaskResultMs = 1, MaxWaitForTaskResultTimeMs = 30 };

        var result = await new AnticaptchaClient("test-key", config, new HttpClient(handler))
            .WaitForTaskResultAsync<ImageToTextSolution>(TaskId);

        Assert.Equal(TaskStatusType.Processing, result.Status);
        Assert.Equal("RequestTimeout", result.ErrorCode);
        Assert.True(handler.RequestCount >= 1);
    }

    private static AnticaptchaClient CreateClient(FakeHttpMessageHandler handler) =>
        new("test-key", new ClientConfig { DelayTimeBetweenCheckingTaskResultMs = 1 }, new HttpClient(handler));
}
