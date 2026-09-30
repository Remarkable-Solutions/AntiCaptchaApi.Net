using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Client;

public class GetTaskResultTests
{
    private const int TaskId = 4242;

    private readonly ClientTestContext _context = new();

    private static string Fixture(string solutionTypeName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "TaskResults", solutionTypeName + ".ready.json"));

    public static IEnumerable<object[]> SolutionTypes() =>
        typeof(Solution).Assembly.GetTypes()
            .Where(t => typeof(Solution).IsAssignableFrom(t) && !t.IsAbstract)
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .Select(t => new object[] { t.Name });

    private static Type SolutionType(string name) => typeof(Solution).Assembly.GetTypes().Single(t => t.Name == name && typeof(Solution).IsAssignableFrom(t));

    [Fact]
    public async Task GetTaskResult_SendsClientKeyAndTaskId()
    {
        _context.Handler.RespondJson("getTaskResult", "{\"errorId\":0,\"status\":\"processing\"}");

        await _context.Client.GetTaskResultAsync<TurnstileSolution>(TaskId);

        var body = Assert.Single(_context.Handler.RequestsFor("getTaskResult")).Json!;
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("{\"clientKey\":\"TEST_KEY\",\"taskId\":4242}"), body), body.ToJsonString());
    }

    [Fact]
    public async Task GetTaskResult_Processing_ReturnsNotReady()
    {
        _context.Handler.RespondJson("getTaskResult", "{\"errorId\":0,\"status\":\"processing\"}");

        var status = await _context.Client.GetTaskResultAsync<TurnstileSolution>(TaskId);

        Assert.Equal(new TaskStatus<TurnstileSolution>(false, null, null, null, null, null, null), status);
    }

    [Fact]
    public async Task GetTaskResult_Ready_MapsCostIpTimesSolveCount()
    {
        _context.Handler.RespondJson("getTaskResult", Fixture(nameof(ImageToTextSolution)));

        var status = await _context.Client.GetTaskResultAsync<ImageToTextSolution>(TaskId);

        Assert.True(status.IsReady);
        Assert.Equal(new ImageToTextSolution("deditur", "http://61.39.233.233/1/147220556452507.jpg"), status.Solution);
        Assert.Equal(0.0007m, status.Cost);
        Assert.Equal("46.98.54.221", status.Ip);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1472205564), status.CreatedAt);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1472205570), status.EndedAt);
        Assert.Equal(TimeSpan.Zero, status.CreatedAt!.Value.Offset);
        Assert.Equal(0, status.SolveCount);
    }

    [Theory]
    [InlineData("\"0.000700\"", "\"2\"")]
    [InlineData("0.000700", "2")]
    public async Task GetTaskResult_CostAsString_ParsesDecimal(string cost, string solveCount)
    {
        _context.Handler.RespondJson("getTaskResult",
            $"{{\"errorId\":0,\"status\":\"ready\",\"solution\":{{\"token\":\"T\"}},\"cost\":{cost},\"solveCount\":{solveCount}}}");

        var status = await _context.Client.GetTaskResultAsync<TurnstileSolution>(TaskId);

        Assert.Equal(0.0007m, status.Cost);
        Assert.Equal(2, status.SolveCount);
        Assert.Null(status.CreatedAt);
    }

    [Fact]
    public async Task GetTaskResult_ApiError_Throws()
    {
        _context.Handler.RespondJson("getTaskResult", ClientTestContext.ApiError(16, "ERROR_NO_SUCH_CAPCHA_ID", "Task you are requesting does not exist"));

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.GetTaskResultAsync<TurnstileSolution>(TaskId));

        Assert.Equal(AntiCaptchaErrorCode.NoSuchCaptchaId, exception.Error.Code);
        Assert.Equal(TaskId, exception.TaskId);
        Assert.Equal(0, exception.Attempts);
    }

    [Theory]
    [InlineData("{\"errorId\":0,\"status\":\"ready\"}")]
    [InlineData("{\"errorId\":0,\"status\":\"ready\",\"solution\":null}")]
    [InlineData("{\"errorId\":0,\"status\":\"ready\",\"solution\":\"not an object\"}")]
    [InlineData("{\"errorId\":0,\"status\":\"ready\",\"solution\":{\"coordinates\":\"oops\"}}")]
    [InlineData("{\"errorId\":0,\"status\":\"somethingElse\"}")]
    [InlineData("{\"errorId\":0}")]
    public async Task GetTaskResult_ReadyWithoutUsableSolutionOrUnknownStatus_ThrowsInvalidResponse(string json)
    {
        _context.Handler.RespondJson("getTaskResult", json);

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.GetTaskResultAsync<ImageToCoordinatesSolution>(TaskId));

        Assert.Equal(AntiCaptchaErrorCode.InvalidResponse, exception.Error.Code);
        Assert.Equal(TaskId, exception.TaskId);
    }

    // Just outside the range DateTimeOffset accepts (years 0001 to 9999), and the long extremes.
    [Theory]
    [InlineData("createTime", 253_402_300_800L)]
    [InlineData("createTime", long.MaxValue)]
    [InlineData("createTime", -62_135_596_801L)]
    [InlineData("createTime", long.MinValue)]
    [InlineData("endTime", 253_402_300_800L)]
    [InlineData("endTime", long.MaxValue)]
    [InlineData("endTime", -62_135_596_801L)]
    [InlineData("endTime", long.MinValue)]
    public async Task GetTaskResult_OutOfRangeUnixTime_ThrowsInvalidResponse(string field, long seconds)
    {
        var createTime = field == "createTime" ? seconds : 1472205564;
        var endTime = field == "endTime" ? seconds : 1472205570;
        _context.Handler.RespondJson("getTaskResult",
            $"{{\"errorId\":0,\"status\":\"ready\",\"solution\":{{\"token\":\"T\"}},\"createTime\":{createTime},\"endTime\":{endTime}}}");

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.GetTaskResultAsync<TurnstileSolution>(TaskId));

        Assert.Equal(AntiCaptchaErrorCode.InvalidResponse, exception.Error.Code);
        Assert.Equal(TaskId, exception.TaskId);
    }

    [Fact]
    public void EverySolutionType_HasReadyFixture()
    {
        var missing = SolutionTypes().Select(row => (string)row[0])
            .Where(name => !File.Exists(Path.Combine(AppContext.BaseDirectory, "Fixtures", "TaskResults", name + ".ready.json")))
            .ToList();

        Assert.True(SolutionTypes().Count() >= 11, "Expected at least the 11 catalog solution types.");
        Assert.True(missing.Count == 0, "Missing Fixtures/TaskResults/<Type>.ready.json for: " + string.Join(", ", missing));
    }

    [Theory]
    [MemberData(nameof(SolutionTypes))]
    public async Task EverySolutionType_DeserializesFromFixture(string solutionTypeName)
    {
        var solutionType = SolutionType(solutionTypeName);
        _context.Handler.RespondJson("getTaskResult", Fixture(solutionTypeName));
        var method = typeof(AntiCaptchaClient).GetMethod(nameof(AntiCaptchaClient.GetTaskResultAsync))!.MakeGenericMethod(solutionType);

        var task = (Task)method.Invoke(_context.Client, new object[] { TaskId, default(System.Threading.CancellationToken) })!;
        await task;
        var status = task.GetType().GetProperty("Result")!.GetValue(task)!;

        Assert.True((bool)status.GetType().GetProperty("IsReady")!.GetValue(status)!);
        var solution = status.GetType().GetProperty("Solution")!.GetValue(status);
        Assert.IsType(solutionType, solution);
        var nullability = new NullabilityInfoContext();
        foreach (var property in solutionType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.DeclaringType == solutionType))
        {
            if (nullability.Create(property).ReadState == NullabilityState.NotNull)
            {
                Assert.True(property.GetValue(solution) is not null, $"{solutionTypeName}.{property.Name} is null");
            }
        }
    }

    [Fact]
    public async Task AntiGateSolution_ReadsCookiesLocalStorageFingerprint()
    {
        _context.Handler.RespondJson("getTaskResult", Fixture(nameof(AntiGateSolution)));

        var solution = (await _context.Client.GetTaskResultAsync<AntiGateSolution>(TaskId)).Solution!;

        Assert.Equal("https://anti-captcha.com/tutorials/success-page?login=some%20login&pass=the%20password", solution.Url);
        Assert.Equal("anti-captcha.com", solution.Domain);
        Assert.Equal("1637841149407895406", (string?)solution.Cookies!["_ym_uid"]);
        Assert.Equal("322553582843", (string?)solution.LocalStorage!["_ym40786994_lsid"]);
        Assert.Equal("session value", (string?)solution.SessionStorage!["session_key"]);
        Assert.Equal(1280, (int?)solution.Fingerprint!["self.screen.width"]);
        Assert.Equal("PGRpdiBpZD0iY29udHJvbCI+VGhlIGNvbnRyb2wgdGV4dDwvZGl2Pg==", (string?)solution.HtmlsInBase64!.AsArray()[0]);
        Assert.Empty(solution.Screenshots!.AsArray());
        Assert.Empty(solution.RequestHeaders!.AsArray());
        Assert.Empty(solution.ResponseHeaders!.AsArray());
    }

    [Fact]
    public async Task AntiGateSolution_EmptyMapsAsArraysOrObjects_AreAccepted()
    {
        // The API encodes empty maps as [] (PHP) and documents HTMLsInBase64/screenshots/headers as arrays;
        // either shape must not turn a paid, ready task into an error.
        _context.Handler.RespondJson("getTaskResult",
            "{\"errorId\":0,\"status\":\"ready\",\"solution\":{\"url\":\"https://example.com/\",\"domain\":\"example.com\",\"cookies\":[],\"localStorage\":[],\"sessionStorage\":[],\"fingerprint\":[],\"HTMLsInBase64\":{\"#control\":\"PGRpdj4=\"},\"screenshots\":{\"0\":\"https://example.com/s.png\"},\"requestHeaders\":{},\"responseHeaders\":{}}}");

        var solution = (await _context.Client.GetTaskResultAsync<AntiGateSolution>(TaskId)).Solution!;

        Assert.Empty(solution.Cookies!.AsArray());
        Assert.Empty(solution.Fingerprint!.AsArray());
        Assert.Equal("PGRpdj4=", (string?)solution.HtmlsInBase64!["#control"]);
        Assert.Empty(solution.RequestHeaders!.AsObject());
    }

    [Fact]
    public async Task AntiGateSolution_LowercaseHtmlsInBase64_IsNotRead()
    {
        _context.Handler.RespondJson("getTaskResult",
            "{\"errorId\":0,\"status\":\"ready\",\"solution\":{\"url\":\"https://example.com/\",\"domain\":\"example.com\",\"htmlsInBase64\":[\"PGRpdj4=\"]}}");

        var solution = (await _context.Client.GetTaskResultAsync<AntiGateSolution>(TaskId)).Solution!;

        Assert.Null(solution.HtmlsInBase64);
    }

    [Fact]
    public async Task ImageToCoordinatesSolution_ReadsNestedArrays()
    {
        _context.Handler.RespondJson("getTaskResult", Fixture(nameof(ImageToCoordinatesSolution)));

        var solution = (await _context.Client.GetTaskResultAsync<ImageToCoordinatesSolution>(TaskId)).Solution!;

        Assert.Equal(2, solution.Coordinates.Count);
        Assert.Equal(new[] { 17, 48, 54, 83 }, solution.Coordinates[0]);
        Assert.Equal(new[] { 76, 93, 140, 164 }, solution.Coordinates[1]);
    }

    [Fact]
    public async Task AmazonWafSolution_ReadsToken()
    {
        _context.Handler.RespondJson("getTaskResult", Fixture(nameof(AmazonWafSolution)));

        var solution = (await _context.Client.GetTaskResultAsync<AmazonWafSolution>(TaskId)).Solution!;

        Assert.StartsWith("fe4c2ff3-6ed6-40fa-95c9-4c738a7dad49:", solution.Token);
    }

    [Fact]
    public async Task GetTaskResult_ThroughInterface_IsAvailable()
    {
        _context.Handler.RespondJson("getTaskResult", "{\"errorId\":0,\"status\":\"processing\"}");
        IAntiCaptchaClient client = _context.Client;

        Assert.False((await client.GetTaskResultAsync<TurnstileSolution>(TaskId)).IsReady);
    }
}
