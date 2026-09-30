using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using AntiCaptchaApi.Net.Tasks;

namespace AntiCaptchaApi.Net.Internal.Json;

/// <summary>
/// Builds the <c>task</c> object of <c>createTask</c>: the task's own properties (serialized by runtime type through
/// <see cref="AntiCaptchaJsonContext"/>), preceded by <c>type</c> and followed by the flattened proxy fields.
/// </summary>
internal static class TaskSerializer
{
    public static JsonObject ToJsonObject(CaptchaTask task)
    {
        var taskType = task.GetType();
        var typeInfo = AntiCaptchaJsonContext.Default.GetTypeInfo(taskType)
            ?? throw new InvalidOperationException(
                $"Task type '{taskType.FullName}' is not registered in AntiCaptchaJsonContext. Add [JsonSerializable(typeof({taskType.Name}))].");

        var properties = (JsonObject)JsonSerializer.SerializeToNode(task, typeInfo)!;
        var result = new JsonObject { ["type"] = task.ResolveTypeName() };
        foreach (var property in Detach(properties))
        {
            result[property.Key] = property.Value;
        }

        if (task.Proxy is { } proxy)
        {
            AddProxy(result, proxy, task.IncludesProxyType);
        }
        return result;
    }

    private static void AddProxy(JsonObject target, Proxy proxy, bool includeType)
    {
        if (includeType)
        {
            target["proxyType"] = JsonSerializer.SerializeToNode(proxy.Type, AntiCaptchaJsonContext.Default.ProxyType);
        }
        target["proxyAddress"] = proxy.Address;
        target["proxyPort"] = proxy.Port;
        if (proxy.Login is not null)
        {
            target["proxyLogin"] = proxy.Login;
        }
        if (proxy.Password is not null)
        {
            target["proxyPassword"] = proxy.Password;
        }
    }

    // A node can have only one parent, so the properties are removed from the serialized object before they move.
    private static List<KeyValuePair<string, JsonNode?>> Detach(JsonObject source)
    {
        var properties = source.ToList();
        source.Clear();
        return properties;
    }
}
