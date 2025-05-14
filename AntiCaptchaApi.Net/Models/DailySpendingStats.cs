namespace AntiCaptchaApi.Net.Models
{
    public class DailySpendingStats
    {
        public int DateFrom { get; set; }
        public int DateTill { get; set; }
        public int Volume { get; set; }
        public double Money { get; set; }
    }
}