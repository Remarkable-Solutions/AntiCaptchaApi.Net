using System;

namespace AntiCaptchaApi.Net.Internal.Pipeline;

/// <summary>The retry delay of spec FR-7: <c>min(max, base × 2^(attempt−1)) × (1 ± 20 % jitter)</c>.</summary>
internal static class Backoff
{
    private const double Jitter = 0.2;

    /// <param name="attempt">The attempt that just failed, starting at 1.</param>
    /// <param name="baseDelay">The delay after the first attempt, before jitter.</param>
    /// <param name="max">The cap before jitter.</param>
    /// <param name="random">A value in [0, 1]; 0.5 means no jitter, 0 means −20 % and 1 means +20 %.</param>
    public static TimeSpan Delay(int attempt, TimeSpan baseDelay, TimeSpan max, Func<double> random)
    {
        // Computed in doubles so that large attempt numbers cannot overflow; the cap bounds the result.
        var exponent = Math.Max(0, attempt - 1);
        var uncapped = baseDelay.Ticks * Math.Pow(2, Math.Min(exponent, 62));
        var capped = Math.Min(max.Ticks, uncapped);
        var factor = 1 + ((2 * random()) - 1) * Jitter;
        return TimeSpan.FromTicks((long)Math.Round(capped * factor));
    }
}
