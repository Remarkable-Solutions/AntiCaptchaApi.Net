using System;
using System.Net.Http;
using AntiCaptchaApi.Net.Tests.UnitTests.Infrastructure;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Client;

/// <summary>An <see cref="AntiCaptchaClient"/> wired to a <see cref="FakeHttpMessageHandler"/> and a <see cref="FakeClock"/>.</summary>
internal sealed class ClientTestContext
{
    public const string Key = "TEST_KEY";

    public ClientTestContext(string key = Key, AntiCaptchaClientOptions? options = null)
    {
        Client = new AntiCaptchaClient(key, new HttpClient(Handler), options, Clock, () => 0.5);
    }

    public FakeHttpMessageHandler Handler { get; } = new();

    public FakeClock Clock { get; } = new();

    public AntiCaptchaClient Client { get; }

    public static string ApiError(int errorId, string code, string description) =>
        $"{{\"errorId\":{errorId},\"errorCode\":\"{code}\",\"errorDescription\":\"{description}\"}}";
}
