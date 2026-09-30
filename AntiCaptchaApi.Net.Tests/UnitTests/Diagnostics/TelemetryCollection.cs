using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Diagnostics;

/// <summary>
/// The <c>AntiCaptcha</c> source and meter are process-wide, so a listener sees every client in the process. Test classes
/// that attach listeners join this collection, which xUnit runs on its own after the parallel collections: no other
/// test emits telemetry while they listen.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class TelemetryCollection
{
    public const string Name = "Telemetry";
}
