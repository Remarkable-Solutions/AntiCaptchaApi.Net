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
    public enum ParityStatus
    {
        Mapped,

        // The 3.0 task type does not exist yet. Parity is skipped until its catalog task (2.6–2.12) switches the entry
        // to Mapped. Task 2.12 removes this status, so that from then on every legacy file must be mapped.
        Pending,
    }

    /// <summary>Every legacy wire file → the 3.0 snapshot (<c>&lt;Type&gt;.&lt;case&gt;</c>) that must contain its keys.</summary>
    public static IReadOnlyList<(string LegacyFile, string Target, ParityStatus Status)> Map { get; } = new[]
    {
        ("AmazonWafProxylessRequest.full", "AmazonWafTask.full", ParityStatus.Pending),
        ("AmazonWafProxylessRequest.minimal", "AmazonWafTask.minimal", ParityStatus.Pending),
        ("AmazonWafRequest.proxy", "AmazonWafTask.proxy", ParityStatus.Pending),
        ("AntiGateRequest.full", "AntiGateTask.full", ParityStatus.Pending),
        ("AntiGateRequest.minimal", "AntiGateTask.minimal", ParityStatus.Pending),
        ("AntiGateRequest.proxy", "AntiGateTask.proxy", ParityStatus.Pending),
        ("FriendlyCaptchaProxylessRequest.full", "FriendlyCaptchaTask.full", ParityStatus.Pending),
        ("FriendlyCaptchaProxylessRequest.minimal", "FriendlyCaptchaTask.minimal", ParityStatus.Pending),
        ("FriendlyCaptchaRequest.proxy", "FriendlyCaptchaTask.proxy", ParityStatus.Pending),
        ("FunCaptchaProxylessRequest.full", "FunCaptchaTask.full", ParityStatus.Pending),
        ("FunCaptchaProxylessRequest.minimal", "FunCaptchaTask.minimal", ParityStatus.Pending),
        ("FunCaptchaRequest.proxy", "FunCaptchaTask.proxy", ParityStatus.Pending),
        ("GeeTestV3ProxylessRequest.full", "GeeTestV3Task.full", ParityStatus.Pending),
        ("GeeTestV3ProxylessRequest.minimal", "GeeTestV3Task.minimal", ParityStatus.Pending),
        ("GeeTestV3Request.proxy", "GeeTestV3Task.proxy", ParityStatus.Pending),
        ("GeeTestV4ProxylessRequest.full", "GeeTestV4Task.full", ParityStatus.Pending),
        ("GeeTestV4ProxylessRequest.minimal", "GeeTestV4Task.minimal", ParityStatus.Pending),
        ("GeeTestV4Request.proxy", "GeeTestV4Task.proxy", ParityStatus.Pending),
        ("ImageToCoordinatesRequest.full", "ImageToCoordinatesTask.full", ParityStatus.Pending),
        ("ImageToCoordinatesRequest.minimal", "ImageToCoordinatesTask.minimal", ParityStatus.Pending),
        ("ImageToTextRequest.full", "ImageToTextTask.full", ParityStatus.Pending),
        ("ImageToTextRequest.minimal", "ImageToTextTask.minimal", ParityStatus.Pending),
        ("ProsopoProxylessRequest.full", "ProsopoTask.full", ParityStatus.Pending),
        ("ProsopoProxylessRequest.minimal", "ProsopoTask.minimal", ParityStatus.Pending),
        ("ProsopoRequest.proxy", "ProsopoTask.proxy", ParityStatus.Pending),
        ("RecaptchaV2EnterpriseProxylessRequest.full", "RecaptchaV2EnterpriseTask.full", ParityStatus.Pending),
        ("RecaptchaV2EnterpriseProxylessRequest.minimal", "RecaptchaV2EnterpriseTask.minimal", ParityStatus.Pending),
        ("RecaptchaV2EnterpriseRequest.proxy", "RecaptchaV2EnterpriseTask.proxy", ParityStatus.Pending),
        ("RecaptchaV2ProxylessRequest.full", "RecaptchaV2Task.full", ParityStatus.Pending),
        ("RecaptchaV2ProxylessRequest.minimal", "RecaptchaV2Task.minimal", ParityStatus.Pending),
        ("RecaptchaV2Request.proxy", "RecaptchaV2Task.proxy", ParityStatus.Pending),
        // 2.x RecaptchaV3EnterpriseRequest is RecaptchaV3Task { IsEnterprise = true }. Its minimal case (isEnterprise
        // true, minScore 0.3) needs its own sample; Task 2.8 decides the final target.
        ("RecaptchaV3EnterpriseRequest.full", "RecaptchaV3Task.full", ParityStatus.Pending),
        ("RecaptchaV3EnterpriseRequest.minimal", "RecaptchaV3Task.minimal", ParityStatus.Pending),
        ("RecaptchaV3Request.full", "RecaptchaV3Task.full", ParityStatus.Pending),
        ("RecaptchaV3Request.minimal", "RecaptchaV3Task.minimal", ParityStatus.Pending),
        ("TurnstileCaptchaProxylessRequest.full", "TurnstileTask.full", ParityStatus.Mapped),
        ("TurnstileCaptchaProxylessRequest.minimal", "TurnstileTask.minimal", ParityStatus.Mapped),
        ("TurnstileCaptchaRequest.proxy", "TurnstileTask.proxy", ParityStatus.Mapped),
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
            foreach (var entry in Map.Where(e => e.Status == ParityStatus.Mapped))
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
        var unknownTargets = Map.Where(e => e.Status == ParityStatus.Mapped && !File.Exists(SnapshotFile("Tasks", e.Target)))
            .Select(e => $"{e.LegacyFile} → {e.Target}");
        Assert.Empty(unknownTargets);
    }

    [Fact]
    public void LegacyParity_PendingEntries_HaveNoImplementedTaskType()
    {
        var implemented = TaskSnapshotTests.ConcreteTaskTypes.Select(t => t.Name).ToHashSet();

        var stale = Map.Where(e => e.Status == ParityStatus.Pending && implemented.Contains(TypeOf(e.Target)))
            .Select(e => $"{e.LegacyFile} → {e.Target}");

        Assert.True(!stale.Any(), "Switch these entries to Mapped: " + string.Join(", ", stale));
    }

    [Fact]
    public void DocumentedDeviations_EveryEntryIsNeeded()
    {
        var unused = DocumentedDeviations.Where(d =>
        {
            var entry = Map.Single(e => e.LegacyFile == d.LegacyFile);
            if (entry.Status != ParityStatus.Mapped)
            {
                return false;
            }
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
