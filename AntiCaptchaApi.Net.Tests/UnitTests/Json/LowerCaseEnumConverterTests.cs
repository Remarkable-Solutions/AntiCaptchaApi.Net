using System.Text.Json;
using AntiCaptchaApi.Net.Internal.Json;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Json;

public class LowerCaseEnumConverterTests
{
    public enum SampleMode
    {
        Points,
        Socks5,
    }

    private static readonly JsonSerializerOptions Options = new() { Converters = { new LowerCaseEnumConverter<SampleMode>() } };

    [Theory]
    [InlineData(SampleMode.Points, "\"points\"")]
    [InlineData(SampleMode.Socks5, "\"socks5\"")]
    public void Serialize_WritesCamelCaseString(SampleMode value, string expected)
    {
        var json = JsonSerializer.Serialize(value, Options);

        Assert.Equal(expected, json);
    }

    [Fact]
    public void Deserialize_Integer_IsRejected()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SampleMode>("1", Options));
    }
}
