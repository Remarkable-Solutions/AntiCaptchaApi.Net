using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Client;

public class ReportTests
{
    private const int TaskId = 12345;
    private const string Success = "{\"errorId\":0,\"status\":\"success\"}";

    private readonly ClientTestContext _context = new();

    [Theory]
    [InlineData(Report.IncorrectImageCaptcha, "reportIncorrectImageCaptcha")]
    [InlineData(Report.IncorrectRecaptcha, "reportIncorrectRecaptcha")]
    [InlineData(Report.CorrectRecaptcha, "reportCorrectRecaptcha")]
    public async Task Report_MapsToEndpoint(Report report, string endpoint)
    {
        _context.Handler.RespondJson(endpoint, Success);

        await _context.Client.ReportAsync(TaskId, report);

        Assert.Equal(endpoint, Assert.Single(_context.Handler.Requests).Method);
    }

    [Fact]
    public async Task Report_SendsClientKeyAndTaskId()
    {
        _context.Handler.RespondJson("reportIncorrectRecaptcha", Success);

        await _context.Client.ReportAsync(TaskId, Report.IncorrectRecaptcha);

        var body = Assert.Single(_context.Handler.Requests).Json;
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("{\"clientKey\":\"TEST_KEY\",\"taskId\":12345}"), body), body?.ToJsonString());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public async Task Report_UndefinedValue_ThrowsArgumentOutOfRange_NoRequest(int value)
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _context.Client.ReportAsync(TaskId, (Report)value));

        Assert.Equal("report", exception.ParamName);
        Assert.Empty(_context.Handler.Requests);
    }

    [Fact]
    public async Task Report_ApiError_Throws()
    {
        _context.Handler.RespondJson("reportIncorrectImageCaptcha", ClientTestContext.ApiError(16, "ERROR_NO_SUCH_CAPCHA_ID", "Task you are requesting does not exist"));

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.ReportAsync(TaskId, Report.IncorrectImageCaptcha));

        Assert.Equal(AntiCaptchaErrorCode.NoSuchCaptchaId, exception.Error.Code);
        Assert.Equal(TaskId, exception.TaskId);
    }

    [Fact]
    public void Report_HasExactlyTheThreeDocumentedValues()
    {
        // Spec R-10: hCaptcha reporting (2.x ReportOutcome.IncorrectHCaptcha) is dropped; the numbers match 2.x.
        Assert.Equal(new[] { Report.IncorrectImageCaptcha, Report.IncorrectRecaptcha, Report.CorrectRecaptcha }, Enum.GetValues<Report>());
        Assert.Equal(new[] { 0, 1, 2 }, Array.ConvertAll(Enum.GetValues<Report>(), r => (int)r));
    }
}
