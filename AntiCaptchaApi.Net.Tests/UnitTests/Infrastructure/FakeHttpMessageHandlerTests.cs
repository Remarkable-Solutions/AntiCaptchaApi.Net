using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Infrastructure;

public class FakeHttpMessageHandlerTests
{
    [Fact]
    public async Task Send_UnscriptedMethod_Throws()
    {
        var handler = new FakeHttpMessageHandler().RespondJson("getBalance", "{\"errorId\":0,\"balance\":1}");
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.anti-captcha.com/") };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.PostAsync("createTask", new StringContent("{}", Encoding.UTF8, "application/json")));

        Assert.Contains("createTask", ex.Message);
    }

    [Fact]
    public async Task Send_RecordsMethodAndBody()
    {
        var handler = new FakeHttpMessageHandler().RespondJson("createTask", "{\"errorId\":0,\"taskId\":7}");
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.anti-captcha.com/") };

        var response = await client.PostAsync("createTask",
            new StringContent("{\"clientKey\":\"TEST_KEY\",\"softId\":1023}", Encoding.UTF8, "application/json"));

        var request = Assert.Single(handler.Requests);
        Assert.Equal("createTask", request.Method);
        Assert.Equal("TEST_KEY", (string?)request.Json!["clientKey"]);
        Assert.Equal(1023, (int?)request.Json!["softId"]);
        Assert.Equal("{\"errorId\":0,\"taskId\":7}", await response.Content.ReadAsStringAsync());
    }
}
