using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Docs;

/// <summary>
/// MIGRATION.md must name every 2.x public type and every 2.x client member (spec FR-11), so a 2.x user finds a row
/// for whatever they used. The 2.x surface comes from the Phase 1 capture <c>Snapshots/Legacy2x/public-api.txt</c>.
/// A name counts only in code formatting (<c>`Name`</c>, <c>`Name&lt;T&gt;`</c>, <c>`Name.Member`</c>, <c>`Name(...)`</c>).
/// </summary>
public class MigrationGuideTests
{
    private static readonly string[] LegacyClientTypes =
    {
        "AntiCaptchaApi.Net.AnticaptchaClient",
        "AntiCaptchaApi.Net.IAnticaptchaClient",
    };

    [Fact]
    public void MigrationGuide_MentionsEveryLegacyPublicType()
    {
        var guide = ReadGuide();

        var missing = LegacyTypes().Select(SimpleName).Distinct().Where(name => !MentionsInCode(guide, name)).ToList();

        Assert.True(missing.Count == 0, "MIGRATION.md does not mention these 2.x types: " + string.Join(", ", missing));
    }

    [Fact]
    public void MigrationGuide_MentionsEveryLegacyClientMethod()
    {
        var guide = ReadGuide();

        var missing = new List<string>();
        foreach (var line in LegacyApi())
        {
            var type = LegacyClientTypes.FirstOrDefault(t => line.StartsWith(t + ".", StringComparison.Ordinal));
            if (type is null)
            {
                continue;
            }

            var member = MemberName(line.Substring(type.Length + 1));
            var name = member == ".ctor" ? SimpleName(type) + "(" : member;
            if (member == ".ctor" ? !guide.Contains("`" + name, StringComparison.Ordinal) : !MentionsInCode(guide, name))
            {
                missing.Add($"{SimpleName(type)}.{member}");
            }
        }

        Assert.True(missing.Count == 0, "MIGRATION.md does not mention these 2.x client members: " + string.Join(", ", missing.Distinct()));
    }

    [Fact]
    public void LegacyTypes_AreDiscoveredFromTheCapture()
    {
        var types = LegacyTypes().ToList();

        Assert.Contains("AntiCaptchaApi.Net.Requests.RecaptchaV2Request", types);
        Assert.Contains("AntiCaptchaApi.Net.Responses.TaskResultResponse<T>", types);
        Assert.DoesNotContain("AntiCaptchaApi.Net.Models.ClientConfig.SolveAsyncRetries", types);
        Assert.Equal(119, types.Count);
    }

    /// <summary>A type line is one that is not a member of another listed type.</summary>
    private static IEnumerable<string> LegacyTypes()
    {
        var lines = LegacyApi();
        var all = new HashSet<string>(lines, StringComparer.Ordinal);
        return lines.Where(l => !l.Contains('(') && !l.Contains(" = ") && !all.Contains(l.Substring(0, Math.Max(0, l.LastIndexOf('.')))));
    }

    private static IReadOnlyList<string> LegacyApi() =>
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Snapshots", "Legacy2x", "public-api.txt"))
            .Where(l => l.Length > 0)
            .ToList();

    private static string SimpleName(string fullName)
    {
        var withoutGenerics = fullName.Split('<')[0];
        return withoutGenerics.Substring(withoutGenerics.LastIndexOf('.') + 1);
    }

    private static string MemberName(string member)
    {
        if (member.StartsWith(".ctor", StringComparison.Ordinal))
        {
            return ".ctor";
        }

        var end = member.IndexOfAny(new[] { '(', '<', ' ' });
        return end < 0 ? member : member.Substring(0, end);
    }

    private static bool MentionsInCode(string guide, string name) =>
        Regex.IsMatch(guide, "`(?:[A-Za-z0-9_]+\\.)*" + Regex.Escape(name) + "(?![A-Za-z0-9_])");

    private static string ReadGuide([CallerFilePath] string sourcePath = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(sourcePath)!);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AntiCaptchaApi.Net.sln")))
        {
            directory = directory.Parent;
        }

        Assert.True(directory is not null, "repository root (AntiCaptchaApi.Net.sln) not found above " + sourcePath);
        var path = Path.Combine(directory!.FullName, "MIGRATION.md");
        Assert.True(File.Exists(path), "MIGRATION.md not found at the repository root: " + path);
        return File.ReadAllText(path);
    }
}
