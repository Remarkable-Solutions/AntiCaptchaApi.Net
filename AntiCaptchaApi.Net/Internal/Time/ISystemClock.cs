using System;
using System.Threading;
using System.Threading.Tasks;

namespace AntiCaptchaApi.Net.Internal.Time;

/// <summary>
/// Injectable time source for the solving pipeline. It is used instead of <c>TimeProvider</c>, which would need
/// an extra package on netstandard2.0 (spec R-5). Tests substitute a fake whose <see cref="Delay"/> returns at once.
/// </summary>
internal interface ISystemClock
{
    DateTimeOffset UtcNow { get; }

    Task Delay(TimeSpan delay, CancellationToken ct);
}
