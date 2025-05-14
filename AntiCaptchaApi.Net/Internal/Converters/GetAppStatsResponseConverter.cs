using System;
using AntiCaptchaApi.Net.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AntiCaptchaApi.Net.Internal.Converters
{
    public class GetAppStatsResponseConverter : JsonConverter<GetAppStatsResponse>
    {
        public override void WriteJson(JsonWriter writer, GetAppStatsResponse value, JsonSerializer serializer)
        {
            // ignore
        }

        public override GetAppStatsResponse ReadJson(JsonReader reader, Type objectType, GetAppStatsResponse existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            var response = jObject.ToObject<GetAppStatsResponse>();
            if (response is { IsErrorResponse: false })
            {
                response.ChartData = ParseChartData(jObject);   
            }
            return response;
        }

        private static JObject ParseChartData(JObject jObject)
        {
            try
            {
                if (jObject["chartData"] is JObject obj)
                    return obj;

                if (!(jObject["chartData"] is JArray array))
                    return null;
            
                var content = new JProperty("array", array);
                return new JObject
                {
                    content
                };

            }
            catch (Exception)
            {
                return null;
            }   
        }
    }
}