using System;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>A captcha task whose successful solve returns a <typeparamref name="TSolution"/>.</summary>
/// <typeparam name="TSolution">The solution type of the task.</typeparam>
public abstract record CaptchaTask<TSolution> : CaptchaTask
    where TSolution : Solution
{
    private const int MinPort = 1;
    private const int MaxPort = 65535;

    private protected CaptchaTask()
    {
    }

    /// <summary>The API type name without the <c>Proxyless</c> suffix, for example <c>TurnstileTask</c>.</summary>
    protected abstract string TypeBase { get; }

    /// <summary>How the task uses a proxy. Defaults to <see cref="Tasks.ProxyMode.Optional"/>.</summary>
    protected virtual ProxyMode ProxyMode => ProxyMode.Optional;

    /// <summary>The wait before the first result poll. Defaults to 10 seconds.</summary>
    protected virtual TimeSpan FirstPollDelay => TimeSpan.FromSeconds(10);

    /// <summary>Adds the type-specific rules to <paramref name="v"/>. Proxy rules are checked by the base.</summary>
    /// <param name="v">The validator that collects the errors.</param>
    protected abstract void Validate(Validator v);

    internal sealed override TimeSpan ResolvedFirstPollDelay => FirstPollDelay;

    internal sealed override string ResolveTypeName() =>
        ProxyMode == ProxyMode.Optional && Proxy is null ? TypeBase + "Proxyless" : TypeBase;

    internal sealed override void RunValidation()
    {
        var v = new Validator();
        ValidateProxy(v);
        Validate(v);
        v.ThrowIfInvalid();
    }

    private void ValidateProxy(Validator v)
    {
        if (Proxy is null)
        {
            v.Must(ProxyMode != ProxyMode.Required, nameof(Proxy), "required");
            return;
        }

        if (ProxyMode == ProxyMode.None)
        {
            v.Must(false, nameof(Proxy), "not supported by " + GetType().Name);
            return;
        }

        v.NotEmpty(Proxy.Address, "Proxy.Address")
            .Range(Proxy.Port, MinPort, MaxPort, "Proxy.Port")
            .Must(Enum.IsDefined(typeof(ProxyType), Proxy.Type), "Proxy.Type", "must be a defined ProxyType value");
    }
}
