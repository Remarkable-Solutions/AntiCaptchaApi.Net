using System.Linq;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Tasks;

public class AmazonWafTaskTests
{
    // 2.x threw KeyNotFoundException for both names because they were missing from its task-name registry.
    [Fact]
    public void TypeName_AmazonTaskProxylessAndAmazonTask()
    {
        var task = new AmazonWafTask(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey, "IV", "CONTEXT");

        Assert.Equal("AmazonTaskProxyless", task.ResolveTypeName());
        Assert.Equal("AmazonTask", (task with { Proxy = TaskSamples.Proxy }).ResolveTypeName());
    }

    // 2.x validated Iv and Context only for the proxyless request; 3.0 checks them in both modes.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validate_EmptyIvOrContext_Fails(bool withProxy)
    {
        var task = new AmazonWafTask(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey, "", " ")
        {
            Proxy = withProxy ? TaskSamples.Proxy : null,
        };

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal(new[] { "Iv", "Context" }, exception.Errors.Select(e => e.Field));
    }

    [Fact]
    public void Validate_ProxyWithoutUserAgent_Passes()
    {
        var task = new AmazonWafTask(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey, "IV", "CONTEXT") { Proxy = TaskSamples.Proxy };

        task.RunValidation();
    }
}
