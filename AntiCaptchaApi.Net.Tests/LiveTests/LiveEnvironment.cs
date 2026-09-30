using System;
using System.Globalization;

namespace AntiCaptchaApi.Net.Tests.LiveTests;

/// <summary>
/// Reads the live-test settings from environment variables. The names are the 2.x <c>TestEnvironment</c> names.
/// Live tests spend real money, so they run only when <c>ClientKey</c> is set.
/// </summary>
internal static class LiveEnvironment
{
    internal const string ClientKeyVariable = "ClientKey";
    internal const string ProxyAddressVariable = "ProxyAddress";
    internal const string ProxyPortVariable = "ProxyPort";
    internal const string ProxyLoginVariable = "ProxyLogin";
    internal const string ProxyPasswordVariable = "ProxyPassword";

    internal const string UserAgent =
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116";

    internal static string? ClientKey => Environment.GetEnvironmentVariable(ClientKeyVariable);

    internal static string? ProxyAddress => Environment.GetEnvironmentVariable(ProxyAddressVariable);

    internal static string? ProxyPort => Environment.GetEnvironmentVariable(ProxyPortVariable);

    internal static string? ProxyLogin => Environment.GetEnvironmentVariable(ProxyLoginVariable);

    internal static string? ProxyPassword => Environment.GetEnvironmentVariable(ProxyPasswordVariable);

    /// <summary>The HTTP proxy from the environment. Call only from a test marked <c>RequiresProxy = true</c>.</summary>
    internal static Proxy Proxy =>
        new(ProxyType.Http, ProxyAddress!, int.Parse(ProxyPort!, CultureInfo.InvariantCulture))
        {
            Login = ProxyLogin,
            Password = ProxyPassword,
        };

    /// <summary>A client for the real API. Call only from a <c>[LiveFact]</c>/<c>[LiveTheory]</c> test (the key is set).</summary>
    internal static AntiCaptchaClient CreateClient() => new(ClientKey!);

    /// <summary>
    /// Returns why a live test must be skipped, or <c>null</c> when it can run.
    /// The reason names the missing variables, never their values.
    /// </summary>
    internal static string? GetSkipReason(Func<string, string?> getVariable, bool requiresProxy)
    {
        if (string.IsNullOrWhiteSpace(getVariable(ClientKeyVariable)))
        {
            return $"Live test: set the {ClientKeyVariable} environment variable to run it (it spends real money).";
        }

        if (requiresProxy &&
            (string.IsNullOrWhiteSpace(getVariable(ProxyAddressVariable)) ||
             string.IsNullOrWhiteSpace(getVariable(ProxyPortVariable)) ||
             string.IsNullOrWhiteSpace(getVariable(ProxyLoginVariable)) ||
             string.IsNullOrWhiteSpace(getVariable(ProxyPasswordVariable))))
        {
            return $"Live proxy test: set {ProxyAddressVariable}, {ProxyPortVariable}, {ProxyLoginVariable} and {ProxyPasswordVariable} to run it.";
        }

        return null;
    }
}
