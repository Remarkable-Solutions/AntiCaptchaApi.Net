using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AntiCaptchaApi.Net.Internal;
using AntiCaptchaApi.Net.Internal.Helpers;
using AntiCaptchaApi.Net.Models.Solutions;
using AntiCaptchaApi.Net.Requests.Abstractions.Interfaces;
using AntiCaptchaApi.Net.Responses;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests;

/// <summary>
/// Guards the hand-kept registries (validators, task names, result converters) against drift:
/// every request/solution type in the library must be registered everywhere.
/// </summary>
public class RegistryCompletenessTests
{
    private static readonly Assembly LibraryAssembly = typeof(AnticaptchaClient).Assembly;

    private static readonly MethodInfo ValidateMethod = GetHelperMethod(nameof(CreateCaptchaRequestHelper.Validate));
    private static readonly MethodInfo BuildMethod = GetHelperMethod(nameof(CreateCaptchaRequestHelper.Build));

    [Fact]
    public void RequestTypeDiscovery_FindsKnownRequestTypes()
    {
        var requestTypes = GetRequestTypes().Select(x => x.RequestType).ToList();

        Assert.True(requestTypes.Count >= 22, $"Only {requestTypes.Count} request types discovered.");
        Assert.Contains(typeof(Requests.AmazonWafRequest), requestTypes);
        Assert.Contains(typeof(Requests.ProsopoRequest), requestTypes);
    }

    [Fact]
    public void EveryRequestType_HasValidator()
    {
        var failures = new List<string>();

        foreach (var (requestType, solutionType) in GetRequestTypes())
        {
            var exception = Invoke(ValidateMethod, requestType, solutionType, out _);
            if (exception != null)
            {
                failures.Add($"{requestType.Name}: {exception.GetType().Name}: {exception.Message}");
            }
        }

        Assert.True(failures.Count == 0, "Request types without a registered validator:\n" + string.Join("\n", failures));
    }

    [Fact]
    public void EveryRequestType_HasTaskName()
    {
        var failures = new List<string>();

        foreach (var (requestType, solutionType) in GetRequestTypes())
        {
            var exception = Invoke(BuildMethod, requestType, solutionType, out var built);
            if (exception != null)
            {
                failures.Add($"{requestType.Name}: {exception.GetType().Name}: {exception.Message}");
                continue;
            }

            var taskName = ((JObject)built!)["type"]?.ToString();
            if (string.IsNullOrEmpty(taskName))
            {
                failures.Add($"{requestType.Name}: Build() produced an empty 'type'");
            }
        }

        Assert.True(failures.Count == 0, "Request types without a registered task name:\n" + string.Join("\n", failures));
    }

    [Fact]
    public void EveryRequestType_SolutionHasConverter()
    {
        var missing = GetRequestTypes()
            .Where(x => !HasConverter(x.SolutionType))
            .Select(x => $"{x.RequestType.Name} (solution {x.SolutionType.Name})")
            .ToList();

        Assert.True(missing.Count == 0, "Request types whose solution has no TaskResultConverter:\n" + string.Join("\n", missing));
    }

    [Fact]
    public void EverySolutionType_HasConverter()
    {
        var solutionTypes = LibraryAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(BaseSolution).IsAssignableFrom(t))
            .ToList();

        var missing = solutionTypes.Where(t => !HasConverter(t)).Select(t => t.Name).ToList();

        Assert.NotEmpty(solutionTypes);
        Assert.True(missing.Count == 0, "Solution types without a TaskResultConverter:\n" + string.Join("\n", missing));
    }

    private static IEnumerable<(Type RequestType, Type SolutionType)> GetRequestTypes() =>
        LibraryAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
            .Select(t => (RequestType: t, Interface: t.GetInterfaces().FirstOrDefault(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICaptchaRequest<>))))
            .Where(x => x.Interface != null)
            .Select(x => (x.RequestType, x.Interface!.GetGenericArguments()[0]))
            .OrderBy(x => x.RequestType.Name);

    private static bool HasConverter(Type solutionType)
    {
        var responseType = typeof(TaskResultResponse<>).MakeGenericType(solutionType);
        return HttpHelper.Converters.Any(c => c.CanConvert(responseType));
    }

    private static MethodInfo GetHelperMethod(string name) =>
        typeof(CreateCaptchaRequestHelper).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
        ?? throw new InvalidOperationException($"CreateCaptchaRequestHelper.{name} not found.");

    /// <summary>
    /// Invokes a generic <see cref="CreateCaptchaRequestHelper"/> method for a default instance of
    /// <paramref name="requestType"/>; returns the unwrapped exception, or null on success.
    /// </summary>
    private static Exception? Invoke(MethodInfo method, Type requestType, Type solutionType, out object? result)
    {
        result = null;
        try
        {
            var request = Activator.CreateInstance(requestType);
            result = method.MakeGenericMethod(solutionType).Invoke(null, new[] { request });
            return null;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            return ex.InnerException;
        }
    }
}
