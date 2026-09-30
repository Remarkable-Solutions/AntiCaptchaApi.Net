using System.Reflection;
using System.Text.Json.Nodes;
using AntiCaptchaApi.Net;
using AntiCaptchaApi.Net.Internal.Json;
using AntiCaptchaApi.Net.Tasks;
using System.Runtime.Versioning;

// Console probe for manual verification of AntiCaptchaApi.Net 3.0 (see conductor/tracks/core_v3_client_20260930/plan.md).
// Commands run offline unless their name starts with "live-"; live commands read ClientKey from the environment
// and are meant to be run by the user only. Later phases add commands to the table below.

var commands = new Dictionary<string, (string Description, Func<string[], int> Run)>(StringComparer.OrdinalIgnoreCase)
{
    ["info"] = ("Print the core assembly version, target framework and referenced assemblies.", _ => Info()),
    ["serialize"] = ("Print the createTask JSON of a sample of every task type, plus validation and redaction examples.", _ => Serialize()),
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

static int Serialize()
{
    const string Url = "https://example.com/login";
    var proxy = new Proxy(ProxyType.Socks5, "203.0.113.10", 1080) { Login = "proxyUser", Password = "proxyPass" };
    const string Ua = "Mozilla/5.0 (probe)";
    var samples = new (string Name, CaptchaTask Task)[]
    {
        ("ImageToText", ImageToTextTask.FromBytes(new byte[] { 1, 2, 3 }) with { Comment = "enter the red text" }),
        ("ImageToCoordinates", ImageToCoordinatesTask.FromBytes(new byte[] { 1, 2, 3 })),
        ("RecaptchaV2 (proxyless)", new RecaptchaV2Task(Url, "SITE_KEY")),
        ("RecaptchaV2 (proxy)", new RecaptchaV2Task(Url, "SITE_KEY") { Proxy = proxy, UserAgent = Ua }),
        ("RecaptchaV2Enterprise", new RecaptchaV2EnterpriseTask(Url, "SITE_KEY")),
        ("RecaptchaV3", new RecaptchaV3Task(Url, "SITE_KEY") { MinScore = MinScore.Score09, PageAction = "login" }),
        ("Turnstile", new TurnstileTask(Url, "SITE_KEY") { Action = "login", CData = "c" }),
        ("FunCaptcha", new FunCaptchaTask(Url, "PUBLIC_KEY")),
        ("GeeTestV3", new GeeTestV3Task(Url, "GT", "CHALLENGE")),
        ("GeeTestV4", new GeeTestV4Task(Url, "CAPTCHA_ID")),
        ("AmazonWaf (proxy)", new AmazonWafTask(Url, "SITE_KEY", "IV", "CONTEXT") { Proxy = proxy }),
        ("Prosopo", new ProsopoTask(Url, "SITE_KEY")),
        ("FriendlyCaptcha", new FriendlyCaptchaTask(Url, "SITE_KEY")),
        ("AntiGate (proxy)", new AntiGateTask(Url, "Sign-in and wait for control text", new JsonObject { ["login_input_css"] = "#login" }) { Proxy = proxy }),
    };

    foreach (var (name, task) in samples)
    {
        task.RunValidation();
        Console.WriteLine($"{name,-24} {TaskSerializer.ToJsonObject(task).ToJsonString()}");
    }

    Console.WriteLine();
    Console.WriteLine("Validation of new TurnstileTask(\"not-a-url\", \"\"):");
    try
    {
        new TurnstileTask("not-a-url", "").RunValidation();
        Console.WriteLine("  (no errors — unexpected)");
    }
    catch (AntiCaptchaValidationException ex)
    {
        Console.WriteLine("  " + ex.Message.Replace("\n", "\n  "));
    }

    Console.WriteLine();
    Console.WriteLine("Proxy.ToString(): " + proxy);
    Console.WriteLine("AntiGate task ToString(): " + samples[^1].Task);
    return 0;
}
