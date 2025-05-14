using AntiCaptchaApi.Net.Responses;

namespace AntiCaptchaApi.Net.Internal.Models
{
    internal class GetQueueStatsPayload : Payload<GetQueueStatsResponse>
    {
        public GetQueueStatsPayload(int queueId)
        {
            QueueId = queueId;
        }

        public int QueueId { get; }
    }
}