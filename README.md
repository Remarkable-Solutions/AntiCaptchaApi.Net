# AntiCaptchaApi.Net [![NuGet Version](https://img.shields.io/nuget/v/AntiCaptchaApi.Net.svg)](https://www.nuget.org/packages/AntiCaptchaApi.Net/)

A .NET library (netstandard2.1, net8.0, net9.0) that implements the [anti-captcha.com API](https://anti-captcha.com/apidoc).

You create a request object for a captcha type, call `SolveCaptchaAsync`, and get back the solution or an error description. The library validates requests before any HTTP call, polls for the result and handles the JSON for you.

## Features

*   Supported task types:
    *   ImageToText, ImageToCoordinates
    *   reCAPTCHA v2, v2 Enterprise, v3, v3 Enterprise
    *   FunCaptcha (Arkose Labs)
    *   GeeTest v3, v4
    *   Cloudflare Turnstile
    *   Amazon WAF
    *   Prosopo
    *   Friendly Captcha
    *   AntiGate (custom browser templates)
*   Proxy and proxyless variants for the types that anti-captcha offers both for.
*   Async API with `CancellationToken` support.
*   Works with or without dependency injection.
*   Balance, queue, spending and app statistics, task reporting, and AntiGate variables.

## Installation

```bash
dotnet add package AntiCaptchaApi.Net
```

The package depends on `Microsoft.Extensions.Http` (≥ 8.0.0) and `Newtonsoft.Json` (≥ 13.0.1). These are minimum versions, so your application can use newer ones.

## Quick start

```csharp
using AntiCaptchaApi.Net;
using AntiCaptchaApi.Net.Enums;
using AntiCaptchaApi.Net.Requests;

var client = new AnticaptchaClient("YOUR_API_KEY");

var result = await client.SolveCaptchaAsync(new RecaptchaV2ProxylessRequest
{
    WebsiteUrl = "https://example.com/login",
    WebsiteKey = "6Le-wvkSAAAAAPBMRTvw0Q4Muexq9bi0DJwx_mJ-",
});

if (result.Status == TaskStatusType.Ready)
    Console.WriteLine(result.Solution.GRecaptchaResponse);
else
    Console.WriteLine($"{result.ErrorCode}: {result.ErrorDescription}");
```

Errors are returned as data, not thrown. Check `Status` (`Ready`, `Processing` or `Error`) and read `ErrorCode` / `ErrorDescription` when the solve did not succeed.

## Creating a client

Both ways are supported. Use the one that fits your application.

**With dependency injection** (recommended for ASP.NET Core and hosted apps). `AddAnticaptcha` registers `IAnticaptchaClient` and a named `HttpClient` from `IHttpClientFactory` whose timeout is `ClientConfig.MaxHttpRequestTimeMs`.

```csharp
using AntiCaptchaApi.Net;
using Microsoft.Extensions.DependencyInjection;

services.AddAnticaptcha("YOUR_API_KEY", options =>
{
    options.MaxWaitForTaskResultTimeMs = 180000;
});

public class MyService
{
    private readonly IAnticaptchaClient _anticaptchaClient;

    public MyService(IAnticaptchaClient anticaptchaClient)
    {
        _anticaptchaClient = anticaptchaClient;
    }
}
```

**With the constructor.** Without an `HttpClient`, the client creates one with `MaxHttpRequestTimeMs` as its timeout. If you pass your own `HttpClient`, you configure it yourself. Reuse one client instance instead of creating one per solve.

```csharp
using AntiCaptchaApi.Net;
using AntiCaptchaApi.Net.Models;

var client = new AnticaptchaClient("YOUR_API_KEY");

var config = new ClientConfig { MaxWaitForTaskResultTimeMs = 120000 };
var clientWithConfig = new AnticaptchaClient("YOUR_API_KEY", config);

var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var clientWithHttpClient = new AnticaptchaClient("YOUR_API_KEY", config, httpClient);
```

## Configuration

`ClientConfig` controls timeouts, polling and retries:

| Property | Default | Meaning |
|---|---|---|
| `MaxWaitForTaskResultTimeMs` | 120000 | How long `SolveCaptchaAsync` / `WaitForTaskResultAsync` polls before it gives up with a `RequestTimeout` error. |
| `DelayTimeBetweenCheckingTaskResultMs` | 1000 | Delay between two `getTaskResult` polls. |
| `MaxHttpRequestTimeMs` | 60000 | Timeout of a single HTTP request (the DI client, or the client the constructor creates). |
| `SolveAsyncRetries` | 1 | Total number of attempts `SolveCaptchaAsync` makes when the task ends with an error. `2` means one retry. |

## Examples

### ImageToText

```csharp
var result = await client.SolveCaptchaAsync(new ImageToTextRequest
{
    FilePath = "captcha.png", // or set BodyBase64
    // Optional: Phrase, Case, Numeric, Math, MinLength, MaxLength, Comment
});

if (result.Status == TaskStatusType.Ready)
    Console.WriteLine(result.Solution.Text);
```

### reCAPTCHA v3

```csharp
var result = await client.SolveCaptchaAsync(new RecaptchaV3Request
{
    WebsiteUrl = "https://example.com/",
    WebsiteKey = "6LeYOUR_SITE_KEY",
    MinScore = 0.7m,        // 0.3 (default), 0.7 or 0.9
    PageAction = "login",   // optional
});
```

`IsEnterprise` is optional: leaving it unset means a non-Enterprise site. Use `RecaptchaV3EnterpriseRequest` for Enterprise sites. `MinScore = 0.5` is still accepted for backward compatibility, but it is not one of anti-captcha's score queues, so avoid it. It will be removed in 3.0.

### Amazon WAF

```csharp
using AntiCaptchaApi.Net.Requests;

var result = await client.SolveCaptchaAsync(new AmazonWafProxylessRequest
{
    WebsiteUrl = "https://example.com/protected",
    WebsiteKey = "AQIDAHjcYu/GjX+QlghicBgQ/7bFaQZ+m5FKCMDnO+vTbNg96AF5H1K", // "key" from the page
    Iv = "CgAHbCe2GgAAAAAj",                                           // "iv" from the page
    Context = "9BUgmlm48F92WUoqv97a49ZuEJJ50TCk9MVr3C7WMtQ0X6flVbufM4n8m", // "context" from the page
    // Optional: CaptchaScript, ChallengeScript
});

if (result.Status == TaskStatusType.Ready)
    Console.WriteLine(result.Solution.Token); // value for the aws-waf-token cookie
```

Use `AmazonWafRequest` with `ProxyConfig` to solve through your own proxy.

### AntiGate

[AntiGate](https://anti-captcha.com/apidoc/task-types/AntiGateTask) runs a template in a real browser and returns cookies, storage and the browser fingerprint. The variables must match what the template expects.

```csharp
using AntiCaptchaApi.Net.Models;
using AntiCaptchaApi.Net.Requests;
using Newtonsoft.Json.Linq;

var result = await client.SolveCaptchaAsync(new AntiGateRequest
{
    WebsiteUrl = "https://example.com/",
    TemplateName = "YOUR_TEMPLATE_NAME", // a template from the AntiGate catalog
    Variables = JObject.FromObject(new { css_selector = "#content" }),
    DomainsOfInterest = new List<string> { "auth.example.com" }, // optional
    ProxyConfig = new ProxyConfig { ProxyAddress = "203.0.113.10", ProxyPort = 3128 }, // optional, HTTPS proxy by IP
});

if (result.Status == TaskStatusType.Ready)
{
    Console.WriteLine(result.Solution.Url);
    Console.WriteLine(result.Solution.Cookies);
    Console.WriteLine(result.Solution.Fingerprint);
}
```

The solution also exposes `Domain`, `LocalStorage`, `SessionStorage`, `HTMLsInBase64`, `Screenshots`, `RequestHeaders` and `ResponseHeaders` when the template returns them.

Some templates wait for a value that you only know after the task has started, for example a code sent by email. Create the task, push the variable, then wait:

```csharp
var created = await client.CreateCaptchaTaskAsync(antiGateRequest);
if (created.IsErrorResponse || !created.TaskId.HasValue)
    return;

// Later, when the value is known:
await client.PushAntiGateVariableAsync(created.TaskId.Value, "verification_code", "123456");

var solved = await client.WaitForTaskResultAsync<AntiGateSolution>(created.TaskId.Value);
```

### Two-step solving

`SolveCaptchaAsync` creates the task and polls for the result. You can also do both steps yourself:

```csharp
var created = await client.CreateCaptchaTaskAsync(request);
if (created.IsErrorResponse || !created.TaskId.HasValue)
{
    Console.WriteLine($"Failed to create task: {created.ErrorCode} - {created.ErrorDescription}");
    return;
}

var result = await client.WaitForTaskResultAsync<ImageToTextSolution>(created.TaskId.Value);
```

## Behavior worth knowing

*   **Validation first.** Missing or invalid values (URL, site key, proxy, port 1-65535, ...) are reported with `ErrorCode = "BadRequest"` before any HTTP call, so nothing is charged. `SolveCaptchaAsync` does not retry them.
*   **Resilient polling.** A created task is paid for, so a single network error while polling does not abandon it. Transport errors (`HttpError`, `JsonError`, `UnexpectedError`, and an HTTP timeout) are retried on the next poll. The wait gives up only after 3 consecutive failed polls and then returns the last error. API errors (for example `ERROR_CAPTCHA_UNSOLVABLE`) return immediately. Cancelling your `CancellationToken` stops polling.
*   **Redacted `RawPayload`.** Every response has `RawPayload` (the request JSON) and `RawResponse` (the API's JSON) for debugging. In `RawPayload` the `clientKey` and every `proxyPassword` are replaced with `"***"`, so it is safe to log. The request sent to anti-captcha is unchanged.
*   **Thread safety.** One client instance can run many solves in parallel.

## API client methods

The methods on `IAnticaptchaClient`:

*   `GetBalanceAsync(...)`: [docs](https://anti-captcha.com/apidoc/methods/getBalance)
    ```csharp
    var balance = await client.GetBalanceAsync();
    Console.WriteLine(balance.Balance);
    ```
*   `SolveCaptchaAsync<TSolution>(request, ...)`: `CreateCaptchaTaskAsync` plus `WaitForTaskResultAsync`, with retries per `SolveAsyncRetries`.
*   `CreateCaptchaTaskAsync<T>(request, ...)`: [docs](https://anti-captcha.com/apidoc/methods/createTask)
*   `GetTaskResultAsync<TSolution>(taskId, cancellationToken)`: [docs](https://anti-captcha.com/apidoc/methods/getTaskResult). Checks once; the task may still be processing.
    ```csharp
    var result = await client.GetTaskResultAsync<ImageToTextSolution>(taskId, CancellationToken.None);
    ```
*   `WaitForTaskResultAsync<TSolution>(taskId, ...)`: polls `getTaskResult` until the task is ready, fails or times out.
*   `GetQueueStatsAsync(queueType, ...)`: [docs](https://anti-captcha.com/apidoc/methods/getQueueStats)
    ```csharp
    var stats = await client.GetQueueStatsAsync(QueueType.RecaptchaB2WithoutProxy);
    ```
*   `GetSpendingStatsAsync(...)`: [docs](https://anti-captcha.com/apidoc/methods/getSpendingStats)
    ```csharp
    var spending = await client.GetSpendingStatsAsync(queue: "ImageToTextTask");
    ```
*   `GetAppStatsAsync(softId, ...)`: [docs](https://anti-captcha.com/apidoc/methods/getAppStats)
    ```csharp
    var appStats = await client.GetAppStatsAsync(123, AppStatsMode.Errors); // your SoftID
    ```
*   `ReportTaskOutcomeAsync(taskId, outcome, ...)`: reports a wrong (or correct) solution; `outcome` selects the API method ([docs](https://anti-captcha.com/apidoc/methods/reportIncorrectImageCaptcha)).
    ```csharp
    await client.ReportTaskOutcomeAsync(taskId, ReportOutcome.IncorrectRecaptcha);
    ```
*   `PushAntiGateVariableAsync(taskId, name, value, ...)`: [docs](https://anti-captcha.com/apidoc/methods/pushAntiGateVariable). See the AntiGate example.
*   `SolveProsopoAsync`, `SolveProsopoProxylessAsync`, `SolveFriendlyCaptchaAsync`, `SolveFriendlyCaptchaProxylessAsync`: typed shortcuts for `SolveCaptchaAsync`.

## Deprecations

These still work in 2.x and will be removed in 3.0:

*   The hCaptcha value of `ReportOutcome`: the library has no hCaptcha request type, so there is nothing to report.
*   `RecaptchaV3Request.MinScore = 0.5`: not an anti-captcha score queue. Use 0.3, 0.7 or 0.9.

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## Contributing

1.  Clone the repository: `git clone https://github.com/RemarkableSolutionsAdmin/AntiCaptchaApi.Net.git`
2.  Create a new branch for your feature or bug fix.
3.  Make your changes and add tests.
4.  Run the offline unit tests: `dotnet test AntiCaptchaApi.Net.sln --filter "FullyQualifiedName~UnitTests"`. The tests under `IntegrationTests/` call the live, paid API and need your own key.
5.  Push your branch to your fork and create a pull request.

## License

MIT - see the [LICENSE](LICENSE) file for details.

## Credits

Copyright (c) 2022-2025 Remarkable Solutions
