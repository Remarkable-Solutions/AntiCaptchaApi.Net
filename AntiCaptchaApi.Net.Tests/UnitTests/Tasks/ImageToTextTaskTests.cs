using System;
using System.IO;
using System.Linq;
using AntiCaptchaApi.Net.Internal.Json;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Tasks;

public class ImageToTextTaskTests
{
    [Fact]
    public void FromFile_MissingFile_ThrowsFileNotFound()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.png");

        Assert.Throws<FileNotFoundException>(() => ImageToTextTask.FromFile(path));
    }

    [Fact]
    public void FromFile_ExistingFile_SetsBase64Body()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, new byte[] { 1, 2, 3 });

            var task = ImageToTextTask.FromFile(path);

            Assert.Equal("AQID", task.Body);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FromBytes_SetsBase64Body()
    {
        var task = ImageToTextTask.FromBytes(new byte[] { 1, 2, 3 });

        Assert.Equal("AQID", task.Body);
    }

    [Fact]
    public void Validate_EmptyBody_Fails()
    {
        var exception = Assert.Throws<AntiCaptchaValidationException>(new ImageToTextTask(" ").RunValidation);

        Assert.Equal("Body", Assert.Single(exception.Errors).Field);
    }

    [Fact]
    public void Validate_DataUriPrefix_Fails()
    {
        var task = new ImageToTextTask("data:image/png;base64," + TaskSamples.ImageBody);

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal(new ValidationError("Body", "must be raw base64 without a data: URI prefix"), Assert.Single(exception.Errors));
    }

    [Fact]
    public void Validate_MinGreaterThanMax_Fails()
    {
        var task = new ImageToTextTask(TaskSamples.ImageBody) { MinLength = 8, MaxLength = 4 };

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal(new ValidationError("MinLength", "must not be greater than MaxLength"), Assert.Single(exception.Errors));
    }

    [Fact]
    public void Validate_NegativeLength_Fails()
    {
        var task = new ImageToTextTask(TaskSamples.ImageBody) { MinLength = -1, MaxLength = -1 };

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal(new[] { "MinLength", "MaxLength" }, exception.Errors.Select(e => e.Field));
    }

    [Fact]
    public void Validate_Proxy_FailsNotSupported()
    {
        var task = new ImageToTextTask(TaskSamples.ImageBody) { Proxy = TaskSamples.Proxy };

        var exception = Assert.Throws<AntiCaptchaValidationException>(task.RunValidation);

        Assert.Equal(new ValidationError("Proxy", "not supported by ImageToTextTask"), Assert.Single(exception.Errors));
    }

    [Fact]
    public void TypeName_IsImageToTextTask()
    {
        Assert.Equal("ImageToTextTask", new ImageToTextTask(TaskSamples.ImageBody).ResolveTypeName());
    }

    [Fact]
    public void FirstPollDelay_Is3Seconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(3), new ImageToTextTask(TaskSamples.ImageBody).ResolvedFirstPollDelay);
    }

    [Theory]
    [InlineData(NumericOption.NoRequirements, 0)]
    [InlineData(NumericOption.NumbersOnly, 1)]
    [InlineData(NumericOption.AnyLettersExceptNumbers, 2)]
    public void Numeric_SerializesAsNumber(NumericOption option, int expected)
    {
        var node = TaskSerializer.ToJsonObject(new ImageToTextTask(TaskSamples.ImageBody) { Numeric = option });

        Assert.Equal(expected, (int)node["numeric"]!);
    }
}
