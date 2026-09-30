# AntiCaptchaApi.Net

[![NuGet](https://img.shields.io/nuget/vpre/AntiCaptchaApi.Net.svg)](https://www.nuget.org/packages/AntiCaptchaApi.Net/)
[![NuGet downloads](https://img.shields.io/nuget/dt/AntiCaptchaApi.Net.svg)](https://www.nuget.org/packages/AntiCaptchaApi.Net/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A .NET client for the [anti-captcha.com API](https://anti-captcha.com/apidoc). Describe the captcha with one record and call `SolveAsync`. The library validates the task before it spends money, polls for the result, retries what is worth retrying, and never abandons a task you have already paid for.

- Targets netstandard2.0 (.NET Framework 4.6.2+ and .NET Core) and net8.0 (.NET 8, 9, 10).
- Depends only on System.Text.Json and System.Diagnostics.DiagnosticSource, and only on netstandard2.0. On net8.0 it has no dependencies.
- 13 task types: image to text, image to coordinates, reCAPTCHA v2 / v2 Enterprise / v3, Cloudflare Turnstile, FunCaptcha, GeeTest v3 / v4, Amazon WAF, Prosopo, Friendly Captcha and AntiGate.
- Traces and metrics through `ActivitySource` and `Meter`, ready for OpenTelemetry.

> Upgrading from 2.x? Read [MIGRATION.md](MIGRATION.md). Every 2.x type and method has a row there.

## Install

```bash
dotnet add package AntiCaptchaApi.Net --prerelease
```

## Quick start

```csharp
using AntiCaptchaApi.Net;
using AntiCaptchaApi.Net.Tasks;

var client = new AntiCaptchaClient("YOUR_API_KEY");

var solution = await client.SolveAsync(
    new TurnstileTask("https://example.com/login", "0x4AAAAAAAA-your-site-key"));

Console.WriteLine(solution.Token);
```

`SolveAsync` returns the solution or throws `AntiCaptchaException`. Create one client per API key and reuse it; it is thread-safe.

### Without exceptions

`TrySolveAsync` returns a `SolveResult` instead of throwing on API and network errors:

```csharp
var result = await client.TrySolveAsync(new TurnstileTask("https://example.com/login", "0x4AAAAAAAA-your-site-key"));

if (result.IsSuccess)
{
    Console.WriteLine($"{result.Solution!.Token} (task {result.TaskId}, {result.Cost} USD, {result.Attempts} attempt(s))");
}
else
{
    Console.WriteLine($"Failed: {result.Error!.Code} - {result.Error.Description}");
}
```

It still throws `AntiCaptchaValidationException` for an invalid task and `OperationCanceledException` when you cancel. Both are thrown before any money is spent, or because you asked for it.

## Task types

The examples assume `using AntiCaptchaApi.Net;` and `using AntiCaptchaApi.Net.Tasks;` (plus `System.Text.Json.Nodes` for AntiGate), and a `client` created as in the quick start.

Tasks live in `AntiCaptchaApi.Net.Tasks`. Required values are constructor parameters. Optional values are `init` properties that use anti-captcha's defaults when you leave them out. Each record links its anti-captcha docs page in its XML docs.

### Image to text

```csharp
var task = ImageToTextTask.FromFile("captcha.png") with
{
    Numeric = NumericOption.NumbersOnly,
    MinLength = 4,
    MaxLength = 6,
};
var text = (await client.SolveAsync(task)).Text;
```

`FromFile` throws `FileNotFoundException` when the file is missing. Use `ImageToTextTask.FromBytes(bytes)` or `new ImageToTextTask(base64)` when the image is already in memory.

### Image to coordinates

```csharp
var task = ImageToCoordinatesTask.FromFile("cats.jpeg") with
{
    Comment = "Select all cats",
    Mode = ImageToCoordinatesMode.Rectangles,
};
foreach (var shape in (await client.SolveAsync(task)).Coordinates)
{
    Console.WriteLine(string.Join(", ", shape)); // x1, y1, x2, y2 for a rectangle
}
```

### reCAPTCHA v2

```csharp
var solution = await client.SolveAsync(new RecaptchaV2Task("https://example.com/signup", "6Lc_your-site-key")
{
    IsInvisible = true, // optional
});
Console.WriteLine(solution.GRecaptchaResponse);
```

### reCAPTCHA v2 Enterprise

```csharp
var solution = await client.SolveAsync(new RecaptchaV2EnterpriseTask("https://example.com/signup", "6Lc_your-site-key")
{
    EnterprisePayload = new Dictionary<string, string> { ["s"] = "value-from-grecaptcha.enterprise.render" },
});
Console.WriteLine(solution.GRecaptchaResponse);
```

### reCAPTCHA v3

```csharp
var solution = await client.SolveAsync(new RecaptchaV3Task("https://example.com/login", "6Lc_your-site-key")
{
    MinScore = MinScore.Score07, // Score03 (default), Score07 or Score09
    PageAction = "login",
    IsEnterprise = false,        // true for reCAPTCHA v3 Enterprise
});
Console.WriteLine(solution.GRecaptchaResponse);
```

### Cloudflare Turnstile

```csharp
var solution = await client.SolveAsync(new TurnstileTask("https://example.com/login", "0x4AAAAAAAA-your-site-key")
{
    Action = "login", // optional; also CData and ChlPageData for challenge pages
});
Console.WriteLine(solution.Token);
```

### FunCaptcha (Arkose Labs)

```csharp
var solution = await client.SolveAsync(new FunCaptchaTask("https://example.com/signup", "DF9C4D87-CB7B-4062-9FEB-BADB6ADA61E6")
{
    ApiJsSubdomain = "client-api.arkoselabs.com", // optional
});
Console.WriteLine(solution.Token);
```

### GeeTest v3

```csharp
var solution = await client.SolveAsync(new GeeTestV3Task("https://example.com/login", "gt-from-the-page", "fresh-challenge"));
Console.WriteLine($"{solution.Challenge} {solution.Validate} {solution.Seccode}");
```

### GeeTest v4

```csharp
var solution = await client.SolveAsync(new GeeTestV4Task("https://example.com/login", "captcha-id-from-the-page")
{
    InitParameters = new Dictionary<string, string> { ["riskType"] = "slide" }, // optional
});
Console.WriteLine($"{solution.CaptchaId} {solution.LotNumber} {solution.PassToken} {solution.GenTime} {solution.CaptchaOutput}");
```

### Amazon WAF

```csharp
var solution = await client.SolveAsync(new AmazonWafTask(
    "https://example.com/protected",
    "AQIDAHjcYu/GjX+QlghicBgQ/7bFaQZ+m5FKCMDnO+vTbNg96AF5H1K", // "key" from the page
    "CgAHbCe2GgAAAAAj",                                        // "iv" from the page
    "9BUgmlm48F92WUoqv97a49ZuEJJ50TCk9MVr3C7WMtQ0X6flVbufM4n8m")); // "context" from the page
Console.WriteLine(solution.Token); // the aws-waf-token cookie value
```

### Prosopo

```csharp
var solution = await client.SolveAsync(new ProsopoTask("https://example.com/login", "your-prosopo-site-key"));
Console.WriteLine(solution.Token);
```

### Friendly Captcha

```csharp
var solution = await client.SolveAsync(new FriendlyCaptchaTask("https://example.com/login", "your-friendly-captcha-site-key"));
Console.WriteLine(solution.Token);
```

### AntiGate (browser templates)

[AntiGate](https://anti-captcha.com/apidoc/task-types/AntiGateTask) runs a template in a real browser and returns cookies, storage and the browser fingerprint. The variables must match the template.

```csharp
var solution = await client.SolveAsync(new AntiGateTask(
    "https://example.com/",
    "YOUR_TEMPLATE_NAME",
    new JsonObject { ["css_selector"] = "#content" })
{
    DomainsOfInterest = new[] { "auth.example.com" }, // optional
});
Console.WriteLine(solution.Cookies?.ToJsonString());
```

The cookie, storage, fingerprint, screenshot and header members are `JsonNode?`, because the API sends an empty map as `[]`.

When a template waits for a value you learn later (for example a code sent by email), create the task, push the variable and then wait:

```csharp
var taskId = await client.CreateTaskAsync(antiGateTask);

// Later, when the value is known:
await client.PushAntiGateVariableAsync(taskId, "verification_code", "123456");

var solution = await client.WaitForResultAsync<AntiGateSolution>(taskId);
```

## Proxies

Set `Proxy` to have anti-captcha solve through your proxy. This selects the proxy variant of the task type (for example `RecaptchaV2Task` instead of `RecaptchaV2TaskProxyless`).

```csharp
var proxy = new Proxy(ProxyType.Http, "203.0.113.10", 3128)
{
    Login = "user",
    Password = "secret",
};

var solution = await client.SolveAsync(new RecaptchaV2Task("https://example.com/signup", "6Lc_your-site-key")
{
    Proxy = proxy,
    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0 Safari/537.36",
});
```

- `ProxyType` is `Http`, `Socks4` or `Socks5`. The port must be 1–65535.
- With a proxy, `UserAgent` is required for RecaptchaV2, RecaptchaV2Enterprise, Turnstile, FunCaptcha, GeeTest v3/v4, Prosopo and Friendly Captcha. Use the user agent of the browser that will submit the token.
- Image tasks and `RecaptchaV3Task` have no proxy variant. Setting `Proxy` on them is a validation error, not silently ignored.
- `Proxy.ToString()` prints `Password = ***`, and the password never appears in exceptions or telemetry.

## Other API methods

```csharp
decimal balance = await client.GetBalanceAsync();

QueueStats queue = await client.GetQueueStatsAsync(QueueType.RecaptchaV2WithoutProxy);
Console.WriteLine($"{queue.Waiting} workers waiting, load {queue.Load} %, bid {queue.Bid} USD");

SpendingStats spending = await client.GetSpendingStatsAsync(new SpendingQuery { Date = DateTimeOffset.UtcNow.AddDays(-1) });
foreach (var period in spending.Days)
{
    Console.WriteLine($"{period.DateFrom:u}: {period.Volume} tasks, {period.Money} USD");
}

// The site rejected a token: tell anti-captcha.
await client.ReportAsync(taskId, Report.IncorrectRecaptcha);
```

- `CreateTaskAsync` → task id; `GetTaskResultAsync<TSolution>` → one poll (`TaskStatus<TSolution>`); `WaitForResultAsync<TSolution>` → polls an existing task until it is ready.
- `ReportAsync` accepts `Report.IncorrectImageCaptcha`, `Report.IncorrectRecaptcha` and `Report.CorrectRecaptcha`.
- `GetAppStatsAsync` returns the statistics of an application you own.

## Configuration

Per call, pass `SolveOptions`. For every call of a client, set `AntiCaptchaClientOptions.DefaultSolveOptions`.

```csharp
var solution = await client.SolveAsync(task, new SolveOptions
{
    Timeout = TimeSpan.FromMinutes(5),
    MaxAttempts = 2,
    LanguagePool = "rn",
});
```

| `SolveOptions` | Default | Meaning |
|---|---|---|
| `Timeout` | 180 s | How long one attempt waits for its task, from `createTask`. |
| `PollInterval` | 3 s | Delay between `getTaskResult` polls. |
| `FirstPollDelay` | `null` | Delay before the first poll. `null` means 3 s for image tasks and 10 s for the others. |
| `MaxAttempts` | 3 | Total attempts, counting the first one. A new attempt creates a new task. |
| `MaxConsecutivePollErrors` | 5 | Transient poll errors in a row that are tolerated on the same task. |
| `RetryBaseDelay` / `RetryMaxDelay` | 2 s / 30 s | Backoff between attempts: `min(max, base × 2^(attempt−1))`, ±20 % jitter. |
| `LanguagePool` | `null` | anti-captcha's `languagePool` (for example `"en"` or `"rn"`). |
| `CallbackUrl` | `null` | anti-captcha's `callbackUrl`. |

| `AntiCaptchaClientOptions` | Default | Meaning |
|---|---|---|
| `BaseAddress` | `https://api.anti-captcha.com/` | API root. |
| `RequestTimeout` | 30 s | Timeout of each HTTP call. |
| `DefaultSolveOptions` | `new SolveOptions()` | Used when a call passes no `SolveOptions`. |

Invalid values (a zero or negative time, `MaxAttempts < 1`) throw `ArgumentOutOfRangeException` before any request.

### Using IHttpClientFactory

The core package has no dependency injection code. A separate `AntiCaptchaApi.Net.DependencyInjection` package is planned. Until then, pass an `HttpClient` from the factory. The client never disposes it and never changes its `Timeout` or `BaseAddress`; `AntiCaptchaClientOptions` applies instead.

```csharp
services.AddHttpClient("anticaptcha");
services.AddTransient<IAntiCaptchaClient>(sp => new AntiCaptchaClient(
    Environment.GetEnvironmentVariable("ANTICAPTCHA_API_KEY")!,
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("anticaptcha"),
    new AntiCaptchaClientOptions { RequestTimeout = TimeSpan.FromSeconds(20) }));
```

`new AntiCaptchaClient(apiKey)` without an `HttpClient` shares one pooled handler across all instances, so it does not exhaust sockets either.

## Errors and retries

- **Invalid task:** `AntiCaptchaValidationException`, thrown before any request. `Errors` lists every problem as `ValidationError(Field, Message)`.
- **API or network error:** `AntiCaptchaException`. Its `Error` has the parsed `Code` (`AntiCaptchaErrorCode`), the API's `RawCode` (such as `ERROR_ZERO_BALANCE`), a `Description` and the `ErrorId`. The exception also carries `TaskId` (when a task was created) and `Attempts`. From `TrySolveAsync` you get the same `AntiCaptchaError` in `SolveResult.Error`.

```csharp
try
{
    var solution = await client.SolveAsync(new TurnstileTask(url, siteKey));
}
catch (AntiCaptchaValidationException ex)
{
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"{error.Field}: {error.Message}");
    }
}
catch (AntiCaptchaException ex) when (ex.Error.Code == AntiCaptchaErrorCode.ZeroBalance)
{
    Console.WriteLine("Top up your anti-captcha balance.");
}
```

How the solve pipeline treats an error (`AntiCaptchaError.IsTransient` / `IsRetryable`):

| Kind | Errors | What happens |
|---|---|---|
| Transient | network failure, HTTP 408 / 429 / 5xx, `HttpTimeout`, `InvalidResponse` while polling | The same task is polled again. The task is already paid for, so it is not abandoned. After `MaxConsecutivePollErrors` in a row, the solve fails with that error. |
| Retryable | `NoSlotAvailable`, `CaptchaUnsolvable`, `ProxyConnectionRefused`, transient HTTP errors on `createTask` | A new attempt after the backoff, up to `MaxAttempts`. |
| Fatal | everything else, for example `KeyDoesNotExist`, `ZeroBalance`, `IpBlocked`, `SolveTimeout`, other 4xx, `Unknown` | The solve fails at once. `SolveTimeout` is not retried because the timed-out task may still be charged. |

Client-side codes: `HttpError` (network failure or non-2xx status), `HttpTimeout`, `InvalidResponse` (unparseable JSON, or a ready task without a usable solution) and `SolveTimeout`. A code the library does not know is `Unknown`, with the original string in `RawCode`.

## Cancellation and timeouts

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
try
{
    var solution = await client.SolveAsync(task, ct: cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Cancelled by us.");
}
catch (AntiCaptchaException ex) when (ex.Error.Code == AntiCaptchaErrorCode.SolveTimeout)
{
    Console.WriteLine($"Task {ex.TaskId} was not ready in time.");
}
```

- **Your `CancellationToken`** always ends the call with `OperationCanceledException`. It is never wrapped and never reported as an error.
- **`AntiCaptchaClientOptions.RequestTimeout`** limits one HTTP call. When it expires, the error is `HttpTimeout`, which is transient while polling.
- **`SolveOptions.Timeout`** limits one attempt. When it expires, the error is `SolveTimeout`.

## Observability

The library emits traces and metrics under the names in `AntiCaptchaDiagnostics`. Both are `"AntiCaptcha"`.

- Activities: one `AntiCaptcha.Solve` per solve, with the tags `anticaptcha.task.type`, `anticaptcha.task.id`, `anticaptcha.attempts` and, on failure, `anticaptcha.error.code` and an Error status. There is one child `AntiCaptcha.<apiMethod>` activity per API call, for example `AntiCaptcha.getTaskResult`.
- Metrics: `anticaptcha.tasks.created`, `anticaptcha.solves` (`outcome` = `success` or `failure`), `anticaptcha.failures` (by `error.code`), `anticaptcha.solve.duration` (seconds) and `anticaptcha.cost` (USD). All are tagged with `task.type`.
- Telemetry never contains the API key, proxy details, website data or solution content.

With OpenTelemetry (the `OpenTelemetry` and `OpenTelemetry.Exporter.Console` packages):

```csharp
using var tracing = Sdk.CreateTracerProviderBuilder()
    .AddSource(AntiCaptchaDiagnostics.ActivitySourceName)
    .AddConsoleExporter()
    .Build();

using var metrics = Sdk.CreateMeterProviderBuilder()
    .AddMeter(AntiCaptchaDiagnostics.MeterName)
    .AddConsoleExporter()
    .Build();
```

In an ASP.NET Core or generic host, call the same `AddSource` and `AddMeter` inside `services.AddOpenTelemetry().WithTracing(...)` and `.WithMetrics(...)`.

## Troubleshooting

- **`AntiCaptchaValidationException`:** the task was rejected locally and nothing was sent. `Errors` names each field. Common causes are a relative or non-http(s) `WebsiteUrl`, an empty key, a proxy without `UserAgent`, and an image passed with a `data:` prefix.
- **`KeyDoesNotExist` / `ZeroBalance` / `IpNotAllowed`:** check the key, the balance and the IP allow-list in your anti-captcha account. These errors are not retried.
- **`SolveTimeout`:** the task was not solved in `SolveOptions.Timeout`. Hard reCAPTCHAs can take minutes, so raise `Timeout` rather than `MaxAttempts`.
- **`HttpTimeout` or `HttpError` from account methods:** your network could not reach `api.anti-captcha.com` within `RequestTimeout`. While polling, these errors are retried automatically.
- **Proxy errors (`ProxyConnectionRefused`, `ProxyBanned`, `ProxyTransparent`, ...):** anti-captcha's workers could not use your proxy. It must be reachable from the internet, and it must not be a transparent proxy.
- **The website rejects a token:** send the token soon after you get it, from the same IP (use `Proxy`) and with the same user agent. Report bad tokens with `ReportAsync(taskId, Report.IncorrectRecaptcha)`. The task id is in `SolveResult.TaskId` or `AntiCaptchaException.TaskId`.
- **Seeing what happens:** subscribe to the `AntiCaptcha` activity source (see [Observability](#observability)). Each API call is an activity with its error code.

## Links

- [MIGRATION.md](MIGRATION.md): upgrading from 2.x.
- [CHANGELOG.md](CHANGELOG.md): release notes.
- [anti-captcha.com API docs](https://anti-captcha.com/apidoc).

## Contributing

1. Clone `https://github.com/Remarkable-Solutions/AntiCaptchaApi.Net` and create a branch.
2. Make your change and add tests.
3. Run the offline tests: `dotnet test AntiCaptchaApi.Net.sln --filter "FullyQualifiedName~UnitTests"`.
4. The live tests (`--filter "Category=Live"`) call the paid API. They are skipped unless the `ClientKey` environment variable is set; the proxy variants also need `ProxyAddress`, `ProxyPort`, `ProxyLogin` and `ProxyPassword`.
5. Open a pull request.

## License

MIT. See [LICENSE](LICENSE).

Copyright (c) 2022-2026 Remarkable Solutions
