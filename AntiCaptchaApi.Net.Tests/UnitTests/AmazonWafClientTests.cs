using System.Net.Http;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Enums;
using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Requests;
using AntiCaptchaApi.Net.Tests.Helpers;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests;

public class AmazonWafClientTests
{
    [Fact]
    public async Task CreateCaptchaTaskAsync_AmazonWafProxyless_SendsAmazonTaskProxyless()
    {
        var handler = new FakeHttpMessageHandler().EnqueueJson("{\"errorId\":0,\"taskId\":7654321}");
        var client = new AnticaptchaClient("test-key", new ClientConfig(), new HttpClient(handler));
        var request = new AmazonWafProxylessRequest
        {
            WebsiteUrl = "https://example.com/waf",
            WebsiteKey = "AQIDAHjcYu/GjX+QlghicBgQ/7bFaQZ+m5FKCMDnO+vTbNg96AF5H1K",
            Iv = "CgAHbCe2GgAAAAAj",
            Context = "9BUgmlm48F92WUoqv97a49ZuEJJ50TCk9MVr3C7WMtQ0X6flVbufM4n8mjFLmbLVAPgaQ1Jydeaja94iAS49ljb",
        };

        var response = await client.CreateCaptchaTaskAsync(request);

        Assert.False(response.IsErrorResponse, response.ErrorDescription);
        Assert.Equal(7654321, response.TaskId);
        var task = (JObject)JObject.Parse(Assert.Single(handler.RequestBodies))["task"]!;
        Assert.Equal("AmazonTaskProxyless", task["type"]!.ToString());
        Assert.False(task.ContainsKey("proxyConfig"));
    }

    [Fact]
    public async Task CreateCaptchaTaskAsync_AmazonWafWithProxy_SendsAmazonTask()
    {
        var handler = new FakeHttpMessageHandler().EnqueueJson("{\"errorId\":0,\"taskId\":7654322}");
        var client = new AnticaptchaClient("test-key", new ClientConfig(), new HttpClient(handler));
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
            },
        };

        var response = await client.CreateCaptchaTaskAsync(request);

        Assert.False(response.IsErrorResponse, response.ErrorDescription);
        Assert.Equal(7654322, response.TaskId);
        var task = (JObject)JObject.Parse(Assert.Single(handler.RequestBodies))["task"]!;
        Assert.Equal("AmazonTask", task["type"]!.ToString());
        Assert.Equal("10.0.0.1", task["proxyAddress"]!.ToString());
    }
}
