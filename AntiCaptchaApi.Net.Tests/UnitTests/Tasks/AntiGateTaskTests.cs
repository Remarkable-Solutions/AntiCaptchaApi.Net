using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using AntiCaptchaApi.Net.Internal.Json;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Tasks;

public class AntiGateTaskTests
{
    private static AntiGateTask NewTask() =>
        new(TaskSamples.WebsiteUrl, "TEMPLATE", new JsonObject { ["css_selector"] = "#x" });

    [Fact]
    public void TypeName_AlwaysAntiGateTask_WithAndWithoutProxy()
    {
        var task = NewTask();

        Assert.Equal("AntiGateTask", task.ResolveTypeName());
        Assert.Equal("AntiGateTask", (task with { Proxy = TaskSamples.Proxy }).ResolveTypeName());
    }

    [Fact]
    public void Variables_SerializeAsNestedObject()
    {
        var variables = new JsonObject { ["css_selector"] = "#x", ["nested"] = new JsonObject { ["count"] = 2 } };

        var node = TaskSerializer.ToJsonObject(new AntiGateTask(TaskSamples.WebsiteUrl, "TEMPLATE", variables));

        Assert.True(JsonNode.DeepEquals(variables, node["variables"]));
        Assert.Equal("TEMPLATE", (string?)node["templateName"]);
    }

    [Fact]
    public void Validate_EmptyTemplateName_Fails()
    {
        var task = NewTask() with { TemplateName = " " };

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal("TemplateName", Assert.Single(exception.Errors).Field);
    }

    [Fact]
    public void Validate_NullVariables_Fails()
    {
        // null! simulates a caller that ignores the non-nullable annotation (for example, from non-nullable code).
        var task = new AntiGateTask(TaskSamples.WebsiteUrl, "TEMPLATE", null!);

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal(new ValidationError("Variables", "must not be null"), Assert.Single(exception.Errors));
    }

    [Fact]
    public void Validate_ProxyIsOptional()
    {
        NewTask().RunValidation();
        (NewTask() with { Proxy = TaskSamples.Proxy }).RunValidation();
    }

    // anti-captcha documents no proxyType for AntiGateTask (spec Q-1), and the 2.x hotfix request did not send one.
    [Fact]
    public void Proxy_FieldsMatchLegacyAntiGateContract()
    {
        var task = TaskSamples.Get(nameof(AntiGateTask), TaskSamples.ProxyCase);
        var legacy = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Snapshots", "Legacy2x", "AntiGateRequest.proxy.json")))!.AsObject();

        var node = TaskSerializer.ToJsonObject(task);

        Assert.False(node.ContainsKey("proxyType"));
        Assert.Equal(
            legacy.Select(p => p.Key).Where(k => k.StartsWith("proxy", StringComparison.Ordinal)).OrderBy(k => k, StringComparer.Ordinal),
            node.Select(p => p.Key).Where(k => k.StartsWith("proxy", StringComparison.Ordinal)).OrderBy(k => k, StringComparer.Ordinal));
    }
}
