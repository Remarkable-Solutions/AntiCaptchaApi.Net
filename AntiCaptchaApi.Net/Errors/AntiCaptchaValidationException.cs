using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AntiCaptchaApi.Net;

/// <summary>
/// Thrown when a task fails client-side validation. It is always thrown <em>before</em> any request is sent,
/// so no money is spent. The message lists every error; it never contains field values such as proxy passwords.
/// </summary>
public sealed class AntiCaptchaValidationException : Exception
{
    /// <summary>Creates the exception from the failed rules.</summary>
    /// <param name="errors">The validation errors; the collection is copied.</param>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> is <see langword="null"/>.</exception>
    public AntiCaptchaValidationException(IEnumerable<ValidationError> errors)
        : this(Copy(errors))
    {
    }

    private AntiCaptchaValidationException(ReadOnlyCollection<ValidationError> errors)
        : base(BuildMessage(errors))
    {
        Errors = errors;
    }

    /// <summary>Every failed rule, in the order in which they were checked.</summary>
    public IReadOnlyList<ValidationError> Errors { get; }

    private static ReadOnlyCollection<ValidationError> Copy(IEnumerable<ValidationError> errors) =>
        new((errors ?? throw new ArgumentNullException(nameof(errors))).ToArray());

    private static string BuildMessage(IEnumerable<ValidationError> errors) =>
        "The task is invalid: " + string.Join("; ", errors.Select(e => $"{e.Field}: {e.Message}"));
}
