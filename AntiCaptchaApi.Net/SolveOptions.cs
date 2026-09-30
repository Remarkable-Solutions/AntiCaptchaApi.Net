using System;

namespace AntiCaptchaApi.Net;

/// <summary>
/// Settings of one solve (<c>SolveAsync</c>, <c>TrySolveAsync</c>, <c>WaitForResultAsync</c>). It also carries the
/// <see cref="CreateOptions"/> of the <c>createTask</c> calls. Client-wide defaults are
/// <see cref="AntiCaptchaClientOptions.DefaultSolveOptions"/>. Invalid values throw
/// <see cref="ArgumentOutOfRangeException"/> before any HTTP call.
/// </summary>
public sealed record SolveOptions : CreateOptions
{
    /// <summary>
    /// The time limit of each attempt, measured from <c>createTask</c>. When it elapses the solve fails with
    /// <see cref="AntiCaptchaErrorCode.SolveTimeout"/>, which is not retried (the task may still be charged). It must be
    /// positive. Defaults to 180 seconds.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(180);

    /// <summary>The wait between two <c>getTaskResult</c> polls. It must be positive. Defaults to 3 seconds.</summary>
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// The wait before the first poll. <see langword="null"/> (the default) uses the task type's own delay (for example
    /// 3 seconds for image tasks and 10 seconds for token tasks); <c>WaitForResultAsync</c> then polls at once. It must
    /// not be negative.
    /// </summary>
    public TimeSpan? FirstPollDelay { get; init; }

    /// <summary>
    /// The maximum number of <c>createTask</c> calls of one solve. Retryable errors (for example
    /// <see cref="AntiCaptchaErrorCode.NoSlotAvailable"/>) start a new attempt until this is reached. It must be at least 1.
    /// Defaults to 3.
    /// </summary>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>
    /// How many transient polling errors in a row are tolerated (for example network failures); one more fails the solve,
    /// without a retry. The task is already paid for, so polling it again is preferred to creating a new one. It must not be
    /// negative. Defaults to 5.
    /// </summary>
    public int MaxConsecutivePollErrors { get; init; } = 5;

    /// <summary>
    /// The delay before the second attempt; it doubles with every further attempt, up to <see cref="RetryMaxDelay"/>, and
    /// a ±20 % jitter is applied. It must be positive. Defaults to 2 seconds.
    /// </summary>
    public TimeSpan RetryBaseDelay { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>The cap of the retry delay before jitter. It must be positive. Defaults to 30 seconds.</summary>
    public TimeSpan RetryMaxDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Throws <see cref="ArgumentOutOfRangeException"/> (with <paramref name="paramName"/>) for an invalid value.</summary>
    internal SolveOptions Validate(string paramName)
    {
        Positive(Timeout, nameof(Timeout), paramName);
        Positive(PollInterval, nameof(PollInterval), paramName);
        Positive(RetryBaseDelay, nameof(RetryBaseDelay), paramName);
        Positive(RetryMaxDelay, nameof(RetryMaxDelay), paramName);
        if (FirstPollDelay < TimeSpan.Zero)
        {
            throw Invalid(paramName, $"{nameof(FirstPollDelay)} must not be negative.");
        }
        if (MaxAttempts < 1)
        {
            throw Invalid(paramName, $"{nameof(MaxAttempts)} must be at least 1.");
        }
        if (MaxConsecutivePollErrors < 0)
        {
            throw Invalid(paramName, $"{nameof(MaxConsecutivePollErrors)} must not be negative.");
        }
        return this;
    }

    private static void Positive(TimeSpan value, string option, string paramName)
    {
        if (value <= TimeSpan.Zero)
        {
            throw Invalid(paramName, $"{option} must be positive.");
        }
    }

    private static ArgumentOutOfRangeException Invalid(string paramName, string message) =>
        new(paramName, "SolveOptions." + message);
}
