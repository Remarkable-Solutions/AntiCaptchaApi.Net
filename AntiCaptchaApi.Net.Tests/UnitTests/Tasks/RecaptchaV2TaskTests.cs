using System.Linq;
using AntiCaptchaApi.Net.Internal.Json;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Tasks;

public class RecaptchaV2TaskTests
{
    [Fact]
    public void TypeName_ProxylessAndProxy()
    {
        var task = new RecaptchaV2Task(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey);

        Assert.Equal("RecaptchaV2TaskProxyless", task.ResolveTypeName());
        Assert.Equal("RecaptchaV2Task", (task with { Proxy = TaskSamples.Proxy }).ResolveTypeName());
    }

    [Fact]
    public void Validate_ProxyWithoutUserAgent_Fails()
    {
        var task = new RecaptchaV2Task(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey) { Proxy = TaskSamples.Proxy };

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal(new ValidationError("UserAgent", "required when Proxy is set"), Assert.Single(exception.Errors));
    }

    [Fact]
    public void Validate_InvalidUrlAndEmptyKey_ReportsBoth()
    {
        var exception = Assert.Throws<AntiCaptchaValidationException>(new RecaptchaV2Task("/login", " ").RunValidation);

        Assert.Equal(new[] { "WebsiteUrl", "WebsiteKey" }, exception.Errors.Select(e => e.Field));
    }

    [Fact]
    public void Serialize_CookiesAndIsInvisible()
    {
        var task = new RecaptchaV2Task(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey) { Cookies = "a=b", IsInvisible = true };

        var node = TaskSerializer.ToJsonObject(task);

        Assert.Equal("a=b", (string?)node["cookies"]);
        Assert.True((bool)node["isInvisible"]!);
    }
}
