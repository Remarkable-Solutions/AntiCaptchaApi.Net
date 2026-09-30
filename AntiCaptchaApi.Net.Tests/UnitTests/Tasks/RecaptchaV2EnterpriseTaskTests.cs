using System.Collections.Generic;
using System.Text.Json.Nodes;
using AntiCaptchaApi.Net.Internal.Json;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Tasks;

public class RecaptchaV2EnterpriseTaskTests
{
    [Fact]
    public void EnterprisePayload_SerializesAsObject()
    {
        var task = new RecaptchaV2EnterpriseTask(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey)
        {
            EnterprisePayload = new Dictionary<string, string> { ["s"] = "S_VALUE" },
        };

        var node = TaskSerializer.ToJsonObject(task);

        Assert.True(JsonNode.DeepEquals(new JsonObject { ["s"] = "S_VALUE" }, node["enterprisePayload"]));
    }

    [Fact]
    public void Validate_ApiDomainNotAllowed_Fails()
    {
        var task = new RecaptchaV2EnterpriseTask(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey) { ApiDomain = "evil.example.com" };

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal(new ValidationError("ApiDomain", "must be one of: www.google.com, www.recaptcha.net"), Assert.Single(exception.Errors));
    }

    [Fact]
    public void Validate_ApiDomainRecaptchaNet_Passes()
    {
        var task = new RecaptchaV2EnterpriseTask(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey) { ApiDomain = "www.recaptcha.net" };

        task.RunValidation();
    }

    [Fact]
    public void Validate_ProxyWithoutUserAgent_Fails()
    {
        var task = new RecaptchaV2EnterpriseTask(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey) { Proxy = TaskSamples.Proxy };

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal(new ValidationError("UserAgent", "required when Proxy is set"), Assert.Single(exception.Errors));
    }

    [Fact]
    public void TypeName_ProxylessAndProxy()
    {
        var task = new RecaptchaV2EnterpriseTask(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey);

        Assert.Equal("RecaptchaV2EnterpriseTaskProxyless", task.ResolveTypeName());
        Assert.Equal("RecaptchaV2EnterpriseTask", (task with { Proxy = TaskSamples.Proxy }).ResolveTypeName());
    }
}
