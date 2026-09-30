using System;
using System.Text.Json.Serialization;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>
/// Base of every captcha task. Use a concrete task record such as <c>TurnstileTask</c>; the hierarchy is closed,
/// because every task type must be registered for serialization inside this library.
/// </summary>
public abstract record CaptchaTask
{
    private protected CaptchaTask()
    {
    }

    /// <summary>
    /// The proxy that workers use to solve the task, or <see langword="null"/> to solve it without a proxy.
    /// Whether a proxy is allowed or required depends on the task type.
    /// </summary>
    [JsonIgnore]
    public Proxy? Proxy { get; init; }

    /// <summary>The API type name (for example <c>TurnstileTaskProxyless</c>), derived from the proxy mode.</summary>
    internal abstract string ResolveTypeName();

    /// <summary>Checks the proxy rules, then the type rules, and throws <see cref="AntiCaptchaValidationException"/> listing every failure.</summary>
    internal abstract void RunValidation();

    /// <summary>How long to wait after <c>createTask</c> before the first <c>getTaskResult</c> poll.</summary>
    internal abstract TimeSpan ResolvedFirstPollDelay { get; }
}
