using System;
using System.IO;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.LiveTests;

/// <summary>
/// The cheapest live check (one image solve). The 2.x "incorrect file path" test is not ported:
/// <see cref="ImageToTextTask.FromFile"/> now throws <see cref="FileNotFoundException"/>, which the offline tests cover.
/// </summary>
[Trait("Category", "Live")]
[Collection("Live")]
public class ImageToTextLiveTests
{
    [LiveFact]
    public async Task Solve_ExampleImage_ReturnsExpectedText()
    {
        var task = ImageToTextTask.FromFile(Path.Combine(AppContext.BaseDirectory, "Resources", "captchaexample.png"));

        var result = await LiveEnvironment.CreateClient().TrySolveAsync(task);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Equal("W68HP", result.Solution!.Text);
        Assert.False(string.IsNullOrEmpty(result.Solution.Url));
        Assert.NotNull(result.TaskId);
        Assert.NotNull(result.Cost);
        Assert.NotNull(result.WorkerIp);
        Assert.NotNull(result.SolveTime);
    }
}
