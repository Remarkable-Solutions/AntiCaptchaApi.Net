using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Docs;

/// <summary>
/// Every C# block of README.md must also be in the probe's <c>ReadmeSnippets.cs</c>, which the solution build compiles.
/// A README example that no longer compiles therefore breaks the build instead of misleading users.
/// Lines are compared trimmed, without blank lines and without the README's <c>using AntiCaptchaApi…</c> directives.
/// </summary>
public class ReadmeSnippetsTests
{
    [Fact]
    public void Readme_EveryCSharpSnippet_IsCompiledByTheProbe()
    {
        var root = RepositoryRoot();
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));
        var probe = Normalize(File.ReadAllText(Path.Combine(root, "tools", "AntiCaptchaApi.Net.Probe", "ReadmeSnippets.cs")));

        var blocks = Regex.Matches(readme, "```csharp\\r?\\n(.*?)```", RegexOptions.Singleline).Select(m => m.Groups[1].Value).ToList();
        var missing = blocks.Select(Normalize).Where(block => !probe.Contains(block, StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(blocks);
        Assert.True(missing.Count == 0, "README.md snippets not found in ReadmeSnippets.cs:\n" + string.Join("\n---\n", missing));
    }

    [Fact]
    public void Readme_LinksMigrationAndChangelog()
    {
        var readme = File.ReadAllText(Path.Combine(RepositoryRoot(), "README.md"));

        Assert.Contains("(MIGRATION.md)", readme, StringComparison.Ordinal);
        Assert.Contains("(CHANGELOG.md)", readme, StringComparison.Ordinal);
    }

    private static string Normalize(string code) =>
        string.Join("\n", code.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith("using AntiCaptchaApi", StringComparison.Ordinal)));

    private static string RepositoryRoot([CallerFilePath] string sourcePath = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(sourcePath)!);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AntiCaptchaApi.Net.sln")))
        {
            directory = directory.Parent;
        }

        Assert.True(directory is not null, "repository root (AntiCaptchaApi.Net.sln) not found above " + sourcePath);
        return directory!.FullName;
    }
}
