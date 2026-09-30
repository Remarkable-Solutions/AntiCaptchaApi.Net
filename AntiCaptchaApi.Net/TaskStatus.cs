using System;
using AntiCaptchaApi.Net.Tasks;

namespace AntiCaptchaApi.Net;

/// <summary>
/// The state of a task, as returned by <c>getTaskResult</c>. When <see cref="IsReady"/> is <see langword="true"/>,
/// <see cref="Solution"/> is never <see langword="null"/>; while the task is processing, every other member is null.
/// (The generic arity avoids a clash with <see cref="System.Threading.Tasks.TaskStatus"/>.)
/// </summary>
/// <typeparam name="TSolution">The solution type of the task.</typeparam>
/// <param name="IsReady">Whether the task is solved.</param>
/// <param name="Solution">The solution, when ready.</param>
/// <param name="Cost">The task cost in USD (<c>cost</c>).</param>
/// <param name="Ip">The IP address of the worker (<c>ip</c>).</param>
/// <param name="CreatedAt">When the task was created (<c>createTime</c>), in UTC.</param>
/// <param name="EndedAt">When the task was completed (<c>endTime</c>), in UTC.</param>
/// <param name="SolveCount">The number of workers who tried the task (<c>solveCount</c>).</param>
public sealed record TaskStatus<TSolution>(
    bool IsReady,
    TSolution? Solution,
    decimal? Cost,
    string? Ip,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? EndedAt,
    int? SolveCount)
    where TSolution : Solution;
