using AntiCaptchaApi.Net.Responses;

namespace AntiCaptchaApi.Net.Internal.Models;

internal class GetSpendingStatsPayload(
    string clientKey,
    string queue = null,
    int? softId = null,
    int? date = null,
    string ip = null)
    : ClientPayload<GetSpendingStatsResponse>(clientKey)
{
    public int? Date { get; set;} = date;
    public string Queue { get; set; } = queue;
    public int? SoftId { get; set; } = softId;
    public string Ip { get; set; } = ip;
}