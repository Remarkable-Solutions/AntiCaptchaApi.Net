using System;
using AntiCaptchaApi.Net.Tasks;

namespace AntiCaptchaApi.Net;

/// <summary>
/// The outcome of <c>TrySolveAsync</c>: the solution on success, otherwise the error. It never contains the client key
/// or proxy passwords.
/// </summary>
/// <typeparam name="TSolution">The solution type of the task.</typeparam>
/// <param name="TaskId">The id of the last created task, or <see langword="null"/> when no task was created.</param>
/// <param name="Solution">The solution, on success.</param>
/// <param name="Error">Why the solve failed, or <see langword="null"/> on success.</param>
/// <param name="Cost">The cost in USD reported for the solved task.</param>
/// <param name="SolveTime">The time from task creation to completion, as reported by the API.</param>
/// <param name="WorkerIp">The IP address of the worker who solved the task.</param>
/// <param name="Attempts">The number of <c>createTask</c> calls made.</param>
public sealed record SolveResult<TSolution>(
    int? TaskId,
    TSolution? Solution,
    AntiCaptchaError? Error,
    decimal? Cost,
    TimeSpan? SolveTime,
    string? WorkerIp,
    int Attempts)
    where TSolution : Solution
{
    /// <summary>Whether the solve succeeded: there is a solution and no error.</summary>
    public bool IsSuccess => Solution is not null && Error is null;
}
