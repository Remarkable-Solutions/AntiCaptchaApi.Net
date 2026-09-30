using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Internal.Errors;
using AntiCaptchaApi.Net.Tasks;

namespace AntiCaptchaApi.Net.Internal.Pipeline;

/// <summary>
/// The solving pipeline of spec FR-7 (design §4.4). All waiting goes through the client's <c>ISystemClock</c>, so tests
/// run it instantly with a fake clock.
/// </summary>
internal sealed class SolvePipeline
{
    private readonly AntiCaptchaClient _client;

    public SolvePipeline(AntiCaptchaClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Steps 2–3 for a validated task: create it, then poll it. API and transport failures are returned as the
    /// <see cref="SolveOutcome{TSolution}.Failure"/> (with the last task id and the attempt count); cancellation escapes.
    /// </summary>
    public async Task<SolveOutcome<TSolution>> SolveAsync<TSolution>(
        CaptchaTask<TSolution> task,
        SolveOptions options,
        SecretRedactor redactor,
        CancellationToken ct)
        where TSolution : Solution
    {
        int? taskId = null;
        var attempts = 0;
        try
        {
            attempts++;
            var createdAt = _client.Clock.UtcNow;
            taskId = await _client.CreateTaskAsync(task, options, ct).ConfigureAwait(false);
            var status = await PollAsync<TSolution>(
                taskId.Value,
                options,
                options.FirstPollDelay ?? task.ResolvedFirstPollDelay,
                createdAt + options.Timeout,
                redactor,
                attempts,
                ct).ConfigureAwait(false);
            return SolveOutcome<TSolution>.Success(taskId.Value, status, attempts);
        }
        catch (AntiCaptchaException ex)
        {
            var failure = ex.TaskId == taskId && ex.Attempts == attempts ? ex : new AntiCaptchaException(ex.Error, taskId, attempts, ex);
            return SolveOutcome<TSolution>.Failed(failure);
        }
    }

    /// <summary>
    /// Step 2.2: waits <paramref name="firstPollDelay"/>, then polls every <see cref="SolveOptions.PollInterval"/> until the
    /// task is ready. Transient errors are tolerated up to <see cref="SolveOptions.MaxConsecutivePollErrors"/> in a row;
    /// any other error is rethrown at once (the caller decides about retries). Past <paramref name="deadline"/> it fails
    /// with <see cref="AntiCaptchaErrorCode.SolveTimeout"/>. Every exception carries <paramref name="taskId"/> and
    /// <paramref name="attempts"/>.
    /// </summary>
    public async Task<TaskStatus<TSolution>> PollAsync<TSolution>(
        int taskId,
        SolveOptions options,
        TimeSpan firstPollDelay,
        DateTimeOffset deadline,
        SecretRedactor redactor,
        int attempts,
        CancellationToken ct)
        where TSolution : Solution
    {
        var clock = _client.Clock;
        var delay = firstPollDelay;
        var consecutiveErrors = 0;
        while (true)
        {
            // The last wait is shortened so that the final poll happens exactly at the deadline.
            var remaining = deadline - clock.UtcNow;
            await clock.Delay(remaining < delay ? Max(remaining, TimeSpan.Zero) : delay, ct).ConfigureAwait(false);
            try
            {
                var status = await _client.GetTaskResultCoreAsync<TSolution>(taskId, redactor, ct).ConfigureAwait(false);
                if (status.IsReady)
                {
                    return status;
                }
                consecutiveErrors = 0;
            }
            catch (AntiCaptchaException ex) when (ex.Error.IsTransient)
            {
                // Polling is free and the task is already paid for, so transient trouble (including an unreadable or
                // solution-less "ready" answer) is simply asked again, up to the limit.
                if (++consecutiveErrors > options.MaxConsecutivePollErrors)
                {
                    throw new AntiCaptchaException(ex.Error, taskId, attempts, ex);
                }
            }
            catch (AntiCaptchaException ex)
            {
                throw new AntiCaptchaException(ex.Error, taskId, attempts, ex);
            }

            if (clock.UtcNow >= deadline)
            {
                throw new AntiCaptchaException(SolveTimeout(options.Timeout), taskId, attempts);
            }
            delay = options.PollInterval;
        }
    }

    private static AntiCaptchaError SolveTimeout(TimeSpan timeout) =>
        new(
            AntiCaptchaErrorCode.SolveTimeout,
            null,
            $"The task was not ready within the solve timeout of {timeout.TotalSeconds.ToString(CultureInfo.InvariantCulture)} s.",
            null);

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;
}
