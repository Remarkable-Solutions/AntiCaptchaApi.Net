using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Internal.Json.Envelopes;

/// <summary>Body of <c>POST getQueueStats</c>. The method is public, so no <c>clientKey</c> is sent (as documented and as in 2.x).</summary>
internal sealed class GetQueueStatsRequest
{
    public GetQueueStatsRequest(int queueId)
    {
        QueueId = queueId;
    }

    [JsonPropertyName("queueId")]
    public int QueueId { get; }
}
