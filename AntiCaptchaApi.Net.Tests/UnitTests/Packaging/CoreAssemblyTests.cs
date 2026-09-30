using System;
using System.Linq;
using AntiCaptchaApi.Net.Internal;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Packaging;

public class CoreAssemblyTests
{
    [Fact]
    public void CoreAssembly_DoesNotReferenceNewtonsoftOrMicrosoftExtensions()
    {
        var referenced = typeof(SoftId).Assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        Assert.DoesNotContain(referenced, name =>
            name.StartsWith("Newtonsoft.", StringComparison.Ordinal) ||
            name.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal));
    }

    [Fact]
    public void SoftId_Value_Is1023()
    {
        var value = SoftId.Value;

        Assert.Equal(1023, value);
    }
}
