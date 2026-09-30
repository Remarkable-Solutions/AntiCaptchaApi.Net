using AntiCaptchaApi.Net.Tasks;

namespace AntiCaptchaApi.Net.Internal.Pipeline;

/// <summary>
/// The pipeline's answer: the public result, plus the exception that <c>SolveAsync</c> throws on failure (it keeps
/// the inner exception chain, which the result cannot carry).
/// </summary>
internal sealed class SolveOutcome<TSolution>
    where TSolution : Solution
{
    private SolveOutcome(SolveResult<TSolution> result, AntiCaptchaException? failure)
    {
        Result = result;
        Failure = failure;
    }

    public SolveResult<TSolution> Result { get; }

    public AntiCaptchaException? Failure { get; }

    public static SolveOutcome<TSolution> Success(int taskId, TaskStatus<TSolution> status, int attempts) =>
        new(
            new SolveResult<TSolution>(
                taskId,
                status.Solution,
                null,
                status.Cost,
                status.EndedAt - status.CreatedAt,
                status.Ip,
                attempts),
            null);

    public static SolveOutcome<TSolution> Failed(AntiCaptchaException failure) =>
        new(new SolveResult<TSolution>(failure.TaskId, null, failure.Error, null, null, null, failure.Attempts), failure);
}
