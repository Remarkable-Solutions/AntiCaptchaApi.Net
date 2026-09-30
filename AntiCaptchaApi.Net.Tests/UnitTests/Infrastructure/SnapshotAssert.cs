using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Infrastructure;

/// <summary>
/// Semantic JSON snapshot assertions. Key order is ignored. A mismatch lists every missing, extra and changed key
/// (JSONPath-like). With <c>UPDATE_SNAPSHOTS=1</c>, the snapshot in the <em>source</em> tree (located through
/// <see cref="CallerFilePathAttribute"/>) is rewritten, and the test still fails so that the change is reviewed.
/// </summary>
internal static class SnapshotAssert
{
    private const string UpdateVariable = "UPDATE_SNAPSHOTS";

    /// <summary>Compares <paramref name="actualJson"/> with the snapshot file at <paramref name="snapshotPath"/> (relative to the test project).</summary>
    public static void Match(string actualJson, string snapshotPath, [CallerFilePath] string callerFilePath = "")
    {
        var outputFile = Path.Combine(AppContext.BaseDirectory, snapshotPath);
        var expectedJson = File.Exists(outputFile) ? File.ReadAllText(outputFile) : null;
        var differences = expectedJson is null ? null : Diff(expectedJson, actualJson);

        if (differences is { Count: 0 })
        {
            return;
        }

        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            var sourceFile = Path.Combine(FindProjectDirectory(callerFilePath), snapshotPath);
            Directory.CreateDirectory(Path.GetDirectoryName(sourceFile)!);
            File.WriteAllText(sourceFile, Normalize(actualJson) + "\n");
            Assert.Fail($"snapshot updated — review: {snapshotPath}");
        }

        if (differences is null)
        {
            Assert.Fail($"snapshot missing: {snapshotPath}. Run with {UpdateVariable}=1 to create it.");
        }

        Assert.Fail(Format(snapshotPath, differences));
    }

    /// <summary>Compares two JSON documents semantically. It never writes files.</summary>
    public static void Equal(string expectedJson, string actualJson)
    {
        var differences = Diff(expectedJson, actualJson);
        if (differences.Count > 0)
        {
            Assert.Fail(Format("(inline expected JSON)", differences));
        }
    }

    /// <summary>Compact JSON with keys sorted ordinally at every level: the canonical on-disk snapshot form.</summary>
    public static string Normalize(string json) => Sort(JsonNode.Parse(json))?.ToJsonString() ?? "null";

    private static List<string> Diff(string expectedJson, string actualJson)
    {
        var lines = new List<string>();
        Diff(JsonNode.Parse(expectedJson), JsonNode.Parse(actualJson), "$", lines);
        return lines;
    }

    private static void Diff(JsonNode? expected, JsonNode? actual, string path, List<string> lines)
    {
        if (expected is JsonObject expectedObject && actual is JsonObject actualObject)
        {
            foreach (var (key, value) in expectedObject)
            {
                if (actualObject.TryGetPropertyValue(key, out var actualValue))
                {
                    Diff(value, actualValue, $"{path}.{key}", lines);
                }
                else
                {
                    lines.Add($"missing: {path}.{key}");
                }
            }
            foreach (var (key, _) in actualObject)
            {
                if (!expectedObject.ContainsKey(key))
                {
                    lines.Add($"extra: {path}.{key}");
                }
            }
            return;
        }

        if (!JsonNode.DeepEquals(expected, actual))
        {
            lines.Add($"changed: {path} expected {Show(expected)} but was {Show(actual)}");
        }
    }

    private static string Format(string snapshotPath, IEnumerable<string> differences) =>
        $"Snapshot mismatch: {snapshotPath}\n  " +
        string.Join("\n  ", differences.OrderBy(Rank).ThenBy(l => l, StringComparer.Ordinal));

    private static int Rank(string line) =>
        line.StartsWith("missing", StringComparison.Ordinal) ? 0 : line.StartsWith("extra", StringComparison.Ordinal) ? 1 : 2;

    private static string Show(JsonNode? node) => node?.ToJsonString() ?? "null";

    private static JsonNode? Sort(JsonNode? node) => node switch
    {
        JsonObject obj => new JsonObject(obj
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => KeyValuePair.Create(p.Key, Sort(p.Value)))),
        JsonArray array => new JsonArray(array.Select(Sort).ToArray()),
        _ => node?.DeepClone(),
    };

    private static string FindProjectDirectory(string callerFilePath)
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(callerFilePath)!);
        while (directory is not null && directory.GetFiles("*.csproj").Length == 0)
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new InvalidOperationException($"No test project found above '{callerFilePath}'.");
    }
}
