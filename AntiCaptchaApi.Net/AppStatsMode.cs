using System.Text.Json.Serialization;
using AntiCaptchaApi.Net.Internal.Json;

namespace AntiCaptchaApi.Net;

/// <summary>The kind of statistics for <see cref="IAntiCaptchaClient.GetAppStatsAsync"/>. Sent in lowercase (<c>"errors"</c>).</summary>
[JsonConverter(typeof(LowerCaseEnumConverter<AppStatsMode>))]
public enum AppStatsMode
{
    /// <summary>Errors data; the API default when no mode is sent.</summary>
    Errors = 0,

    /// <summary>App page views.</summary>
    Views = 1,

    /// <summary>Clicks on the "download" link.</summary>
    Downloads = 2,

    /// <summary>The number of users.</summary>
    Users = 3,

    /// <summary>Money earned.</summary>
    Money = 4,
}
