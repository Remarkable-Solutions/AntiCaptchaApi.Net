using System;
using System.IO;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.LiveTests;

[Trait("Category", "Live")]
[Collection("Live")]
public class ImageToCoordinatesLiveTests
{
    [LiveTheory]
    [InlineData(ImageToCoordinatesMode.Points, 2)]
    [InlineData(ImageToCoordinatesMode.Rectangles, 4)]
    public async Task Solve_TwoCats_ReturnsTwoShapes(ImageToCoordinatesMode mode, int valuesPerShape)
    {
        var task = ImageToCoordinatesTask.FromFile(Path.Combine(AppContext.BaseDirectory, "Resources", "cats.jpeg")) with
        {
            Comment = "Select all cats",
            Mode = mode,
        };

        var solution = await LiveEnvironment.CreateClient().SolveAsync(task);

        // There are two cats on the image.
        Assert.Equal(2, solution.Coordinates.Count);
        Assert.All(solution.Coordinates, shape => Assert.Equal(valuesPerShape, shape.Count));
    }
}
