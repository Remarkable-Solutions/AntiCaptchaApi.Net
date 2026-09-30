using System;
using System.Threading;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Tasks;

namespace AntiCaptchaApi.Net;

/// <summary>
/// The anti-captcha.com API client: create and solve captcha tasks and query the account.
/// The default implementation is <see cref="AntiCaptchaClient"/>.
/// </summary>
public interface IAntiCaptchaClient
{
    /// <summary>
    /// Validates <paramref name="task"/> and creates it (<c>createTask</c>) without waiting for the result.
    /// Use this with a callback URL, or to poll yourself.
    /// </summary>
    /// <param name="task">The captcha task.</param>
    /// <param name="o">Optional <c>createTask</c> settings.</param>
    /// <param name="ct">Cancels the call; cancellation always surfaces as <see cref="OperationCanceledException"/>.</param>
    /// <returns>The id of the created task.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="task"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="o"/> has a relative <see cref="CreateOptions.CallbackUrl"/>.</exception>
    /// <exception cref="AntiCaptchaValidationException">The task is invalid. Nothing was sent.</exception>
    /// <exception cref="AntiCaptchaException">The API or the transport failed.</exception>
    Task<int> CreateTaskAsync(CaptchaTask task, CreateOptions? o = null, CancellationToken ct = default);
}
