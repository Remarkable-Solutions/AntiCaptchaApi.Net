using System;
using System.IO;
using AntiCaptchaApi.Net.Internal.Json;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Tasks;

public class ImageToCoordinatesTaskTests
{
    [Theory]
    [InlineData(ImageToCoordinatesMode.Points, "points")]
    [InlineData(ImageToCoordinatesMode.Rectangles, "rectangles")]
    public void Mode_SerializesLowercaseString(ImageToCoordinatesMode mode, string expected)
    {
        var node = TaskSerializer.ToJsonObject(new ImageToCoordinatesTask(TaskSamples.ImageBody) { Mode = mode });

        Assert.Equal(expected, (string?)node["mode"]);
    }

    [Fact]
    public void Mode_Default_IsPointsAndAlwaysSerialized()
    {
        var node = TaskSerializer.ToJsonObject(new ImageToCoordinatesTask(TaskSamples.ImageBody));

        Assert.Equal("points", (string?)node["mode"]);
    }

    [Fact]
    public void FromFile_MissingFile_ThrowsFileNotFound()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.png");

        Assert.Throws<FileNotFoundException>(() => ImageToCoordinatesTask.FromFile(path));
    }

    [Fact]
    public void FromBytes_SetsBase64Body()
    {
        Assert.Equal("AQID", ImageToCoordinatesTask.FromBytes(new byte[] { 1, 2, 3 }).Body);
    }

    [Fact]
    public void Validate_EmptyBody_Fails()
    {
        var exception = Assert.Throws<AntiCaptchaValidationException>(new ImageToCoordinatesTask("").RunValidation);

        Assert.Equal("Body", Assert.Single(exception.Errors).Field);
    }

    [Fact]
    public void Validate_InvalidOptionalWebsiteUrl_Fails()
    {
        var task = new ImageToCoordinatesTask(TaskSamples.ImageBody) { WebsiteUrl = "example.com" };

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal("WebsiteUrl", Assert.Single(exception.Errors).Field);
    }

    [Fact]
    public void FirstPollDelay_Is3Seconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(3), new ImageToCoordinatesTask(TaskSamples.ImageBody).ResolvedFirstPollDelay);
    }
}
