using System;
using System.Collections.Generic;
using System.Linq;

namespace AntiCaptchaApi.Net.Internal.Errors;

/// <summary>
/// Replaces the client key and known proxy passwords with <c>***</c> in text that flows into errors (spec FR-9).
/// This is defence in depth: API descriptions can echo proxy strings. Null or empty secrets are ignored, and longer
/// secrets are replaced first so that one secret containing another is fully hidden.
/// </summary>
internal sealed class SecretRedactor
{
    private const string Mask = "***";

    private readonly string[] _secrets;

    public SecretRedactor(string? clientKey, IEnumerable<string?> proxyPasswords)
        : this(proxyPasswords.Prepend(clientKey))
    {
    }

    private SecretRedactor(IEnumerable<string?> secrets)
    {
        _secrets = secrets
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => s!)
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(s => s.Length)
            .ToArray();
    }

    /// <summary>A redactor that also hides <paramref name="password"/>, for example the proxy password of a task.</summary>
    public SecretRedactor With(string? password) =>
        string.IsNullOrEmpty(password) ? this : new SecretRedactor(_secrets.Append(password));

    public string? Redact(string? text)
    {
        if (text is null)
        {
            return null;
        }
        var result = text;
        foreach (var secret in _secrets)
        {
            result = result.Replace(secret, Mask);
        }
        return result;
    }
}
