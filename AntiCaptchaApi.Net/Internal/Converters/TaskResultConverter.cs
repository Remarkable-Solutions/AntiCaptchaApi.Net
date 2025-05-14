using System;
using AntiCaptchaApi.Net.Enums;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AntiCaptchaApi.Net.Internal.Converters
{
    public class TaskResultConverter<T> : JsonConverter<TaskResultResponse<T>>
        where T : BaseSolution, new()
    {
        protected JObject jObject;

        public override void WriteJson(JsonWriter writer, TaskResultResponse<T> value, JsonSerializer serializer)
        {
        }

        public override TaskResultResponse<T> ReadJson(JsonReader reader, Type objectType,
            TaskResultResponse<T> existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            jObject = JObject.Load(reader);
            var taskResultResponse = jObject.ToObject<TaskResultResponse<T>>();


            if (taskResultResponse is { Status: TaskStatusType.Ready })
            {
                var createTime = (double?)jObject["createTime"];
                var endTime = (double?)jObject["endTime"];
                taskResultResponse.CreateTimeUtc = UnixTimeStampToDateTime(createTime);
                taskResultResponse.EndTimeUtc = UnixTimeStampToDateTime(endTime);
            }

            if (taskResultResponse is { IsErrorResponse: true })
            {
                taskResultResponse.Status = TaskStatusType.Error;
            }

            return taskResultResponse;
        }


        private static DateTime? UnixTimeStampToDateTime(double? unixTimeStamp)
        {
            if (unixTimeStamp == null)
                return null;

            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return dtDateTime.AddSeconds((double)unixTimeStamp).ToUniversalTime();
        }

        protected static JObject ParseSolutionJObject(JObject jObject, string name)
        {
            try
            {
                if (jObject["solution"][name] is JObject obj)
                    return obj;

                if (!(jObject["solution"][name] is JArray array))
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