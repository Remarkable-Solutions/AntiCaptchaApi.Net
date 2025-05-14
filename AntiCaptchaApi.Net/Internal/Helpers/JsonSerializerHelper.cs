using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AntiCaptchaApi.Net.Internal.Helpers
{
    internal static class JsonSerializerHelper
    {
        private static readonly JsonSerializer JsonSerializer = new JsonSerializer()
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            ContractResolver = new DefaultContractResolver()
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }
        };

        internal static JsonSerializer GetJsonSerializer() => JsonSerializer;
    }
}