using System;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.LiveTests;

/// <summary>
/// A <see cref="FactAttribute"/> for tests against the real, paid API. It is skipped unless <c>ClientKey</c> is set
/// (and, with <see cref="RequiresProxy"/>, the proxy variables). Test classes also carry <c>[Trait("Category", "Live")]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class LiveFactAttribute : FactAttribute
{
    private readonly Func<string, string?> getVariable;

    /// <summary>Reads the settings from the process environment.</summary>
    public LiveFactAttribute()
        : this(Environment.GetEnvironmentVariable)
    {
    }

    internal LiveFactAttribute(Func<string, string?> getVariable)
    {
        this.getVariable = getVariable;
    }

    /// <summary>When <c>true</c>, the test is also skipped unless the proxy variables are set.</summary>
    public bool RequiresProxy { get; set; }

    /// <inheritdoc />
    public override string? Skip
    {
        get => base.Skip ?? LiveEnvironment.GetSkipReason(getVariable, RequiresProxy);
        set => base.Skip = value;
    }
}
