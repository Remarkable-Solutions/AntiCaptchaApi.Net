using System;
using System.Net.Http;

namespace AntiCaptchaApi.Net.Internal.Http;

/// <summary>
/// The one handler that every <c>new AntiCaptchaClient(apiKey)</c> shares, so that connections are pooled and sockets
/// are not exhausted. On net8.0 connections are recycled every 2 minutes so that DNS changes are picked up.
/// </summary>
internal static class SharedHandler
{
    public static HttpMessageHandler Instance { get; } = Create();

    private static HttpMessageHandler Create() =>
#if NET8_0_OR_GREATER
        new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(2) };
#else
        new HttpClientHandler();
#endif
}
