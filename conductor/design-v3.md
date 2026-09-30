# v3 Design — AntiCaptchaApi.Net family

Agreed with the owner on 2026-09-30. Decisions: no multi-provider abstraction (for now); softId stays internal and non-overridable; Playwright package ID is `Playwright.Anticaptcha`; a `Compat` package (obsolete 2.x type shims) is deferred — decide after checking NuGet download stats per 2.x version.

## 1. Why (root cause of 2.x defects)
One captcha type is spread over ~15 hand-synchronized places (2 request classes, 2+ interfaces, 2 validators, `RequestTaskNameHelper`, validator dictionary in `CreateCaptchaRequestHelper`, `Build()` switch, converter list in `HttpHelper`; plus 6+ places in Selenium). Missing one entry caused: Amazon WAF `KeyNotFoundException`, Turnstile proxy validator unused, missing Prosopo/WAF converters. Other structural issues: ~40 micro-interfaces, proxy/proxyless duplication, errors as both data and exceptions.

## 2. Goals
1. Correct by construction (one place per type). 2. One-call solving. 3. Never waste a paid task. 4. Reach (.NET Framework → .NET 10; Selenium + Playwright; loose dependency floors). 5. Offline-testable.

## 3. Package map
| Today | v3 | Depends on |
|---|---|---|
| `AntiCaptchaApi.Net` (netstandard2.1, Newtonsoft, M.E.Http 10.x) | `AntiCaptchaApi.Net` 3.0 — netstandard2.0 + net8.0 | System.Text.Json (package on netstandard2.0 only) |
| DI inside core | `AntiCaptchaApi.Net.DependencyInjection` | Microsoft.Extensions.Http ≥ 8.0 |
| – | `AntiCaptcha.Browser` (Selenium repo) | core |
| `Selenium.AntiCaptcha` | `Selenium.AntiCaptcha` 3.0 thin adapter | engine, Selenium.WebDriver ≥ 4.11 |
| – | `Playwright.Anticaptcha` (Selenium repo) | engine, Microsoft.Playwright ≥ 1.40 |
| `Selenium.FramesSearcher.Extensions` | deprecated | – |

## 4. Core client
### 4.1 Task model
```csharp
public abstract record CaptchaTask<TSolution> : CaptchaTask where TSolution : Solution
{
    protected abstract string TypeBase { get; }                        // "TurnstileTask"
    protected virtual ProxyMode ProxyMode => ProxyMode.Optional;       // None | Optional | Required | OptionalSameName
    protected virtual TimeSpan FirstPollDelay => TimeSpan.FromSeconds(10);
    protected abstract void Validate(Validator v);
}
public sealed record TurnstileTask(string WebsiteUrl, string WebsiteKey) : CaptchaTask<TurnstileSolution>
{
    public string? Action { get; init; }  public string? CData { get; init; }  public string? ChlPageData { get; init; }
    protected override string TypeBase => "TurnstileTask";
    protected override void Validate(Validator v) => v.Url(WebsiteUrl).NotEmpty(WebsiteKey);
}
```
- Base: `Proxy? Proxy`, `UserAgent`, `Cookies` where relevant. Type name = `TypeBase` or `TypeBase + "Proxyless"` per `ProxyMode`. Proxy fields flattened (`proxyType`, `proxyAddress`, `proxyPort`, `proxyLogin`, `proxyPassword`). Proxy on a `None` type → validation error.
- System.Text.Json by runtime type, source-generated context, compact output.
- Catalog: ImageToText (`FromFile` throws on missing file; 3 s first poll), ImageToCoordinates, RecaptchaV2, RecaptchaV2Enterprise, RecaptchaV3 (`IsEnterprise` default false; `MinScore` enum 0.3/0.7/0.9), Turnstile, FunCaptcha, GeeTestV3, GeeTestV4, AmazonWaf (`AmazonTask`), Prosopo, FriendlyCaptcha, **AntiGate** (template + variables; `OptionalSameName` proxy).
- softId: internal constant `1023` on `createTask`.

### 4.2 Client
```csharp
public interface IAntiCaptchaClient
{
    Task<TSolution> SolveAsync<TSolution>(CaptchaTask<TSolution> task, SolveOptions? o = null, CancellationToken ct = default);
    Task<SolveResult<TSolution>> TrySolveAsync<TSolution>(CaptchaTask<TSolution> task, SolveOptions? o = null, CancellationToken ct = default);
    Task<int> CreateTaskAsync(CaptchaTask task, CreateOptions? o = null, CancellationToken ct = default);
    Task<TaskStatus<TSolution>> GetTaskResultAsync<TSolution>(int taskId, CancellationToken ct = default);
    Task<TSolution> WaitForResultAsync<TSolution>(int taskId, SolveOptions? o = null, CancellationToken ct = default);
    Task ReportAsync(int taskId, Report report, CancellationToken ct = default);
    Task PushAntiGateVariableAsync(int taskId, string name, object value, CancellationToken ct = default);
    Task<decimal> GetBalanceAsync(CancellationToken ct = default);
    Task<QueueStats> GetQueueStatsAsync(QueueType q, CancellationToken ct = default);
    Task<SpendingStats> GetSpendingStatsAsync(SpendingQuery? q = null, CancellationToken ct = default);
    Task<AppStats> GetAppStatsAsync(int softId, AppStatsMode? mode = null, CancellationToken ct = default);
}
public sealed record SolveResult<TSolution>(int? TaskId, TSolution? Solution, AntiCaptchaError? Error,
    decimal? Cost, TimeSpan? SolveTime, string? WorkerIp, int Attempts) { public bool IsSuccess => Solution is not null && Error is null; }
```
### 4.3 Errors
`AntiCaptchaErrorCode` enum (+ `Unknown` with raw string). Retryable: NoSlotAvailable, CaptchaUnsolvable, ProxyConnectionRefused, transient HTTP. Fatal: KeyDoesNotExist, ZeroBalance, IpBlocked, validation. `AntiCaptchaException(Error, TaskId, Attempts)`, `AntiCaptchaValidationException(errors)` thrown before HTTP. Cancellation → `OperationCanceledException`; HTTP timeout → distinct error. Client key and proxy password redacted everywhere.
### 4.4 Pipeline
Validate → createTask → wait `FirstPollDelay` → poll every `PollInterval`; transient poll error → keep polling (max N consecutive); retryable error → backoff + jitter, retry up to `MaxAttempts`; `Timeout` per attempt; optional `MaxCostPerSolve`.
### 4.5 Transport / DI / observability
`new AntiCaptchaClient(apiKey)` uses a shared static handler; `new AntiCaptchaClient(apiKey, httpClient, options)`. DI package: `services.AddAntiCaptcha(o => …)` returns `IHttpClientBuilder`; named clients via `IAntiCaptchaClientFactory`. `ActivitySource("AntiCaptcha")`, `Meter` (tasks, failures by code, cost, duration); `ILogger` in DI package.

## 5. Browser engine (Selenium.AntiCaptcha repo — separate Conductor setup)
- `IBrowserPage` (Url, Frames, GetUserAgentAsync, GetCookiesAsync, ScreenshotElementAsync, AddInitScriptAsync) / `IBrowserFrame` (Url, EvaluateAsync).
- `ICaptchaModule` per type: `DetectScript` (JS reading DOM + globals such as `___grecaptcha_cfg`, `.cf-turnstile`, `window.gokuProps`), `CreateTask(detection, ctx)`, `InjectAsync` (fill fields, fire input/change, invoke callbacks; values passed as script args), `IsSolvedAsync`.
- Hook mode: init script wrapping `turnstile.render` / `grecaptcha.render|execute` (Playwright always; Selenium on Chromium via DevTools).
- Orchestration: detect once per frame → pick → context (real UA, cookies, proxy) → TrySolve → inject → optional submit/verify → report incorrect + bounded retry. Events + `ILogger`.
- Selenium adapter: frame scope restores the original frame in `finally`. API: `driver.SolveCaptchaAsync(client, o => …)`, `DetectCaptchasAsync()`, `WaitAndSolveCaptchaAsync(...)`. Playwright: `page.SolveCaptchaAsync(client)`, `context.EnableCaptchaHooksAsync()`.

## 6. Testing
Unit: reflection-discovered snapshot per task type (fails when a type lacks a snapshot); client vs fake `HttpMessageHandler` (polling, recovery, retries, error mapping, redaction, cancellation). Fixture (browser repo): local HTML with stub widget scripts, headless Chrome, both adapters. Contract: nightly/manual real solves on captchas.info with a spend cap. API compat: package validation vs last 3.x.

## 7. Release
Dependabot PRs (build + test), manual releases on tags with CHANGELOG, SourceLink, deterministic builds, icon, README per package, lowest dependency floors. 2.x branch for fixes.

## 8. Phases
0. 2.x hotfix → 2.2.0. 1. Core 3.0. 2. DI + observability + release pipeline. 3. Engine + Selenium 3.0 (reCAPTCHA v2/v3, Turnstile, image, WAF). 4. Playwright.Anticaptcha + hook mode + GeeTest/FunCaptcha/Prosopo/Friendly. 5. Docs, samples, launch.
