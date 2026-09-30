using Xunit;
using Xunit.Sdk;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Infrastructure;

public class SnapshotAssertTests
{
    [Fact]
    public void Match_DifferentKeyOrder_Passes()
    {
        // The checked-in file has the keys in the order a, b, nested{x, y}.
        const string actual = "{\"nested\":{\"y\":[1,2],\"x\":\"v\"},\"b\":2,\"a\":\"one\"}";

        SnapshotAssert.Match(actual, "Snapshots/Infrastructure/SnapshotAssertTests.sample.json");
    }

    [Fact]
    public void Mismatch_ReportsMissingExtraChangedKeys()
    {
        const string expected = "{\"a\":\"one\",\"b\":2,\"nested\":{\"x\":\"v\"}}";
        const string actual = "{\"b\":3,\"nested\":{\"x\":\"w\"},\"c\":true}";

        var ex = Assert.ThrowsAny<XunitException>(() => SnapshotAssert.Equal(expected, actual));

        Assert.Contains("missing: $.a", ex.Message);
        Assert.Contains("extra: $.c", ex.Message);
        Assert.Contains("changed: $.b expected 2 but was 3", ex.Message);
        Assert.Contains("changed: $.nested.x expected \"v\" but was \"w\"", ex.Message);
    }
}
