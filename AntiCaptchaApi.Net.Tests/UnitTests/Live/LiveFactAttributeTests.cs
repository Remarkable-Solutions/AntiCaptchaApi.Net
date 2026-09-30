using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AntiCaptchaApi.Net.Tests.LiveTests;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Live;

public class LiveFactAttributeTests
{
    private static readonly IReadOnlyDictionary<string, string> Key = new Dictionary<string, string>
    {
        [LiveEnvironment.ClientKeyVariable] = "key",
    };

    private static readonly IReadOnlyDictionary<string, string> KeyAndProxy = new Dictionary<string, string>
    {
        [LiveEnvironment.ClientKeyVariable] = "key",
        [LiveEnvironment.ProxyAddressVariable] = "127.0.0.1",
        [LiveEnvironment.ProxyPortVariable] = "8080",
        [LiveEnvironment.ProxyLoginVariable] = "login",
        [LiveEnvironment.ProxyPasswordVariable] = "password",
    };

    [Fact]
    public void Skip_WhenClientKeyMissing_IsSet()
    {
        var attribute = new LiveFactAttribute(Lookup(new Dictionary<string, string>()));

        Assert.False(string.IsNullOrEmpty(attribute.Skip));
        Assert.Contains(LiveEnvironment.ClientKeyVariable, attribute.Skip);
    }

    [Fact]
    public void Skip_WhenClientKeyWhitespace_IsSet()
    {
        var attribute = new LiveFactAttribute(Lookup(new Dictionary<string, string> { [LiveEnvironment.ClientKeyVariable] = "  " }));

        Assert.False(string.IsNullOrEmpty(attribute.Skip));
    }

    [Fact]
    public void Skip_WhenClientKeyPresent_IsNull()
    {
        var attribute = new LiveFactAttribute(Lookup(Key));

        Assert.Null(attribute.Skip);
    }

    [Fact]
    public void Skip_WhenProxyRequiredAndMissing_IsSet()
    {
        var attribute = new LiveFactAttribute(Lookup(Key)) { RequiresProxy = true };

        Assert.False(string.IsNullOrEmpty(attribute.Skip));
        Assert.Contains(LiveEnvironment.ProxyAddressVariable, attribute.Skip);
    }

    [Fact]
    public void Skip_WhenProxyRequiredAndDefined_IsNull()
    {
        var attribute = new LiveFactAttribute(Lookup(KeyAndProxy)) { RequiresProxy = true };

        Assert.Null(attribute.Skip);
    }

    [Fact]
    public void Skip_WhenExplicitReasonGiven_KeepsIt()
    {
        var attribute = new LiveFactAttribute(Lookup(KeyAndProxy)) { Skip = "no target site" };

        Assert.Equal("no target site", attribute.Skip);
    }

    [Fact]
    public void LiveTheory_Skip_WhenClientKeyMissing_IsSet()
    {
        var attribute = new LiveTheoryAttribute(Lookup(new Dictionary<string, string>()));

        Assert.False(string.IsNullOrEmpty(attribute.Skip));
    }

    [Fact]
    public void LiveTheory_Skip_WhenClientKeyPresent_IsNull()
    {
        var attribute = new LiveTheoryAttribute(Lookup(Key));

        Assert.Null(attribute.Skip);
    }

    [Fact]
    public void LiveTests_UseOnlyLiveAttributesAndCarryLiveCategory()
    {
        var liveClasses = typeof(LiveEnvironment).Assembly.GetTypes()
            .Where(t => t.Namespace == typeof(LiveEnvironment).Namespace && t.IsClass && !t.IsAbstract)
            .Where(t => t.GetMethods().Any(m => m.GetCustomAttributes<FactAttribute>(inherit: true).Any()))
            .ToList();

        var offenders = new List<string>();
        foreach (var type in liveClasses)
        {
            var hasTrait = type.GetCustomAttributesData().Any(a =>
                a.AttributeType == typeof(TraitAttribute) &&
                (string?)a.ConstructorArguments[0].Value == "Category" &&
                (string?)a.ConstructorArguments[1].Value == "Live");
            if (!hasTrait)
            {
                offenders.Add($"{type.Name}: missing [Trait(\"Category\", \"Live\")]");
            }

            foreach (var method in type.GetMethods())
            {
                var fact = method.GetCustomAttribute<FactAttribute>(inherit: true);
                if (fact is not null && fact is not LiveFactAttribute && fact is not LiveTheoryAttribute)
                {
                    offenders.Add($"{type.Name}.{method.Name}: uses [{fact.GetType().Name}] instead of [LiveFact]/[LiveTheory]");
                }
            }
        }

        Assert.NotEmpty(liveClasses);
        Assert.Empty(offenders);
    }

    private static Func<string, string?> Lookup(IReadOnlyDictionary<string, string> values) =>
        name => values.TryGetValue(name, out var value) ? value : null;
}
