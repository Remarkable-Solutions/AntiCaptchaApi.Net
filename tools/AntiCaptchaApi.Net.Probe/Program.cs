using System.Reflection;
using System.Runtime.Versioning;

// Console probe for manual verification of AntiCaptchaApi.Net 3.0 (see conductor/tracks/core_v3_client_20260930/plan.md).
// Commands run offline unless their name starts with "live-"; live commands read ClientKey from the environment
// and are meant to be run by the user only. Later phases add commands to the table below.

var commands = new Dictionary<string, (string Description, Func<string[], int> Run)>(StringComparer.OrdinalIgnoreCase)
{
    ["info"] = ("Print the core assembly version, target framework and referenced assemblies.", _ => Info()),
};

if (args.Length == 0 || !commands.TryGetValue(args[0], out var command))
{
    Console.WriteLine("Usage: dotnet run --project tools/AntiCaptchaApi.Net.Probe -- <command>");
    Console.WriteLine();
    foreach (var (name, (description, _)) in commands.OrderBy(c => c.Key, StringComparer.Ordinal))
    {
        Console.WriteLine($"  {name,-20} {description}");
    }
    return args.Length == 0 ? 0 : 1;
}

return command.Run(args[1..]);

static int Info()
{
    // Loaded by name: the core has no public types yet, and this keeps "info" independent of the API surface.
    var core = Assembly.Load(new AssemblyName("AntiCaptchaApi.Net"));
    var informational = core.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
    var framework = core.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ?? "(unknown)";

    Console.WriteLine($"Assembly:          {core.GetName().Name}");
    Console.WriteLine($"Version:           {core.GetName().Version}");
    Console.WriteLine($"Informational:     {informational ?? "(none)"}");
    Console.WriteLine($"Target framework:  {framework}");
    Console.WriteLine($"Location:          {core.Location}");
    Console.WriteLine("Referenced assemblies:");
    foreach (var reference in core.GetReferencedAssemblies().OrderBy(r => r.Name, StringComparer.Ordinal))
    {
        Console.WriteLine($"  {reference.Name} {reference.Version}");
    }
    return 0;
}
