using System.Text.Json;
using AntiCaptchaApi.Net.Internal.Json;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Tasks;

public class MinScoreJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new MinScoreJsonConverter() } };

    [Theory]
    [InlineData("0.3", MinScore.Score03)]
    [InlineData("0.7", MinScore.Score07)]
    [InlineData("0.9", MinScore.Score09)]
    public void Read_DocumentedNumber_MapsEnum(string json, MinScore expected)
    {
        Assert.Equal(expected, JsonSerializer.Deserialize<MinScore>(json, Options));
    }

    [Theory]
    [InlineData("0.5")]
    [InlineData("\"0.3\"")]
    public void Read_OtherValue_ThrowsJsonException(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<MinScore>(json, Options));
    }

    [Fact]
    public void Write_UndefinedValue_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Serialize((MinScore)42, Options));
    }
}
