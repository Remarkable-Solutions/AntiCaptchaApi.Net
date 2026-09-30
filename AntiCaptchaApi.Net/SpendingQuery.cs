using System;

namespace AntiCaptchaApi.Net;

/// <summary>
/// Filters for <see cref="IAntiCaptchaClient.GetSpendingStatsAsync"/>. Every property is optional; unset ones are not sent
/// (<see href="https://anti-captcha.com/apidoc/methods/getSpendingStats"/>).
/// </summary>
public sealed record SpendingQuery
{
    /// <summary>The start of the 24-hour period (the API expects the start of an hour). Sent as unix seconds.</summary>
    public DateTimeOffset? Date { get; init; }

    /// <summary>A queue name as shown in the anti-captcha statistics, for example <c>"Recaptcha Proxyless"</c>; all queues when unset.</summary>
    public string? Queue { get; init; }

    /// <summary>Only tasks created by the app with this Developer Center id.</summary>
    public int? SoftId { get; init; }

    /// <summary>Only calls made from this IP address.</summary>
    public string? Ip { get; init; }
}
