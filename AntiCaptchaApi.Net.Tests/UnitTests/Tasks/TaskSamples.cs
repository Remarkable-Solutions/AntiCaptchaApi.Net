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

    // Extra case of RecaptchaV3Task: the 2.x RecaptchaV3EnterpriseRequest minimal file (isEnterprise true, default score).
    public const string EnterpriseCase = "enterprise";

    public const string WebsiteUrl = "https://example.com/login";
    public const string WebsiteKey = "SITE_KEY";
    public const string UserAgent = "Mozilla/5.0 (probe)";
    public const string Cookies = "cookie1=value1; cookie2=value2";
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

        var recaptchaV2Full = new RecaptchaV2Task(WebsiteUrl, WebsiteKey)
        {
            RecaptchaDataSValue = "DATA_S_VALUE",
            IsInvisible = true,
            UserAgent = UserAgent,
            Cookies = Cookies,
        };
        Add(samples, new RecaptchaV2Task(WebsiteUrl, WebsiteKey), recaptchaV2Full, recaptchaV2Full with { Proxy = Proxy });

        var recaptchaV2EnterpriseFull = new RecaptchaV2EnterpriseTask(WebsiteUrl, WebsiteKey)
        {
            EnterprisePayload = new Dictionary<string, string> { ["s"] = "ENTERPRISE_S_VALUE" },
            ApiDomain = "www.recaptcha.net",
            UserAgent = UserAgent,
            Cookies = Cookies,
        };
        Add(samples, new RecaptchaV2EnterpriseTask(WebsiteUrl, WebsiteKey), recaptchaV2EnterpriseFull,
            recaptchaV2EnterpriseFull with { Proxy = Proxy });

        var recaptchaV3 = new RecaptchaV3Task(WebsiteUrl, WebsiteKey);
        Add(samples, recaptchaV3, new RecaptchaV3Task(WebsiteUrl, WebsiteKey)
        {
            MinScore = MinScore.Score07,
            PageAction = "login",
            IsEnterprise = true,
            ApiDomain = "www.recaptcha.net",
        });
        AddCase(samples, EnterpriseCase, recaptchaV3 with { IsEnterprise = true });

        var funCaptchaFull = new FunCaptchaTask(WebsiteUrl, WebsiteKey)
        {
            ApiJsSubdomain = "client-api.arkoselabs.com",
            Data = "{\"blob\":\"BLOB_VALUE\"}",
            UserAgent = UserAgent,
        };
        Add(samples, new FunCaptchaTask(WebsiteUrl, WebsiteKey), funCaptchaFull, funCaptchaFull with { Proxy = Proxy });

        var geeTestV3Full = new GeeTestV3Task(WebsiteUrl, "GT_VALUE", "CHALLENGE_VALUE")
        {
            GeetestApiServerSubdomain = "api-na.geetest.com",
            GeetestGetLib = "GETLIB_VALUE",
            UserAgent = UserAgent,
        };
        Add(samples, new GeeTestV3Task(WebsiteUrl, "GT_VALUE", "CHALLENGE_VALUE"), geeTestV3Full, geeTestV3Full with { Proxy = Proxy });

        var geeTestV4Full = new GeeTestV4Task(WebsiteUrl, "GT_VALUE")
        {
            Challenge = "CHALLENGE_VALUE",
            GeetestApiServerSubdomain = "api-na.geetest.com",
            GeetestGetLib = "GETLIB_VALUE",
            InitParameters = new Dictionary<string, string> { ["riskType"] = "slide" },
            UserAgent = UserAgent,
        };
        Add(samples, new GeeTestV4Task(WebsiteUrl, "GT_VALUE"), geeTestV4Full, geeTestV4Full with { Proxy = Proxy });

        var amazonWafFull = new AmazonWafTask(WebsiteUrl, WebsiteKey, "IV_VALUE", "CONTEXT_VALUE")
        {
            CaptchaScript = "https://example.com/captcha.js",
            ChallengeScript = "https://example.com/challenge.js",
        };
        Add(samples, new AmazonWafTask(WebsiteUrl, WebsiteKey, "IV_VALUE", "CONTEXT_VALUE"), amazonWafFull,
            amazonWafFull with { Proxy = Proxy });

        var prosopoFull = new ProsopoTask(WebsiteUrl, WebsiteKey) { UserAgent = UserAgent };
        Add(samples, new ProsopoTask(WebsiteUrl, WebsiteKey), prosopoFull, prosopoFull with { Proxy = Proxy });

        var friendlyCaptchaFull = new FriendlyCaptchaTask(WebsiteUrl, WebsiteKey) { UserAgent = UserAgent };
        Add(samples, new FriendlyCaptchaTask(WebsiteUrl, WebsiteKey), friendlyCaptchaFull, friendlyCaptchaFull with { Proxy = Proxy });

        return samples;
    }

    private static void AddCase(Dictionary<Type, IReadOnlyList<(string Case, CaptchaTask Task)>> samples, string caseName, CaptchaTask task) =>
        samples[task.GetType()] = samples[task.GetType()].Append((caseName, task)).ToList();

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
