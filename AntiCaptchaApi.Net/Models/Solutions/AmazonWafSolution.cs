
using Newtonsoft.Json;

namespace AntiCaptchaApi.Net.Models.Solutions
{
    public class AmazonWafSolution : BaseSolution
    {
        /// <summary>
        /// Use this token as a cookie value with name "aws-waf-token" in your request to the target web page.
        /// </summary>
        [JsonProperty("token")]
        public string Token { get; set; }

        public override bool IsValid()
        {
            return !string.IsNullOrEmpty(Token);
        }
    }
}