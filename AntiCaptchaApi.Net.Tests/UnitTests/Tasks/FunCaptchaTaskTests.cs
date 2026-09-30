using AntiCaptchaApi.Net.Internal.Json;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Tasks;

public class FunCaptchaTaskTests
{
    [Fact]
    public void Serialize_ApiJsSubdomain_UsesFuncaptchaApiJSSubdomainName()
    {
        var task = new FunCaptchaTask(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey) { ApiJsSubdomain = "client-api.arkoselabs.com" };

        var node = TaskSerializer.ToJsonObject(task);

        Assert.Equal("client-api.arkoselabs.com", (string?)node["funcaptchaApiJSSubdomain"]);
        Assert.Equal(TaskSamples.WebsiteKey, (string?)node["websitePublicKey"]);
    }

    [Fact]
    public void Validate_EmptyPublicKey_Fails()
    {
        var task = new FunCaptchaTask(TaskSamples.WebsiteUrl, "");

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal("WebsitePublicKey", Assert.Single(exception.Errors).Field);
    }

    [Fact]
    public void Validate_ProxyWithoutUserAgent_Fails()
    {
        var task = new FunCaptchaTask(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey) { Proxy = TaskSamples.Proxy };

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal(new ValidationError("UserAgent", "required when Proxy is set"), Assert.Single(exception.Errors));
    }

    [Fact]
    public void TypeName_ProxylessAndProxy()
    {
        var task = new FunCaptchaTask(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey);

        Assert.Equal("FunCaptchaTaskProxyless", task.ResolveTypeName());
        Assert.Equal("FunCaptchaTask", (task with { Proxy = TaskSamples.Proxy }).ResolveTypeName());
    }
}
