using System;
using System.ComponentModel;
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
        /// <summary>
        /// No longer populated. Kept only for binary compatibility; converter instances are shared,
        /// so per-call state in a field is not thread-safe. Override <see cref="OnResultParsed"/> instead.
        /// </summary>
        [Obsolete("Not thread-safe; no longer populated. Override OnResultParsed instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected JObject jObject;

        public override void WriteJson(JsonWriter writer, TaskResultResponse<T> value, JsonSerializer serializer)
        {
        }

        public override TaskResultResponse<T> ReadJson(JsonReader reader, Type objectType,
            TaskResultResponse<T> existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var source = JObject.Load(reader);
            var taskResultResponse = source.ToObject<TaskResultResponse<T>>();

            if (taskResultResponse is { Status: TaskStatusType.Ready })
            {
                var createTime = (double?)source["createTime"];
                var endTime = (double?)source["endTime"];
                taskResultResponse.CreateTimeUtc = UnixTimeStampToDateTime(createTime);
                taskResultResponse.EndTimeUtc = UnixTimeStampToDateTime(endTime);
            }

            if (taskResultResponse is { IsErrorResponse: true })
            {
                taskResultResponse.Status = TaskStatusType.Error;
            }

            OnResultParsed(source, taskResultResponse);
            return taskResultResponse;
        }

        /// <summary>
        /// Called after the base parsing, with the JSON object this call parsed, so derived converters can
        /// read extra fields without keeping per-call state on the (shared) converter instance.
        /// </summary>
        /// <param name="source">The JSON object of the response being parsed.</param>
        /// <param name="response">The parsed response (may be null for a JSON <c>null</c>).</param>
        protected virtual void OnResultParsed(JObject source, TaskResultResponse<T> response)
        {
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