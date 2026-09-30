using System;

namespace AntiCaptchaApi.Net;

/// <summary>One record of <see cref="SpendingStats.Days"/>: tasks and money spent between two points in time.</summary>
/// <param name="DateFrom">The start of the record period (UTC).</param>
/// <param name="DateTill">The end of the record period (UTC).</param>
/// <param name="Volume">The number of tasks.</param>
/// <param name="Money">The money spent, in USD.</param>
public sealed record DailySpending(DateTimeOffset DateFrom, DateTimeOffset DateTill, int Volume, decimal Money);
