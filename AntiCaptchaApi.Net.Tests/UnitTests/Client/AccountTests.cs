using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Client;

public class AccountTests
{
    private readonly ClientTestContext _context = new();

    private static void AssertBody(string expected, JsonNode? actual) =>
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(expected), actual), actual?.ToJsonString());

    [Theory]
    [InlineData("12.3456")]
    [InlineData("\"12.3456\"")]
    public async Task GetBalance_SendsClientKey_ReturnsDecimal(string balance)
    {
        _context.Handler.RespondJson("getBalance", $"{{\"errorId\":0,\"balance\":{balance},\"captchaCredits\":85000}}");

        var result = await _context.Client.GetBalanceAsync();

        Assert.Equal(12.3456m, result);
        AssertBody("{\"clientKey\":\"TEST_KEY\"}", Assert.Single(_context.Handler.RequestsFor("getBalance")).Json);
    }

    [Fact]
    public async Task GetBalance_ApiError_Throws()
    {
        _context.Handler.RespondJson("getBalance", ClientTestContext.ApiError(1, "ERROR_KEY_DOES_NOT_EXIST", "Account authorization key not found in the system"));

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.GetBalanceAsync());

        Assert.Equal(AntiCaptchaErrorCode.KeyDoesNotExist, exception.Error.Code);
        Assert.Null(exception.TaskId);
    }

    [Fact]
    public async Task GetBalance_MissingBalance_ThrowsInvalidResponse()
    {
        _context.Handler.RespondJson("getBalance", "{\"errorId\":0}");

        var exception = await Assert.ThrowsAsync<AntiCaptchaException>(() => _context.Client.GetBalanceAsync());

        Assert.Equal(AntiCaptchaErrorCode.InvalidResponse, exception.Error.Code);
    }
}
