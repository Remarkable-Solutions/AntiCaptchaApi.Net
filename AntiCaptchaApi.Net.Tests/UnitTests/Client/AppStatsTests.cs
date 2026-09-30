using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Client;

public class AppStatsTests
{
    private const int UserSoftId = 123;

    // Shortened documented example (https://anti-captcha.com/apidoc/methods/getAppStats).
    private const string DocExample = "{\"errorId\":0,\"chartData\":[{\"name\":\"Accepted (paid)\",\"data\":[{\"date\":\"25 January\","
        + "\"shortdate\":\"25 Jan\",\"y\":134587,\"beginstamp\":1548374400,\"endstamp\":1548460799,\"stamp\":1548374400}],"
        + "\"itemname\":\"Captchas\",\"errorId\":0,\"code\":\"\",\"description\":\"\"}],\"fromDate\":\"25 Jan 00:00\",\"toDate\":\"24 Feb 08:09\"}";

    private readonly ClientTestContext _context = new();

    public static TheoryData<AppStatsMode, string> AllModes => new()
    {
        { AppStatsMode.Errors, "errors" },
        { AppStatsMode.Views, "views" },
        { AppStatsMode.Downloads, "downloads" },
        { AppStatsMode.Users, "users" },
        { AppStatsMode.Money, "money" },
    };

    private JsonNode? SentBody() => Assert.Single(_context.Handler.RequestsFor("getAppStats")).Json;

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task GetAppStats_SendsSoftIdAndLowercaseMode(AppStatsMode mode, string wire)
    {
        _context.Handler.RespondJson("getAppStats", DocExample);

        await _context.Client.GetAppStatsAsync(UserSoftId, mode);

        var expected = new JsonObject { ["clientKey"] = ClientTestContext.Key, ["softId"] = UserSoftId, ["mode"] = wire };
        Assert.True(JsonNode.DeepEquals(expected, SentBody()), SentBody()?.ToJsonString());
    }

    [Fact]
    public void AppStatsMode_EveryValueIsCoveredByTheModeTheory()
    {
        Assert.Equal(Enum.GetValues<AppStatsMode>().Length, AllModes.Count);
    }

    [Fact]
    public async Task GetAppStats_NullMode_OmitsMode()
    {
        _context.Handler.RespondJson("getAppStats", DocExample);

        await _context.Client.GetAppStatsAsync(UserSoftId);

        // The user's softId filters the stats; it is unrelated to the library's createTask softId (1023).
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("{\"clientKey\":\"TEST_KEY\",\"softId\":123}"), SentBody()), SentBody()?.ToJsonString());
    }

    [Fact]
    public async Task GetAppStats_ReadsFromToAndChartData()
    {
        _context.Handler.RespondJson("getAppStats", DocExample);

        var stats = await _context.Client.GetAppStatsAsync(UserSoftId, AppStatsMode.Errors);

        Assert.Equal("25 Jan 00:00", stats.FromDate);
        Assert.Equal("24 Feb 08:09", stats.ToDate);
        Assert.Equal(JsonValueKind.Array, stats.ChartData.ValueKind);
        var series = stats.ChartData[0];
        Assert.Equal("Accepted (paid)", series.GetProperty("name").GetString());
        Assert.Equal(134587, series.GetProperty("data")[0].GetProperty("y").GetInt32());
    }

    [Fact]
    public async Task GetAppStats_ObjectChartData_IsKept()
    {
        // 2.x accepted an object as well as the documented array.
        _context.Handler.RespondJson("getAppStats", "{\"errorId\":0,\"chartData\":{\"series\":[]},\"fromDate\":\"a\",\"toDate\":\"b\"}");

        var stats = await _context.Client.GetAppStatsAsync(UserSoftId);

        Assert.Equal(JsonValueKind.Object, stats.ChartData.ValueKind);
    }

    [Theory]
    [InlineData("{\"errorId\":0,\"fromDate\":\"a\",\"toDate\":\"b\"}")]
    [InlineData("{\"errorId\":0,\"chartData\":null,\"fromDate\":\"a\",\"toDate\":\"b\"}")]
    [InlineData("{\"errorId\":0,\"chartData\":[],\"toDate\":\"b\"}")]
    [InlineData("{\"errorId\":0,\"chartData\":[],\"fromDate\":\"a\"}")]
    public async Task GetAppStats_MissingField_ThrowsInvalidResponse(string json)
    {
        _context.Handler.RespondJson("getAppStats", json);

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.GetAppStatsAsync(UserSoftId));

        Assert.Equal(AntiCaptchaErrorCode.InvalidResponse, exception.Error.Code);
    }

    [Fact]
    public async Task GetAppStats_ApiError_Throws()
    {
        _context.Handler.RespondJson("getAppStats", ClientTestContext.ApiError(1, "ERROR_KEY_DOES_NOT_EXIST", "Account authorization key not found in the system"));

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.GetAppStatsAsync(UserSoftId));

        Assert.Equal(AntiCaptchaErrorCode.KeyDoesNotExist, exception.Error.Code);
        Assert.Equal("ERROR_KEY_DOES_NOT_EXIST", exception.Error.RawCode);
    }

    [Fact]
    public async Task GetAppStats_AccessDenied_MapsToAccessDenied()
    {
        // The getAppStats page names ERROR_ACCESS_DENIED for non-owners but documents no errorId; 20 is an arbitrary id.
        _context.Handler.RespondJson("getAppStats", ClientTestContext.ApiError(20, "ERROR_ACCESS_DENIED", "Access denied"));

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.GetAppStatsAsync(UserSoftId));

        Assert.Equal(AntiCaptchaErrorCode.AccessDenied, exception.Error.Code);
        Assert.Equal("ERROR_ACCESS_DENIED", exception.Error.RawCode);
        Assert.Equal(20, exception.Error.ErrorId);
        Assert.False(exception.Error.IsRetryable);
        Assert.False(exception.Error.IsTransient);
    }
}
