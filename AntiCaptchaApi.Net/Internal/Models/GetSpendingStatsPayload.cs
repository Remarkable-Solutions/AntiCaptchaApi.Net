using AntiCaptchaApi.Net.Responses;

namespace AntiCaptchaApi.Net.Internal.Models
{
    internal class GetSpendingStatsPayload : ClientPayload<GetSpendingStatsResponse>
    {
        public GetSpendingStatsPayload(string clientKey,
            string queue = null,
            int? softId = null,
            int? date = null,
            string ip = null) : base(clientKey)
        {
            Date = date;
            Queue = queue;
            SoftId = softId;
            Ip = ip;
        }

        public int? Date { get; set;}
        public string Queue { get; set; }
        public int? SoftId { get; set; }
        public string Ip { get; set; }
    }
}