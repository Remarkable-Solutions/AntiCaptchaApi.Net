using System.Collections.Generic;

namespace AntiCaptchaApi.Net;

/// <summary>Account spending for a 24-hour period, as returned by <see cref="IAntiCaptchaClient.GetSpendingStatsAsync"/>.</summary>
/// <param name="Days">The records of the period, in API order (the documented example has one per hour).</param>
public sealed record SpendingStats(IReadOnlyList<DailySpending> Days);
