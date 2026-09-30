using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AntiCaptchaApi.Net.Internal.Json.Envelopes;
using AntiCaptchaApi.Net.Internal.Time;
using AntiCaptchaApi.Net.Tasks;

namespace AntiCaptchaApi.Net.Internal.Json;

/// <summary>
/// Turns a <c>getTaskResult</c> payload into a <see cref="TaskStatus{TSolution}"/>. A ready task without a usable solution,
/// an out-of-range <c>createTime</c>/<c>endTime</c>, or an unknown status, is an <see cref="AntiCaptchaErrorCode.InvalidResponse"/>.
/// </summary>
internal static class TaskResultMapper
{
    public static TaskStatus<TSolution> Map<TSolution>(GetTaskResultResponse response, int taskId)
        where TSolution : Solution
    {
        switch (response.Status)
        {
            case GetTaskResultResponse.Processing:
                return new TaskStatus<TSolution>(false, null, null, null, null, null, null);
            case GetTaskResultResponse.Ready:
                var solution = ReadSolution<TSolution>(response.Solution)
                    ?? throw InvalidResponse(taskId, "the task is ready but the response has no usable solution.");
                return new TaskStatus<TSolution>(
                    true,
                    solution,
                    response.Cost,
                    response.Ip,
                    FromUnixSeconds(response.CreateTime, taskId, "createTime"),
                    FromUnixSeconds(response.EndTime, taskId, "endTime"),
                    response.SolveCount);
            default:
                throw InvalidResponse(taskId, "the response has an unknown task status.");
        }
    }

    private static TSolution? ReadSolution<TSolution>(JsonElement? element)
        where TSolution : Solution
    {
        if (element is not { ValueKind: JsonValueKind.Object } solution)
        {
            return null;
        }
        var typeInfo = AntiCaptchaJsonContext.Default.GetTypeInfo(typeof(TSolution)) as JsonTypeInfo<TSolution>
            ?? throw new InvalidOperationException(
                $"Solution type '{typeof(TSolution).FullName}' is not registered in AntiCaptchaJsonContext. Add [JsonSerializable(typeof({typeof(TSolution).Name}))].");
        try
        {
            return solution.Deserialize(typeInfo);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static DateTimeOffset? FromUnixSeconds(long? seconds, int taskId, string field)
    {
        if (seconds is not { } value)
        {
            return null;
        }
        return UnixTime.TryFromSeconds(value, out var time)
            ? time
            : throw InvalidResponse(taskId, $"the response has an invalid {field}.");
    }

    private static AntiCaptchaException InvalidResponse(int taskId, string reason) =>
        new(new AntiCaptchaError(AntiCaptchaErrorCode.InvalidResponse, null, "getTaskResult: " + reason, null), taskId, 0);
}
