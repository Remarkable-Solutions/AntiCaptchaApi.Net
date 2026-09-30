using System;
using System.Text.Json;
using AntiCaptchaApi.Net.Internal.Json;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Tasks;

public class RecaptchaV3TaskTests
{
    [Theory]
    [InlineData(MinScore.Score03, "0.3")]
    [InlineData(MinScore.Score07, "0.7")]
    [InlineData(MinScore.Score09, "0.9")]
    public void MinScore_SerializesAsNumber(MinScore score, string expected)
    {
        var node = TaskSerializer.ToJsonObject(new RecaptchaV3Task(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey) { MinScore = score });

        Assert.Equal(JsonValueKind.Number, node["minScore"]!.GetValueKind());
        Assert.Equal(expected, node["minScore"]!.ToJsonString());
    }

    [Fact]
    public void MinScore_Default_Is03()
    {
        var task = new RecaptchaV3Task(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey);

        Assert.Equal(MinScore.Score03, task.MinScore);
        Assert.Equal("0.3", TaskSerializer.ToJsonObject(task)["minScore"]!.ToJsonString());
    }

    [Fact]
    public void IsEnterprise_Default_FalseAndAlwaysSerialized()
    {
        var task = new RecaptchaV3Task(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey);

        var node = TaskSerializer.ToJsonObject(task);

        Assert.False(task.IsEnterprise);
        Assert.False((bool)node["isEnterprise"]!);
    }

    [Fact]
    public void TypeName_IsRecaptchaV3TaskProxyless()
    {
        Assert.Equal("RecaptchaV3TaskProxyless", new RecaptchaV3Task(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey).ResolveTypeName());
    }

    [Fact]
    public void Validate_Proxy_FailsNotSupported()
    {
        var task = new RecaptchaV3Task(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey) { Proxy = TaskSamples.Proxy };

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal(new ValidationError("Proxy", "not supported by RecaptchaV3Task"), Assert.Single(exception.Errors));
    }

    [Fact]
    public void Validate_UndefinedMinScore_Fails()
    {
        var task = new RecaptchaV3Task(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey) { MinScore = (MinScore)42 };

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal("MinScore", Assert.Single(exception.Errors).Field);
    }

    [Fact]
    public void Validate_ApiDomainNotAllowed_Fails()
    {
        var task = new RecaptchaV3Task(TaskSamples.WebsiteUrl, TaskSamples.WebsiteKey) { ApiDomain = "google.com" };

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal("ApiDomain", Assert.Single(exception.Errors).Field);
    }
}
