using Newtonsoft.Json;

namespace AntiCaptchaApi.Net.Models.Solutions
{
    public class GeeTestV4Solution : BaseSolution
    {
        [JsonProperty("captcha_id")]
        public string CaptchaId { get; internal set; }
        [JsonProperty("lot_number")]
        public string LotNumber { get; internal set; }
        [JsonProperty("pass_token")]
        public string PassToken { get; internal set; }
        [JsonProperty("gen_time")]
        public string GenTime { get; internal set; }
        [JsonProperty("captcha_output")]
        public string CaptchaOutput { get; internal set; }

        public override bool IsValid() =>
            CaptchaId != null &&
            LotNumber != null &&
            PassToken != null &&
            GenTime != null &&
            CaptchaOutput != null;
    }
}