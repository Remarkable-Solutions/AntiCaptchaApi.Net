using System;
using System.Net.Http;
using System.Threading.Tasks;
using AntiCaptchaApi.Net.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAnticaptcha_ConfiguresNamedClientTimeout()
    {
        var services = new ServiceCollection();
        services.AddAnticaptcha("test-key", c => c.MaxHttpRequestTimeMs = 12345);
        using var provider = services.BuildServiceProvider();

        var httpClient = provider.GetRequiredService<IHttpClientFactory>()
            .CreateClient(AnticaptchaServiceCollectionExtensions.HttpClientName);

        Assert.Equal(TimeSpan.FromMilliseconds(12345), httpClient.Timeout);
    }

    [Fact]
    public async Task AddAnticaptcha_ClientUsesNamedHttpClient()
    {
        var namedHandler = new FakeHttpMessageHandler().EnqueueJson("{\"errorId\":0,\"balance\":1.5}");
        // Guard: the unnamed default client must not be used (and must never reach the network in a red run).
        var unnamedHandler = new FakeHttpMessageHandler();
        var services = new ServiceCollection();
        services.AddAnticaptcha("test-key");
        services.AddHttpClient(AnticaptchaServiceCollectionExtensions.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => namedHandler);
        services.AddHttpClient(Options.DefaultName)
            .ConfigurePrimaryHttpMessageHandler(() => unnamedHandler);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IAnticaptchaClient>();

        var balance = await client.GetBalanceAsync();

        Assert.Equal(1, namedHandler.RequestCount);
        Assert.Equal(0, unnamedHandler.RequestCount);
        Assert.False(balance.IsErrorResponse, balance.ErrorDescription);
        Assert.Equal(1.5, (double?)balance.Balance);
    }
}
