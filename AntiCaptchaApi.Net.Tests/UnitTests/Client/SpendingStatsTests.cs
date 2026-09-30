using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Client;

public class SpendingStatsTests
{
    // The documented response example (https://anti-captcha.com/apidoc/methods/getSpendingStats), with one number as a string.
    private const string DocExample = "{\"errorId\":0,\"data\":["
        + "{\"dateFrom\":1550533500,\"dateTill\":1550537099,\"volume\":1899,\"money\":7.495948},"
        + "{\"dateFrom\":1550537100,\"dateTill\":1550540699,\"volume\":2217,\"money\":\"7.861741\"}]}";

    private const string Empty = "{\"errorId\":0,\"data\":[]}";

    private readonly ClientTestContext _context = new();

    private JsonNode? SentBody() => Assert.Single(_context.Handler.RequestsFor("getSpendingStats")).Json;

    private static void AssertBody(string expected, JsonNode? actual) =>
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(expected), actual), actual?.ToJsonString());

    [Fact]
    public async Task GetSpendingStats_NullQuery_SendsOnlyClientKey()
    {
        _context.Handler.RespondJson("getSpendingStats", Empty);

        await _context.Client.GetSpendingStatsAsync();

        AssertBody("{\"clientKey\":\"TEST_KEY\"}", SentBody());
    }

    [Fact]
    public async Task GetSpendingStats_EmptyQuery_SendsOnlyClientKey()
    {
        _context.Handler.RespondJson("getSpendingStats", Empty);

        await _context.Client.GetSpendingStatsAsync(new SpendingQuery());

        AssertBody("{\"clientKey\":\"TEST_KEY\"}", SentBody());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(-5)]
    public async Task GetSpendingStats_DateSentAsUnixSeconds(int offsetHours)
    {
        _context.Handler.RespondJson("getSpendingStats", Empty);
        var date = new DateTimeOffset(2022, 12, 28, 0, 0, 0, TimeSpan.Zero).ToOffset(TimeSpan.FromHours(offsetHours));

        await _context.Client.GetSpendingStatsAsync(new SpendingQuery { Date = date });

        AssertBody("{\"clientKey\":\"TEST_KEY\",\"date\":1672185600}", SentBody());
    }

    [Fact]
    public async Task GetSpendingStats_SendsQueueSoftIdIp()
    {
        _context.Handler.RespondJson("getSpendingStats", Empty);

        await _context.Client.GetSpendingStatsAsync(new SpendingQuery { Queue = "Recaptcha Proxyless", SoftId = 777, Ip = "198.51.100.7" });

        AssertBody("{\"clientKey\":\"TEST_KEY\",\"queue\":\"Recaptcha Proxyless\",\"softId\":777,\"ip\":\"198.51.100.7\"}", SentBody());
    }

    [Fact]
    public async Task GetSpendingStats_ReadsDataArray()
    {
        _context.Handler.RespondJson("getSpendingStats", DocExample);

        var stats = await _context.Client.GetSpendingStatsAsync();

        Assert.Equal(
            new[]
            {
                new DailySpending(DateTimeOffset.FromUnixTimeSeconds(1550533500), DateTimeOffset.FromUnixTimeSeconds(1550537099), 1899, 7.495948m),
                new DailySpending(DateTimeOffset.FromUnixTimeSeconds(1550537100), DateTimeOffset.FromUnixTimeSeconds(1550540699), 2217, 7.861741m),
            },
            stats.Days);
    }

    [Fact]
    public async Task GetSpendingStats_EmptyData_ReturnsNoDays()
    {
        _context.Handler.RespondJson("getSpendingStats", Empty);

        var stats = await _context.Client.GetSpendingStatsAsync();

        Assert.Empty(stats.Days);
    }

    [Theory]
    [InlineData("{\"errorId\":0}")]
    [InlineData("{\"errorId\":0,\"data\":null}")]
    [InlineData("{\"errorId\":0,\"AllDailySpendingStats\":[]}")]
    [InlineData("{\"errorId\":0,\"data\":[{\"dateFrom\":1550533500,\"volume\":1,\"money\":1}]}")]
    [InlineData("{\"errorId\":0,\"data\":[null]}")]
    [InlineData("{\"errorId\":0,\"data\":[{\"dateFrom\":99999999999999,\"dateTill\":1550537099,\"volume\":1,\"money\":1}]}")]
    public async Task GetSpendingStats_MissingDataOrField_ThrowsInvalidResponse(string json)
    {
        _context.Handler.RespondJson("getSpendingStats", json);

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.GetSpendingStatsAsync());

        Assert.Equal(AntiCaptchaErrorCode.InvalidResponse, exception.Error.Code);
    }

    [Fact]
    public async Task GetSpendingStats_ApiError_Throws()
    {
        _context.Handler.RespondJson("getSpendingStats", ClientTestContext.ApiError(1, "ERROR_KEY_DOES_NOT_EXIST", "Account authorization key not found in the system"));

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.GetSpendingStatsAsync());

        Assert.Equal(AntiCaptchaErrorCode.KeyDoesNotExist, exception.Error.Code);
    }
}
