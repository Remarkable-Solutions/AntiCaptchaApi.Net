using System;
using System.Collections.Generic;
using System.Linq;
using AntiCaptchaApi.Net.Tasks;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Tasks;

/// <summary>
/// Canonical sample of every concrete task type, per case (<c>minimal</c>, <c>full</c> and, unless the type is
/// <see cref="ProxyMode.None"/>, <c>proxy</c>). The values are the Task 1.2 capture values, so that each 3.0 snapshot
/// can be compared key by key with its <c>Snapshots/Legacy2x</c> counterpart.
/// </summary>
internal static class TaskSamples
{
    public const string Minimal = "minimal";
    public const string Full = "full";
    public const string ProxyCase = "proxy";

    public const string WebsiteUrl = "https://example.com/login";
    public const string WebsiteKey = "SITE_KEY";
    public const string UserAgent = "Mozilla/5.0 (probe)";
    public const string ImageBody = "iVBORw0KGgoAAAANSUhEUg==";

    public static readonly Proxy Proxy = new(ProxyType.Socks5, "203.0.113.10", 1080) { Login = "proxyUser", Password = "proxyPass" };

    public static IReadOnlyDictionary<Type, IReadOnlyList<(string Case, CaptchaTask Task)>> All { get; } = Build();

    public static CaptchaTask Get(string typeName, string caseName) =>
        All.Single(pair => pair.Key.Name == typeName).Value.Single(sample => sample.Case == caseName).Task;

    private static IReadOnlyDictionary<Type, IReadOnlyList<(string Case, CaptchaTask Task)>> Build()
    {
        var samples = new Dictionary<Type, IReadOnlyList<(string Case, CaptchaTask Task)>>();

        var turnstileFull = new TurnstileTask(WebsiteUrl, WebsiteKey)
        {
            Action = "login",
            CData = "CDATA_VALUE",
            ChlPageData = "CHL_PAGE_DATA_VALUE",
            UserAgent = UserAgent,
        };
        Add(samples, new TurnstileTask(WebsiteUrl, WebsiteKey), turnstileFull, turnstileFull with { Proxy = Proxy });

        Add(samples, new ImageToTextTask(ImageBody), new ImageToTextTask(ImageBody)
        {
            Phrase = true,
            Case = true,
            Numeric = NumericOption.NumbersOnly,
            Math = true,
            MinLength = 4,
            MaxLength = 8,
            Comment = "enter the text",
            WebsiteUrl = WebsiteUrl,
        });

        Add(samples, new ImageToCoordinatesTask(ImageBody), new ImageToCoordinatesTask(ImageBody)
        {
            Comment = "select all cars",
            Mode = ImageToCoordinatesMode.Rectangles,
            WebsiteUrl = WebsiteUrl,
        });

        return samples;
    }

    private static void Add<TTask>(Dictionary<Type, IReadOnlyList<(string Case, CaptchaTask Task)>> samples, TTask minimal, TTask full, TTask? proxy = null)
        where TTask : CaptchaTask
    {
        var list = new List<(string Case, CaptchaTask Task)> { (Minimal, minimal), (Full, full) };
        if (proxy is not null)
        {
            list.Add((ProxyCase, proxy));
        }
        samples.Add(typeof(TTask), list);
    }
}
