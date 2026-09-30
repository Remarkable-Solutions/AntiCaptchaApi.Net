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
    /// Solves <paramref name="task"/>: validates it, creates it, polls until it is ready, and retries retryable errors
    /// (for example <see cref="AntiCaptchaErrorCode.NoSlotAvailable"/>) with backoff, up to <see cref="SolveOptions.MaxAttempts"/>.
    /// </summary>
    /// <typeparam name="TSolution">The solution type, inferred from the task.</typeparam>
    /// <param name="task">The captcha task, for example <see cref="TurnstileTask"/>.</param>
    /// <param name="o">Solve settings, or <see langword="null"/> for <see cref="AntiCaptchaClientOptions.DefaultSolveOptions"/>.</param>
    /// <param name="ct">Cancels the solve; cancellation always surfaces as <see cref="OperationCanceledException"/>.</param>
    /// <returns>The solution (never null).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="task"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="o"/> has an invalid value. Nothing was sent.</exception>
    /// <exception cref="AntiCaptchaValidationException">The task is invalid. Nothing was sent.</exception>
    /// <exception cref="AntiCaptchaException">
    /// The solve failed; <see cref="AntiCaptchaException.TaskId"/> is the last created task and
    /// <see cref="AntiCaptchaException.Attempts"/> the number of <c>createTask</c> calls.
    /// </exception>
    Task<TSolution> SolveAsync<TSolution>(CaptchaTask<TSolution> task, SolveOptions? o = null, CancellationToken ct = default)
        where TSolution : Solution;

    /// <summary>
    /// Solves <paramref name="task"/> like <see cref="SolveAsync"/>, but returns API and transport failures in
    /// <see cref="SolveResult{TSolution}.Error"/> instead of throwing. Programmer errors (an invalid task or options) and
    /// cancellation still throw.
    /// </summary>
    /// <typeparam name="TSolution">The solution type, inferred from the task.</typeparam>
    /// <param name="task">The captcha task, for example <see cref="TurnstileTask"/>.</param>
    /// <param name="o">Solve settings, or <see langword="null"/> for <see cref="AntiCaptchaClientOptions.DefaultSolveOptions"/>.</param>
    /// <param name="ct">Cancels the solve; cancellation always surfaces as <see cref="OperationCanceledException"/>.</param>
    /// <returns>The result: the solution, cost, solve time and worker IP on success, otherwise the error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="task"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="o"/> has an invalid value. Nothing was sent.</exception>
    /// <exception cref="AntiCaptchaValidationException">The task is invalid. Nothing was sent.</exception>
    Task<SolveResult<TSolution>> TrySolveAsync<TSolution>(CaptchaTask<TSolution> task, SolveOptions? o = null, CancellationToken ct = default)
        where TSolution : Solution;

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

    /// <summary>Reads the state of a task once (<c>getTaskResult</c>), without waiting.</summary>
    /// <typeparam name="TSolution">The solution type of the task, for example <see cref="TurnstileSolution"/>.</typeparam>
    /// <param name="taskId">The id returned by <see cref="CreateTaskAsync"/>.</param>
    /// <param name="ct">Cancels the call; cancellation always surfaces as <see cref="OperationCanceledException"/>.</param>
    /// <returns>The state; when <see cref="TaskStatus{TSolution}.IsReady"/> is true the solution is never null.</returns>
    /// <exception cref="AntiCaptchaException">
    /// The API or the transport failed, or the task is ready without a usable solution (<see cref="AntiCaptchaErrorCode.InvalidResponse"/>).
    /// <see cref="AntiCaptchaException.TaskId"/> is <paramref name="taskId"/>.
    /// </exception>
    Task<TaskStatus<TSolution>> GetTaskResultAsync<TSolution>(int taskId, CancellationToken ct = default)
        where TSolution : Solution;

    /// <summary>
    /// Polls an existing task (<c>getTaskResult</c>) until it is ready, without creating tasks or retrying. The first poll
    /// waits <see cref="SolveOptions.FirstPollDelay"/> (zero when unset), then every <see cref="SolveOptions.PollInterval"/>.
    /// Transient errors (network failures, 408/429/5xx, timeouts, unreadable responses) are tolerated up to
    /// <see cref="SolveOptions.MaxConsecutivePollErrors"/> in a row.
    /// </summary>
    /// <typeparam name="TSolution">The solution type of the task, for example <see cref="TurnstileSolution"/>.</typeparam>
    /// <param name="taskId">The id returned by <see cref="CreateTaskAsync"/>.</param>
    /// <param name="o">Solve settings, or <see langword="null"/> for <see cref="AntiCaptchaClientOptions.DefaultSolveOptions"/>.</param>
    /// <param name="ct">Cancels the wait; cancellation always surfaces as <see cref="OperationCanceledException"/>.</param>
    /// <returns>The solution (never null).</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="o"/> has an invalid value. Nothing was sent.</exception>
    /// <exception cref="AntiCaptchaException">
    /// The API reported an error, the transient-error limit was exceeded, or <see cref="SolveOptions.Timeout"/> elapsed
    /// (<see cref="AntiCaptchaErrorCode.SolveTimeout"/>). <see cref="AntiCaptchaException.TaskId"/> is <paramref name="taskId"/>.
    /// </exception>
    Task<TSolution> WaitForResultAsync<TSolution>(int taskId, SolveOptions? o = null, CancellationToken ct = default)
        where TSolution : Solution;

    /// <summary>
    /// Reports how a solved task worked out (<c>reportIncorrectImageCaptcha</c>, <c>reportIncorrectRecaptcha</c> or
    /// <c>reportCorrectRecaptcha</c>). Send it within 60 seconds of completion; only one report per task is accepted.
    /// </summary>
    /// <param name="taskId">The id of the solved task.</param>
    /// <param name="report">What to report.</param>
    /// <param name="ct">Cancels the call; cancellation always surfaces as <see cref="OperationCanceledException"/>.</param>
    /// <returns>A task that completes when the API accepted the report.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="report"/> is not a defined value. Nothing was sent.</exception>
    /// <exception cref="AntiCaptchaException">
    /// The API or the transport failed, for example <see cref="AntiCaptchaErrorCode.NoSuchCaptchaId"/> when the report is late.
    /// <see cref="AntiCaptchaException.TaskId"/> is <paramref name="taskId"/>.
    /// </exception>
    Task ReportAsync(int taskId, Report report, CancellationToken ct = default);

    /// <summary>
    /// Reads the account balance (<c>getBalance</c>). The API asks not to call this more than once every 30 seconds;
    /// cache the value instead.
    /// </summary>
    /// <param name="ct">Cancels the call; cancellation always surfaces as <see cref="OperationCanceledException"/>.</param>
    /// <returns>The balance in USD.</returns>
    /// <exception cref="AntiCaptchaException">The API or the transport failed.</exception>
    Task<decimal> GetBalanceAsync(CancellationToken ct = default);

    /// <summary>
    /// Reads the load of a worker queue (<c>getQueueStats</c>), for example to pick a good time to create tasks.
    /// The API caches the answer for 10 seconds. The call does not send the API key.
    /// </summary>
    /// <param name="q">The queue.</param>
    /// <param name="ct">Cancels the call; cancellation always surfaces as <see cref="OperationCanceledException"/>.</param>
    /// <returns>The queue statistics.</returns>
    /// <exception cref="AntiCaptchaException">The API or the transport failed.</exception>
    Task<QueueStats> GetQueueStatsAsync(QueueType q, CancellationToken ct = default);
}
