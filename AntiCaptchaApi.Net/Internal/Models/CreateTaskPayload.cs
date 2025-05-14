using AntiCaptchaApi.Net.Responses;
using Newtonsoft.Json.Linq;

namespace AntiCaptchaApi.Net.Internal.Models
{
    internal class CreateTaskPayload : SoftAndClientPayload<CreateTaskResponse>
    {
        public CreateTaskPayload(string clientKey, JObject task, string languagePool = null, string callbackUrl = null) : base(clientKey)
        {
            Task = task;
            LanguagePool = languagePool;
            CallbackUrl = callbackUrl;
        }

        public JObject Task { get; }

        public string LanguagePool { get; }

        public string CallbackUrl { get; }
    }
}