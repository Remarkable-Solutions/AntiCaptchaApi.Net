using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Tests.Helpers;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests;

public class FakeHttpMessageHandlerTests
{
    private static readonly Uri TestUri = new("https://api.anti-captcha.com/getBalance");

    [Fact]
    public async Task SendAsync_QueuedJson_ReturnsJsonInOrder()
    {
        var handler = new FakeHttpMessageHandler()
            .EnqueueJson("{\"errorId\":0,\"balance\":1.5}")
            .EnqueueJson("{\"errorId\":1}", HttpStatusCode.InternalServerError);
        using var client = new HttpClient(handler);

        using var first = await client.PostAsync(TestUri, new StringContent("{}"));
        using var second = await client.PostAsync(TestUri, new StringContent("{}"));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal("{\"errorId\":0,\"balance\":1.5}", await first.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.InternalServerError, second.StatusCode);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task SendAsync_QueuedHttpRequestException_Throws()
    {
        var expected = new HttpRequestException("network down");
        var handler = new FakeHttpMessageHandler().EnqueueException(expected);
        using var client = new HttpClient(handler);

        var actual = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.PostAsync(TestUri, new StringContent("{}")));

        Assert.Same(expected, actual);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task SendAsync_AnyRequest_RecordsUriAndBody()
    {
        var handler = new FakeHttpMessageHandler().EnqueueJson("{}");
        using var client = new HttpClient(handler);
        const string payload = "{\"clientKey\":\"test-key\"}";

        using var _ = await client.PostAsync(TestUri, new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.Equal(TestUri, Assert.Single(handler.RequestUris));
        Assert.Equal(payload, Assert.Single(handler.RequestBodies));
    }

    [Fact]
    public async Task GetBalanceAsync_ThroughAnticaptchaClient_UsesFakeHandler()
    {
        var handler = new FakeHttpMessageHandler().EnqueueJson("{\"errorId\":0,\"balance\":2.5}");
        var client = new AnticaptchaClient("test-key", new ClientConfig(), new HttpClient(handler));

        var response = await client.GetBalanceAsync();

        Assert.Equal(2.5, response.Balance);
        Assert.EndsWith("getBalance", Assert.Single(handler.RequestUris)!.AbsolutePath);
        Assert.Contains("test-key", Assert.Single(handler.RequestBodies));
    }

    [Fact]
    public async Task SendAsync_Responder_ReceivesRequest()
    {
        HttpRequestMessage? seen = null;
        var handler = new FakeHttpMessageHandler().Enqueue(request =>
        {
            seen = request;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = new HttpClient(handler);

        using var _ = await client.PostAsync(TestUri, new StringContent("{}"));

        Assert.NotNull(seen);
        Assert.Equal(TestUri, seen!.RequestUri);
    }

    [Fact]
    public async Task SendAsync_EmptyQueue_ThrowsInvalidOperationAndCountsRequest()
    {
        var handler = new FakeHttpMessageHandler();
        using var client = new HttpClient(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.PostAsync(TestUri, new StringContent("{}")));

        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task SendAsync_CancelledToken_ThrowsWithoutRecording()
    {
        var handler = new FakeHttpMessageHandler().EnqueueJson("{}");
        using var client = new HttpClient(handler);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.PostAsync(TestUri, new StringContent("{}"), cts.Token));

        Assert.Equal(0, handler.RequestCount);
    }
}
