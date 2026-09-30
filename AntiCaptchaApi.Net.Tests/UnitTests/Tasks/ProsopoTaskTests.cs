using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Tasks;

public class ProsopoTaskTests
{
    [Fact]
    public void Validate_ProxyWithoutUserAgent_Fails()
    {
        var task = new ProsopoTask(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey) { Proxy = TaskSamples.Proxy };

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal(new ValidationError("UserAgent", "required when Proxy is set"), Assert.Single(exception.Errors));
    }

    [Fact]
    public void TypeName_ProxylessAndProxy()
    {
        var task = new ProsopoTask(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey);

        Assert.Equal("ProsopoTaskProxyless", task.ResolveTypeName());
        Assert.Equal("ProsopoTask", (task with { Proxy = TaskSamples.Proxy }).ResolveTypeName());
    }
}
