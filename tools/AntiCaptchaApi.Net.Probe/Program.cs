using System.Diagnostics;
using System.Diagnostics.Metrics;
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
    ["telemetry"] = ("Attach an ActivityListener and a MeterListener, run the offline-solve scenario (task with a proxy), print the activities and measurements; fails if a secret appears.", _ => TelemetryProbe().GetAwaiter().GetResult()),
    ["offline-account"] = ("Run every Phase 5 method (balance, queue, report, push, spending, app stats) against a scripted handler; print masked requests and results.", _ => OfflineAccount().GetAwaiter().GetResult()),
    ["live-balance"] = ("USER ONLY (read-only, free, needs ClientKey): print the account balance.", _ => LiveBalance().GetAwaiter().GetResult()),
    ["live-queue"] = ("USER ONLY (read-only, free): <QueueType|queueId>; print the queue stats (no key sent; ClientKey still required).", a => LiveQueue(a).GetAwaiter().GetResult()),
    ["live-appstats"] = ("USER ONLY (read-only, free, needs ClientKey): <softId> [AppStatsMode]; print the app stats period and chart data.", a => LiveAppStats(a).GetAwaiter().GetResult()),
    ["live-spending"] = ("USER ONLY (read-only, free, needs ClientKey): [date (ISO 8601 or unix s)] [queue]; print the spending records.", a => LiveSpending(a).GetAwaiter().GetResult()),
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
    var timeline = OfflineSolveTimeline();
    var result = await timeline.Client.TrySolveAsync(OfflineSolveTask());
    return timeline.Report("TrySolveAsync(TurnstileTask), default SolveOptions", result.ToString(), result.IsSuccess && result.Attempts == 1);
}

// The offline-solve script (also used by "telemetry"): processing -> transient network failure -> ready.
static Timeline OfflineSolveTimeline()
{
    var timeline = new Timeline();
    timeline.Script("createTask", "{\"errorId\":0,\"taskId\":123456}");
    timeline.Script("getTaskResult", "{\"errorId\":0,\"status\":\"processing\"}");
    timeline.ScriptFailure("getTaskResult", new HttpRequestException("connection reset by peer (simulated)"));
    timeline.Script("getTaskResult", "{\"errorId\":0,\"status\":\"ready\",\"solution\":{\"token\":\"0.SIMULATED_TURNSTILE_TOKEN\"},"
        + "\"cost\":\"0.00200\",\"ip\":\"198.51.100.7\",\"createTime\":1700000000,\"endTime\":1700000016,\"solveCount\":\"1\"}");
    return timeline;
}

static TurnstileTask OfflineSolveTask() => new("https://example.com/login", "SITE_KEY");

static async Task<int> TelemetryProbe()
{
    // A recognisable fake proxy password; together with the Timeline key it must never be printed.
    const string ProxyPassword = "SECRET_PROXY_PASS";
    var original = Console.Out;
    var captured = new StringBuilder();
    Console.SetOut(new TeeWriter(original, captured));
    try
    {
        var started = new List<Activity>();
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == AntiCaptchaDiagnostics.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => { lock (started) { started.Add(activity); } },
        };
        ActivitySource.AddActivityListener(activityListener);

        var measurements = new List<string>();
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == AntiCaptchaDiagnostics.MeterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };
        void Record(Instrument instrument, object value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            var formatted = new List<string>();
            foreach (var tag in tags)
            {
                formatted.Add($"{tag.Key}={tag.Value}");
            }
            lock (measurements)
            {
                measurements.Add($"{instrument.Name} ({instrument.GetType().Name.Replace("`1", "")}<{value.GetType().Name}>) = {value} {instrument.Unit}  {{{string.Join(", ", formatted)}}}");
            }
        }
        meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Record(instrument, value, tags));
        meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Record(instrument, value, tags));
        meterListener.Start();

        Console.WriteLine($"Listening to ActivitySource \"{AntiCaptchaDiagnostics.ActivitySourceName}\" and Meter \"{AntiCaptchaDiagnostics.MeterName}\".");
        Console.WriteLine("Scenario: offline-solve (processing -> transient network failure -> ready), with a proxy (login + password) on the task.");
        Console.WriteLine();
        var timeline = OfflineSolveTimeline();
        var task = OfflineSolveTask() with
        {
            Proxy = new Proxy(ProxyType.Http, "203.0.113.10", 8080) { Login = "proxyUser", Password = ProxyPassword },
            UserAgent = "Mozilla/5.0 (probe)",
        };
        var result = await timeline.Client.TrySolveAsync(task);

        Console.WriteLine();
        Console.WriteLine("Activities (start order; children indented under their parent):");
        foreach (var activity in started)
        {
            var depth = 0;
            for (var parent = activity.Parent; parent is not null && started.Contains(parent); parent = parent.Parent)
            {
                depth++;
            }
            var indent = new string(' ', 2 + 4 * depth);
            var status = activity.Status == ActivityStatusCode.Unset ? "Unset" : $"{activity.Status} \"{activity.StatusDescription}\"";
            Console.WriteLine($"{indent}{activity.OperationName}  kind={activity.Kind}  status={status}");
            foreach (var tag in activity.TagObjects)
            {
                Console.WriteLine($"{indent}    {tag.Key} = {tag.Value}");
            }
            foreach (var e in activity.Events)
            {
                Console.WriteLine($"{indent}    event {e.Name}");
            }
        }
        Console.WriteLine();
        Console.WriteLine("Measurements (record order):");
        foreach (var measurement in measurements)
        {
            Console.WriteLine("  " + measurement);
        }
        Console.WriteLine();
        Console.WriteLine("SolveResult: " + result);

        var text = captured.ToString();
        var leaked = text.Contains(Timeline.Key, StringComparison.Ordinal) || text.Contains(ProxyPassword, StringComparison.Ordinal);
        var expected = result.IsSuccess && result.Attempts == 1
            && started.Count(a => a.OperationName == "AntiCaptcha.Solve") == 1
            && started.Count(a => a.OperationName.StartsWith("AntiCaptcha.", StringComparison.Ordinal)) == 5
            && measurements.Count == 4;
        Console.SetOut(original);
        Console.WriteLine();
        Console.WriteLine(leaked ? "FAIL: the probe key or the proxy password appears in the output above." : "OK: neither the probe key nor the proxy password appears in the output above.");
        Console.WriteLine(expected ? "OK: 1 solve activity + 4 call activities, 4 measurements (no failure), success with 1 attempt." : "FAIL: unexpected telemetry or outcome.");
        return leaked || !expected ? 1 : 0;
    }
    finally
    {
        Console.SetOut(original);
    }
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

static async Task<int> OfflineAccount()
{
    // Recognisable fake secrets: neither may appear in anything printed below (request bodies are masked by the probe).
    const string Key = "SECRET_PROBE_KEY";
    var output = new StringBuilder();
    void Print(string line)
    {
        Console.WriteLine(line);
        output.AppendLine(line);
    }

    var handler = new AccountHandler(Key, Print);
    handler.Script("getBalance", "{\"errorId\":0,\"balance\":12.3456,\"captchaCredits\":85000}");
    handler.Script("getQueueStats", "{\"waiting\":242,\"load\":60.33,\"bid\":\"0.0008600982\",\"speed\":10.77,\"total\":610}");
    handler.Script("reportIncorrectImageCaptcha", "{\"errorId\":0,\"status\":\"success\"}");
    handler.Script("reportIncorrectRecaptcha", "{\"errorId\":0,\"status\":\"success\"}");
    handler.Script("reportCorrectRecaptcha", "{\"errorId\":0,\"status\":\"success\"}");
    handler.Script("pushAntiGateVariable", "{\"errorId\":0,\"status\":\"success\"}");
    handler.Script("pushAntiGateVariable", "{\"errorId\":0,\"status\":\"success\"}");
    handler.Script("getSpendingStats", "{\"errorId\":0,\"data\":[{\"dateFrom\":1672185600,\"dateTill\":1672189199,\"volume\":1899,\"money\":7.495948},"
        + "{\"dateFrom\":1672189200,\"dateTill\":1672192799,\"volume\":2217,\"money\":\"7.861741\"}]}");
    handler.Script("getAppStats", "{\"errorId\":0,\"chartData\":[{\"name\":\"Accepted (paid)\",\"data\":[{\"date\":\"25 January\",\"y\":134587,"
        + "\"beginstamp\":1548374400,\"endstamp\":1548460799}],\"itemname\":\"Captchas\"}],\"fromDate\":\"25 Jan 00:00\",\"toDate\":\"24 Feb 08:09\"}");
    handler.Script("getAppStats", "{\"errorId\":0,\"chartData\":[],\"fromDate\":\"1 Sep 00:00\",\"toDate\":\"30 Sep 12:00\"}");
    handler.Script("getBalance", $"{{\"errorId\":1,\"errorCode\":\"ERROR_KEY_DOES_NOT_EXIST\",\"errorDescription\":\"key {Key} not found\"}}");
    handler.Script("reportIncorrectRecaptcha", "{\"errorId\":16,\"errorCode\":\"ERROR_NO_SUCH_CAPCHA_ID\",\"errorDescription\":\"Task you are requesting does not exist\"}");
    IAntiCaptchaClient client = new AntiCaptchaClient(Key, new HttpClient(handler));

    async Task Step(string title, Func<Task<string>> run)
    {
        Print($"== {title}");
        try
        {
            Print("  result:   " + await run());
        }
        catch (AntiCaptchaException ex)
        {
            Print($"  AntiCaptchaException: {ex.Message} (TaskId = {ex.TaskId?.ToString() ?? "null"}, RawCode = {ex.Error.RawCode})");
        }
        catch (ArgumentException ex)
        {
            Print($"  {ex.GetType().Name} (param '{ex.ParamName}'), nothing sent: {ex.Message}");
        }
    }

    await Step("GetBalanceAsync()", async () => (await client.GetBalanceAsync()).ToString(System.Globalization.CultureInfo.InvariantCulture));
    await Step("GetQueueStatsAsync(QueueType.RecaptchaV2WithoutProxy)", async () => (await client.GetQueueStatsAsync(QueueType.RecaptchaV2WithoutProxy)).ToString());
    foreach (var report in Enum.GetValues<Report>())
    {
        await Step($"ReportAsync(4242, Report.{report})", async () => { await client.ReportAsync(4242, report); return "accepted"; });
    }
    await Step("PushAntiGateVariableAsync(4242, \"my_late_variable\", \"late value\")", async () => { await client.PushAntiGateVariableAsync(4242, "my_late_variable", "late value"); return "accepted"; });
    await Step("PushAntiGateVariableAsync(4242, \"form\", JsonObject)", async () =>
    {
        await client.PushAntiGateVariableAsync(4242, "form", new JsonObject { ["login"] = "user", ["remember"] = true, ["tries"] = 3 });
        return "accepted";
    });
    await Step("GetSpendingStatsAsync(Date = 2022-12-28T02:00+02:00, Queue, SoftId, Ip)", async () =>
    {
        var stats = await client.GetSpendingStatsAsync(new SpendingQuery
        {
            Date = new DateTimeOffset(2022, 12, 28, 2, 0, 0, TimeSpan.FromHours(2)),
            Queue = "Recaptcha Proxyless",
            SoftId = 777,
            Ip = "198.51.100.7",
        });
        return $"{stats.Days.Count} record(s)" + string.Concat(stats.Days.Select(d => "\n            " + d));
    });
    await Step("GetAppStatsAsync(123, AppStatsMode.Money)", async () =>
    {
        var stats = await client.GetAppStatsAsync(123, AppStatsMode.Money);
        return $"FromDate = {stats.FromDate}, ToDate = {stats.ToDate}, ChartData ({stats.ChartData.ValueKind}) = {stats.ChartData.GetRawText()}";
    });
    await Step("GetAppStatsAsync(123) (null mode is omitted)", async () => (await client.GetAppStatsAsync(123)).FromDate);
    await Step("GetBalanceAsync() -> API error echoing the key", async () => (await client.GetBalanceAsync()).ToString(System.Globalization.CultureInfo.InvariantCulture));
    await Step("ReportAsync(4242, Report.IncorrectRecaptcha) -> late report", async () => { await client.ReportAsync(4242, Report.IncorrectRecaptcha); return "accepted"; });
    await Step("ReportAsync(4242, (Report)7)", async () => { await client.ReportAsync(4242, (Report)7); return "accepted"; });
    await Step("PushAntiGateVariableAsync(4242, \"when\", DateTime)", async () => { await client.PushAntiGateVariableAsync(4242, "when", new DateTime(2026, 9, 30)); return "accepted"; });

    var leaked = output.ToString().Contains("SECRET", StringComparison.Ordinal);
    var drained = handler.Unused.Count == 0;
    Console.WriteLine();
    Console.WriteLine(leaked ? "FAIL: a SECRET string appears in the output above." : "OK: no SECRET string appears in the output above.");
    Console.WriteLine(drained ? "OK: every scripted response was used." : "FAIL: unused scripted responses: " + string.Join(", ", handler.Unused));
    return leaked || !drained ? 1 : 0;
}

static async Task<int> LiveBalance()
{
    if (RequireClientKey() is not { } key)
    {
        return 2;
    }
    return await LiveCall("getBalance", async () => $"Balance = {await new AntiCaptchaClient(key).GetBalanceAsync()} USD");
}

static async Task<int> LiveQueue(string[] args)
{
    if (RequireClientKey() is not { } key)
    {
        return 2;
    }
    if (args.Length < 1 || !Enum.TryParse<QueueType>(args[0], ignoreCase: true, out var queue))
    {
        Console.Error.WriteLine("Usage: live-queue <QueueType|queueId>; QueueType is one of: " + string.Join(", ", Enum.GetValues<QueueType>().Select(q => $"{q} ({(int)q})")));
        return 2;
    }
    return await LiveCall("getQueueStats", async () => $"{queue} ({(int)queue}): {await new AntiCaptchaClient(key).GetQueueStatsAsync(queue)}");
}

static async Task<int> LiveAppStats(string[] args)
{
    if (RequireClientKey() is not { } key)
    {
        return 2;
    }
    AppStatsMode? mode = null;
    var softId = 0;
    var validSoftId = args.Length > 0 && int.TryParse(args[0], out softId);
    if (validSoftId && args.Length > 1)
    {
        mode = Enum.TryParse<AppStatsMode>(args[1], ignoreCase: true, out var parsed) ? parsed : null;
    }
    if (!validSoftId || (args.Length > 1 && mode is null))
    {
        Console.Error.WriteLine("Usage: live-appstats <softId> [" + string.Join("|", Enum.GetNames<AppStatsMode>()) + "]");
        return 2;
    }
    return await LiveCall("getAppStats", async () =>
    {
        var stats = await new AntiCaptchaClient(key).GetAppStatsAsync(softId, mode);
        return $"FromDate = {stats.FromDate}, ToDate = {stats.ToDate}\nChartData ({stats.ChartData.ValueKind}):\n{stats.ChartData.GetRawText()}";
    });
}

static async Task<int> LiveSpending(string[] args)
{
    if (RequireClientKey() is not { } key)
    {
        return 2;
    }
    DateTimeOffset? date = null;
    if (args.Length > 0)
    {
        if (long.TryParse(args[0], out var seconds))
        {
            date = DateTimeOffset.FromUnixTimeSeconds(seconds);
        }
        else if (DateTimeOffset.TryParse(args[0], System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed))
        {
            date = parsed;
        }
        else
        {
            Console.Error.WriteLine("Usage: live-spending [date (ISO 8601 or unix seconds)] [queue name, e.g. \"Recaptcha Proxyless\"]");
            return 2;
        }
    }
    var query = new SpendingQuery { Date = date, Queue = args.Length > 1 ? args[1] : null };
    return await LiveCall("getSpendingStats", async () =>
    {
        var stats = await new AntiCaptchaClient(key).GetSpendingStatsAsync(query);
        return $"{query}\n{stats.Days.Count} record(s); total volume = {stats.Days.Sum(d => d.Volume)}, total money = {stats.Days.Sum(d => d.Money)} USD"
            + string.Concat(stats.Days.Select(d => "\n  " + d));
    });
}

// Runs one read-only live call and prints its result, or the (already redacted) error.
static async Task<int> LiveCall(string method, Func<Task<string>> call)
{
    try
    {
        Console.WriteLine(await call());
        return 0;
    }
    catch (AntiCaptchaException ex)
    {
        Console.Error.WriteLine($"{method} failed: {ex.Message} (RawCode = {ex.Error.RawCode}, retryable = {ex.Error.IsRetryable})");
        return 1;
    }
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

/// <summary>
/// Answers each API method from its own queue of scripted JSON bodies and prints every request body with the client key
/// masked (the masking is the probe's; the library itself only redacts errors).
/// </summary>
sealed class AccountHandler(string key, Action<string> print) : HttpMessageHandler
{
    private readonly Dictionary<string, Queue<string>> _scripts = new(StringComparer.Ordinal);

    public IReadOnlyList<string> Unused => _scripts.Where(s => s.Value.Count > 0).Select(s => $"{s.Key} x{s.Value.Count}").ToList();

    public void Script(string method, string json)
    {
        if (!_scripts.TryGetValue(method, out var queue))
        {
            _scripts[method] = queue = new Queue<string>();
        }
        queue.Enqueue(json);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var method = request.RequestUri!.Segments[^1];
        var body = request.Content is null ? "(none)" : await request.Content.ReadAsStringAsync(cancellationToken);
        print($"  request:  POST {method} {body.Replace(key, "***")}");
        if (!_scripts.TryGetValue(method, out var queue) || queue.Count == 0)
        {
            throw new InvalidOperationException($"unscripted call to {method}");
        }
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(queue.Dequeue(), Encoding.UTF8, "application/json") };
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

/// <summary>Writes to the console and keeps a copy, so that a command can check its whole output for secrets.</summary>
sealed class TeeWriter(TextWriter inner, StringBuilder copy) : TextWriter
{
    public override Encoding Encoding => inner.Encoding;

    public override void Write(char value)
    {
        inner.Write(value);
        copy.Append(value);
    }

    public override void Write(string? value)
    {
        inner.Write(value);
        copy.Append(value);
    }
}

/// <summary>
/// An offline client on a simulated clock: delays return at once and advance the clock, and every delay and API call is
/// logged with its simulated time. The client key is a recognisable fake secret that must never be printed.
/// </summary>
sealed class Timeline : ISystemClock
{
    public const string Key = "SECRET_PROBE_KEY";
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
