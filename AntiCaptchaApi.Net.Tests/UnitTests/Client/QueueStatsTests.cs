using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Client;

public class QueueStatsTests
{
    private const string DocExample = "{\"waiting\":242,\"load\":60.33,\"bid\":\"0.0008600982\",\"speed\":10.77,\"total\":610}";

    // 2.x misspelled the reCAPTCHA v2 queues as "B2"; 3.0 fixes the names and keeps the numbers (spec FR-8).
    private static readonly IReadOnlyDictionary<string, string> Renames = new Dictionary<string, string>
    {
        ["RecaptchaB2WithProxy"] = nameof(QueueType.RecaptchaV2WithProxy),
        ["RecaptchaB2WithoutProxy"] = nameof(QueueType.RecaptchaV2WithoutProxy),
    };

    private readonly ClientTestContext _context = new();

    public static TheoryData<QueueType> AllQueueTypes => new(Enum.GetValues<QueueType>());

    [Theory]
    [MemberData(nameof(AllQueueTypes))]
    public async Task GetQueueStats_SendsQueueId(QueueType queue)
    {
        _context.Handler.RespondJson("getQueueStats", DocExample);

        await _context.Client.GetQueueStatsAsync(queue);

        // getQueueStats is public: the documented body has no clientKey, so the key is not sent (as in 2.x).
        var body = Assert.Single(_context.Handler.RequestsFor("getQueueStats")).Json;
        Assert.True(JsonNode.DeepEquals(new JsonObject { ["queueId"] = (int)queue }, body), body?.ToJsonString());
    }

    [Fact]
    public async Task GetQueueStats_MapsAllFields()
    {
        _context.Handler.RespondJson("getQueueStats", DocExample);

        var stats = await _context.Client.GetQueueStatsAsync(QueueType.RecaptchaV2WithoutProxy);

        Assert.Equal(new QueueStats(242, 60.33, 0.0008600982m, 10.77, 610), stats);
    }

    [Fact]
    public async Task GetQueueStats_NumbersAsStrings_AreAccepted()
    {
        _context.Handler.RespondJson("getQueueStats", "{\"errorId\":0,\"waiting\":\"1\",\"load\":\"2.5\",\"bid\":0.001,\"speed\":\"3\",\"total\":\"4\"}");

        var stats = await _context.Client.GetQueueStatsAsync(QueueType.ImageToTextEng);

        Assert.Equal(new QueueStats(1, 2.5, 0.001m, 3, 4), stats);
    }

    [Theory]
    [InlineData("{\"errorId\":0}")]
    [InlineData("{\"waiting\":242,\"load\":60.33,\"bid\":\"0.0008600982\",\"speed\":10.77}")]
    public async Task GetQueueStats_MissingField_ThrowsInvalidResponse(string json)
    {
        _context.Handler.RespondJson("getQueueStats", json);

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.GetQueueStatsAsync(QueueType.ImageToTextEng));

        Assert.Equal(AntiCaptchaErrorCode.InvalidResponse, exception.Error.Code);
    }

    [Fact]
    public async Task GetQueueStats_ApiError_Throws()
    {
        _context.Handler.RespondJson("getQueueStats", ClientTestContext.ApiError(7, "ERROR_IP_BLOCKED", "Your IP is blocked"));

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.GetQueueStatsAsync(QueueType.ImageToTextEng));

        Assert.Equal(AntiCaptchaErrorCode.IpBlocked, exception.Error.Code);
    }

    [Fact]
    public void QueueType_Values_MatchLegacyNumbers()
    {
        var legacy = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Snapshots", "Legacy2x", "public-api.txt"))
            .Select(line => Regex.Match(line, @"^AntiCaptchaApi\.Net\.Enums\.QueueType\.(\w+) = (\d+)$"))
            .Where(m => m.Success)
            .ToDictionary(m => m.Groups[1].Value, m => int.Parse(m.Groups[2].Value));

        Assert.Equal(14, legacy.Count);
        foreach (var (legacyName, number) in legacy)
        {
            var name = Renames.TryGetValue(legacyName, out var renamed) ? renamed : legacyName;
            Assert.True(Enum.TryParse<QueueType>(name, out var value), $"QueueType.{name} (2.x {legacyName}) is missing.");
            Assert.True(number == (int)value, $"QueueType.{name} = {(int)value}, but 2.x {legacyName} = {number}.");
        }
    }

    [Fact]
    public void QueueType_NewerDocumentedQueues_HaveDocumentedNumbers()
    {
        // Listed on https://anti-captcha.com/apidoc/methods/getQueueStats (checked 2026-09-30); spec Q-4.
        Assert.Equal(26, (int)QueueType.TurnstileWithProxy);
        Assert.Equal(27, (int)QueueType.TurnstileWithoutProxy);
        Assert.Equal(28, (int)QueueType.ImageToCoordinates);
        Assert.Equal(17, Enum.GetValues<QueueType>().Length);
    }
}
