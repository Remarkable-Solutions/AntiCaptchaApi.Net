using System;
using AntiCaptchaApi.Net.Enums;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AntiCaptchaApi.Net.Internal.Converters
{
    public class AntiGateTaskResultConverter : TaskResultConverter<AntiGateSolution>
    {
        public override void WriteJson(JsonWriter writer, TaskResultResponse<AntiGateSolution> value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override TaskResultResponse<AntiGateSolution> ReadJson(JsonReader reader, Type objectType, TaskResultResponse<AntiGateSolution> existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var antiGateTaskResultResponse = base.ReadJson(reader, objectType, existingValue, hasExistingValue, serializer);
        
            if (antiGateTaskResultResponse is { Status: TaskStatusType.Ready })
            {
                antiGateTaskResultResponse.Solution ??= new AntiGateSolution();
                ParseJObjects(jObject, antiGateTaskResultResponse);
            }
        
            return antiGateTaskResultResponse;
        }

        private static void ParseJObjects(JObject jObject, TaskResultResponse<AntiGateSolution> AntiGateTaskResultResponse)
        {
            AntiGateTaskResultResponse.Solution.Cookies = ParseSolutionJObject(jObject, "cookies");
            AntiGateTaskResultResponse.Solution.LocalStorage = ParseSolutionJObject(jObject, "localStorage");
            AntiGateTaskResultResponse.Solution.Fingerprint = ParseSolutionJObject(jObject, "fingerprint");
            AntiGateTaskResultResponse.Solution.HTMLsInBase64 = ParseSolutionJObject(jObject, "htmlsInBase64");
        }
    
    }
}