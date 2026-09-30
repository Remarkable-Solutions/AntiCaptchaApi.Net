using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AntiCaptchaApi.Net.Tasks;

/// <summary>
/// Collects validation errors for a task. Every check returns the validator so that checks can be chained, and
/// every failed check is reported (validation does not stop at the first error). The field name defaults to the
/// expression passed as the value (<c>v.NotEmpty(WebsiteKey)</c> reports <c>WebsiteKey</c>); pass it explicitly to override.
/// Checks that receive <see langword="null"/> fail, so guard optional values before checking them.
/// </summary>
public sealed class Validator
{
    private const string ThisPrefix = "this.";

    private readonly List<ValidationError> _errors = new();

    internal Validator()
    {
    }

    internal IReadOnlyList<ValidationError> Errors => _errors;

    /// <summary>Requires an absolute <c>http</c> or <c>https</c> URL.</summary>
    /// <param name="value">The URL to check.</param>
    /// <param name="field">The reported field name; defaults to the <paramref name="value"/> expression.</param>
    /// <returns>This validator.</returns>
    public Validator Url(string? value, [CallerArgumentExpression(nameof(value))] string field = "")
    {
        var valid = Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        return Must(valid, field, "must be an absolute http(s) URL");
    }

    /// <summary>Requires a string that is not <see langword="null"/>, empty or whitespace.</summary>
    /// <param name="value">The string to check.</param>
    /// <param name="field">The reported field name; defaults to the <paramref name="value"/> expression.</param>
    /// <returns>This validator.</returns>
    public Validator NotEmpty(string? value, [CallerArgumentExpression(nameof(value))] string field = "") =>
        Must(!string.IsNullOrWhiteSpace(value), field, "must not be empty");

    /// <summary>Requires a value that is not <see langword="null"/>.</summary>
    /// <param name="value">The value to check.</param>
    /// <param name="field">The reported field name; defaults to the <paramref name="value"/> expression.</param>
    /// <returns>This validator.</returns>
    public Validator NotNull(object? value, [CallerArgumentExpression(nameof(value))] string field = "") =>
        Must(value is not null, field, "must not be null");

    /// <summary>Requires <paramref name="min"/> ≤ <paramref name="value"/> ≤ <paramref name="max"/>.</summary>
    /// <typeparam name="T">A comparable type, such as <see cref="int"/>.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The inclusive upper bound.</param>
    /// <param name="field">The reported field name; defaults to the <paramref name="value"/> expression.</param>
    /// <returns>This validator.</returns>
    public Validator Range<T>(T value, T min, T max, [CallerArgumentExpression(nameof(value))] string field = "")
        where T : IComparable<T> =>
        Must(value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0, field, $"must be between {min} and {max}");

    /// <summary>Requires a value that is one of <paramref name="allowed"/>.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to check; <see langword="null"/> is never in the set.</param>
    /// <param name="allowed">The allowed values. They are listed in the error message.</param>
    /// <param name="field">The reported field name; defaults to the <paramref name="value"/> expression.</param>
    /// <returns>This validator.</returns>
    public Validator OneOf<T>(T? value, IReadOnlyCollection<T> allowed, [CallerArgumentExpression(nameof(value))] string field = "")
    {
        var valid = value is not null && allowed.Contains(value);
        return Must(valid, field, "must be one of: " + string.Join(", ", allowed));
    }

    /// <summary>Reports <paramref name="message"/> for <paramref name="field"/> unless <paramref name="condition"/> holds.</summary>
    /// <param name="condition">The rule; <see langword="false"/> adds an error.</param>
    /// <param name="field">The reported field name.</param>
    /// <param name="message">The error message. Do not include the checked value.</param>
    /// <returns>This validator.</returns>
    public Validator Must(bool condition, string field, string message)
    {
        if (!condition)
        {
            _errors.Add(new ValidationError(Clean(field), message));
        }
        return this;
    }

    /// <summary>Requires a user agent whenever <paramref name="proxy"/> is set (browser-based tasks; 2.x parity).</summary>
    internal Validator UserAgentWhenProxy(Proxy? proxy, string? userAgent) =>
        proxy is null ? this : Must(!string.IsNullOrWhiteSpace(userAgent), "UserAgent", "required when Proxy is set");

    /// <summary>Throws <see cref="AntiCaptchaValidationException"/> when any check failed.</summary>
    internal void ThrowIfInvalid()
    {
        if (_errors.Count > 0)
        {
            throw new AntiCaptchaValidationException(_errors);
        }
    }

    private static string Clean(string field) =>
        field.StartsWith(ThisPrefix, StringComparison.Ordinal) ? field.Substring(ThisPrefix.Length) : field;
}
