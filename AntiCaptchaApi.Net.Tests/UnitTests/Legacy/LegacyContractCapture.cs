using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using AntiCaptchaApi.Net.Enums;
using AntiCaptchaApi.Net.Internal;
using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Requests;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Legacy;

/// <summary>
/// Temporary characterization capture of the 2.x wire contract and public API (track core_v3_client_20260930, Task 1.2).
/// The output under Snapshots/Legacy2x is checked in; this file is deleted in Task 1.3 once 2.x is excluded from compilation.
/// Canonical sample values are reused by the 3.0 TaskSamples in Phase 2.
/// </summary>
public class LegacyContractCapture
{
    private const string Url = "https://example.com/login";
    private const string Key = "SITE_KEY";
    private const string UserAgent = "Mozilla/5.0 (probe)";
    private const string Cookies = "cookie1=value1; cookie2=value2";
    private const string ImageBody = "iVBORw0KGgoAAAANSUhEUg==";

    private static ProxyConfig Proxy() => new()
    {
        ProxyType = ProxyTypeOption.Socks5,
        ProxyAddress = "203.0.113.10",
        ProxyPort = 1080,
        ProxyLogin = "proxyUser",
        ProxyPassword = "proxyPass",
    };

    private static JObject AntiGateVariables() => new()
    {
        ["login_input_css"] = "#login",
        ["login_input_value"] = "user",
    };

    // Proxyless classes: minimal + full. Proxy classes: proxy (every settable field plus the proxy).
    // AntiGateRequest is one class for both modes (OptionalSameName), so it gets all three cases.
    private static readonly IReadOnlyList<(string Type, string Case, Func<JObject> Build)> Samples =
        new (string, string, Func<JObject>)[]
        {
            ("AmazonWafProxylessRequest", "minimal", () => CreateCaptchaRequestHelper.Build(new AmazonWafProxylessRequest
                { WebsiteUrl = Url, WebsiteKey = Key, Iv = "IV_VALUE", Context = "CONTEXT_VALUE" })),
            ("AmazonWafProxylessRequest", "full", () => CreateCaptchaRequestHelper.Build(new AmazonWafProxylessRequest
                { WebsiteUrl = Url, WebsiteKey = Key, Iv = "IV_VALUE", Context = "CONTEXT_VALUE",
                  CaptchaScript = "https://example.com/captcha.js", ChallengeScript = "https://example.com/challenge.js" })),
            ("AmazonWafRequest", "proxy", () => CreateCaptchaRequestHelper.Build(new AmazonWafRequest
                { WebsiteUrl = Url, WebsiteKey = Key, Iv = "IV_VALUE", Context = "CONTEXT_VALUE",
                  CaptchaScript = "https://example.com/captcha.js", ChallengeScript = "https://example.com/challenge.js",
                  ProxyConfig = Proxy() })),

            ("AntiGateRequest", "minimal", () => CreateCaptchaRequestHelper.Build(new AntiGateRequest
                { WebsiteUrl = Url, TemplateName = "Sign-in and wait for control text", Variables = AntiGateVariables() })),
            ("AntiGateRequest", "full", () => CreateCaptchaRequestHelper.Build(new AntiGateRequest
                { WebsiteUrl = Url, TemplateName = "Sign-in and wait for control text", Variables = AntiGateVariables(),
                  DomainsOfInterest = new List<string> { "example.com", "auth.example.com" } })),
            ("AntiGateRequest", "proxy", () => CreateCaptchaRequestHelper.Build(new AntiGateRequest
                { WebsiteUrl = Url, TemplateName = "Sign-in and wait for control text", Variables = AntiGateVariables(),
                  DomainsOfInterest = new List<string> { "example.com", "auth.example.com" }, ProxyConfig = Proxy() })),

            ("FriendlyCaptchaProxylessRequest", "minimal", () => CreateCaptchaRequestHelper.Build(new FriendlyCaptchaProxylessRequest(Url, Key))),
            ("FriendlyCaptchaProxylessRequest", "full", () => CreateCaptchaRequestHelper.Build(new FriendlyCaptchaProxylessRequest(Url, Key))),
            ("FriendlyCaptchaRequest", "proxy", () => CreateCaptchaRequestHelper.Build(new FriendlyCaptchaRequest(Url, Key, UserAgent, Proxy()))),

            ("FunCaptchaProxylessRequest", "minimal", () => CreateCaptchaRequestHelper.Build(new FunCaptchaProxylessRequest
                { WebsiteUrl = Url, WebsitePublicKey = Key })),
            ("FunCaptchaProxylessRequest", "full", () => CreateCaptchaRequestHelper.Build(new FunCaptchaProxylessRequest
                { WebsiteUrl = Url, WebsitePublicKey = Key, FunCaptchaApiJsSubdomain = "client-api.arkoselabs.com", Data = "{\"blob\":\"BLOB_VALUE\"}" })),
            ("FunCaptchaRequest", "proxy", () => CreateCaptchaRequestHelper.Build(new FunCaptchaRequest
                { WebsiteUrl = Url, WebsitePublicKey = Key, FunCaptchaApiJsSubdomain = "client-api.arkoselabs.com", Data = "{\"blob\":\"BLOB_VALUE\"}",
                  UserAgent = UserAgent, ProxyConfig = Proxy() })),

            ("GeeTestV3ProxylessRequest", "minimal", () => CreateCaptchaRequestHelper.Build(new GeeTestV3ProxylessRequest
                { WebsiteUrl = Url, Gt = "GT_VALUE", Challenge = "CHALLENGE_VALUE" })),
            ("GeeTestV3ProxylessRequest", "full", () => CreateCaptchaRequestHelper.Build(new GeeTestV3ProxylessRequest
                { WebsiteUrl = Url, Gt = "GT_VALUE", Challenge = "CHALLENGE_VALUE", GeetestApiServerSubdomain = "api-na.geetest.com", GeetestGetLib = "GETLIB_VALUE" })),
            ("GeeTestV3Request", "proxy", () => CreateCaptchaRequestHelper.Build(new GeeTestV3Request
                { WebsiteUrl = Url, Gt = "GT_VALUE", Challenge = "CHALLENGE_VALUE", GeetestApiServerSubdomain = "api-na.geetest.com", GeetestGetLib = "GETLIB_VALUE",
                  UserAgent = UserAgent, ProxyConfig = Proxy() })),

            ("GeeTestV4ProxylessRequest", "minimal", () => CreateCaptchaRequestHelper.Build(new GeeTestV4ProxylessRequest
                { WebsiteUrl = Url, Gt = "GT_VALUE" })),
            ("GeeTestV4ProxylessRequest", "full", () => CreateCaptchaRequestHelper.Build(new GeeTestV4ProxylessRequest
                { WebsiteUrl = Url, Gt = "GT_VALUE", Challenge = "CHALLENGE_VALUE", GeetestApiServerSubdomain = "api-na.geetest.com", GeetestGetLib = "GETLIB_VALUE",
                  InitParameters = new Dictionary<string, string> { ["riskType"] = "slide" } })),
            ("GeeTestV4Request", "proxy", () => CreateCaptchaRequestHelper.Build(new GeeTestV4Request
                { WebsiteUrl = Url, Gt = "GT_VALUE", Challenge = "CHALLENGE_VALUE", GeetestApiServerSubdomain = "api-na.geetest.com", GeetestGetLib = "GETLIB_VALUE",
                  InitParameters = new Dictionary<string, string> { ["riskType"] = "slide" }, UserAgent = UserAgent, ProxyConfig = Proxy() })),

            ("ImageToCoordinatesRequest", "minimal", () => CreateCaptchaRequestHelper.Build(new ImageToCoordinatesRequest
                { Body = ImageBody })),
            ("ImageToCoordinatesRequest", "full", () => CreateCaptchaRequestHelper.Build(new ImageToCoordinatesRequest
                { Body = ImageBody, Comment = "select all cars", Mode = ImageToCoordinatesMode.Rectangles, WebsiteUrl = Url, WebsiteURL = Url })),

            ("ImageToTextRequest", "minimal", () => CreateCaptchaRequestHelper.Build(new ImageToTextRequest
                { BodyBase64 = ImageBody })),
            ("ImageToTextRequest", "full", () => CreateCaptchaRequestHelper.Build(new ImageToTextRequest
                { BodyBase64 = ImageBody, Phrase = true, Case = true, Numeric = NumericOption.NumbersOnly, Math = true,
                  MinLength = 4, MaxLength = 8, Comment = "enter the text", WebsiteUrl = Url })),

            ("ProsopoProxylessRequest", "minimal", () => CreateCaptchaRequestHelper.Build(new ProsopoProxylessRequest(Url, Key))),
            ("ProsopoProxylessRequest", "full", () => CreateCaptchaRequestHelper.Build(new ProsopoProxylessRequest(Url, Key))),
            ("ProsopoRequest", "proxy", () => CreateCaptchaRequestHelper.Build(new ProsopoRequest(Url, Key, UserAgent, Proxy()))),

            ("RecaptchaV2EnterpriseProxylessRequest", "minimal", () => CreateCaptchaRequestHelper.Build(new RecaptchaV2EnterpriseProxylessRequest
                { WebsiteUrl = Url, WebsiteKey = Key })),
            ("RecaptchaV2EnterpriseProxylessRequest", "full", () => CreateCaptchaRequestHelper.Build(new RecaptchaV2EnterpriseProxylessRequest
                { WebsiteUrl = Url, WebsiteKey = Key, EnterprisePayload = new Dictionary<string, string> { ["s"] = "ENTERPRISE_S_VALUE" }, ApiDomain = "www.recaptcha.net" })),
            ("RecaptchaV2EnterpriseRequest", "proxy", () => CreateCaptchaRequestHelper.Build(new RecaptchaV2EnterpriseRequest
                { WebsiteUrl = Url, WebsiteKey = Key, EnterprisePayload = new Dictionary<string, string> { ["s"] = "ENTERPRISE_S_VALUE" }, ApiDomain = "www.recaptcha.net",
                  UserAgent = UserAgent, Cookies = Cookies, ProxyConfig = Proxy() })),

            ("RecaptchaV2ProxylessRequest", "minimal", () => CreateCaptchaRequestHelper.Build(new RecaptchaV2ProxylessRequest
                { WebsiteUrl = Url, WebsiteKey = Key })),
            ("RecaptchaV2ProxylessRequest", "full", () => CreateCaptchaRequestHelper.Build(new RecaptchaV2ProxylessRequest
                { WebsiteUrl = Url, WebsiteKey = Key, RecaptchaDataSValue = "DATA_S_VALUE", IsInvisible = true })),
            ("RecaptchaV2Request", "proxy", () => CreateCaptchaRequestHelper.Build(new RecaptchaV2Request
                { WebsiteUrl = Url, WebsiteKey = Key, RecaptchaDataSValue = "DATA_S_VALUE", IsInvisible = true,
                  UserAgent = UserAgent, Cookies = Cookies, ProxyConfig = Proxy() })),

            ("RecaptchaV3EnterpriseRequest", "minimal", () => CreateCaptchaRequestHelper.Build(new RecaptchaV3EnterpriseRequest
                { WebsiteUrl = Url, WebsiteKey = Key })),
            ("RecaptchaV3EnterpriseRequest", "full", () => CreateCaptchaRequestHelper.Build(new RecaptchaV3EnterpriseRequest
                { WebsiteUrl = Url, WebsiteKey = Key, MinScore = 0.7m, PageAction = "login", ApiDomain = "www.recaptcha.net" })),

            ("RecaptchaV3Request", "minimal", () => CreateCaptchaRequestHelper.Build(new RecaptchaV3Request
                { WebsiteUrl = Url, WebsiteKey = Key })),
            ("RecaptchaV3Request", "full", () => CreateCaptchaRequestHelper.Build(new RecaptchaV3Request
                { WebsiteUrl = Url, WebsiteKey = Key, MinScore = 0.7m, PageAction = "login", IsEnterprise = true, ApiDomain = "www.recaptcha.net" })),

            ("TurnstileCaptchaProxylessRequest", "minimal", () => CreateCaptchaRequestHelper.Build(new TurnstileCaptchaProxylessRequest
                { WebsiteUrl = Url, WebsiteKey = Key })),
            ("TurnstileCaptchaProxylessRequest", "full", () => CreateCaptchaRequestHelper.Build(new TurnstileCaptchaProxylessRequest
                { WebsiteUrl = Url, WebsiteKey = Key })),
            ("TurnstileCaptchaRequest", "proxy", () => CreateCaptchaRequestHelper.Build(new TurnstileCaptchaRequest
                { WebsiteUrl = Url, WebsiteKey = Key, UserAgent = UserAgent, ProxyConfig = Proxy() })),
        };

    public static IEnumerable<object[]> SampleCases() => Samples.Select(s => new object[] { s.Type, s.Case });

    [Fact]
    public void Capture_CoversEveryConcreteLegacyRequestType()
    {
        var concrete = typeof(AnticaptchaClient).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsPublic: true })
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICaptchaRequest<>)))
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        var captured = Samples.Select(s => s.Type).Distinct().OrderBy(n => n, StringComparer.Ordinal).ToList();

        Assert.Equal(concrete, captured);
    }

    [Theory]
    [MemberData(nameof(SampleCases))]
    public void Capture_EveryLegacyRequestType_WritesWireSnapshot(string requestType, string @case)
    {
        var json = Samples.Single(s => s.Type == requestType && s.Case == @case).Build();

        var compact = Sort(json).ToString(Formatting.None) + "\n";

        AssertOrCreate(Path.Combine(SnapshotDirectory(), $"{requestType}.{@case}.json"), compact);
    }

    [Fact]
    public void Capture_LegacyPublicApi_WritesList()
    {
        var lines = new List<string>();
        foreach (var type in typeof(AnticaptchaClient).Assembly.GetExportedTypes())
        {
            var typeName = FormatType(type);
            lines.Add(typeName);
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            if (type.IsEnum)
            {
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    lines.Add($"{typeName}.{field.Name} = {Convert.ToInt64(field.GetRawConstantValue())}");
                }
                continue;
            }

            foreach (var ctor in type.GetConstructors(flags))
            {
                lines.Add($"{typeName}..ctor({FormatParameters(ctor.GetParameters())})");
            }
            foreach (var method in type.GetMethods(flags).Where(m => !m.IsSpecialName))
            {
                var generic = method.IsGenericMethodDefinition
                    ? "<" + string.Join(", ", method.GetGenericArguments().Select(a => a.Name)) + ">"
                    : string.Empty;
                lines.Add($"{typeName}.{method.Name}{generic}({FormatParameters(method.GetParameters())})");
            }
            foreach (var property in type.GetProperties(flags))
            {
                var indexer = property.GetIndexParameters();
                lines.Add(indexer.Length == 0
                    ? $"{typeName}.{property.Name}"
                    : $"{typeName}.{property.Name}[{FormatParameters(indexer)}]");
            }
            foreach (var field in type.GetFields(flags))
            {
                lines.Add($"{typeName}.{field.Name}");
            }
            foreach (var @event in type.GetEvents(flags))
            {
                lines.Add($"{typeName}.{@event.Name}");
            }
        }

        var content = string.Join("\n", lines.Distinct().OrderBy(l => l, StringComparer.Ordinal)) + "\n";

        AssertOrCreate(Path.Combine(SnapshotDirectory(), "public-api.txt"), content);
    }

    private static void AssertOrCreate(string path, string expected)
    {
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, expected);
            Assert.Fail($"snapshot created — review: {path}");
        }

        Assert.Equal(expected, File.ReadAllText(path));
    }

    private static string SnapshotDirectory([CallerFilePath] string sourceFile = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFile)!, "..", "..", "Snapshots", "Legacy2x"));

    private static JToken Sort(JToken token) => token switch
    {
        JObject obj => new JObject(obj.Properties()
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .Select(p => new JProperty(p.Name, Sort(p.Value)))),
        JArray array => new JArray(array.Select(Sort)),
        _ => token.DeepClone(),
    };

    private static string FormatParameters(IEnumerable<ParameterInfo> parameters) =>
        string.Join(", ", parameters.Select(p => FormatType(p.ParameterType)));

    private static string FormatType(Type type)
    {
        if (type.IsGenericParameter)
        {
            return type.Name;
        }
        if (type.IsByRef)
        {
            return FormatType(type.GetElementType()!) + "&";
        }
        if (type.IsArray)
        {
            return FormatType(type.GetElementType()!) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
        }
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return FormatType(underlying) + "?";
        }
        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var name = definition.FullName ?? definition.Name;
            name = name.Substring(0, name.IndexOf('`'));
            return name + "<" + string.Join(", ", type.GetGenericArguments().Select(FormatType)) + ">";
        }
        return type.FullName ?? type.Name;
    }
}
