using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using AntiCaptchaApi.Net.Tasks;

namespace AntiCaptchaApi.Net.Internal.Diagnostics;

/// <summary>
/// The process-wide activity source, meter and instruments of spec FR-10. With no listener attached they cost a flag check:
/// no activity is created and instruments record nothing. Tags hold only task type names, error code names, task ids and
/// attempt counts, never the key, proxy, website data or solution content.
/// </summary>
internal static class Telemetry
{
    public const string TaskTypeTag = "anticaptcha.task.type";
    public const string TaskIdTag = "anticaptcha.task.id";
    public const string AttemptsTag = "anticaptcha.attempts";
    public const string ErrorCodeTag = "anticaptcha.error.code";

    // Metric tag names are the ones of the spec FR-10 instrument table.
    private const string MetricTaskTypeTag = "task.type";
    private const string MetricOutcomeTag = "outcome";
    private const string MetricErrorCodeTag = "error.code";

    private const string SolveActivityName = "AntiCaptcha.Solve";
    private const string CallActivityPrefix = "AntiCaptcha.";

    private static readonly string? Version = typeof(Telemetry).Assembly.GetName().Version?.ToString();

    public static readonly ActivitySource Source = new(AntiCaptchaDiagnostics.ActivitySourceName, Version);

    private static readonly Meter Meter = new(AntiCaptchaDiagnostics.MeterName, Version);

    public static readonly Counter<long> TasksCreated =
        Meter.CreateCounter<long>("anticaptcha.tasks.created", "{task}", "Tasks created with createTask.");

    public static readonly Counter<long> Solves =
        Meter.CreateCounter<long>("anticaptcha.solves", "{solve}", "Finished solves, by outcome (success or failure).");

    public static readonly Counter<long> Failures =
        Meter.CreateCounter<long>("anticaptcha.failures", "{failure}", "Failed solves, by error code.");

    public static readonly Histogram<double> SolveDuration =
        Meter.CreateHistogram<double>("anticaptcha.solve.duration", "s", "Duration of a solve, including retries.");

    public static readonly Counter<double> Cost =
        Meter.CreateCounter<double>("anticaptcha.cost", "USD", "Cost reported by the API for solved tasks.");

    /// <summary>Starts the <c>AntiCaptcha.Solve</c> activity, or returns <see langword="null"/> when nobody listens.</summary>
    public static Activity? StartSolve(string taskType)
    {
        var activity = Source.StartActivity(SolveActivityName, ActivityKind.Internal);
        activity?.SetTag(TaskTypeTag, taskType);
        return activity;
    }

    /// <summary>
    /// Starts the <c>AntiCaptcha.&lt;apiMethod&gt;</c> activity of one API call. The listener check comes first, so that the
    /// name is not even built when nobody listens.
    /// </summary>
    public static Activity? StartCall(string method) =>
        Source.HasListeners() ? Source.StartActivity(CallActivityPrefix + method, ActivityKind.Client) : null;

    /// <summary>
    /// Marks <paramref name="activity"/> as failed. The status description is the error code name, never the error
    /// description, which comes from the API and could echo request data.
    /// </summary>
    public static void SetFailure(Activity? activity, AntiCaptchaErrorCode code)
    {
        if (activity is null)
        {
            return;
        }
        var name = code.ToString();
        activity.SetTag(ErrorCodeTag, name);
        activity.SetStatus(ActivityStatusCode.Error, name);
    }

    /// <summary>Counts one created task. The type name is resolved only when somebody listens.</summary>
    public static void RecordTaskCreated(CaptchaTask task)
    {
        if (TasksCreated.Enabled)
        {
            TasksCreated.Add(1, new KeyValuePair<string, object?>(MetricTaskTypeTag, task.ResolveTypeName()));
        }
    }

    /// <summary>
    /// Ends a solve: tags and status on <paramref name="activity"/>, then the cost (success only, when reported), the
    /// failure by error code, the outcome and the duration.
    /// </summary>
    public static void RecordSolve<TSolution>(Activity? activity, string taskType, SolveResult<TSolution> result, TimeSpan duration)
        where TSolution : Solution
    {
        var error = result.Error;
        if (activity is not null)
        {
            if (result.TaskId is { } taskId)
            {
                activity.SetTag(TaskIdTag, taskId);
            }
            activity.SetTag(AttemptsTag, result.Attempts);
            if (error is not null)
            {
                SetFailure(activity, error.Code);
            }
        }

        var type = new KeyValuePair<string, object?>(MetricTaskTypeTag, taskType);
        if (error is null && result.Cost is { } cost)
        {
            Cost.Add((double)cost, type);
        }
        if (error is not null)
        {
            Failures.Add(1, type, new KeyValuePair<string, object?>(MetricErrorCodeTag, error.Code.ToString()));
        }
        var outcome = new KeyValuePair<string, object?>(MetricOutcomeTag, error is null ? "success" : "failure");
        Solves.Add(1, type, outcome);
        SolveDuration.Record(duration.TotalSeconds, type, outcome);
    }
}
