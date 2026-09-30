using System;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Internal.Errors;
using AntiCaptchaApi.Net.Internal.Http;
using AntiCaptchaApi.Net.Internal.Json;
using AntiCaptchaApi.Net.Internal.Json.Envelopes;
using AntiCaptchaApi.Net.Tests.UnitTests.Infrastructure;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Http;

public class ApiTransportTests
{
    private const string Key = "TRANSPORT_KEY";
    private const string ProxyPassword = "TRANSPORT_PROXY_PASS";
    private static readonly Uri BaseAddress = new("https://api.example.test/");

    private readonly FakeHttpMessageHandler _handler = new();
    private readonly SecretRedactor _redactor = new(Key, new[] { ProxyPassword });

    private ApiTransport Transport(TimeSpan? requestTimeout = null, Uri? baseAddress = null) =>
        new(new HttpClient(_handler), baseAddress ?? BaseAddress, requestTimeout ?? TimeSpan.FromSeconds(30));

    private Task<CreateTaskResponse> Post(ApiTransport transport, CancellationToken ct = default) =>
        transport.PostAsync(
            "createTask",
            new CreateTaskRequest(Key, new JsonObject { ["type"] = "TestTask" }),
            AntiCaptchaJsonContext.Default.CreateTaskRequest,
            AntiCaptchaJsonContext.Default.CreateTaskResponse,
            _redactor,
            ct);

    private async Task<AntiCaptchaError> PostExpectingError(ApiTransport transport)
    {
        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => Post(transport));
        return exception.Error;
    }

    [Theory]
    [InlineData("https://api.example.test/", "https://api.example.test/createTask")]
    [InlineData("https://gateway.example.test/anticaptcha", "https://gateway.example.test/anticaptcha/createTask")]
    public async Task PostAsync_PostsJsonToBaseAddressPlusMethod(string baseAddress, string expectedUri)
    {
        HttpRequestMessage? seen = null;
        _handler.Script("createTask", request =>
        {
            seen = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"errorId\":0,\"taskId\":7}") });
        });

        var response = await Post(Transport(baseAddress: new Uri(baseAddress)));

        Assert.Equal(7, response.TaskId);
        Assert.NotNull(seen);
        Assert.Equal(HttpMethod.Post, seen!.Method);
        Assert.Equal(expectedUri, seen.RequestUri!.ToString());
        Assert.Equal("application/json", seen.Content!.Headers.ContentType!.MediaType);
        var body = _handler.RequestsFor("createTask")[0].Json!;
        Assert.Equal(Key, (string?)body["clientKey"]);
        Assert.Equal("TestTask", (string?)body["task"]!["type"]);
    }

    [Fact]
    public async Task PostAsync_ErrorIdNonZero_ThrowsMappedAntiCaptchaException()
    {
        _handler.RespondJson("createTask", "{\"errorId\":10,\"errorCode\":\"ERROR_ZERO_BALANCE\",\"errorDescription\":\"Account has zero balance\"}");

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => Post(Transport()));

        Assert.Equal(new AntiCaptchaError(AntiCaptchaErrorCode.ZeroBalance, "ERROR_ZERO_BALANCE", "Account has zero balance", 10), exception.Error);
        Assert.Null(exception.TaskId);
        Assert.Equal(0, exception.Attempts);
    }

    [Fact]
    public async Task PostAsync_HttpRequestException_ThrowsTransientHttpError()
    {
        _handler.Throw("createTask", new HttpRequestException("No such host is known."));

        var error = await PostExpectingError(Transport());

        Assert.Equal(AntiCaptchaErrorCode.HttpError, error.Code);
        Assert.Null(error.RawCode);
        Assert.True(error.IsTransient);
        Assert.Contains("No such host is known.", error.Description);
    }

    [Fact]
    public async Task PostAsync_Status503_ThrowsTransientHttpError()
    {
        _handler.RespondStatus("createTask", HttpStatusCode.ServiceUnavailable);

        var error = await PostExpectingError(Transport());

        Assert.Equal(AntiCaptchaErrorCode.HttpError, error.Code);
        Assert.Equal("503", error.RawCode);
        Assert.True(error.IsTransient);
        Assert.True(error.IsRetryable);
    }

    [Fact]
    public async Task PostAsync_Status400_ThrowsFatalHttpError()
    {
        _handler.RespondStatus("createTask", HttpStatusCode.BadRequest);

        var error = await PostExpectingError(Transport());

        Assert.Equal(AntiCaptchaErrorCode.HttpError, error.Code);
        Assert.Equal("400", error.RawCode);
        Assert.False(error.IsTransient);
        Assert.False(error.IsRetryable);
    }

    [Fact]
    public async Task PostAsync_RequestTimeoutElapses_ThrowsHttpTimeout()
    {
        _handler.Script("createTask", async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            throw new InvalidOperationException("unreachable");
        });

        var error = await PostExpectingError(Transport(TimeSpan.FromMilliseconds(50)));

        Assert.Equal(AntiCaptchaErrorCode.HttpTimeout, error.Code);
        Assert.True(error.IsTransient);
    }

    [Fact]
    public async Task PostAsync_HttpClientTimeout_ThrowsHttpTimeout()
    {
        _handler.Throw("createTask", new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout."));

        var error = await PostExpectingError(Transport());

        Assert.Equal(AntiCaptchaErrorCode.HttpTimeout, error.Code);
    }

    [Fact]
    public async Task PostAsync_UserCancels_ThrowsOperationCanceled_NotAntiCaptchaException()
    {
        using var cts = new CancellationTokenSource();
        _handler.Script("createTask", async (_, ct) =>
        {
            cts.Cancel();
            await Task.Delay(Timeout.Infinite, ct);
            throw new InvalidOperationException("unreachable");
        });

        var exception = await Record.ExceptionAsync(() => Post(Transport(), cts.Token));

        Assert.IsAssignableFrom<OperationCanceledException>(exception);
    }

    [Fact]
    public async Task PostAsync_AlreadyCancelledToken_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        _handler.RespondJson("createTask", "{\"errorId\":0,\"taskId\":7}");

        var exception = await Record.ExceptionAsync(() => Post(Transport(), cts.Token));

        Assert.IsAssignableFrom<OperationCanceledException>(exception);
    }

    [Theory]
    [InlineData("<html>Bad gateway TRANSPORT_KEY</html>")]
    [InlineData("")]
    [InlineData("null")]
    public async Task PostAsync_InvalidJson_ThrowsInvalidResponse_WithoutBody(string body)
    {
        _handler.RespondJson("createTask", body);

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => Post(Transport()));

        Assert.Equal(AntiCaptchaErrorCode.InvalidResponse, exception.Error.Code);
        Assert.DoesNotContain("html", exception.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Key, exception.ToString());
    }

    [Fact]
    public async Task PostAsync_ErrorDescriptionEchoesSecrets_IsRedacted()
    {
        _handler.RespondJson("createTask",
            "{\"errorId\":25,\"errorCode\":\"ERROR_PROXY_CONNECT_REFUSED\",\"errorDescription\":\"key TRANSPORT_KEY proxy user:TRANSPORT_PROXY_PASS@203.0.113.10 refused\"}");

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => Post(Transport()));

        Assert.Equal("key *** proxy user:***@203.0.113.10 refused", exception.Error.Description);
        Assert.DoesNotContain(Key, exception.ToString());
        Assert.DoesNotContain(ProxyPassword, exception.ToString());
    }

    [Fact]
    public async Task PostAsync_HttpRequestExceptionEchoesSecret_IsRedacted()
    {
        _handler.Throw("createTask", new HttpRequestException("proxy TRANSPORT_PROXY_PASS failed"));

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => Post(Transport()));

        Assert.DoesNotContain(ProxyPassword, exception.ToString());
    }
}
