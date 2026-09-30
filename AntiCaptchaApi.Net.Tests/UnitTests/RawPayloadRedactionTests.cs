using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Enums;
using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Requests;
using AntiCaptchaApi.Net.Responses;
using AntiCaptchaApi.Net.Tests.Helpers;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests;

public class RawPayloadRedactionTests
{
    private const string ClientKey = "test-key";
    private const string ProxyPassword = "proxy-s3cr3t";

    [Fact]
    public async Task CreateCaptchaTaskAsync_Success_RawPayloadRedacted()
    {
        var handler = new FakeHttpMessageHandler().EnqueueJson("{\"errorId\":0,\"taskId\":1}");

        var response = await CreateTaskAsync(handler);

        Assert.False(response.IsErrorResponse, response.ErrorDescription);
        AssertRedacted(response, handler);
    }

    [Fact]
    public async Task CreateCaptchaTaskAsync_HttpRequestException_RawPayloadRedacted()
    {
        var handler = new FakeHttpMessageHandler().EnqueueException(new HttpRequestException("boom"));

        var response = await CreateTaskAsync(handler);

        Assert.Equal("HttpError", response.ErrorCode);
        AssertRedacted(response, handler);
    }

    [Fact]
    public async Task CreateCaptchaTaskAsync_InvalidJsonResponse_RawPayloadRedacted()
    {
        var handler = new FakeHttpMessageHandler().EnqueueJson("{not json");

        var response = await CreateTaskAsync(handler);

        Assert.Equal("JsonError", response.ErrorCode);
        AssertRedacted(response, handler);
    }

    [Fact]
    public async Task CreateCaptchaTaskAsync_UnexpectedException_RawPayloadRedacted()
    {
        var handler = new FakeHttpMessageHandler().EnqueueException(new InvalidOperationException("unexpected"));

        var response = await CreateTaskAsync(handler);

        Assert.Equal("UnexpectedError", response.ErrorCode);
        AssertRedacted(response, handler);
    }

    [Fact]
    public async Task CreateCaptchaTaskAsync_Cancelled_RawPayloadRedacted()
    {
        var handler = new FakeHttpMessageHandler().EnqueueException(new TaskCanceledException("timeout"));

        var response = await CreateTaskAsync(handler);

        Assert.Equal("Cancelled", response.ErrorCode);
        AssertRedacted(response, handler);
    }

    private static async Task<CreateTaskResponse> CreateTaskAsync(FakeHttpMessageHandler handler)
    {
        var client = new AnticaptchaClient(ClientKey, new ClientConfig(), new HttpClient(handler));
        var request = new AmazonWafRequest
        {
            WebsiteUrl = "https://example.com/waf",
            WebsiteKey = "AQIDAHjcYu/GjX+QlghicBgQ/7bFaQZ+m5FKCMDnO+vTbNg96AF5H1K",
            Iv = "CgAHbCe2GgAAAAAj",
            Context = "9BUgmlm48F92WUoqv97a49ZuEJJ50TCk9MVr3C7WMtQ0X6flVbufM4n8mjFLmbLVAPgaQ1Jydeaja94iAS49ljb",
            ProxyConfig = new ProxyConfig
            {
                ProxyType = ProxyTypeOption.Http,
                ProxyAddress = "10.0.0.1",
                ProxyPort = 8080,
                ProxyLogin = "proxy-user",
                ProxyPassword = ProxyPassword,
            },
        };

        return await client.CreateCaptchaTaskAsync(request, cancellationToken: CancellationToken.None);
    }

    private static void AssertRedacted(CreateTaskResponse response, FakeHttpMessageHandler handler)
    {
        Assert.NotNull(response.RawPayload);
        Assert.DoesNotContain(ClientKey, response.RawPayload);
        Assert.DoesNotContain(ProxyPassword, response.RawPayload);
        var raw = JObject.Parse(response.RawPayload);
        Assert.Equal("***", raw["clientKey"]!.ToString());
        Assert.Equal("***", raw["task"]!["proxyPassword"]!.ToString());

        var wireBody = Assert.Single(handler.RequestBodies);
        Assert.Contains(ClientKey, wireBody);
        Assert.Contains(ProxyPassword, wireBody);
    }
}
