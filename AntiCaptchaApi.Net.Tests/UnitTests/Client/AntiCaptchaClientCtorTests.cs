using System;
using System.Net.Http;
using AntiCaptchaApi.Net.Internal.Http;
using AntiCaptchaApi.Net.Internal.Time;
using AntiCaptchaApi.Net.Tests.UnitTests.Infrastructure;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Client;

public class AntiCaptchaClientCtorTests
{
    private const string Key = "CTOR_SECRET_KEY";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Ctor_NullOrWhitespaceKey_ThrowsArgumentException(string? apiKey)
    {
        var keyOnly = Assert.ThrowsAny<ArgumentException>(() => new AntiCaptchaClient(apiKey!));
        var withClient = Assert.ThrowsAny<ArgumentException>(() => new AntiCaptchaClient(apiKey!, new HttpClient()));

        foreach (var exception in new[] { keyOnly, withClient })
        {
            Assert.Equal("apiKey", exception.ParamName);
            if (!string.IsNullOrEmpty(apiKey))
            {
                Assert.DoesNotContain(apiKey, exception.Message.Replace(Environment.NewLine, " "));
            }
        }
    }

    [Fact]
    public void Ctor_KeyOnly_TwoInstances_ShareHandler()
    {
        var first = new AntiCaptchaClient(Key);
        var second = new AntiCaptchaClient(Key);

        Assert.NotNull(first.SharedHandlerInUse);
        Assert.Same(SharedHandler.Instance, first.SharedHandlerInUse);
        Assert.Same(first.SharedHandlerInUse, second.SharedHandlerInUse);
    }

    [Fact]
    public void SharedHandler_OnNet8_IsSocketsHttpHandlerWithTwoMinuteLifetime()
    {
        var handler = Assert.IsType<SocketsHttpHandler>(SharedHandler.Instance);

        Assert.Equal(TimeSpan.FromMinutes(2), handler.PooledConnectionLifetime);
    }

    [Fact]
    public void Ctor_WithHttpClient_DoesNotMutateTimeoutOrBaseAddress()
    {
        var baseAddress = new Uri("https://my-gateway.example.test/");
        var httpClient = new HttpClient(new FakeHttpMessageHandler()) { Timeout = TimeSpan.FromSeconds(12), BaseAddress = baseAddress };

        var client = new AntiCaptchaClient(Key, httpClient, new AntiCaptchaClientOptions { RequestTimeout = TimeSpan.FromSeconds(5) });

        Assert.Equal(TimeSpan.FromSeconds(12), httpClient.Timeout);
        Assert.Same(baseAddress, httpClient.BaseAddress);
        Assert.Null(client.SharedHandlerInUse);
    }

    [Fact]
    public void Ctor_NullHttpClient_ThrowsArgumentNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new AntiCaptchaClient(Key, null!));

        Assert.Equal("httpClient", exception.ParamName);
    }

    [Fact]
    public void Ctor_NullOptions_UsesDefaults()
    {
        var client = new AntiCaptchaClient(Key, new HttpClient(), null);

        Assert.Equal(new AntiCaptchaClientOptions(), client.Options);
    }

    [Fact]
    public void Ctor_KeyOnly_UsesSystemClock()
    {
        Assert.Same(SystemClock.Instance, new AntiCaptchaClient(Key).Clock);
    }

    [Fact]
    public void Ctor_Internal_UsesInjectedClockAndRandom()
    {
        var clock = new FakeClock();
        Func<double> random = () => 0.5;

        var client = new AntiCaptchaClient(Key, new HttpClient(), null, clock, random);

        Assert.Same(clock, client.Clock);
        Assert.Same(random, client.Random);
    }

    public static TheoryData<AntiCaptchaClientOptions> InvalidOptions => new()
    {
        new AntiCaptchaClientOptions { RequestTimeout = TimeSpan.Zero },
        new AntiCaptchaClientOptions { RequestTimeout = TimeSpan.FromSeconds(-1) },
        new AntiCaptchaClientOptions { BaseAddress = new Uri("/relative", UriKind.Relative) },
        new AntiCaptchaClientOptions { BaseAddress = null! },
    };

    [Theory]
    [MemberData(nameof(InvalidOptions))]
    public void Ctor_InvalidOptions_ThrowsArgumentException(AntiCaptchaClientOptions options)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() => new AntiCaptchaClient(Key, new HttpClient(), options));

        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void ToString_DoesNotContainKey()
    {
        var keyOnly = new AntiCaptchaClient(Key);
        var withClient = new AntiCaptchaClient(Key, new HttpClient(), new AntiCaptchaClientOptions());

        Assert.DoesNotContain(Key, keyOnly.ToString());
        Assert.DoesNotContain(Key, withClient.ToString());
        Assert.Contains("api.anti-captcha.com", keyOnly.ToString());
    }

    [Fact]
    public void Options_Defaults_MatchSpec()
    {
        var options = new AntiCaptchaClientOptions();

        Assert.Equal(new Uri("https://api.anti-captcha.com/"), options.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(30), options.RequestTimeout);
    }

    [Fact]
    public void Client_ImplementsInterface()
    {
        Assert.IsAssignableFrom<IAntiCaptchaClient>(new AntiCaptchaClient(Key));
    }
}
