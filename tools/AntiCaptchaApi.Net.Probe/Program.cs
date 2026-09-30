using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using AntiCaptchaApi.Net;
using AntiCaptchaApi.Net.Internal.Json;
using AntiCaptchaApi.Net.Internal.Time;
using AntiCaptchaApi.Net.Tasks;
using System.Runtime.Versioning;

// Console probe for manual verification of AntiCaptchaApi.Net 3.0 (see conductor/tracks/core_v3_client_20260930/plan.md).
// Commands run offline unless their name starts with "live-"; live commands read ClientKey from the environment
// and are meant to be run by the user only. Later phases add commands to the table below.

var commands = new Dictionary<string, (string Description, Func<string[], int> Run)>(StringComparer.OrdinalIgnoreCase)
{
    ["info"] = ("Print the core assembly version, target framework and referenced assemblies.", _ => Info()),
    ["serialize"] = ("Print the createTask JSON of a sample of every task type, plus validation and redaction examples.", _ => Serialize()),
    ["offline-create"] = ("Run CreateTaskAsync against an in-process scripted handler: masked request JSON, then error scenarios.", _ => OfflineCreate().GetAwaiter().GetResult()),
    ["offline-solve"] = ("Run TrySolveAsync offline on a simulated clock: processing -> transient error -> ready; print the timeline and the SolveResult.", _ => OfflineSolve().GetAwaiter().GetResult()),
    ["offline-retry"] = ("Run TrySolveAsync offline on a simulated clock: ERROR_NO_SLOT_AVAILABLE x2 -> success; print the timeline, backoff delays and the SolveResult.", _ => OfflineRetry().GetAwaiter().GetResult()),
    ["live-solve-image"] = ("USER ONLY (paid, needs ClientKey): solve ImageToText from captchaexample.png [path] and print the text, cost and attempts.", a => LiveSolveImage(a).GetAwaiter().GetResult()),
    ["live-create-image"] = ("USER ONLY (paid, needs ClientKey): create one ImageToText task from captchaexample.png [path] and print the taskId.", a => LiveCreateImage(a).GetAwaiter().GetResult()),
    ["live-amazon-shape"] = ("USER ONLY (paid, needs ClientKey): <websiteUrl> <websiteKey> <iv> <context>; solve AmazonWaf and print the raw solution JSON (spec Q-2).", a => LiveAmazonShape(a).GetAwaiter().GetResult()),
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

static async Task<int> OfflineCreate()
{
    // Deliberately recognisable fake secrets: none of them may appear in anything printed below.
    const string Key = "SECRET_PROBE_KEY";
    const string ProxyPassword = "SECRET_PROXY_PASS";
    var output = new StringBuilder();
    void Print(string line)
    {
        Console.WriteLine(line);
        output.AppendLine(line);
    }
    string Mask(string text) => text.Replace(Key, "***").Replace(ProxyPassword, "***");

    var proxy = new Proxy(ProxyType.Http, "203.0.113.10", 8080) { Login = "proxyUser", Password = ProxyPassword };
    var task = new TurnstileTask("https://example.com/login", "SITE_KEY") { Proxy = proxy, UserAgent = "Mozilla/5.0 (probe)" };
    const string Echo = "echo key=" + Key + " proxy=proxyUser:" + ProxyPassword + "@203.0.113.10:8080";

    var scenarios = new (string Name, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Respond)[]
    {
        ("success", (_, _) => Task.FromResult(Json("{\"errorId\":0,\"taskId\":123456}"))),
        ("API error echoing both secrets", (_, _) => Task.FromResult(Json($"{{\"errorId\":25,\"errorCode\":\"ERROR_PROXY_CONNECT_REFUSED\",\"errorDescription\":\"{Echo}\"}}"))),
        ("unknown API code containing the key", (_, _) => Task.FromResult(Json($"{{\"errorId\":99,\"errorCode\":\"ERROR_{Key}\",\"errorDescription\":\"{Echo}\"}}"))),
        ("network failure echoing both secrets", (_, _) => throw new HttpRequestException(Echo)),
        ("HTTP 503", (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent(Echo) })),
        ("invalid JSON body echoing both secrets", (_, _) => Task.FromResult(Json("<html>" + Echo + "</html>"))),
        ("request timeout (RequestTimeout = 200 ms)", async (_, ct) => { await Task.Delay(Timeout.Infinite, ct); throw new InvalidOperationException("unreachable"); }),
    };

    foreach (var (name, respond) in scenarios)
    {
        var handler = new ScriptedHandler(respond);
        var client = new AntiCaptchaClient(Key, new HttpClient(handler), new AntiCaptchaClientOptions { RequestTimeout = TimeSpan.FromMilliseconds(200) });
        Print($"== {name}");
        try
        {
            var taskId = await client.CreateTaskAsync(task);
            Print($"  request:  {Mask(handler.LastBody ?? "(none)")}");
            Print($"  taskId:   {taskId}");
        }
        catch (AntiCaptchaException ex)
        {
            Print($"  request:  {Mask(handler.LastBody ?? "(none)")}");
            Print($"  message:  {ex.Message}");
            Print($"  error:    {ex.Error}");
            Print($"  retryable={ex.Error.IsRetryable} transient={ex.Error.IsTransient}");
        }
    }

    Print("== validation (invalid task with a proxy password, nothing is sent)");
    try
    {
        await new AntiCaptchaClient(Key, new HttpClient(new ScriptedHandler((_, _) => throw new InvalidOperationException("must not be called"))))
            .CreateTaskAsync(new TurnstileTask("not-a-url", "") { Proxy = proxy with { Port = 0 } });
    }
    catch (AntiCaptchaValidationException ex)
    {
        Print($"  message:  {ex.Message}");
    }
    Print($"  client:   {new AntiCaptchaClient(Key)}");

    // The masked request lines are the only place where masking (not the library) hides the secrets.
    var leaked = output.ToString().Contains("SECRET", StringComparison.Ordinal);
    Console.WriteLine();
    Console.WriteLine(leaked ? "FAIL: a SECRET string appears in the output above." : "OK: no SECRET string appears in the output above.");
    return leaked ? 1 : 0;

    static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}

static async Task<int> OfflineSolve()
{
    var timeline = new Timeline();
    timeline.Script("createTask", "{\"errorId\":0,\"taskId\":123456}");
    timeline.Script("getTaskResult", "{\"errorId\":0,\"status\":\"processing\"}");
    timeline.ScriptFailure("getTaskResult", new HttpRequestException("connection reset by peer (simulated)"));
    timeline.Script("getTaskResult", "{\"errorId\":0,\"status\":\"ready\",\"solution\":{\"token\":\"0.SIMULATED_TURNSTILE_TOKEN\"},"
        + "\"cost\":\"0.00200\",\"ip\":\"198.51.100.7\",\"createTime\":1700000000,\"endTime\":1700000016,\"solveCount\":\"1\"}");

    var result = await timeline.Client.TrySolveAsync(new TurnstileTask("https://example.com/login", "SITE_KEY"));
    return timeline.Report("TrySolveAsync(TurnstileTask), default SolveOptions", result.ToString(), result.IsSuccess && result.Attempts == 1);
}

static async Task<int> OfflineRetry()
{
    var timeline = new Timeline();
    const string NoSlot = "{\"errorId\":2,\"errorCode\":\"ERROR_NO_SLOT_AVAILABLE\",\"errorDescription\":\"No idle workers are available at the moment.\"}";
    timeline.Script("createTask", NoSlot);
    timeline.Script("createTask", NoSlot);
    timeline.Script("createTask", "{\"errorId\":0,\"taskId\":654321}");
    timeline.Script("getTaskResult", "{\"errorId\":0,\"status\":\"ready\",\"solution\":{\"text\":\"deditur\"},"
        + "\"cost\":\"0.00070\",\"ip\":\"203.0.113.9\",\"createTime\":1700000000,\"endTime\":1700000004,\"solveCount\":\"0\"}");

    var result = await timeline.Client.TrySolveAsync(ImageToTextTask.FromBytes(new byte[] { 0x89, 0x50, 0x4E, 0x47 }));
    return timeline.Report("TrySolveAsync(ImageToTextTask), default SolveOptions (backoff 2 s, 4 s; jitter off)", result.ToString(), result.IsSuccess && result.Attempts == 3);
}

static async Task<int> LiveSolveImage(string[] args)
{
    if (RequireClientKey() is not { } key)
    {
        return 2;
    }
    var path = args.Length > 0 ? args[0] : FindUpwards(Path.Combine("AntiCaptchaApi.Net.Tests", "Resources", "captchaexample.png"));
    if (path is null || !File.Exists(path))
    {
        Console.Error.WriteLine("captchaexample.png not found; pass the image path as the first argument.");
        return 2;
    }

    // TrySolveAsync (not SolveAsync) so that the cost and the attempt count can be printed as well.
    var result = await new AntiCaptchaClient(key).TrySolveAsync(ImageToTextTask.FromFile(path));
    if (result.IsSuccess)
    {
        Console.WriteLine($"Text = {result.Solution!.Text}, cost = {result.Cost} USD, attempts = {result.Attempts}, taskId = {result.TaskId}, solve time = {result.SolveTime}");
        return 0;
    }
    Console.Error.WriteLine($"Solve failed after {result.Attempts} attempt(s), taskId = {result.TaskId}: {result.Error}");
    return 1;
}

static string? RequireClientKey()
{
    var key = Environment.GetEnvironmentVariable("ClientKey");
    if (string.IsNullOrWhiteSpace(key))
    {
        Console.Error.WriteLine("Set the ClientKey environment variable to run live commands (they spend money; user only).");
        return null;
    }
    return key;
}

static async Task<int> LiveCreateImage(string[] args)
{
    if (RequireClientKey() is not { } key)
    {
        return 2;
    }
    var path = args.Length > 0 ? args[0] : FindUpwards(Path.Combine("AntiCaptchaApi.Net.Tests", "Resources", "captchaexample.png"));
    if (path is null || !File.Exists(path))
    {
        Console.Error.WriteLine("captchaexample.png not found; pass the image path as the first argument.");
        return 2;
    }

    var client = new AntiCaptchaClient(key);
    try
    {
        var taskId = await client.CreateTaskAsync(ImageToTextTask.FromFile(path));
        Console.WriteLine($"Created ImageToText task from {path}: taskId = {taskId}");
        return 0;
    }
    catch (AntiCaptchaException ex)
    {
        Console.Error.WriteLine($"createTask failed: {ex.Message} (retryable={ex.Error.IsRetryable})");
        return 1;
    }
}

static async Task<int> LiveAmazonShape(string[] args)
{
    if (RequireClientKey() is not { } key)
    {
        return 2;
    }
    if (args.Length < 4)
    {
        Console.Error.WriteLine("Usage: live-amazon-shape <websiteUrl> <websiteKey> <iv> <context>");
        return 2;
    }

    // Logs the raw getTaskResult body (key masked) so that the solution shape can be compared with AmazonWafSolution.
    var logging = new BodyLoggingHandler(key) { InnerHandler = new SocketsHttpHandler() };
    var client = new AntiCaptchaClient(key, new HttpClient(logging));
    try
    {
        var taskId = await client.CreateTaskAsync(new AmazonWafTask(args[0], args[1], args[2], args[3]));
        Console.WriteLine($"taskId = {taskId}; polling every 5 s for up to 180 s ...");
        var deadline = DateTimeOffset.UtcNow.AddSeconds(180);
        while (DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            var status = await client.GetTaskResultAsync<AmazonWafSolution>(taskId);
            if (status.IsReady)
            {
                Console.WriteLine($"Typed: Token = {(string.IsNullOrEmpty(status.Solution!.Token) ? "(missing!)" : status.Solution.Token[..Math.Min(24, status.Solution.Token.Length)] + "...")}, cost = {status.Cost}");
                return 0;
            }
        }
        Console.Error.WriteLine("Timed out waiting for the task.");
        return 1;
    }
    catch (AntiCaptchaException ex)
    {
        Console.Error.WriteLine($"Failed: {ex.Message}");
        return 1;
    }
}

static string? FindUpwards(string relativePath)
{
    for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
    {
        var candidate = Path.Combine(directory.FullName, relativePath);
        if (File.Exists(candidate))
        {
            return candidate;
        }
    }
    return null;
}

/// <summary>Answers every request with one scripted responder and keeps the last request body.</summary>
sealed class ScriptedHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    public string? LastBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return await respond(request, cancellationToken);
    }
}

/// <summary>Prints each getTaskResult response body with the client key masked.</summary>
sealed class BodyLoggingHandler(string key) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        if (request.RequestUri?.AbsolutePath.EndsWith("/getTaskResult", StringComparison.Ordinal) == true)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine("getTaskResult raw: " + body.Replace(key, "***"));
        }
        return response;
    }
}

/// <summary>
/// An offline client on a simulated clock: delays return at once and advance the clock, and every delay and API call is
/// logged with its simulated time. The client key is a recognisable fake secret that must never be printed.
/// </summary>
sealed class Timeline : ISystemClock
{
    private const string Key = "SECRET_PROBE_KEY";
    private readonly DateTimeOffset _start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private readonly Dictionary<string, Queue<Func<HttpResponseMessage>>> _scripts = new(StringComparer.Ordinal);
    private readonly StringBuilder _output = new();

    public Timeline()
    {
        UtcNow = _start;
        // Random 0.5 = no jitter, so the printed backoff delays are exactly RetryBaseDelay x 2^(attempt-1).
        Client = new AntiCaptchaClient(Key, new HttpClient(new Handler(this)), null, this, () => 0.5);
    }

    public AntiCaptchaClient Client { get; }

    public DateTimeOffset UtcNow { get; private set; }

    public Task Delay(TimeSpan delay, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Log($"wait {delay.TotalSeconds:0.###} s");
        UtcNow += delay;
        return Task.CompletedTask;
    }

    public void Script(string method, string json) =>
        Enqueue(method, () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });

    public void ScriptFailure(string method, Exception exception) => Enqueue(method, () => throw exception);

    public int Report(string title, string result, bool expected)
    {
        Print("");
        Print("SolveResult: " + result);
        var leaked = _output.ToString().Contains("SECRET", StringComparison.Ordinal);
        Console.WriteLine();
        Console.WriteLine(leaked ? "FAIL: a SECRET string appears in the output above." : "OK: no SECRET string appears in the output above.");
        Console.WriteLine(expected ? "OK: the outcome matches the scenario." : "FAIL: unexpected outcome.");
        Console.WriteLine($"(scenario: {title})");
        return leaked || !expected ? 1 : 0;
    }

    private void Enqueue(string method, Func<HttpResponseMessage> respond)
    {
        if (!_scripts.TryGetValue(method, out var queue))
        {
            _scripts[method] = queue = new Queue<Func<HttpResponseMessage>>();
        }
        queue.Enqueue(respond);
    }

    private void Log(string text) => Print($"  t = {(UtcNow - _start).TotalSeconds,6:0.###} s  {text}");

    private void Print(string line)
    {
        Console.WriteLine(line);
        _output.AppendLine(line);
    }

    private sealed class Handler(Timeline timeline) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var method = request.RequestUri!.Segments[^1];
            var body = JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!;
            var taskId = body["taskId"] is { } id ? $" taskId={id}" : "";
            if (!timeline._scripts.TryGetValue(method, out var queue) || queue.Count == 0)
            {
                throw new InvalidOperationException($"unscripted call to {method}");
            }
            try
            {
                var response = queue.Dequeue()();
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                timeline.Log($"{method}{taskId} -> {content}");
                return response;
            }
            catch (HttpRequestException ex)
            {
                timeline.Log($"{method}{taskId} -> HttpRequestException: {ex.Message}");
                throw;
            }
        }
    }
}
