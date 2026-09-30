using AntiCaptchaApi.Net.Internal.Helpers;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests;

public class PayloadRedactorTests
{
    [Fact]
    public void Redact_ClientKey_Masked()
    {
        const string payload = "{\"clientKey\":\"test-key\",\"taskId\":42}";

        var redacted = PayloadRedactor.Redact(payload);

        Assert.DoesNotContain("test-key", redacted);
        Assert.Equal("***", JObject.Parse(redacted)["clientKey"]!.ToString());
    }

    [Fact]
    public void Redact_NestedProxyPassword_Masked()
    {
        const string payload =
            "{\"clientKey\":\"test-key\",\"task\":{\"type\":\"AmazonTask\",\"proxyLogin\":\"user\",\"proxyPassword\":\"s3cr3t-pw\"," +
            "\"inner\":[{\"proxyPassword\":\"deeper-pw\"}]}}";

        var redacted = PayloadRedactor.Redact(payload);

        Assert.DoesNotContain("s3cr3t-pw", redacted);
        Assert.DoesNotContain("deeper-pw", redacted);
        var json = JObject.Parse(redacted);
        Assert.Equal("***", json["task"]!["proxyPassword"]!.ToString());
        Assert.Equal("***", json["task"]!["inner"]![0]!["proxyPassword"]!.ToString());
    }

    [Fact]
    public void Redact_OtherFields_Unchanged()
    {
        const string payload =
            "{\"clientKey\":\"test-key\",\"softId\":0,\"task\":{\"type\":\"AmazonTask\",\"websiteURL\":\"https://example.com\"," +
            "\"proxyLogin\":\"user\",\"proxyPort\":8080,\"proxyPassword\":\"pw\"},\"languagePool\":\"en\"}";
        var expected = JObject.Parse(payload);
        expected["clientKey"] = "***";
        expected["task"]!["proxyPassword"] = "***";

        var redacted = PayloadRedactor.Redact(payload);

        Assert.True(JToken.DeepEquals(expected, JObject.Parse(redacted)), redacted);
    }

    [Fact]
    public void Redact_InvalidJson_ReturnsPlaceholder()
    {
        const string payload = "{\"clientKey\":\"test-key\", not json";

        var redacted = PayloadRedactor.Redact(payload);

        Assert.Equal(PayloadRedactor.UnparseablePlaceholder, redacted);
        Assert.DoesNotContain("test-key", redacted);
    }
}
