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

        /// <inheritdoc />
        /// <remarks>Kept for API compatibility; the AntiGate-specific parsing happens in <see cref="OnResultParsed"/>.</remarks>
        public override TaskResultResponse<AntiGateSolution> ReadJson(JsonReader reader, Type objectType, TaskResultResponse<AntiGateSolution> existingValue, bool hasExistingValue,
            JsonSerializer serializer) =>
            base.ReadJson(reader, objectType, existingValue, hasExistingValue, serializer);

        /// <inheritdoc />
        protected override void OnResultParsed(JObject source, TaskResultResponse<AntiGateSolution> response)
        {
            if (response is { Status: TaskStatusType.Ready })
            {
                response.Solution ??= new AntiGateSolution();
                ParseJObjects(source, response);
            }
        }

        private static void ParseJObjects(JObject jObject, TaskResultResponse<AntiGateSolution> AntiGateTaskResultResponse)
        {
            AntiGateTaskResultResponse.Solution.Cookies = ParseSolutionJObject(jObject, "cookies");
            AntiGateTaskResultResponse.Solution.LocalStorage = ParseSolutionJObject(jObject, "localStorage");
            AntiGateTaskResultResponse.Solution.Fingerprint = ParseSolutionJObject(jObject, "fingerprint");
            AntiGateTaskResultResponse.Solution.HTMLsInBase64 = ParseSolutionJObject(jObject, "htmlsInBase64");
            AntiGateTaskResultResponse.Solution.SessionStorage = ParseSolutionJObject(jObject, "sessionStorage");
            AntiGateTaskResultResponse.Solution.Screenshots = ParseSolutionJObject(jObject, "screenshots");
            AntiGateTaskResultResponse.Solution.RequestHeaders = ParseSolutionJObject(jObject, "requestHeaders");
            AntiGateTaskResultResponse.Solution.ResponseHeaders = ParseSolutionJObject(jObject, "responseHeaders");
        }
    
    }
}