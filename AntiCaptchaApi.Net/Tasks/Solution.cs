namespace AntiCaptchaApi.Net.Tasks;

/// <summary>
/// Base of every task solution, such as <c>TurnstileSolution</c>. A successful solve always returns a non-null solution.
/// </summary>
public abstract record Solution
{
    private protected Solution()
    {
    }
}
