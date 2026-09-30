using AntiCaptchaApi.Net.Internal.Json;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Tasks;

public class GeeTestV3TaskTests
{
    [Fact]
    public void Serialize_Version3_AndTypeGeeTestTask()
    {
        var task = new GeeTestV3Task(TaskSamples.WebsiteUrl, "GT", "CHALLENGE") { Proxy = TaskSamples.Proxy, UserAgent = TaskSamples.UserAgent };

        var node = TaskSerializer.ToJsonObject(task);

        Assert.Equal(3, (int)node["version"]!);
        Assert.Equal("GeeTestTask", (string?)node["type"]);
        Assert.Equal("GeeTestTaskProxyless", (string?)TaskSerializer.ToJsonObject(task with { Proxy = null })["type"]);
    }

    [Fact]
    public void Validate_EmptyChallenge_Fails()
    {
        var task = new GeeTestV3Task(TaskSamples.WebsiteUrl, "GT", " ");

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal("Challenge", Assert.Single(exception.Errors).Field);
    }

    [Fact]
    public void Validate_ProxyWithoutUserAgent_Fails()
    {
        var task = new GeeTestV3Task(TaskSamples.WebsiteUrl, "GT", "CHALLENGE") { Proxy = TaskSamples.Proxy };

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal(new ValidationError("UserAgent", "required when Proxy is set"), Assert.Single(exception.Errors));
    }
}
