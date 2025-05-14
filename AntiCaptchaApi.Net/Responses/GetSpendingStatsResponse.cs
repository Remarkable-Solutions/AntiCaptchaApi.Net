using System.Collections.Generic;
using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Responses.Abstractions;

namespace AntiCaptchaApi.Net.Responses
{
    public class GetSpendingStatsResponse : BaseResponse
    {
        public List<DailySpendingStats> AllDailySpendingStats { get; set; }
    }
}