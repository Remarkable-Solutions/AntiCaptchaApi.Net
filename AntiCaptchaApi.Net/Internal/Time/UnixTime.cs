using System;

namespace AntiCaptchaApi.Net.Internal.Time;

/// <summary>
/// Converts unix seconds from API responses without letting an absurd value escape as
/// <see cref="ArgumentOutOfRangeException"/>; callers report a <see langword="false"/> result as <c>InvalidResponse</c>.
/// </summary>
internal static class UnixTime
{
    // The range DateTimeOffset.FromUnixTimeSeconds accepts (years 0001 to 9999).
    private const long MinSeconds = -62_135_596_800L;
    private const long MaxSeconds = 253_402_300_799L;

    public static bool TryFromSeconds(long seconds, out DateTimeOffset value)
    {
        if (seconds is < MinSeconds or > MaxSeconds)
        {
            value = default;
            return false;
        }
        value = DateTimeOffset.FromUnixTimeSeconds(seconds);
        return true;
    }
}
