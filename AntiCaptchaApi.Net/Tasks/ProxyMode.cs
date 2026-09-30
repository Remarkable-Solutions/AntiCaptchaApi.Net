namespace AntiCaptchaApi.Net.Tasks;

/// <summary>How a task type uses <see cref="CaptchaTask.Proxy"/>, and how its API type name is derived from it.</summary>
public enum ProxyMode
{
    /// <summary>The task type never uses a proxy. Setting one is a validation error. The type name is the base name.</summary>
    None,

    /// <summary>
    /// A proxy is optional. Without one the type name gets the <c>Proxyless</c> suffix (<c>TurnstileTaskProxyless</c>);
    /// with one it is the base name (<c>TurnstileTask</c>).
    /// </summary>
    Optional,

    /// <summary>A proxy is required. A missing proxy is a validation error. The type name is the base name.</summary>
    Required,

    /// <summary>A proxy is optional and the type name is the base name either way (for example <c>AntiGateTask</c>).</summary>
    OptionalSameName,
}
