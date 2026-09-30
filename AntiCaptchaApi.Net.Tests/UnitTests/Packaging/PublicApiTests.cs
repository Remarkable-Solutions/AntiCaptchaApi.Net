using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Xml.Linq;
using AntiCaptchaApi.Net.Tasks;
using Xunit;

namespace AntiCaptchaApi.Net.Tests.UnitTests.Packaging;

/// <summary>Pins the public surface: the client interface (spec FR-4), namespaces, XML docs and exposed JSON types.</summary>
public class PublicApiTests
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly Assembly Core = typeof(AntiCaptchaClient).Assembly;

    // Spec FR-4, verbatim (design §4.2 is the same without the generic constraints).
    private static readonly string[] ExpectedClientMethods =
    {
        "Task<TSolution> SolveAsync<TSolution>(CaptchaTask<TSolution> task, SolveOptions? o = null, CancellationToken ct = default) where TSolution : Solution",
        "Task<SolveResult<TSolution>> TrySolveAsync<TSolution>(CaptchaTask<TSolution> task, SolveOptions? o = null, CancellationToken ct = default) where TSolution : Solution",
        "Task<int> CreateTaskAsync(CaptchaTask task, CreateOptions? o = null, CancellationToken ct = default)",
        "Task<TaskStatus<TSolution>> GetTaskResultAsync<TSolution>(int taskId, CancellationToken ct = default) where TSolution : Solution",
        "Task<TSolution> WaitForResultAsync<TSolution>(int taskId, SolveOptions? o = null, CancellationToken ct = default) where TSolution : Solution",
        "Task ReportAsync(int taskId, Report report, CancellationToken ct = default)",
        "Task PushAntiGateVariableAsync(int taskId, string name, object value, CancellationToken ct = default)",
        "Task<decimal> GetBalanceAsync(CancellationToken ct = default)",
        "Task<QueueStats> GetQueueStatsAsync(QueueType q, CancellationToken ct = default)",
        "Task<SpendingStats> GetSpendingStatsAsync(SpendingQuery? q = null, CancellationToken ct = default)",
        "Task<AppStats> GetAppStatsAsync(int softId, AppStatsMode? mode = null, CancellationToken ct = default)",
    };

    private static readonly string[] AllowedNamespaces = { "AntiCaptchaApi.Net", "AntiCaptchaApi.Net.Tasks" };

    // The documented exception (plan Task 5.7): raw JSON where the API payload is free-form.
    private static readonly Type[] JsonTypesAllowedOn = { typeof(AntiGateTask), typeof(AntiGateSolution), typeof(AppStats) };
    private static readonly string[] AllowedJsonTypes = { "System.Text.Json.Nodes.JsonObject", "System.Text.Json.Nodes.JsonNode", "System.Text.Json.JsonElement" };

    private static readonly Dictionary<Type, string> Keywords = new()
    {
        [typeof(int)] = "int", [typeof(long)] = "long", [typeof(bool)] = "bool", [typeof(string)] = "string",
        [typeof(object)] = "object", [typeof(decimal)] = "decimal", [typeof(double)] = "double", [typeof(void)] = "void",
    };

    private static IEnumerable<Type> PublicTypes() => Core.GetExportedTypes().OrderBy(t => t.FullName, StringComparer.Ordinal);

    [Fact]
    public void IAntiCaptchaClient_Methods_MatchDesign()
    {
        var actual = typeof(IAntiCaptchaClient).GetMethods().Select(Signature).OrderBy(s => s, StringComparer.Ordinal).ToList();

        Assert.Equal(ExpectedClientMethods.OrderBy(s => s, StringComparer.Ordinal), actual);
        Assert.Empty(typeof(IAntiCaptchaClient).GetProperties());
        Assert.Empty(typeof(IAntiCaptchaClient).GetEvents());
        Assert.Empty(typeof(IAntiCaptchaClient).GetInterfaces());
    }

    [Fact]
    public void IAntiCaptchaClient_EveryMethod_EndsWithOptionalCancellationTokenCt()
    {
        foreach (var method in typeof(IAntiCaptchaClient).GetMethods())
        {
            var last = method.GetParameters().Last();

            Assert.True(last.ParameterType == typeof(CancellationToken), $"{method.Name}: last parameter is {last.ParameterType.Name}.");
            Assert.Equal("ct", last.Name);
            Assert.True(last.IsOptional && last.HasDefaultValue && last.DefaultValue is null, $"{method.Name}: ct must default to default.");
        }
    }

    [Fact]
    public void AntiCaptchaClient_IsSealedAndImplementsTheInterface()
    {
        Assert.True(typeof(AntiCaptchaClient).IsSealed);
        Assert.Contains(typeof(IAntiCaptchaClient), typeof(AntiCaptchaClient).GetInterfaces());
    }

    [Fact]
    public void EveryPublicType_HasXmlDocs()
    {
        var path = Path.ChangeExtension(Core.Location, ".xml");
        Assert.True(File.Exists(path), "The core XML documentation file is missing: " + path);
        var documented = XDocument.Load(path).Descendants("member")
            .Where(m => m.Elements().Any(e => !string.IsNullOrWhiteSpace(e.Value) || e.Name == "inheritdoc"))
            .Select(m => (string)m.Attribute("name")!)
            .ToHashSet(StringComparer.Ordinal);

        // Implicit parameterless constructors (records with init-only properties) need no docs under CS1591, and reflection
        // cannot tell them from explicit ones, so parameterless instance constructors are not checked.
        var missing = PublicTypes()
            .SelectMany(t => new[] { DocId.Of(t) }.Concat(VisibleDeclaredMembers(t)
                .Where(m => m is not ConstructorInfo { IsStatic: false } c || c.GetParameters().Length > 0)
                .Select(DocId.Of)))
            .Where(id => !documented.Contains(id))
            .ToList();

        Assert.True(PublicTypes().Count() > 40, "Expected the full 3.0 surface.");
        Assert.True(missing.Count == 0, "Undocumented public API:\n" + string.Join("\n", missing));
    }

    [Fact]
    public void PublicTypes_OnlyInAllowedNamespaces()
    {
        var outside = PublicTypes().Where(t => !AllowedNamespaces.Contains(t.Namespace)).Select(t => t.FullName).ToList();

        Assert.True(outside.Count == 0, "Public types outside AntiCaptchaApi.Net(.Tasks):\n" + string.Join("\n", outside));
    }

    [Fact]
    public void NoPublicMember_ExposesInternalOrThirdPartyJsonTypes()
    {
        var violations = new List<string>();
        foreach (var type in PublicTypes())
        {
            var exposed = new List<(string Where, Type Type)>();
            if (type.BaseType is { } baseType)
            {
                exposed.Add(("base type", baseType));
            }
            exposed.AddRange(type.GetInterfaces().Select(i => ("interface", i)));
            foreach (var member in VisibleDeclaredMembers(type))
            {
                exposed.AddRange(ExposedTypes(member).Select(t => (member.Name, t)));
            }

            foreach (var (where, exposedType) in exposed)
            {
                foreach (var part in Flatten(exposedType))
                {
                    if (!IsAllowed(type, part))
                    {
                        violations.Add($"{type.FullName}.{where}: {part.FullName ?? part.Name}");
                    }
                }
            }
        }

        Assert.True(violations.Count == 0, "Public members expose disallowed types:\n" + string.Join("\n", violations.Distinct()));
    }

    [Fact]
    public void JsonTypeException_IsActuallyUsed()
    {
        // Keeps the allow-list honest: each listed type exposes at least one JSON type.
        foreach (var type in JsonTypesAllowedOn)
        {
            var json = VisibleDeclaredMembers(type).SelectMany(ExposedTypes).SelectMany(Flatten).Where(t => AllowedJsonTypes.Contains(t.FullName));
            Assert.True(json.Any(), $"{type.Name} is allow-listed for JSON types but exposes none.");
        }
    }

    private static bool IsAllowed(Type owner, Type type)
    {
        if (type.IsGenericParameter)
        {
            return true;
        }
        if (type.Assembly == Core)
        {
            return type.IsVisible;
        }
        var name = type.FullName ?? type.Name;
        if (name.StartsWith("System.Text.Json", StringComparison.Ordinal))
        {
            return JsonTypesAllowedOn.Contains(owner) && AllowedJsonTypes.Contains(name);
        }
        return type.Namespace is { } ns && (ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal));
    }

    private static IEnumerable<Type> Flatten(Type type)
    {
        if (type.HasElementType)
        {
            return Flatten(type.GetElementType()!);
        }
        if (type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            return new[] { type.GetGenericTypeDefinition() }.Concat(type.GetGenericArguments().SelectMany(Flatten));
        }
        return new[] { type };
    }

    private static IEnumerable<Type> ExposedTypes(MemberInfo member) => member switch
    {
        PropertyInfo p => new[] { p.PropertyType }.Concat(p.GetIndexParameters().Select(i => i.ParameterType)),
        FieldInfo f => new[] { f.FieldType },
        EventInfo e => new[] { e.EventHandlerType! },
        MethodInfo m => new[] { m.ReturnType }.Concat(m.GetParameters().Select(p => p.ParameterType)),
        ConstructorInfo c => c.GetParameters().Select(p => p.ParameterType),
        Type nested => new[] { nested },
        _ => Array.Empty<Type>(),
    };

    // Public and protected members that the type itself declares, without accessors and compiler-synthesized record members.
    private static IEnumerable<MemberInfo> VisibleDeclaredMembers(Type type) =>
        type.GetMembers(Declared).Where(member => member switch
        {
            MethodBase m => IsVisible(m) && !m.IsSpecialName_Accessor() && !m.IsDefined(typeof(CompilerGeneratedAttribute), false),
            PropertyInfo p => p.GetAccessors(true).Any(IsVisible) && !p.IsDefined(typeof(CompilerGeneratedAttribute), false),
            FieldInfo f => (f.IsPublic || f.IsFamily || f.IsFamilyOrAssembly) && !f.IsSpecialName,
            EventInfo e => e.AddMethod is { } add && IsVisible(add),
            Type nested => nested.IsNestedPublic || nested.IsNestedFamily || nested.IsNestedFamORAssem,
            _ => false,
        });

    private static bool IsVisible(MethodBase method) => method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;

    private static string Signature(MethodInfo method)
    {
        var nullability = new NullabilityInfoContext();
        var generics = method.IsGenericMethod ? "<" + string.Join(", ", method.GetGenericArguments().Select(a => a.Name)) + ">" : "";
        var parameters = method.GetParameters().Select(p =>
        {
            var nullable = !p.ParameterType.IsValueType && nullability.Create(p).WriteState == NullabilityState.Nullable ? "?" : "";
            var text = $"{TypeName(p.ParameterType)}{nullable} {p.Name}";
            if (p.HasDefaultValue)
            {
                var isPlainStruct = p.ParameterType.IsValueType && Nullable.GetUnderlyingType(p.ParameterType) is null;
                text += isPlainStruct ? " = default" : " = " + (p.DefaultValue?.ToString() ?? "null");
            }
            return text;
        });
        var constraints = method.IsGenericMethod
            ? string.Concat(method.GetGenericArguments().Select(a =>
                a.GetGenericParameterConstraints() is { Length: > 0 } c ? $" where {a.Name} : {string.Join(", ", c.Select(TypeName))}" : ""))
            : "";
        return $"{TypeName(method.ReturnType)} {method.Name}{generics}({string.Join(", ", parameters)}){constraints}";
    }

    private static string TypeName(Type type)
    {
        if (Keywords.TryGetValue(type, out var keyword))
        {
            return keyword;
        }
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return TypeName(underlying) + "?";
        }
        if (type.IsGenericType)
        {
            var name = type.Name[..type.Name.IndexOf('`')];
            return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>";
        }
        return type.Name;
    }
}

/// <summary>Builds XML documentation ids (ECMA-334 annex D) for reflection members.</summary>
internal static class DocId
{
    public static string Of(MemberInfo member) => member switch
    {
        Type t => "T:" + TypeId(t),
        ConstructorInfo c => $"M:{TypeId(c.DeclaringType!)}.{(c.IsStatic ? "#cctor" : "#ctor")}{Parameters(c)}",
        MethodInfo m => $"M:{TypeId(m.DeclaringType!)}.{m.Name.Replace('.', '#')}{(m.IsGenericMethod ? "``" + m.GetGenericArguments().Length : "")}{Parameters(m)}"
            + (m.Name is "op_Implicit" or "op_Explicit" ? "~" + ParameterType(m.ReturnType) : ""),
        PropertyInfo p => $"P:{TypeId(p.DeclaringType!)}.{p.Name}" + (p.GetIndexParameters() is { Length: > 0 } ip ? "(" + string.Join(",", ip.Select(i => ParameterType(i.ParameterType))) + ")" : ""),
        FieldInfo f => $"F:{TypeId(f.DeclaringType!)}.{f.Name}",
        EventInfo e => $"E:{TypeId(e.DeclaringType!)}.{e.Name}",
        _ => throw new NotSupportedException(member.GetType().Name),
    };

    public static bool IsSpecialName_Accessor(this MethodBase method) =>
        method.IsSpecialName && (method.Name.StartsWith("get_", StringComparison.Ordinal) || method.Name.StartsWith("set_", StringComparison.Ordinal)
            || method.Name.StartsWith("add_", StringComparison.Ordinal) || method.Name.StartsWith("remove_", StringComparison.Ordinal));

    private static string TypeId(Type type) =>
        type.IsNested ? TypeId(type.DeclaringType!) + "." + type.Name : type.Namespace + "." + type.Name;

    private static string Parameters(MethodBase method)
    {
        var parameters = method.GetParameters();
        return parameters.Length == 0 ? "" : "(" + string.Join(",", parameters.Select(p => ParameterType(p.ParameterType))) + ")";
    }

    private static string ParameterType(Type type)
    {
        if (type.IsByRef)
        {
            return ParameterType(type.GetElementType()!) + "@";
        }
        if (type.IsArray)
        {
            return ParameterType(type.GetElementType()!) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
        }
        if (type.IsGenericParameter)
        {
            return (type.DeclaringMethod is null ? "`" : "``") + type.GenericParameterPosition;
        }
        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var name = TypeId(definition);
            return name[..name.LastIndexOf('`')] + "{" + string.Join(",", type.GetGenericArguments().Select(ParameterType)) + "}";
        }
        return TypeId(type);
    }
}
