using System;

namespace AntiCaptchaApi.Net;

/// <summary>
/// Thrown when an anti-captcha call or solve fails because of an API or transport error. The message is
/// <c>"{Code}: {Description}"</c>; it never contains the client key or proxy passwords. Cancellation of the caller's
/// token is never reported this way: it always surfaces as <see cref="OperationCanceledException"/>.
/// </summary>
public sealed class AntiCaptchaException : Exception
{
    /// <summary>Creates the exception for a failed call or solve.</summary>
    /// <param name="error">Why the call failed.</param>
    /// <param name="taskId">The task involved (the last created task of a solve), or <see langword="null"/>.</param>
    /// <param name="attempts">The number of <c>createTask</c> calls made; 0 for calls that create no task.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public AntiCaptchaException(AntiCaptchaError error, int? taskId, int attempts, Exception? innerException = null)
        : base(BuildMessage(error ?? throw new ArgumentNullException(nameof(error))), innerException)
    {
        Error = error;
        TaskId = taskId;
        Attempts = attempts;
    }

    /// <summary>Why the call failed.</summary>
    public AntiCaptchaError Error { get; }

    /// <summary>The task involved (for a solve, the last created task), or <see langword="null"/> when none was created.</summary>
    public int? TaskId { get; }

    /// <summary>The number of <c>createTask</c> calls made; 0 for calls that create no task.</summary>
    public int Attempts { get; }

    private static string BuildMessage(AntiCaptchaError error) =>
        error.Description is null ? error.Code.ToString() : $"{error.Code}: {error.Description}";
}
