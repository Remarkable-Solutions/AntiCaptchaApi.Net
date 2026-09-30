using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Enums;
using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Tests.Helpers;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests;

public class ReportOutcomeTests
{
    [Fact]
    public void IncorrectHCaptcha_IsObsolete()
    {
        var member = typeof(ReportOutcome).GetField("IncorrectHCaptcha"); // literal: nameof would raise CS0618

        var obsolete = member!.GetCustomAttribute<ObsoleteAttribute>();

        Assert.NotNull(obsolete);
        Assert.False(obsolete!.IsError, "IncorrectHCaptcha must stay usable in 2.x (warning, not error).");
        Assert.Contains("3.0", obsolete.Message);
    }

    [Fact]
    public async Task ReportTaskOutcomeAsync_IncorrectHCaptcha_StillCallsApi()
    {
        var handler = new FakeHttpMessageHandler().EnqueueJson("{\"errorId\":0,\"status\":\"success\"}");
        var client = new AnticaptchaClient("test-key", new ClientConfig(), new HttpClient(handler));

#pragma warning disable CS0618 // Existing callers of the obsolete value must keep working in 2.x.
        var response = await client.ReportTaskOutcomeAsync(42, ReportOutcome.IncorrectHCaptcha);
#pragma warning restore CS0618

        Assert.False(response.IsErrorResponse, response.ErrorDescription);
        var uri = Assert.Single(handler.RequestUris);
        Assert.EndsWith("reportIncorrectHCaptcha", uri!.AbsolutePath);
        Assert.Equal(42, JObject.Parse(Assert.Single(handler.RequestBodies))["taskId"]!.Value<int>());
    }
}
