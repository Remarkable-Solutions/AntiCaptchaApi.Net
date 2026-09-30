using System;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Tasks;
using AntiCaptchaApi.Net.Tests.UnitTests.Infrastructure;
using AntiCaptchaApi.Net.Tests.UnitTests.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Client;

public class CreateTaskTests
{
    private readonly ClientTestContext _context = new();

    private static TurnstileTask Turnstile() => new(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey);

    [Fact]
    public async Task CreateTaskAsync_InvalidTask_ThrowsValidation_AndSendsNoRequest()
    {
        var exception = await Assert.ThrowsAsync<AntiCaptchaValidationException>(
            () => _context.Client.CreateTaskAsync(new TurnstileTask("not-a-url", "")));

        Assert.Contains(exception.Errors, e => e.Field == "WebsiteUrl");
        Assert.Contains(exception.Errors, e => e.Field == "WebsiteKey");
        Assert.Empty(_context.Handler.Requests);
    }

    [Fact]
    public async Task CreateTaskAsync_SendsClientKeySoftId1023AndTask()
    {
        _context.Handler.RespondJson("createTask", "{\"errorId\":0,\"taskId\":7}");

        await _context.Client.CreateTaskAsync(Turnstile());

        var request = Assert.Single(_context.Handler.RequestsFor("createTask"));
        SnapshotAssert.Match(request.Body!, "Snapshots/Envelopes/createTask.turnstile.json");
    }

    [Fact]
    public async Task CreateTaskAsync_WithCreateOptions_SendsLanguagePoolAndCallbackUrl()
    {
        _context.Handler.RespondJson("createTask", "{\"errorId\":0,\"taskId\":7}");
        var options = new CreateOptions { LanguagePool = "rn", CallbackUrl = new Uri("https://example.com/callback?id=1") };

        await _context.Client.CreateTaskAsync(Turnstile(), options);

        var body = _context.Handler.RequestsFor("createTask")[0].Json!;
        Assert.Equal("rn", (string?)body["languagePool"]);
        Assert.Equal("https://example.com/callback?id=1", (string?)body["callbackUrl"]);
    }

    [Fact]
    public async Task CreateTaskAsync_WithoutCreateOptions_OmitsLanguagePoolAndCallbackUrl()
    {
        _context.Handler.RespondJson("createTask", "{\"errorId\":0,\"taskId\":7}");

        await _context.Client.CreateTaskAsync(Turnstile(), new CreateOptions());

        var body = _context.Handler.RequestsFor("createTask")[0].Json!.AsObject();
        Assert.False(body.ContainsKey("languagePool"));
        Assert.False(body.ContainsKey("callbackUrl"));
    }

    [Fact]
    public async Task CreateTaskAsync_RelativeCallbackUrl_ThrowsArgument_AndSendsNoRequest()
    {
        var options = new CreateOptions { CallbackUrl = new Uri("/callback", UriKind.Relative) };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _context.Client.CreateTaskAsync(Turnstile(), options));

        Assert.Equal("o", exception.ParamName);
        Assert.Empty(_context.Handler.Requests);
    }

    [Fact]
    public async Task CreateTaskAsync_Success_ReturnsTaskId()
    {
        _context.Handler.RespondJson("createTask", "{\"errorId\":0,\"taskId\":123456}");

        var taskId = await _context.Client.CreateTaskAsync(Turnstile());

        Assert.Equal(123456, taskId);
    }

    [Fact]
    public async Task CreateTaskAsync_ApiError_ThrowsAntiCaptchaException()
    {
        _context.Handler.RespondJson("createTask", ClientTestContext.ApiError(10, "ERROR_ZERO_BALANCE", "Account has zero balance"));

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.CreateTaskAsync(Turnstile()));

        Assert.Equal(AntiCaptchaErrorCode.ZeroBalance, exception.Error.Code);
        Assert.Null(exception.TaskId);
    }

    [Fact]
    public async Task CreateTaskAsync_ApiErrorEchoesProxyPassword_IsRedacted()
    {
        _context.Handler.RespondJson("createTask", ClientTestContext.ApiError(25, "ERROR_PROXY_CONNECT_REFUSED", "proxy proxyUser:proxyPass@203.0.113.10 refused"));
        var task = Turnstile() with { Proxy = TaskSamples.Proxy, UserAgent = TaskSamples.UserAgent };

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.CreateTaskAsync(task));

        Assert.Equal("proxy proxyUser:***@203.0.113.10 refused", exception.Error.Description);
    }

    [Fact]
    public async Task CreateTaskAsync_NullTask_ThrowsArgumentNull()
    {
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _context.Client.CreateTaskAsync(null!));

        Assert.Equal("task", exception.ParamName);
        Assert.Empty(_context.Handler.Requests);
    }

    [Fact]
    public async Task CreateTaskAsync_ThroughInterface_IsAvailable()
    {
        _context.Handler.RespondJson("createTask", "{\"errorId\":0,\"taskId\":5}");
        IAntiCaptchaClient client = _context.Client;

        Assert.Equal(5, await client.CreateTaskAsync(Turnstile()));
    }
}
