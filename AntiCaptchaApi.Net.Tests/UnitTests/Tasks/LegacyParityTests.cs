using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Tasks;

/// <summary>
/// Proves that no 2.x createTask field is lost: every key of every <c>Snapshots/Legacy2x</c> wire file must be present,
/// with the same value, in the mapped 3.0 <c>Snapshots/Tasks</c> file, unless the difference is a documented deviation
/// (spec §9). Keys that exist only in 3.0 are additions and are allowed.
/// </summary>
public class LegacyParityTests
{
    /// <summary>Every legacy wire file → the 3.0 snapshot (<c>&lt;Type&gt;.&lt;case&gt;</c>) that must contain its keys.</summary>
    public static IReadOnlyList<(string LegacyFile, string Target)> Map { get; } = new[]
    {
        ("AmazonWafProxylessRequest.full", "AmazonWafTask.full"),
        ("AmazonWafProxylessRequest.minimal", "AmazonWafTask.minimal"),
        ("AmazonWafRequest.proxy", "AmazonWafTask.proxy"),
        ("AntiGateRequest.full", "AntiGateTask.full"),
        ("AntiGateRequest.minimal", "AntiGateTask.minimal"),
        ("AntiGateRequest.proxy", "AntiGateTask.proxy"),
        ("FriendlyCaptchaProxylessRequest.full", "FriendlyCaptchaTask.full"),
        ("FriendlyCaptchaProxylessRequest.minimal", "FriendlyCaptchaTask.minimal"),
        ("FriendlyCaptchaRequest.proxy", "FriendlyCaptchaTask.proxy"),
        ("FunCaptchaProxylessRequest.full", "FunCaptchaTask.full"),
        ("FunCaptchaProxylessRequest.minimal", "FunCaptchaTask.minimal"),
        ("FunCaptchaRequest.proxy", "FunCaptchaTask.proxy"),
        ("GeeTestV3ProxylessRequest.full", "GeeTestV3Task.full"),
        ("GeeTestV3ProxylessRequest.minimal", "GeeTestV3Task.minimal"),
        ("GeeTestV3Request.proxy", "GeeTestV3Task.proxy"),
        ("GeeTestV4ProxylessRequest.full", "GeeTestV4Task.full"),
        ("GeeTestV4ProxylessRequest.minimal", "GeeTestV4Task.minimal"),
        ("GeeTestV4Request.proxy", "GeeTestV4Task.proxy"),
        ("ImageToCoordinatesRequest.full", "ImageToCoordinatesTask.full"),
        ("ImageToCoordinatesRequest.minimal", "ImageToCoordinatesTask.minimal"),
        ("ImageToTextRequest.full", "ImageToTextTask.full"),
        ("ImageToTextRequest.minimal", "ImageToTextTask.minimal"),
        ("ProsopoProxylessRequest.full", "ProsopoTask.full"),
        ("ProsopoProxylessRequest.minimal", "ProsopoTask.minimal"),
        ("ProsopoRequest.proxy", "ProsopoTask.proxy"),
        ("RecaptchaV2EnterpriseProxylessRequest.full", "RecaptchaV2EnterpriseTask.full"),
        ("RecaptchaV2EnterpriseProxylessRequest.minimal", "RecaptchaV2EnterpriseTask.minimal"),
        ("RecaptchaV2EnterpriseRequest.proxy", "RecaptchaV2EnterpriseTask.proxy"),
        ("RecaptchaV2ProxylessRequest.full", "RecaptchaV2Task.full"),
        ("RecaptchaV2ProxylessRequest.minimal", "RecaptchaV2Task.minimal"),
        ("RecaptchaV2Request.proxy", "RecaptchaV2Task.proxy"),
        // 2.x RecaptchaV3EnterpriseRequest is RecaptchaV3Task { IsEnterprise = true }. Its minimal file (isEnterprise
        // true, minScore 0.3) maps to the extra "enterprise" sample; both 2.x full files equal RecaptchaV3Task.full.
        ("RecaptchaV3EnterpriseRequest.full", "RecaptchaV3Task.full"),
        ("RecaptchaV3EnterpriseRequest.minimal", "RecaptchaV3Task.enterprise"),
        ("RecaptchaV3Request.full", "RecaptchaV3Task.full"),
        ("RecaptchaV3Request.minimal", "RecaptchaV3Task.minimal"),
        ("TurnstileCaptchaProxylessRequest.full", "TurnstileTask.full"),
        ("TurnstileCaptchaProxylessRequest.minimal", "TurnstileTask.minimal"),
        ("TurnstileCaptchaRequest.proxy", "TurnstileTask.proxy"),
    };

    /// <summary>
    /// Legacy (file, key) pairs whose 3.0 value may differ, each tied to its spec §9 item. The Task 1.2 capture
    /// (spec §9, note of 2026-09-30) showed that no §9 item changes a captured 2.x key: item 1 (<c>mode</c>) is already
    /// a lowercase string in 2.x, item 3 (port 65535) is validation only, item 2 only drops the 0.5 score (unused by the
    /// samples) and adds <c>isEnterprise</c> (an added key), items 4 and 5 are not createTask fields, item 6 is
    /// behaviour, and item 7 is additive. The list is therefore empty; every entry must be needed (guarded below).
    /// </summary>
    public static IReadOnlyList<(string LegacyFile, string Key, string SpecItem)> DocumentedDeviations { get; } =
        Array.Empty<(string, string, string)>();

    public static TheoryData<string> MappedLegacyFiles
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var entry in Map)
            {
                data.Add(entry.LegacyFile);
            }
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(MappedLegacyFiles))]
    public void LegacyParity_EveryLegacyKeyPresentWithSameValue_ExceptDocumentedDeviations(string legacyFile)
    {
        var entry = Map.Single(e => e.LegacyFile == legacyFile);
        var legacy = Load("Legacy2x", legacyFile);
        var current = Load("Tasks", entry.Target);

        var problems = new List<string>();
        foreach (var (key, legacyValue) in legacy)
        {
            if (IsDocumentedDeviation(legacyFile, key))
            {
                continue;
            }
            if (!current.TryGetPropertyValue(key, out var currentValue))
            {
                problems.Add($"missing: {key} (2.x {Show(legacyValue)})");
            }
            else if (!JsonNode.DeepEquals(legacyValue, currentValue))
            {
                problems.Add($"changed: {key} 2.x {Show(legacyValue)} but 3.0 {Show(currentValue)}");
            }
        }

        Assert.True(problems.Count == 0, $"{legacyFile} → {entry.Target}:\n  " + string.Join("\n  ", problems));
    }

    [Fact]
    public void LegacyParity_EveryLegacyFile_IsMapped()
    {
        var onDisk = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Snapshots", "Legacy2x"), "*.json")
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();
        var listed = Map.Select(e => e.LegacyFile).OrderBy(n => n, StringComparer.Ordinal).ToList();

        Assert.Equal(38, onDisk.Count);
        Assert.Equal(onDisk, listed);
        var unknownTargets = Map.Where(e => !File.Exists(SnapshotFile("Tasks", e.Target)))
            .Select(e => $"{e.LegacyFile} → {e.Target}");
        Assert.Empty(unknownTargets);
    }

    // Every 3.0 task type replaces at least one 2.x request type (spec §8), so each must be the target of a legacy file.
    [Fact]
    public void LegacyParity_EveryConcreteTaskType_IsTargeted()
    {
        var targeted = Map.Select(e => TypeOf(e.Target)).ToHashSet();

        var untargeted = TaskSnapshotTests.ConcreteTaskTypes.Select(t => t.Name).Where(name => !targeted.Contains(name));

        Assert.Empty(untargeted);
    }

    [Fact]
    public void DocumentedDeviations_EveryEntryIsNeeded()
    {
        var unused = DocumentedDeviations.Where(d =>
        {
            var entry = Map.Single(e => e.LegacyFile == d.LegacyFile);
            var legacy = Load("Legacy2x", d.LegacyFile);
            var current = Load("Tasks", entry.Target);
            return !legacy.ContainsKey(d.Key)
                || (current.TryGetPropertyValue(d.Key, out var value) && JsonNode.DeepEquals(legacy[d.Key], value));
        }).Select(d => $"{d.LegacyFile}:{d.Key} (§9 item {d.SpecItem})");

        Assert.True(!unused.Any(), "Remove allow-list entries that no longer differ: " + string.Join(", ", unused));
    }

    private static bool IsDocumentedDeviation(string legacyFile, string key) =>
        DocumentedDeviations.Any(d => d.LegacyFile == legacyFile && d.Key == key);

    private static string TypeOf(string target) => target.Substring(0, target.IndexOf('.'));

    private static string SnapshotFile(string folder, string name) =>
        Path.Combine(AppContext.BaseDirectory, "Snapshots", folder, name + ".json");

    private static JsonObject Load(string folder, string name) =>
        JsonNode.Parse(File.ReadAllText(SnapshotFile(folder, name)))!.AsObject();

    private static string Show(JsonNode? node) => node?.ToJsonString() ?? "null";
}
