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
    
        /// <summary>
        /// Base64-encoded HTML of pages captured by the template (<c>HTMLsInBase64</c>), keyed by URL, or null if not returned.
        /// The lowercase <c>htmlsInBase64</c> key is also accepted.
        /// </summary>
        [JsonIgnore]
        public JObject HTMLsInBase64 { get; set; }

        /// <summary>
        /// Session storage of the final page (<c>sessionStorage</c>), or null if not returned.
        /// </summary>
        [JsonIgnore]
        public JObject SessionStorage { get; set; }

        /// <summary>
        /// Screenshots taken by the template (<c>screenshots</c>), or null if not returned.
        /// A JSON array is wrapped as <c>{"array": [...]}</c>, like the other object fields.
        /// </summary>
        [JsonIgnore]
        public JObject Screenshots { get; set; }

        /// <summary>
        /// Request headers sent by the worker's browser (<c>requestHeaders</c>), or null if not returned.
        /// </summary>
        [JsonIgnore]
        public JObject RequestHeaders { get; set; }

        /// <summary>
        /// Response headers of the final page (<c>responseHeaders</c>), or null if not returned.
        /// </summary>
        [JsonIgnore]
        public JObject ResponseHeaders { get; set; }

        public override bool IsValid() =>
            Url != null 
            && Domain != null 
            && Cookies != null 
            && LocalStorage != null 
            && Fingerprint != null ;
    }
}