using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Enums;
using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Requests;
using AntiCaptchaApi.Net.Tests.Helpers;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests;

public class AntiGateClientTests
{
    private const string ClientKey = "test-key";
    private const string ProxyPassword = "proxy-secret";

    private const string ReadyJson =
        "{\"errorId\":0,\"status\":\"ready\",\"solution\":{" +
        "\"url\":\"https://example.com/done\"," +
        "\"domain\":\"example.com\"," +
        "\"cookies\":{\"session\":\"abc\"}," +
        "\"localStorage\":{\"ls_key\":\"ls_value\"}," +
        "\"sessionStorage\":{\"ss_key\":\"ss_value\"}," +
        "\"fingerprint\":{\"self.navigator.userAgent\":\"Mozilla/5.0\"}," +
        "\"screenshots\":[\"https://files.anti-captcha.com/screenshots/1.png\",\"https://files.anti-captcha.com/screenshots/2.png\"]," +
        "\"requestHeaders\":{\"accept-language\":\"en-US\"}," +
        "\"responseHeaders\":{\"content-type\":\"text/html\"}}," +
        "\"cost\":\"0.00200\",\"ip\":\"1.2.3.4\",\"createTime\":1700000000,\"endTime\":1700000030,\"solveCount\":0}";

    [Fact]
    public async Task SolveCaptchaAsync_AntiGate_ReturnsParsedSolution()
    {
        var handler = new FakeHttpMessageHandler()
            .EnqueueJson("{\"errorId\":0,\"taskId\":555}")
            .EnqueueJson(ReadyJson);
        var client = CreateClient(handler);

        var result = await client.SolveCaptchaAsync(CreateRequest());

        Assert.Equal(TaskStatusType.Ready, result.Status);
        Assert.Equal(555, result.CreateTaskResponse.TaskId);
        var solution = result.Solution;
        Assert.Equal("https://example.com/done", solution.Url);
        Assert.Equal("example.com", solution.Domain);
        Assert.Equal("abc", (string?)solution.Cookies["session"]);
        Assert.Equal("ls_value", (string?)solution.LocalStorage["ls_key"]);
        Assert.Equal("Mozilla/5.0", (string?)solution.Fingerprint["self.navigator.userAgent"]);
        Assert.NotNull(result.CreateTimeUtc);
        Assert.NotNull(result.EndTimeUtc);
    }

    [Fact]
    public async Task SolveCaptchaAsync_AntiGate_ParsesAdditionalDocumentedSolutionFields()
    {
        var handler = new FakeHttpMessageHandler()
            .EnqueueJson("{\"errorId\":0,\"taskId\":556}")
            .EnqueueJson(ReadyJson);
        var client = CreateClient(handler);

        var result = await client.SolveCaptchaAsync(CreateRequest());

        var solution = result.Solution;
        Assert.Equal("ss_value", (string?)solution.SessionStorage?["ss_key"]);
        Assert.Equal("en-US", (string?)solution.RequestHeaders?["accept-language"]);
        Assert.Equal("text/html", (string?)solution.ResponseHeaders?["content-type"]);
        // JSON arrays are wrapped as {"array": [...]}, like the other JObject solution fields.
        var screenshots = Assert.IsType<JArray>(solution.Screenshots?["array"]);
        Assert.Equal("https://files.anti-captcha.com/screenshots/2.png", (string?)screenshots[1]);
    }

    [Fact]
    public async Task CreateCaptchaTaskAsync_AntiGateWithProxy_SendsAntiGateTaskWithoutProxyType()
    {
        var handler = new FakeHttpMessageHandler().EnqueueJson("{\"errorId\":0,\"taskId\":557}");
        var client = CreateClient(handler);
        var request = CreateRequest();
        request.ProxyConfig = new ProxyConfig
        {
            ProxyType = ProxyTypeOption.Http,
            ProxyAddress = "1.2.3.4",
            ProxyPort = 3128,
            ProxyLogin = "proxy-user",
            ProxyPassword = ProxyPassword,
        };

        var response = await client.CreateCaptchaTaskAsync(request);

        Assert.False(response.IsErrorResponse, response.ErrorDescription);
        var task = (JObject)JObject.Parse(Assert.Single(handler.RequestBodies))["task"]!;
        Assert.Equal("AntiGateTask", task["type"]!.ToString());
        Assert.False(task.ContainsKey("proxyType"));
        Assert.False(task.ContainsKey("proxyConfig"));
        Assert.Equal("1.2.3.4", task["proxyAddress"]!.ToString());
        Assert.Equal(3128, (int)task["proxyPort"]!);
        Assert.Equal("#x", (string?)task["variables"]!["css_selector"]);
        Assert.DoesNotContain(ProxyPassword, response.RawPayload);
        Assert.DoesNotContain(ClientKey, response.RawPayload);
    }

    [Fact]
    public async Task PushAntiGateVariableAsync_SendsTaskIdNameValue()
    {
        var handler = new FakeHttpMessageHandler().EnqueueJson("{\"errorId\":0,\"status\":\"success\"}");
        var client = CreateClient(handler);

        var response = await client.PushAntiGateVariableAsync(555, "code", "123456");

        Assert.False(response.IsErrorResponse, response.ErrorDescription);
        Assert.Equal("success", response.Status);
        Assert.EndsWith("/pushAntiGateVariable", Assert.Single(handler.RequestUris)!.AbsolutePath);
        var body = JObject.Parse(Assert.Single(handler.RequestBodies));
        Assert.Equal(555, (int)body["taskId"]!);
        Assert.Equal("code", (string?)body["name"]);
        Assert.Equal("123456", (string?)body["value"]);
        Assert.Equal(ClientKey, (string?)body["clientKey"]);
        Assert.DoesNotContain(ClientKey, response.RawPayload);
        Assert.Equal("***", (string?)JObject.Parse(response.RawPayload)["clientKey"]);
    }

    [Fact]
    public async Task PushAntiGateVariableAsync_ObjectValue_SentAsJsonObject()
    {
        var handler = new FakeHttpMessageHandler().EnqueueJson("{\"errorId\":0,\"status\":\"success\"}");
        var client = CreateClient(handler);

        await client.PushAntiGateVariableAsync(555, "credentials", new JObject { ["login"] = "user", ["attempts"] = 2 });

        var value = JObject.Parse(Assert.Single(handler.RequestBodies))["value"];
        var valueObject = Assert.IsType<JObject>(value);
        Assert.Equal("user", (string?)valueObject["login"]);
        Assert.Equal(2, (int)valueObject["attempts"]!);
    }

    private static AntiGateRequest CreateRequest() => new()
    {
        WebsiteUrl = "https://anti-captcha.com/tutorials/v2-textarea",
        TemplateName = "Wait for control appearance",
        Variables = JObject.FromObject(new { css_selector = "#x" }),
    };

    private static AnticaptchaClient CreateClient(FakeHttpMessageHandler handler) =>
        new(ClientKey, new ClientConfig { DelayTimeBetweenCheckingTaskResultMs = 1 }, new HttpClient(handler));
}
