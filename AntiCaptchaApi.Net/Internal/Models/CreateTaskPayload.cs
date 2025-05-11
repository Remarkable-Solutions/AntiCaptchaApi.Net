using AntiCaptchaApi.Net.Responses;
using Newtonsoft.Json.Linq;

namespace AntiCaptchaApi.Net.Internal.Models;

internal class CreateTaskPayload(string clientKey, JObject task, string languagePool = null, string callbackUrl = null)
    : SoftAndClientPayload<CreateTaskResponse>(clientKey)
{
    public JObject Task { get; } = task;

    public string LanguagePool { get; } = languagePool;

    public string CallbackUrl { get; } = callbackUrl;
}