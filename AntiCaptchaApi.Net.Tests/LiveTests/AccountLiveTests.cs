using System.Text.Json;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Internal;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.LiveTests;

/// <summary>Ports 2.x <c>GetBalanceTests</c>, <c>GetQueueStatsTests</c> and <c>GetAppStatsRequestTests</c> (free calls).</summary>
[Trait("Category", "Live")]
[Collection("Live")]
public class AccountLiveTests
{
    [LiveFact]
    public async Task GetBalance_ReturnsNonNegative()
    {
        var balance = await LiveEnvironment.CreateClient().GetBalanceAsync();

        Assert.True(balance >= 0m);
    }

    [LiveFact]
    public async Task GetQueueStats_RecaptchaV3s07_ReturnsStats()
    {
        var stats = await LiveEnvironment.CreateClient().GetQueueStatsAsync(QueueType.RecaptchaV3s07);

        Assert.NotEqual(0m, stats.Bid);
        Assert.NotEqual(0d, stats.Load);
        Assert.NotEqual(0d, stats.Speed);
        Assert.NotEqual(0, stats.Total);
    }

    /// <remarks>Only the owner of softId 1023 is allowed this call; any other key gets <see cref="AntiCaptchaErrorCode.AccessDenied"/>.</remarks>
    [LiveFact]
    public async Task GetAppStats_OwnSoftId_ReturnsChart()
    {
        var stats = await LiveEnvironment.CreateClient().GetAppStatsAsync(SoftId.Value, AppStatsMode.Errors);

        Assert.NotEqual(JsonValueKind.Undefined, stats.ChartData.ValueKind);
    }
}
