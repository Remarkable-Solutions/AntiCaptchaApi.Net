using AntiCaptchaApi.Net.Responses;

namespace AntiCaptchaApi.Net.Internal.Models;

internal class GetQueueStatsPayload(int queueId) : Payload<GetQueueStatsResponse>
{
    public int QueueId { get; } = queueId;
}