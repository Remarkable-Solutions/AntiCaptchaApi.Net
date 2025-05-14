using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AntiCaptchaApi.Net.Models.Solutions
{
    public class AntiGateSolution : BaseSolution
    {
        public string Url { get; set; }

        public string Domain { get; set; }
    
        [JsonIgnore]
        public JObject Cookies { get; set; }
        
        [JsonIgnore]
        public JObject LocalStorage { get; set; }
        
        [JsonIgnore]
        public JObject Fingerprint { get; set; }
    
        [JsonIgnore]
        public JObject HTMLsInBase64 { get; set; } //TODO: Check if works.

        public override bool IsValid() =>
            Url != null 
            && Domain != null 
            && Cookies != null 
            && LocalStorage != null 
            && Fingerprint != null ;
    }
}