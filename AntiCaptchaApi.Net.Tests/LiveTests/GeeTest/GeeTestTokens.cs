using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.LiveTests.GeeTest;

/// <summary>Fetches a fresh <c>gt</c>/<c>challenge</c> pair from the GeeTest demo (as the 2.x <c>GeeRequestTestsBase</c> did).</summary>
internal static class GeeTestTokens
{
    private const string InitCaptchaUrl = "https://auth.geetest.com/api/init_captcha?time=1561554686474";

    internal static async Task<(string Gt, string Challenge)> FetchAsync()
    {
        using var http = new HttpClient();
        var json = await http.GetStringAsync(InitCaptchaUrl);
        var model = JsonSerializer.Deserialize<GeeTestModel>(json);

        Assert.False(string.IsNullOrEmpty(model?.Data?.Gt), "GeeTest demo returned no gt");
        Assert.False(string.IsNullOrEmpty(model!.Data!.Challenge), "GeeTest demo returned no challenge");
        return (model.Data.Gt!, model.Data.Challenge!);
    }
}
