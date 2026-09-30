using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AntiCaptchaApi.Net.Internal.Json;
using AntiCaptchaApi.Net.Tasks;
using AntiCaptchaApi.Net.Tests.UnitTests.Infrastructure;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Tasks;

/// <summary>
/// Discovers every concrete task type in the core assembly and asserts that it has samples, is registered in the JSON
/// context (with its solution), validates, and serializes exactly to its checked-in <c>Snapshots/Tasks</c> file.
/// </summary>
public class TaskSnapshotTests
{
    public static IEnumerable<Type> ConcreteTaskTypes { get; } = typeof(CaptchaTask).Assembly.GetTypes()
        .Where(t => typeof(CaptchaTask).IsAssignableFrom(t) && !t.IsAbstract)
        .OrderBy(t => t.Name, StringComparer.Ordinal)
        .ToArray();

    public static TheoryData<string, string> AllSamples
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var (type, samples) in TaskSamples.All.OrderBy(p => p.Key.Name, StringComparer.Ordinal))
            {
                foreach (var (caseName, _) in samples)
                {
                    data.Add(type.Name, caseName);
                }
            }
            return data;
        }
    }

    internal static string SnapshotPath(string typeName, string caseName) => Path.Combine("Snapshots", "Tasks", $"{typeName}.{caseName}.json");

    [Fact]
    public void ConcreteTaskTypes_AreDiscovered()
    {
        Assert.Contains(typeof(TurnstileTask), ConcreteTaskTypes);
        Assert.DoesNotContain(typeof(OptionalTestTask), ConcreteTaskTypes);
    }

    [Fact]
    public void EveryConcreteTaskType_HasMinimalFullAndProxySamples()
    {
        var problems = new List<string>();
        foreach (var type in ConcreteTaskTypes)
        {
            if (!TaskSamples.All.TryGetValue(type, out var samples) || samples.Count == 0)
            {
                problems.Add($"{type.Name}: no samples in TaskSamples");
                continue;
            }

            var cases = samples.Select(s => s.Case).ToList();
            var required = ProxyModeOf(samples[0].Task) == ProxyMode.None
                ? new[] { TaskSamples.Minimal, TaskSamples.Full }
                : new[] { TaskSamples.Minimal, TaskSamples.Full, TaskSamples.ProxyCase };
            problems.AddRange(required.Except(cases).Select(c => $"{type.Name}: missing '{c}' sample"));
            problems.AddRange(samples.Where(s => (s.Task.Proxy is not null) != (s.Case == TaskSamples.ProxyCase))
                .Select(s => $"{type.Name}.{s.Case}: only the '{TaskSamples.ProxyCase}' sample sets a proxy"));
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void EveryConcreteTaskType_AndItsSolution_AreRegisteredInJsonContext()
    {
        var missing = new List<string>();
        foreach (var type in ConcreteTaskTypes)
        {
            foreach (var registered in new[] { type, SolutionTypeOf(type) })
            {
                if (AntiCaptchaJsonContext.Default.GetTypeInfo(registered) is null)
                {
                    missing.Add($"[JsonSerializable(typeof({registered.Name}))]");
                }
            }
        }

        Assert.True(missing.Count == 0, "Add to AntiCaptchaJsonContext: " + string.Join(", ", missing));
    }

    [Fact]
    public void EverySample_HasSnapshotFile()
    {
        var missing = TaskSamples.All
            .SelectMany(p => p.Value.Select(s => SnapshotPath(p.Key.Name, s.Case)))
            .Where(path => !File.Exists(Path.Combine(AppContext.BaseDirectory, path)))
            .ToList();

        Assert.True(missing.Count == 0, "Missing snapshot files: " + string.Join(", ", missing));
    }

    [Theory]
    [MemberData(nameof(AllSamples))]
    public void Snapshot_MatchesExpectedJson(string typeName, string caseName)
    {
        var task = TaskSamples.Get(typeName, caseName);

        var json = TaskSerializer.ToJsonObject(task).ToJsonString();

        SnapshotAssert.Match(json, SnapshotPath(typeName, caseName));
    }

    [Theory]
    [MemberData(nameof(AllSamples))]
    public void EverySample_PassesValidation(string typeName, string caseName)
    {
        var task = TaskSamples.Get(typeName, caseName);

        task.RunValidation();
    }

    private static ProxyMode ProxyModeOf(CaptchaTask task) =>
        (ProxyMode)task.GetType().GetProperty("ProxyMode", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(task)!;

    private static Type SolutionTypeOf(Type taskType)
    {
        for (var type = taskType.BaseType; type is not null; type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(CaptchaTask<>))
            {
                return type.GetGenericArguments()[0];
            }
        }
        throw new InvalidOperationException($"{taskType.Name} does not derive from CaptchaTask<TSolution>.");
    }
}
