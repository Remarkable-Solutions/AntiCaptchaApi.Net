# Migrating from 2.x to 3.0

3.0 is a rewrite of `AntiCaptchaApi.Net`. There is one record per captcha type and one call to solve it. Errors are exceptions (or a result object if you prefer), and the package depends only on System.Text.Json. Nothing you could do in 2.x is lost. This guide maps every 2.x public type and client member to its 3.0 replacement.

If you only read one section, read [Minimum change](#minimum-change) and [Errors](#errors).

## Overview

| Topic | 2.x | 3.0 |
|---|---|---|
| Target frameworks | netstandard2.1 | netstandard2.0 (.NET Framework 4.6.2+, .NET Core) and net8.0 |
| Dependencies | Newtonsoft.Json, Microsoft.Extensions.Http | System.Text.Json and System.Diagnostics.DiagnosticSource on netstandard2.0 only; none on net8.0 |
| Namespaces | `AntiCaptchaApi.Net`, `.Enums`, `.Models`, `.Models.Solutions`, `.Requests`, `.Responses`, ... | `AntiCaptchaApi.Net` (client, options, results, errors, proxy) and `AntiCaptchaApi.Net.Tasks` (tasks and solutions) |
| A captcha type | two request classes (proxy and proxyless) plus interfaces and validators | one sealed record; setting `Proxy` switches to the proxy task type |
| Errors | data on every response (`IsErrorResponse`, `ErrorCode`) | `AntiCaptchaException`, or `SolveResult.Error` from `TrySolveAsync` |
| Invalid input | an error response after the call | `AntiCaptchaValidationException` before any HTTP request |
| Polling | a network error abandoned a paid task | transient errors are retried on the same task |
| Secrets | `RawPayload` / `RawResponse` could expose them | no raw payloads; the key and proxy password never appear in output |
| Dependency injection | `AddAnticaptcha` in the core package | not in core; see [Dependency injection](#dependency-injection) |

## Minimum change

2.x:

```csharp
using AntiCaptchaApi.Net;
using AntiCaptchaApi.Net.Requests;

var client = new AnticaptchaClient("API_KEY");
var result = await client.SolveCaptchaAsync(new TurnstileCaptchaProxylessRequest
{
    WebsiteUrl = "https://example.com/login",
    WebsiteKey = "0x4AAAAAAA...",
});
if (result.IsErrorResponse)
{
    Console.WriteLine($"{result.ErrorCode}: {result.ErrorDescription}");
    return;
}
Console.WriteLine(result.Solution.Token);
```

3.0:

```csharp
using AntiCaptchaApi.Net;
using AntiCaptchaApi.Net.Tasks;

var client = new AntiCaptchaClient("API_KEY");
try
{
    var solution = await client.SolveAsync(new TurnstileTask("https://example.com/login", "0x4AAAAAAA..."));
    Console.WriteLine(solution.Token);
}
catch (AntiCaptchaException ex)
{
    Console.WriteLine($"{ex.Error.Code}: {ex.Error.Description}");
}
```

If you prefer checking a result to catching, use `TrySolveAsync`:

```csharp
var result = await client.TrySolveAsync(new TurnstileTask("https://example.com/login", "0x4AAAAAAA..."));
if (!result.IsSuccess)
{
    Console.WriteLine($"{result.Error!.Code}: {result.Error.Description}");
    return;
}
Console.WriteLine(result.Solution!.Token);
```

## Client and constructors

| 2.x | 3.0 |
|---|---|
| `AnticaptchaClient` | `AntiCaptchaClient` (note the capital C; sealed, thread-safe, reuse one per key) |
| `IAnticaptchaClient` | `IAntiCaptchaClient` |
| `AnticaptchaClient(clientKey, clientConfig, httpClient)` | `new AntiCaptchaClient(apiKey)` uses a shared, pooled handler. `new AntiCaptchaClient(apiKey, httpClient, options)` uses your `HttpClient`, never disposes it and never changes its `Timeout` or `BaseAddress`. |
| `AnticaptchaClient.ClientKey` | removed: the key is a secret and is not readable back |
| `AnticaptchaClient.ClientConfig`, `IAnticaptchaClient.ClientConfig` | removed: options are immutable and passed to the constructor |
| `AnticaptchaClient.Configure(ClientConfig)`, `IAnticaptchaClient.Configure` | removed: pass `AntiCaptchaClientOptions` to the constructor, or `SolveOptions` per call |

## Configuration

`ClientConfig` is replaced by two immutable records: `AntiCaptchaClientOptions` for the client and `SolveOptions` for a solve. `AntiCaptchaClientOptions.DefaultSolveOptions` holds the defaults that every `SolveAsync` call without its own `SolveOptions` uses.

| 2.x `ClientConfig` | 2.x default | 3.0 | 3.0 default |
|---|---|---|---|
| `MaxWaitForTaskResultTimeMs` | 120 000 ms | `SolveOptions.Timeout` (per attempt, from `createTask`) | 180 s |
| `DelayTimeBetweenCheckingTaskResultMs` | 1 000 ms | `SolveOptions.PollInterval` | 3 s |
| `SolveAsyncRetries` (total attempts) | 1 | `SolveOptions.MaxAttempts` (total attempts) | 3 |
| `MaxHttpRequestTimeMs` | 60 000 ms | `AntiCaptchaClientOptions.RequestTimeout` (per HTTP call) | 30 s |
| none (2.x waited `PollInterval` before the first poll) | | `SolveOptions.FirstPollDelay` (`null` = per-type default: 3 s for image tasks, 10 s otherwise) | `null` |
| none (2.x abandoned the task on a failed poll) | | `SolveOptions.MaxConsecutivePollErrors` | 5 |
| none | | `SolveOptions.RetryBaseDelay` / `RetryMaxDelay` (exponential backoff with ±20 % jitter) | 2 s / 30 s |
| `languagePool`, `callbackUrl` parameters | | `SolveOptions.LanguagePool`, `SolveOptions.CallbackUrl` (`Uri`); on `CreateTaskAsync` use `CreateOptions` | |
| none (fixed URL) | | `AntiCaptchaClientOptions.BaseAddress` | `https://api.anti-captcha.com/` |

```csharp
var client = new AntiCaptchaClient("API_KEY", httpClient, new AntiCaptchaClientOptions
{
    RequestTimeout = TimeSpan.FromSeconds(20),
    DefaultSolveOptions = new SolveOptions { Timeout = TimeSpan.FromMinutes(5), MaxAttempts = 2 },
});
```

## Methods

Every 3.0 method takes `CancellationToken ct = default` last.

| 2.x | 3.0 |
|---|---|
| `SolveCaptchaAsync<T>(request, languagePool, callbackUrl, ct)` → `TaskResultResponse<T>` | `SolveAsync(task, options, ct)` → the solution, throws `AntiCaptchaException`; or `TrySolveAsync(task, options, ct)` → `SolveResult<T>` |
| `SolveProsopoAsync(request, ...)` | `SolveAsync(new ProsopoTask(url, key) { Proxy = proxy, UserAgent = ua })` |
| `SolveProsopoProxylessAsync(request, ...)` | `SolveAsync(new ProsopoTask(url, key))` |
| `SolveFriendlyCaptchaAsync(request, ...)` | `SolveAsync(new FriendlyCaptchaTask(url, key) { Proxy = proxy, UserAgent = ua })` |
| `SolveFriendlyCaptchaProxylessAsync(request, ...)` | `SolveAsync(new FriendlyCaptchaTask(url, key))` |
| `CreateCaptchaTaskAsync<T>(request, languagePool, callbackUrl, ct)` → `CreateTaskResponse` | `CreateTaskAsync(task, createOptions, ct)` → `int` task id |
| `GetTaskResultAsync<T>(taskId, ct)` → `TaskResultResponse<T>` | `GetTaskResultAsync<T>(taskId, ct)` → `TaskStatus<T>` (one poll, no waiting) |
| `WaitForTaskResultAsync<T>(taskId, ct)` and `WaitForTaskResultAsync<T>(CreateTaskResponse, ct)` | `WaitForResultAsync<T>(taskId, solveOptions, ct)` → the solution (polling only, no new attempts; the first poll is immediate unless `FirstPollDelay` is set) |
| `ReportTaskOutcomeAsync(taskId, ReportOutcome, ct)` → `ActionResponse` | `ReportAsync(taskId, Report, ct)` → `Task` (throws on error) |
| `PushAntiGateVariableAsync(taskId, name, object, ct)` → `ActionResponse` | `PushAntiGateVariableAsync(taskId, name, value, ct)` → `Task`. `value` must be a `string`, `bool`, number, `decimal` or `JsonNode`; other types throw `ArgumentException` before the request. |
| `GetBalanceAsync(ct)` → `BalanceResponse` | `GetBalanceAsync(ct)` → `decimal` |
| `GetQueueStatsAsync(QueueType, ct)` → `GetQueueStatsResponse` | `GetQueueStatsAsync(QueueType, ct)` → `QueueStats` |
| `GetSpendingStatsAsync(date, queue, softId, ip, ct)` → `GetSpendingStatsResponse` | `GetSpendingStatsAsync(new SpendingQuery { Date = ..., Queue = ..., SoftId = ..., Ip = ... }, ct)` → `SpendingStats`. `Date` is a `DateTimeOffset?` (2.x took unix seconds as `int?`). |
| `GetAppStatsAsync(softId, AppStatsMode?, ct)` → `GetAppStatsResponse` | `GetAppStatsAsync(softId, AppStatsMode?, ct)` → `AppStats` |

With a task id from `CreateTaskAsync`, the solution type is the type argument:

```csharp
var taskId = await client.CreateTaskAsync(new RecaptchaV2Task(url, siteKey));
var solution = await client.WaitForResultAsync<RecaptchaSolution>(taskId);
```

## Request classes → task records

All tasks are in `AntiCaptchaApi.Net.Tasks`. Required values are constructor parameters; optional ones are `init` properties. A proxy is set with `Proxy = new Proxy(...)`, which selects the proxy task type. There are no separate `...Proxyless...` classes any more. Base classes: `CaptchaRequest<TSolution>` and `WebsiteCaptchaRequest<TSolution>` become `CaptchaTask<TSolution>` (and the non-generic `CaptchaTask`); `WebsiteUrl` and `WebsiteKey` are constructor parameters.

| 2.x | 3.0 | Notes |
|---|---|---|
| `ImageToTextRequest` | `ImageToTextTask(body)`, `ImageToTextTask.FromFile(path)`, `ImageToTextTask.FromBytes(bytes)` | `BodyBase64` → the constructor's `Body`; `FilePath` → `FromFile`, which throws `FileNotFoundException` for a missing file (2.x silently sent an empty body). `Phrase`, `Case`, `Numeric`, `Math`, `MinLength`, `MaxLength`, `Comment` are unchanged; `WebsiteUrl` is new. |
| `ImageToCoordinatesRequest` | `ImageToCoordinatesTask(body)`, `.FromFile`, `.FromBytes` | `Body` → constructor; `WebsiteURL` → `WebsiteUrl`; `Mode` and `Comment` unchanged. |
| `RecaptchaV2ProxylessRequest`, `RecaptchaV2Request` | `RecaptchaV2Task(websiteUrl, websiteKey)` | `RecaptchaDataSValue`, `IsInvisible`, `UserAgent`, `Cookies`. `UserAgent` and `Cookies` can now be sent without a proxy. |
| `RecaptchaV2EnterpriseProxylessRequest`, `RecaptchaV2EnterpriseRequest` | `RecaptchaV2EnterpriseTask(websiteUrl, websiteKey)` | `EnterprisePayload` is an `IReadOnlyDictionary<string, string>`; `ApiDomain`, `UserAgent`, `Cookies`. |
| `RecaptchaV3Request` | `RecaptchaV3Task(websiteUrl, websiteKey)` | `MinScore` is the enum `MinScore.Score03` / `Score07` / `Score09` (was `decimal`; 0.5 is gone). `IsEnterprise` is `bool` (default `false`). `PageAction`, `ApiDomain` unchanged. |
| `RecaptchaV3EnterpriseRequest` | `RecaptchaV3Task(...) { IsEnterprise = true }` | |
| `TurnstileCaptchaProxylessRequest`, `TurnstileCaptchaRequest` | `TurnstileTask(websiteUrl, websiteKey)` | New: `Action`, `CData`, `ChlPageData`. |
| `FunCaptchaProxylessRequest`, `FunCaptchaRequest` | `FunCaptchaTask(websiteUrl, websitePublicKey)` | `FunCaptchaApiJsSubdomain` → `ApiJsSubdomain`; `Data`, `UserAgent`. |
| `GeeTestV3ProxylessRequest`, `GeeTestV3Request` | `GeeTestV3Task(websiteUrl, gt, challenge)` | `GeetestApiServerSubdomain`, `GeetestGetLib`, `UserAgent`. |
| `GeeTestV4ProxylessRequest`, `GeeTestV4Request` | `GeeTestV4Task(websiteUrl, gt)` | `Challenge` is optional; `InitParameters` is an `IReadOnlyDictionary<string, string>`. |
| `AmazonWafProxylessRequest`, `AmazonWafRequest` | `AmazonWafTask(websiteUrl, websiteKey, iv, context)` | `CaptchaScript`, `ChallengeScript`. `Iv` and `Context` are now validated with a proxy too. |
| `ProsopoProxylessRequest`, `ProsopoRequest` | `ProsopoTask(websiteUrl, websiteKey)` | `UserAgent`. |
| `FriendlyCaptchaProxylessRequest`, `FriendlyCaptchaRequest` | `FriendlyCaptchaTask(websiteUrl, websiteKey)` | `UserAgent`. |
| `AntiGateRequest` | `AntiGateTask(websiteUrl, templateName, variables)` | `Variables` is a `System.Text.Json.Nodes.JsonObject` (was `JObject`); `DomainsOfInterest` is an `IReadOnlyList<string>`. |

The request interfaces are removed. They existed only to share properties between the proxy and proxyless classes: `ICaptchaRequest`, `IWebCaptchaRequest`, `IImageToTextRequest`, `IImageToCoordinatesRequest`, `IRecaptchaV2ProxylessRequest`, `IRecaptchaV2Request`, `IRecaptchaV2EnterpriseProxylessRequest`, `IRecaptchaV2EnterpriseRequest`, `IRecaptchaV3Request`, `IRecaptchaV3EnterpriseRequest`, `ITurnstileCaptchaProxylessRequest`, `ITurnstileCaptchaRequest`, `IFunCaptchaProxylessRequest`, `IFunCaptchaRequest`, `IGeeTestV3ProxylessRequest`, `IGeeTestV3Request`, `IGeeTestV4ProxylessRequest`, `IGeeTestV4Request`, `IAmazonWafProxylessRequest`, `IAmazonWafRequest`, `IProsopoProxylessRequest`, `IProsopoRequest`, `IFriendlyCaptchaProxylessRequest`, `IFriendlyCaptchaRequest`, `IAntiGateRequest`, and the argument interfaces `IRequestArg`, `IWebsiteUrlArg`, `IWebsiteKeyArg`, `IUserAgentArg`, `ICookiesArg`, `IProxyConfigArg`, `IProxyConfigWithUserAgentArgs`, `IApiDomainArg`, `IEnterprisePayloadArg`, `IIsEnterpriseArg`, `IIsInvisibleArg`, `IGeeTestArgs`. The copy constructors that took them (`new ImageToTextRequest(IImageToTextRequest)`, ...) become `with` expressions: `task with { Comment = "..." }`.

### Proxy

| 2.x | 3.0 |
|---|---|
| `ProxyConfig { ProxyType, ProxyAddress, ProxyPort, ProxyLogin, ProxyPassword }` | `new Proxy(ProxyType.Http, "1.2.3.4", 8080) { Login = "...", Password = "..." }` |
| `ProxyTypeOption.Http` / `Socks4` / `Socks5` | `ProxyType.Http` / `Socks4` / `Socks5` |

`Proxy.ToString()` prints `Password = ***`. A proxy on a type that has no proxy variant (`ImageToTextTask`, `ImageToCoordinatesTask`, `RecaptchaV3Task`) is a validation error. For RecaptchaV2, RecaptchaV2Enterprise, Turnstile, FunCaptcha, GeeTest v3/v4, Prosopo and Friendly Captcha, `UserAgent` is required whenever `Proxy` is set, as in 2.x.

## Responses and solutions

| 2.x | 3.0 |
|---|---|
| `TaskResultResponse<T>` from `SolveCaptchaAsync` | the solution (`SolveAsync`) or `SolveResult<T>` (`TrySolveAsync`): `TaskId`, `Solution`, `Error`, `Cost`, `SolveTime`, `WorkerIp`, `Attempts`, `IsSuccess` |
| `TaskResultResponse<T>` from `GetTaskResultAsync` | `TaskStatus<T>`: `Status` → `IsReady`; `Cost` (`decimal?`, was `double?`); `Ip`; `CreateTimeUtc` → `CreatedAt` and `EndTimeUtc` → `EndedAt` (`DateTimeOffset?`); `SolveCount`; `Solution`. `CaptchaRequest` and `CreateTaskResponse` are removed; you already have the task and its id. |
| `CreateTaskResponse` (`TaskId`) | `int` from `CreateTaskAsync`; `SolveResult.TaskId`, `AntiCaptchaException.TaskId` |
| `BalanceResponse` (`Balance`, `double?`) | `decimal` |
| `GetQueueStatsResponse` (`Waiting`, `Load`, `Bid`, `Speed`, `Total`) | `QueueStats(Waiting, Load, Bid, Speed, Total)`; `Bid` is `decimal` (money) |
| `GetSpendingStatsResponse` (`AllDailySpendingStats`) | `SpendingStats(Days)`, read from the documented `data` array (2.x mapped a field the API does not send, so it was always empty) |
| `DailySpendingStats` (`DateFrom`, `DateTill` as unix `int`, `Volume`, `Money` as `double`) | `DailySpending(DateFrom, DateTill, Volume, Money)` with `DateTimeOffset` times and `decimal` money |
| `GetAppStatsResponse` (`FromDate`, `ToDate`, `ChartData` as `JObject`) | `AppStats(FromDate, ToDate, ChartData)`; `ChartData` is a `JsonElement` |
| `ActionResponse` (`Status`) | `Task`: success returns, failure throws `AntiCaptchaException` |
| `BaseResponse` (`ErrorId`, `ErrorCode`, `ErrorDescription`, `IsErrorResponse`) | `AntiCaptchaError` (`ErrorId`, `RawCode`, `Description`, `Code`) on `AntiCaptchaException.Error` or `SolveResult.Error` |
| `BaseResponse.RawPayload`, `BaseResponse.RawResponse` | removed: they could expose the key and proxy password |
| `BaseSolution` | `Solution`. `IsValid()` is gone: a ready task without a usable solution is reported as `InvalidResponse`, so a solution you receive is always usable. |

| 2.x solution (`AntiCaptchaApi.Net.Models.Solutions`) | 3.0 solution (`AntiCaptchaApi.Net.Tasks`) |
|---|---|
| `ImageToTextSolution` | `ImageToTextSolution(Text, Url)` |
| `ImageToCoordinatesSolution` | `ImageToCoordinatesSolution(Coordinates)` |
| `RecaptchaSolution` | `RecaptchaSolution(GRecaptchaResponse, GRecaptchaResponseMd5)` |
| `TurnstileSolution` | `TurnstileSolution(Token, UserAgent)` |
| `FunCaptchaSolution` | `FunCaptchaSolution(Token)` |
| `GeeTestV3Solution` | `GeeTestV3Solution(Challenge, Seccode, Validate)` |
| `GeeTestV4Solution` | `GeeTestV4Solution(CaptchaId, LotNumber, PassToken, GenTime, CaptchaOutput)` |
| `AmazonWafSolution` | `AmazonWafSolution(Token)` |
| `ProsopoSolution` | `ProsopoSolution(Token, UserAgent)` |
| `ProsopoSolution` (as returned for Friendly Captcha) | `FriendlyCaptchaSolution(Token, UserAgent)`: Friendly Captcha has its own solution type now |
| `AntiGateSolution` | `AntiGateSolution(Url, Domain, Cookies, LocalStorage, SessionStorage, Fingerprint, HtmlsInBase64, Screenshots, RequestHeaders, ResponseHeaders)`. The eight data members are `JsonNode?` (were `JObject`), because the API sends an empty map as `[]`. `HTMLsInBase64` → `HtmlsInBase64`. |

Solutions are immutable positional records.

## Enums

| 2.x (`AntiCaptchaApi.Net.Enums`) | 3.0 |
|---|---|
| `ProxyTypeOption` | `ProxyType` |
| `QueueType` | `QueueType` (in `AntiCaptchaApi.Net`). Same numbers. `RecaptchaB2WithProxy` → `RecaptchaV2WithProxy` and `RecaptchaB2WithoutProxy` → `RecaptchaV2WithoutProxy` (typo fixed). New: `TurnstileWithProxy` (26), `TurnstileWithoutProxy` (27), `ImageToCoordinates` (28). |
| `ReportOutcome` | `Report` with `IncorrectImageCaptcha`, `IncorrectRecaptcha`, `CorrectRecaptcha`. `IncorrectHCaptcha` is removed (anti-captcha no longer solves hCaptcha). |
| `AppStatsMode` | `AppStatsMode` (in `AntiCaptchaApi.Net`), same members; sent lowercase as documented |
| `NumericOption` | `NumericOption` (in `AntiCaptchaApi.Net.Tasks`), same members |
| `ImageToCoordinatesMode` (in `Requests.Abstractions.Interfaces`) | `ImageToCoordinatesMode` (in `AntiCaptchaApi.Net.Tasks`), same members |
| `TaskStatusType` (`Processing`, `Ready`, `Error`) | `TaskStatus<T>.IsReady`; an error throws `AntiCaptchaException` |
| `ApiMethod` | internal |

## Errors

In 2.x every response carried error fields that you had to check. In 3.0:

- `SolveAsync`, `CreateTaskAsync`, `GetTaskResultAsync`, `WaitForResultAsync` and the account, report and stats methods throw `AntiCaptchaException` on an API or transport error. `Error` holds an `AntiCaptchaError` with the parsed `Code` (`AntiCaptchaErrorCode`), the `RawCode` string such as `ERROR_ZERO_BALANCE`, the `Description` and the `ErrorId`. The exception also has `TaskId` (when a task was created) and `Attempts`.
- `TrySolveAsync` returns the same error in `SolveResult.Error` instead of throwing.
- Invalid input throws `AntiCaptchaValidationException` before any request, with one `ValidationError(Field, Message)` per problem. `TrySolveAsync` throws it too: it is a programming error and no money was spent.
- Cancelling your `CancellationToken` always throws `OperationCanceledException`. It is never wrapped and never returned as an error.
- Client-side codes: `HttpError` (network failure or non-2xx status), `HttpTimeout` (the per-request timeout, when your token was not cancelled), `InvalidResponse` (unparseable JSON, or ready without a usable solution), `SolveTimeout` (the attempt's `Timeout` elapsed). `AccessDenied` (900) is `ERROR_ACCESS_DENIED`, which the API sends for example from `getAppStats` for an app you do not own; it has no documented `errorId`. An unknown code is `Unknown` with the original string in `RawCode`.
- `AntiCaptchaError.IsRetryable` and `IsTransient` tell you how the pipeline treats an error:
  - **Transient** errors keep polling the same, already paid task: network failures, HTTP 408, 429 and 5xx, `HttpTimeout`, and `InvalidResponse` while polling. After `MaxConsecutivePollErrors` in a row the solve fails with that error and no new attempt is made, because a new attempt could pay twice.
  - **Retryable** errors start a new attempt after a backoff, up to `MaxAttempts`: `NoSlotAvailable`, `CaptchaUnsolvable`, `ProxyConnectionRefused` and transient HTTP errors on `createTask`.
  - Everything else is **fatal** and fails at once: `KeyDoesNotExist`, `ZeroBalance`, `IpBlocked`, `SolveTimeout`, other 4xx, `Unknown`, and so on. `SolveTimeout` is not retried because the timed-out task may still be charged.
- Validation types: the 2.x `ValidationResult` and `ValidationError` (in `Internal.Validation`) are replaced by `AntiCaptchaValidationException.Errors`, a list of `AntiCaptchaApi.Net.ValidationError`.

## Removed

| 2.x | Why / instead |
|---|---|
| `BaseResponse.RawPayload`, `RawResponse` | They could leak the key and proxy password. Use `AntiCaptchaDiagnostics` telemetry for troubleshooting. |
| `AnticaptchaClient.Configure`, `ClientConfig` | Immutable options: `AntiCaptchaClientOptions`, `SolveOptions`. |
| `AnticaptchaClient.ClientKey` | The key is a secret. |
| Public validators: `CaptchaRequestValidator<TRequest, TSolution>`, `WebsiteCaptchaRequestValidator<TRequest, TSolution>`, `ImageToTextRequestValidator`, `ImageToCoordinatesRequestValidator`, `RecaptchaV2ProxylessRequestValidator`, `RecaptchaV2RequestValidator`, `RecaptchaV2EnterpriseProxylessRequestValidator`, `RecaptchaV2EnterpriseRequestValidator`, `RecaptchaV3RequestValidator`, `TurnstileProxylessRequestValidator`, `TurnstileRequestValidator`, `FunCaptchaProxylessRequestValidator`, `FunCaptchaRequestValidator`, `GeeTestV4ProxylessRequestValidator`, `GeeTestV4RequestValidator`, `AmazonWafProxylessRequestValidator`, `AmazonWafRequestValidator` | Each task validates itself before any request; failures throw `AntiCaptchaValidationException`. |
| `ValidationResultExtensions`, `ValidationResult`, `ValidationError` (2.x) | See [Errors](#errors). |
| Public Newtonsoft converters: `TaskResultConverter<T>`, `AntiGateTaskResultConverter`, `GetAppStatsResponseConverter`, and `BaseTaskResultResponseBuilder` | Internal System.Text.Json source generation. Custom converters are not supported; the `OnResultParsed` hook is gone. |
| The 2.x request interfaces and `...Proxyless...` classes | One task record per type (see above). |
| `ReportOutcome.IncorrectHCaptcha`, `RecaptchaV3Request.MinScore = 0.5` | Deprecated in 2.2.0: no hCaptcha type exists, and 0.5 is not an anti-captcha score queue. |
| `AnticaptchaServiceCollectionExtensions.AddAnticaptcha` | See below. |

## Dependency injection

`AnticaptchaServiceCollectionExtensions.AddAnticaptcha(services, clientKey, configure)` is not in the 3.0 core package, which no longer depends on Microsoft.Extensions.*. It is coming in a separate `AntiCaptchaApi.Net.DependencyInjection` package. Meanwhile, register the client yourself with an `HttpClient` from `IHttpClientFactory`:

```csharp
services.AddHttpClient("anticaptcha");
services.AddTransient<IAntiCaptchaClient>(sp => new AntiCaptchaClient(
    configuration["AntiCaptcha:ApiKey"]!,
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("anticaptcha"),
    new AntiCaptchaClientOptions { RequestTimeout = TimeSpan.FromSeconds(30) }));
```

Creating a client is cheap, and a transient registration lets `IHttpClientFactory` rotate its handlers. The client is thread-safe, so you can also keep one instance per key.

## Wire and behaviour differences

The request JSON of every 2.x request class is kept: a test compares every 2.x wire snapshot with its 3.0 counterpart key by key. These are the intended differences:

1. **reCAPTCHA v3 `minScore`** can only be 0.3, 0.7 or 0.9 (2.x also accepted 0.5). `isEnterprise` is always sent (`false` by default); 2.x omitted it when unset.
2. **`getAppStats.mode`** is sent lowercase (`"errors"`), as documented. 2.x sent `"Errors"`.
3. **`getSpendingStats`** is read from the documented `data` array. In 2.x the result was always empty.
4. **`FromFile`** throws `FileNotFoundException` for a missing file. 2.x kept an empty body and failed later.
5. **Turnstile** gains `action`, `cData` and `chlPageData` (new optional fields).
6. **`userAgent` and `cookies`** can be sent on the proxyless variants of the types that support them. 2.x allowed them only on the proxy classes.
7. **Validation happens earlier and more strictly.** `websiteURL` must be an absolute http(s) URL, keys must not be empty, an image body must not be empty or carry a `data:` prefix, `MinLength` ≤ `MaxLength`, `ApiDomain` must be `www.google.com` or `www.recaptcha.net`, and a proxy port must be 1–65535.
8. **Polling survives transient errors** (see [Errors](#errors)), including an unparseable response, instead of abandoning a paid task.
9. **`ImageToCoordinatesTask.mode`** is sent as `"points"` / `"rectangles"`, as in 2.x.

## Changed defaults

| Setting | 2.x | 3.0 | Why |
|---|---|---|---|
| Time to wait for a task | 120 s | 180 s per attempt | A slow reCAPTCHA is not abandoned after you have paid for it. |
| Poll interval | 1 s | 3 s | Fewer calls per solve; a task is rarely ready within a second of the last poll. |
| First poll | after 1 s | after 3 s (image tasks) or 10 s (others) | Tasks are rarely ready sooner. |
| Attempts | 1 | 3, with exponential backoff and jitter | Retryable errors such as `ERROR_NO_SLOT_AVAILABLE` usually pass on a later attempt. |
| HTTP request timeout | 60 s | 30 s | Per call; polling continues after a timeout. |

To restore the 2.x behaviour for a call:

```csharp
var legacyLike = new SolveOptions
{
    Timeout = TimeSpan.FromSeconds(120),
    PollInterval = TimeSpan.FromSeconds(1),
    FirstPollDelay = TimeSpan.FromSeconds(1),
    MaxAttempts = 1,
};
var solution = await client.SolveAsync(task, legacyLike);
```

See [CHANGELOG.md](CHANGELOG.md) for the full list of changes in 3.0.
