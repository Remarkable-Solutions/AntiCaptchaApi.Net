using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Diagnostics;

public class AntiCaptchaDiagnosticsTests
{
    [Fact]
    public void Names_AreAntiCaptcha()
    {
        var names = (AntiCaptchaDiagnostics.ActivitySourceName, AntiCaptchaDiagnostics.MeterName);

        Assert.Equal(("AntiCaptcha", "AntiCaptcha"), names);
    }
}
