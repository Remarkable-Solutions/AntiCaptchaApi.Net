using AntiCaptchaApi.Net.Responses.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AntiCaptchaApi.Net.Responses
{
    public class GetAppStatsResponse : BaseResponse
    {
        public string FromDate { get; set; }
    
        public string ToDate { get; set; }
    
        [JsonIgnore]
        public JObject ChartData { get; set; }
    }
}