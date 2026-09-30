using System.Text.Json;

namespace AntiCaptchaApi.Net;

/// <summary>Daily statistics of your app, as returned by <see cref="IAntiCaptchaClient.GetAppStatsAsync"/>.</summary>
/// <param name="FromDate">The start of the report as the API formats it, for example <c>"25 Jan 00:00"</c>.</param>
/// <param name="ToDate">The end of the report as the API formats it, for example <c>"24 Feb 08:09"</c>.</param>
/// <param name="ChartData">
/// The raw chart series (an array in the documented format, ready for HighCharts). It is a detached copy, safe to keep.
/// </param>
public sealed record AppStats(string FromDate, string ToDate, JsonElement ChartData);
