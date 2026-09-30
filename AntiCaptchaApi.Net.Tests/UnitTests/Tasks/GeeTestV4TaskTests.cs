using System.Collections.Generic;
using System.Text.Json.Nodes;
using AntiCaptchaApi.Net.Internal.Json;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Tasks;

public class GeeTestV4TaskTests
{
    [Fact]
    public void Serialize_Version4_WithInitParametersObject()
    {
        var task = new GeeTestV4Task(TaskSamples.WebsiteUrl, "GT")
        {
            InitParameters = new Dictionary<string, string> { ["riskType"] = "slide" },
        };

        var node = TaskSerializer.ToJsonObject(task);

        Assert.Equal(4, (int)node["version"]!);
        Assert.Equal("GeeTestTaskProxyless", (string?)node["type"]);
        Assert.True(JsonNode.DeepEquals(new JsonObject { ["riskType"] = "slide" }, node["initParameters"]));
    }

    [Fact]
    public void Validate_ChallengeOptional_Passes()
    {
        var task = new GeeTestV4Task(TaskSamples.WebsiteUrl, "GT");

        task.RunValidation();

        Assert.Null(task.Challenge);
    }

    [Fact]
    public void Validate_EmptyGt_Fails()
    {
        var exception = Assert.Throws<AntiCaptchaValidationException>(new GeeTestV4Task(TaskSamples.WebsiteUrl, "").RunValidation);

        Assert.Equal("Gt", Assert.Single(exception.Errors).Field);
    }

    [Fact]
    public void Validate_ProxyWithoutUserAgent_Fails()
    {
        var task = new GeeTestV4Task(TaskSamples.WebsiteUrl, "GT") { Proxy = TaskSamples.Proxy };

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal(new ValidationError("UserAgent", "required when Proxy is set"), Assert.Single(exception.Errors));
    }
}
