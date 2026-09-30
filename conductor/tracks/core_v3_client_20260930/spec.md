# Spec: Core 3.0 client (`AntiCaptchaApi.Net` 3.0.0-preview.1)

Track ID: `core_v3_client_20260930`. Type: feature. Depends on `hotfix_2x_critical_bugs_20260930`.
Authoritative design: `conductor/design-v3.md` §4.1–4.4 plus the no-DI part of §4.5 and the `ActivitySource`/`Meter` part of §4.5.

## 1. Overview
Rewrite the core package as 3.0: one sealed record per captcha task type, a small client (`IAntiCaptchaClient` / `AntiCaptchaClient`), typed errors, a resilient solving pipeline, System.Text.Json with a source-generated context, targets `netstandard2.0; net8.0`, observability through `ActivitySource` and `Meter`, and no Newtonsoft.Json or Microsoft.Extensions.* dependency. Every 2.x capability (every task type and API method) gets a 3.0 equivalent, listed in §8. The last phase deletes the 2.x code and ships `MIGRATION.md`, a new README, a CHANGELOG entry and version `3.0.0-preview.1`. Nothing is published, tagged or pushed.

## 2. Background
In 2.x, the code for one captcha type is spread over about 15 places that must be kept in sync by hand (design §1). This caused shipped defects: the Amazon WAF `KeyNotFoundException`, an unused Turnstile proxy validator and missing converters. Errors are both data (`BaseResponse.ErrorCode`) and exceptions. The client key and proxy password leak through `RawPayload`. A transient HTTP error while polling abandons a task that has already been paid for. Revenue depends on successful solves attributed to softId 1023 (product.md), so the priority is reliability and "it just works".

## 3. Branching and starting point
- This track starts **after** `hotfix_2x_critical_bugs_20260930` is complete. Create branch `v3` from the tip of the hotfix branch. At that point the hotfix has added `AntiGateRequest` and fixed the verified 2.x bugs.
- Work happens only on `v3`. Never push, tag or publish. Never touch `main`.
- The 2.x code stays in the tree as a reference but is **excluded from compilation** from Phase 1 onwards (see §7.3). It is deleted in the last phase.

## 4. Functional Requirements

### FR-1 Project foundation (P0)
- Core targets `netstandard2.0;net8.0`, uses `LangVersion` latest, has `Nullable` enabled and `GenerateDocumentationFile` on, and sets `WarningsAsErrors` to `nullable;CS1591` for the core project.
- System.Text.Json is a `PackageReference` (≥ 8.0.5) **only** on `netstandard2.0`. System.Diagnostics.DiagnosticSource (≥ 8.0.0) is also added on `netstandard2.0` only, in Phase 6.
- The core project has no `Newtonsoft.Json` and no `Microsoft.Extensions.*` references.
- Internal polyfills, compiled only when `!NET5_0_OR_GREATER`: `IsExternalInit`, `CallerArgumentExpressionAttribute`, and the nullable attributes that are needed (`NotNullWhen` and similar).
- The softId is an internal constant `1023` (`Internal/SoftId.cs`). It is not overridable and not public.
- Acceptance: `dotnet build` succeeds for both TFMs with 0 warnings in the core project. A test asserts that the core assembly does not reference `Newtonsoft.Json` or `Microsoft.Extensions.*`. `dotnet pack` produces a nuspec whose `netstandard2.0` group lists only System.Text.Json (plus DiagnosticSource after Phase 6) and whose `net8.0` group has no dependencies.

### FR-2 Task model (P0)
- `public abstract record CaptchaTask` (non-generic, used by `CreateTaskAsync`) holds `Proxy? Proxy { get; init; }`. Its constructor is `private protected` and it has internal abstract members. This makes the hierarchy closed: only the core assembly and friend test assemblies can derive from it.
- `public abstract record CaptchaTask<TSolution> : CaptchaTask where TSolution : Solution` has these members, exactly as in the design:
  - `protected abstract string TypeBase`
  - `protected virtual ProxyMode ProxyMode => ProxyMode.Optional`
  - `protected virtual TimeSpan FirstPollDelay => 10 s`
  - `protected abstract void Validate(Validator v)`
- Type-name resolution by `ProxyMode`:

  | ProxyMode | Proxy null | Proxy set |
  |---|---|---|
  | `None` | `TypeBase` | validation error `Proxy: not supported by <Task>` |
  | `Optional` | `TypeBase + "Proxyless"` | `TypeBase` |
  | `Required` | validation error `Proxy: required` | `TypeBase` |
  | `OptionalSameName` | `TypeBase` | `TypeBase` |

- `public sealed record Proxy(ProxyType Type, string Address, int Port)` has `init` properties `Login` and `Password`. `ProxyType` is `Http | Socks4 | Socks5` and serializes lowercase. The proxy is flattened into the task as `proxyType`, `proxyAddress`, `proxyPort`, `proxyLogin`, `proxyPassword`, with nulls omitted. `Proxy.ToString()` prints `Password = ***`.
- Proxy validation: `Address` must not be empty. `Port` must be in 1–65535. `Type` must be a defined enum value.
- `public sealed class Validator` has an internal constructor and fluent checks: `Url`, `NotEmpty`, `NotNull`, `Range`, `OneOf`, `Must`. The field name comes from `[CallerArgumentExpression]`, with an optional explicit override.
- Validation failures throw `AntiCaptchaValidationException`, which exposes `IReadOnlyList<ValidationError> Errors`, where `ValidationError(string Field, string Message)`. The exception is thrown **before any HTTP call**.
- `public abstract record Solution` is the base of all solutions. There is no `IsValid`. A task that is "ready" but has no solution is reported as an `InvalidResponse` error.
- Acceptance: unit tests on test-only derived records (in the test assembly) cover every row of the type-name table, every proxy validation rule, and the redaction of `Proxy.ToString()`.

### FR-3 Task catalog, wire contract, snapshots (P0)
There is one sealed record per type, in namespace `AntiCaptchaApi.Net.Tasks`, file `Tasks/<Name>Task.cs`. The JSON names below are the 2.x wire names, taken from the 2.x request classes and `RequestSerializationTests`. They are preserved unless listed in §9.

| 3.0 record | TypeBase | ProxyMode | First poll | Required (ctor) | Optional (`init`) → JSON name | Solution (JSON) | Replaces 2.x |
|---|---|---|---|---|---|---|---|
| `ImageToTextTask(Body)` | `ImageToTextTask` | None | 3 s | `body` | `Phrase`→`phrase`, `Case`→`case`, `Numeric`→`numeric` (int 0/1/2), `Math`→`math`, `MinLength`→`minLength`, `MaxLength`→`maxLength`, `Comment`→`comment`, `WebsiteUrl`→`websiteURL` | `ImageToTextSolution(Text, Url)` (`text`,`url`) | `ImageToTextRequest` |
| `ImageToCoordinatesTask(Body)` | `ImageToCoordinatesTask` | None | 3 s | `body` | `Comment`→`comment`, `Mode`→`mode` (`points`/`rectangles`), `WebsiteUrl`→`websiteURL` | `ImageToCoordinatesSolution(Coordinates)` | `ImageToCoordinatesRequest` |
| `RecaptchaV2Task(WebsiteUrl, WebsiteKey)` | `RecaptchaV2Task` | Optional | 10 s | `websiteURL`,`websiteKey` | `RecaptchaDataSValue`→`recaptchaDataSValue`, `IsInvisible`→`isInvisible`, `UserAgent`→`userAgent`, `Cookies`→`cookies` | `RecaptchaSolution(GRecaptchaResponse, GRecaptchaResponseMd5?)` | `RecaptchaV2Request`, `RecaptchaV2ProxylessRequest` |
| `RecaptchaV2EnterpriseTask(WebsiteUrl, WebsiteKey)` | `RecaptchaV2EnterpriseTask` | Optional | 10 s | same | `EnterprisePayload`→`enterprisePayload` (object), `ApiDomain`→`apiDomain`, `UserAgent`, `Cookies` | `RecaptchaSolution` | `RecaptchaV2Enterprise(Proxyless)Request` |
| `RecaptchaV3Task(WebsiteUrl, WebsiteKey)` | `RecaptchaV3TaskProxyless` | None | 10 s | same | `MinScore`→`minScore` (enum `MinScore.Score03/Score07/Score09` → number 0.3/0.7/0.9, default `Score03`, always sent), `PageAction`→`pageAction`, `IsEnterprise`→`isEnterprise` (bool, default false, always sent), `ApiDomain`→`apiDomain` | `RecaptchaSolution` | `RecaptchaV3Request`, `RecaptchaV3EnterpriseRequest` (= `IsEnterprise = true`) |
| `TurnstileTask(WebsiteUrl, WebsiteKey)` | `TurnstileTask` | Optional | 10 s | same | `Action`→`action`, `CData`→`cData`, `ChlPageData`→`chlPageData` (new), `UserAgent` | `TurnstileSolution(Token, UserAgent?)` | `TurnstileCaptcha(Proxyless)Request` |
| `FunCaptchaTask(WebsiteUrl, WebsitePublicKey)` | `FunCaptchaTask` | Optional | 10 s | `websiteURL`,`websitePublicKey` | `ApiJsSubdomain`→`funcaptchaApiJSSubdomain`, `Data`→`data`, `UserAgent` | `FunCaptchaSolution(Token)` | `FunCaptcha(Proxyless)Request` |
| `GeeTestV3Task(WebsiteUrl, Gt, Challenge)` | `GeeTestTask` | Optional | 10 s | `websiteURL`,`gt`,`challenge` | `GeetestApiServerSubdomain`, `GeetestGetLib`, `UserAgent`; constant `version`=3 | `GeeTestV3Solution(Challenge, Seccode, Validate)` | `GeeTestV3(Proxyless)Request` |
| `GeeTestV4Task(WebsiteUrl, Gt)` | `GeeTestTask` | Optional | 10 s | `websiteURL`,`gt` | `Challenge`, `GeetestApiServerSubdomain`, `GeetestGetLib`, `InitParameters`→`initParameters` (object), `UserAgent`; constant `version`=4 | `GeeTestV4Solution` (`captcha_id`,`lot_number`,`pass_token`,`gen_time`,`captcha_output`) | `GeeTestV4(Proxyless)Request` |
| `AmazonWafTask(WebsiteUrl, WebsiteKey, Iv, Context)` | `AmazonTask` | Optional | 10 s | `websiteURL`,`websiteKey`,`iv`,`context` | `CaptchaScript`→`captchaScript`, `ChallengeScript`→`challengeScript` | `AmazonWafSolution(Token)` (see Q-2) | `AmazonWaf(Proxyless)Request` |
| `ProsopoTask(WebsiteUrl, WebsiteKey)` | `ProsopoTask` | Optional | 10 s | `websiteURL`,`websiteKey` | `UserAgent` | `ProsopoSolution(Token, UserAgent?)` | `Prosopo(Proxyless)Request` |
| `FriendlyCaptchaTask(WebsiteUrl, WebsiteKey)` | `FriendlyCaptchaTask` | Optional | 10 s | same | `UserAgent` | `FriendlyCaptchaSolution(Token, UserAgent?)` (2.x reused `ProsopoSolution`) | `FriendlyCaptcha(Proxyless)Request` |
| `AntiGateTask(WebsiteUrl, TemplateName, Variables)` | `AntiGateTask` | OptionalSameName | 10 s | `websiteURL`,`templateName`,`variables` (`JsonObject`) | `DomainsOfInterest`→`domainsOfInterest` | `AntiGateSolution(Url, Domain, Cookies, LocalStorage, Fingerprint, HtmlsInBase64)` | `AntiGateRequest` (hotfix) |

Rules:
- `ImageToTextTask.FromFile(path)` and `ImageToCoordinatesTask.FromFile(path)` throw `FileNotFoundException` when the file is missing (2.x silently ignored this). `FromBytes(byte[])` is also provided.
- Per-type validation keeps the 2.x rules and adds early-fail checks:
  - `websiteURL` must be an absolute http(s) URL.
  - Keys must not be empty.
  - `body` must not be empty and must not have a `data:` prefix.
  - `MinLength` and `MaxLength` must be ≥ 0, and `MinLength` ≤ `MaxLength` when both are > 0.
  - `ApiDomain`, when set, must be one of `www.google.com` or `www.recaptcha.net`.
  - AntiGate needs a non-empty `TemplateName` and a non-null `Variables`.
  - `UserAgent` is required whenever `Proxy` is set for RecaptchaV2, RecaptchaV2Enterprise, Turnstile, FunCaptcha, GeeTestV3, GeeTestV4, Prosopo and FriendlyCaptcha (2.x parity).
- Serialization uses System.Text.Json by runtime type through `AntiCaptchaJsonContext`, a source-generated context with compact output, nulls omitted and explicit `[JsonPropertyName]`. The base adds `type` and the flattened proxy fields to the serialized node.
- Snapshot tests discover every concrete `CaptchaTask` subtype in the core assembly by reflection. Each type needs at least a `minimal` sample, a `full` sample and (except for `None` types) a `proxy` sample, each with a checked-in `Snapshots/Tasks/<Type>.<case>.json`. The test fails when a type has no sample or no snapshot file, or when the type or its solution is not registered in the JSON context.
- A legacy parity test compares every 2.x wire snapshot captured in Phase 1 (`Snapshots/Legacy2x/`) with its 3.0 counterpart. Every 2.x key must be present with the same value unless the difference is listed in §9.
- Acceptance: all 13 types exist, and snapshot, discovery and parity tests pass.

### FR-4 Client surface (P0)
The public API matches design §4.2 exactly. Methods are added incrementally across the phases.
```csharp
public interface IAntiCaptchaClient
{
    Task<TSolution> SolveAsync<TSolution>(CaptchaTask<TSolution> task, SolveOptions? o = null, CancellationToken ct = default) where TSolution : Solution;
    Task<SolveResult<TSolution>> TrySolveAsync<TSolution>(CaptchaTask<TSolution> task, SolveOptions? o = null, CancellationToken ct = default) where TSolution : Solution;
    Task<int> CreateTaskAsync(CaptchaTask task, CreateOptions? o = null, CancellationToken ct = default);
    Task<TaskStatus<TSolution>> GetTaskResultAsync<TSolution>(int taskId, CancellationToken ct = default) where TSolution : Solution;
    Task<TSolution> WaitForResultAsync<TSolution>(int taskId, SolveOptions? o = null, CancellationToken ct = default) where TSolution : Solution;
    Task ReportAsync(int taskId, Report report, CancellationToken ct = default);
    Task PushAntiGateVariableAsync(int taskId, string name, object value, CancellationToken ct = default);
    Task<decimal> GetBalanceAsync(CancellationToken ct = default);
    Task<QueueStats> GetQueueStatsAsync(QueueType q, CancellationToken ct = default);
    Task<SpendingStats> GetSpendingStatsAsync(SpendingQuery? q = null, CancellationToken ct = default);
    Task<AppStats> GetAppStatsAsync(int softId, AppStatsMode? mode = null, CancellationToken ct = default);
}
```
- `public sealed record SolveResult<TSolution>(int? TaskId, TSolution? Solution, AntiCaptchaError? Error, decimal? Cost, TimeSpan? SolveTime, string? WorkerIp, int Attempts)` has `IsSuccess => Solution is not null && Error is null`.
- `public sealed record TaskStatus<TSolution>(bool IsReady, TSolution? Solution, decimal? Cost, string? Ip, DateTimeOffset? CreatedAt, DateTimeOffset? EndedAt, int? SolveCount)`. The generic arity avoids a clash with `System.Threading.Tasks.TaskStatus`.
- `public record CreateOptions { string? LanguagePool; Uri? CallbackUrl; }` and `public sealed record SolveOptions : CreateOptions` (see FR-7). `SolveOptions` inherits `CreateOptions` because `SolveAsync` must be able to pass `languagePool` and `callbackUrl` (2.x parity).
- `TrySolveAsync` returns API and transport failures in `SolveResult.Error`. It still **throws** `AntiCaptchaValidationException` (a programmer error, and no money is spent) and `OperationCanceledException`.
- `SolveAsync` throws `AntiCaptchaException(Error, TaskId, Attempts)` on failure.
- `GetTaskResultAsync`, `WaitForResultAsync` and every account/report method throw `AntiCaptchaException` on API or transport errors.
- A reflection test pins the surface of `IAntiCaptchaClient`: method names, parameter types and order, and `CancellationToken ct = default` in last position.

### FR-5 Constructors and transport (P0)
- `new AntiCaptchaClient(string apiKey)` uses a **shared static handler** for all instances:
  - `net8.0`: `SocketsHttpHandler { PooledConnectionLifetime = 2 min }`.
  - `netstandard2.0`: `HttpClientHandler`.
  - The handler is wrapped in `new HttpClient(handler, disposeHandler: false)`.
- `new AntiCaptchaClient(string apiKey, HttpClient httpClient, AntiCaptchaClientOptions? options = null)` uses the caller's client, never disposes it, and never mutates `HttpClient.Timeout` or `BaseAddress`.
- `AntiCaptchaClientOptions` (sealed record) has:
  - `Uri BaseAddress = https://api.anti-captcha.com/`
  - `TimeSpan RequestTimeout = 30 s` (per HTTP call)
  - `SolveOptions DefaultSolveOptions = new()`
- There is also an internal constructor that additionally takes `ISystemClock` and `Func<double> random`, for tests.
- `apiKey` null or whitespace throws `ArgumentException`, and the message never contains the value.
- Transport rules: every call is `POST {BaseAddress}{method}` with a JSON body. `ConfigureAwait(false)` is used everywhere. The `ct` is passed to every async API that accepts one; on `net8.0` this includes `ReadAsStringAsync(ct)`, handled with `#if`.

### FR-6 Errors (P0)
- `AntiCaptchaErrorCode` enum: every documented anti-captcha `ERROR_*` code, mapped from the raw string, plus client-side codes:
  - `HttpError` (network failure / non-2xx)
  - `HttpTimeout` (per-request timeout or `HttpClient.Timeout`, when the user's `ct` was **not** cancelled)
  - `InvalidResponse` (unparseable JSON, or ready without a solution)
  - `SolveTimeout` (per-attempt `Timeout` elapsed)
  - `CostLimitReached`
  - `Unknown` (the raw string is preserved)
- `public sealed record AntiCaptchaError(AntiCaptchaErrorCode Code, string? RawCode, string? Description, int? ErrorId)` with `bool IsRetryable` and `bool IsTransient` (internal classifier).
- Classification:
  - **Retryable** (new attempt): `NoSlotAvailable`, `CaptchaUnsolvable`, `ProxyConnectionRefused`, transient HTTP.
  - **Transient** (keep polling the same task): `HttpError` with network failure, 408, 429 or 5xx; `HttpTimeout`; `InvalidResponse` during polling.
  - **Fatal**: everything else, including `KeyDoesNotExist`, `ZeroBalance`, `IpBlocked`, `SolveTimeout`, other 4xx and `Unknown`.
- `AntiCaptchaException(AntiCaptchaError Error, int? TaskId, int Attempts) : Exception`. Its message is `"{Code}: {Description}"`, redacted.
- Cancellation: if the caller's `ct` is cancelled, an `OperationCanceledException` always escapes. It is never wrapped and never returned as an `Error`.

### FR-7 Solving pipeline (P0)
`SolveOptions` defaults:

| Option | Default | 2.x equivalent |
|---|---|---|
| `Timeout` (per attempt, from createTask) | 180 s | `MaxWaitForTaskResultTimeMs` 120 s |
| `PollInterval` | 3 s | `DelayTimeBetweenCheckingTaskResultMs` 1 s |
| `FirstPollDelay` (`TimeSpan?`; null = per-type default) | null | none (2.x waited `PollInterval`) |
| `MaxAttempts` | 3 | `SolveAsyncRetries` 1 |
| `MaxConsecutivePollErrors` | 5 | none (2.x abandoned the task) |
| `RetryBaseDelay` / `RetryMaxDelay` | 2 s / 30 s | none |
| `MaxCostPerSolve` (`decimal?`) | null | none |

Invalid option values (non-positive times, `MaxAttempts < 1`, negative cost) throw `ArgumentOutOfRangeException` before any HTTP call.

Algorithm:
1. Validate the task.
2. Attempt loop, `attempt = 1..MaxAttempts`:
   1. `createTask`. A retryable error goes to step 3.
   2. Wait `FirstPollDelay`, then poll `getTaskResult`:
      - `ready`: return success.
      - `processing`: wait `PollInterval`.
      - Transient error: increment the consecutive-error counter and keep polling. More than `MaxConsecutivePollErrors` in a row is a failure.
      - Retryable task error: go to step 3.
      - Fatal error: fail.
      - Past `Timeout`: fail with `SolveTimeout`.
3. Before retrying, compute the delay as `min(RetryMaxDelay, RetryBaseDelay × 2^(attempt−1)) × (1 ± 20 % jitter)`. If there are no attempts left, fail with the last error.

Rules:
- `MaxCostPerSolve`: before starting a retry attempt, if the sum of costs reported for tasks created in this call is ≥ the limit, fail with `CostLimitReached`. A solution that has already been received is always returned.
- `WaitForResultAsync(taskId)` runs only step 2.2 for an existing task, with no retries. Its first-poll delay is `options.FirstPollDelay ?? TimeSpan.Zero`.
- `SolveResult` fields:
  - `TaskId`: the last created task.
  - `Attempts`: the number of `createTask` calls made.
  - `Cost`, `WorkerIp`: taken from the ready status.
  - `SolveTime`: `EndedAt − CreatedAt`.
- Time is injectable through internal `ISystemClock { DateTimeOffset UtcNow; Task Delay(TimeSpan, CancellationToken) }`. Pipeline tests use a `FakeClock` whose `Delay` returns immediately and advances `UtcNow`, so they run fast and deterministically.

### FR-8 Account, report and stats endpoints (P0)
| Method | Endpoint | Request body | Result |
|---|---|---|---|
| `GetBalanceAsync` | `getBalance` | `clientKey` | `decimal` (`balance`) |
| `GetQueueStatsAsync(QueueType)` | `getQueueStats` | `queueId` (int) | `QueueStats(Waiting, Load, Bid, Speed, Total)` |
| `ReportAsync(taskId, Report.IncorrectImageCaptcha)` | `reportIncorrectImageCaptcha` | `clientKey`,`taskId` | `Task` (throws on error) |
| `ReportAsync(taskId, Report.IncorrectRecaptcha)` | `reportIncorrectRecaptcha` | same | same |
| `ReportAsync(taskId, Report.CorrectRecaptcha)` | `reportCorrectRecaptcha` | same | same |
| `PushAntiGateVariableAsync` | `pushAntiGateVariable` | `clientKey`,`taskId`,`name`,`value` | `Task` |
| `GetSpendingStatsAsync(SpendingQuery?)` | `getSpendingStats` | `clientKey`, `date` (unix seconds from `DateTimeOffset?`), `queue`, `softId`, `ip` | `SpendingStats(IReadOnlyList<DailySpending> Days)` from `data[]` (`dateFrom`,`dateTill`,`volume`,`money`) |
| `GetAppStatsAsync(softId, mode?)` | `getAppStats` | `clientKey`,`softId`,`mode` (lowercase) | `AppStats(FromDate, ToDate, JsonElement ChartData)` |

- `QueueType` keeps the 2.x numeric values and fixes the typo names (`RecaptchaB2WithProxy` becomes `RecaptchaV2WithProxy`, and the same for `RecaptchaB2WithoutProxy`).
- `PushAntiGateVariableAsync` accepts `string`, `bool`, integral and floating-point numbers, `decimal`, and `JsonNode`. Any other type throws `ArgumentException` before HTTP, because source generation cannot serialize an arbitrary `object`.
- An undefined `Report` value throws `ArgumentOutOfRangeException`.

### FR-9 Secret redaction (P0)
- The client key and proxy passwords never appear in any of these: exception messages, `ToString()` of any public type, `SolveResult`/`TaskStatus`/`AntiCaptchaError` values, `Activity` tags or events, metric tags.
- There is no `RawPayload`/`RawResponse` equivalent.
- The internal `SecretRedactor` replaces the client key and any known proxy password with `***` in every string that flows into an error description. This is defence in depth, because API descriptions can echo proxy strings.

### FR-10 Observability (P1)
- `public static class AntiCaptchaDiagnostics` with `const string ActivitySourceName = "AntiCaptcha"` and `const string MeterName = "AntiCaptcha"`.
- Activities:
  - `AntiCaptcha.Solve`, with tags `anticaptcha.task.type`, `anticaptcha.task.id`, `anticaptcha.attempts`, `anticaptcha.error.code`, and status set to Error on failure.
  - One child activity per HTTP call, `AntiCaptcha.<apiMethod>`.
- Instruments:

  | Instrument | Type | Tags / unit |
  |---|---|---|
  | `anticaptcha.tasks.created` | `Counter<long>` | `task.type` |
  | `anticaptcha.solves` | `Counter<long>` | `task.type`, `outcome` = success\|failure |
  | `anticaptcha.failures` | `Counter<long>` | `task.type`, `error.code` |
  | `anticaptcha.solve.duration` | `Histogram<double>` | unit `s` |
  | `anticaptcha.cost` | `Counter<double>` | unit `USD`, `task.type` |

- There is no `ILogger` in core; it belongs to the DI package in a later track.

### FR-11 Removal, migration and docs (P0)
- Delete all 2.x source, 2.x-only tests and 2.x DI code.
- Port the 2.x `IntegrationTests` to opt-in live tests under `LiveTests/` that use the 3.0 API. They are skipped when the `ClientKey` env var is missing, carry `[Trait("Category","Live")]`, and are excluded by the `UnitTests` filter.
- `MIGRATION.md` (repo root) must contain a 2.x → 3.0 row for **every** 2.x public type and public member. This is checked by a test against `Snapshots/Legacy2x/public-api.txt`, which is captured in Phase 1.
- README rewrite for the core package, in this order: install, 10-line quick start, per-type examples, configuration, errors and retries, observability, troubleshooting, migration link.
- `CHANGELOG.md` gets a `3.0.0-preview.1` entry. Version and package version are `3.0.0-preview.1`. `PackageReleaseNotes` points to CHANGELOG. The README is packed (`PackageReadmeFile`).

## 5. Non-Functional Requirements
- **NFR-1 Reach**: runs on .NET Framework 4.6.2+ (through netstandard2.0) and .NET 8–10. Code uses no netstandard2.1+ APIs without `#if` (for example `Random.Shared`, `ArgumentNullException.ThrowIfNull`, `SocketsHttpHandler`, `ReadAsStringAsync(ct)`).
- **NFR-2 Dependencies**: `netstandard2.0` depends on System.Text.Json ≥ 8.0.5 and System.Diagnostics.DiagnosticSource ≥ 8.0.0 only. `net8.0` has no dependencies.
- **NFR-3 AOT/trim-safe**: no reflection-based serialization and no `dynamic` in library code. Reflection is allowed only in tests.
- **NFR-4 Tests**: offline, deterministic, no real delays in pipeline tests (whole suite < 30 s), coverage ≥ 80 % on new code. The test project targets `net10.0`, which is the only runtime installed locally. A `net8.0` test TFM is deferred to the CI track.
- **NFR-5 Security**: FR-9. Any validation or exception text that could include user values excludes proxy passwords and the key.
- **NFR-6 Performance**: overhead is negligible next to network time. Serialization cost is not a goal, but no per-call allocations of `JsonSerializerOptions` or `HttpClient`.
- **NFR-7 Docs**: every public member has XML docs (enforced by CS1591 as an error). Each task record's `<summary>` links its anti-captcha docs page.

## 6. User Stories
- **US-1 One-call solve.** As a hobbyist I want `await new AntiCaptchaClient(key).SolveAsync(new TurnstileTask(url, siteKey))` to return a token, so that I don't have to build JSON or write a polling loop.
  - Given a valid key and task, when the API returns processing twice and then ready, then `SolveAsync` returns a `TurnstileSolution` and made exactly 1 `createTask` call and 3 `getTaskResult` calls.
- **US-2 Fail early.** As a developer I want an invalid task to throw before any request, so that I don't pay for mistakes.
  - Given `new TurnstileTask("not-a-url", "")`, when solved, then `AntiCaptchaValidationException` lists `WebsiteUrl` and `WebsiteKey`, and the fake handler received 0 requests.
- **US-3 Never waste a paid task.** As a service owner I want transient network errors during polling to be tolerated.
  - Given 2 consecutive `HttpRequestException`s while polling and then ready, when solving, then the result is success with `Attempts == 1`.
- **US-4 Retry the right things.** Given `ERROR_NO_SLOT_AVAILABLE` on createTask twice and then success, then the result is success with `Attempts == 3`, and the delays between attempts grow. Given `ERROR_ZERO_BALANCE`, then it fails immediately with `Attempts == 1`, and there is no retry.
- **US-5 Secrets stay secret.** Given key `SECRET_KEY` and proxy password `PX_PASS`, when any error path runs, then no exception message, `ToString()`, activity tag or metric tag contains either value.
- **US-6 Cancellation.** Given a cancelled token during a poll delay, then `OperationCanceledException` is thrown, never `AntiCaptchaException` and never a failed `SolveResult`. Given a per-request timeout without cancellation, then the error code is `HttpTimeout`.
- **US-7 Migrate.** As a 2.x user I want `MIGRATION.md` to show the 3.0 replacement for every type I use.

## 7. Technical Considerations
### 7.1 Layout (namespaces: `AntiCaptchaApi.Net` for the client, options, results, errors and proxy; `AntiCaptchaApi.Net.Tasks` for tasks and solutions)
```
AntiCaptchaApi.Net/
  AntiCaptchaClient.cs  IAntiCaptchaClient.cs  AntiCaptchaClientOptions.cs  CreateOptions.cs  SolveOptions.cs
  SolveResult.cs  TaskStatus.cs  Proxy.cs  ProxyType.cs  Report.cs  QueueType.cs  QueueStats.cs
  SpendingQuery.cs  SpendingStats.cs  DailySpending.cs  AppStats.cs  AppStatsMode.cs  AntiCaptchaDiagnostics.cs
  Errors/        AntiCaptchaErrorCode.cs AntiCaptchaError.cs AntiCaptchaException.cs AntiCaptchaValidationException.cs ValidationError.cs
  Tasks/         CaptchaTask.cs CaptchaTaskOfT.cs ProxyMode.cs Validator.cs Solution.cs MinScore.cs NumericOption.cs ImageToCoordinatesMode.cs <Name>Task.cs
  Tasks/Solutions/  <Name>Solution.cs
  Internal/Json/    AntiCaptchaJsonContext.cs TaskSerializer.cs LowerCaseEnumConverter.cs MinScoreJsonConverter.cs Envelopes/*.cs
  Internal/Http/    ApiTransport.cs SharedHandler.cs
  Internal/Errors/  ErrorCodeParser.cs ErrorClassifier.cs SecretRedactor.cs
  Internal/Pipeline/ SolvePipeline.cs Backoff.cs
  Internal/Time/    ISystemClock.cs SystemClock.cs
  Internal/Diagnostics/ Telemetry.cs
  Internal/Polyfills/ IsExternalInit.cs CallerArgumentExpressionAttribute.cs NullableAttributes.cs
  Internal/SoftId.cs
```
New folders do not overlap the 2.x folders, so the 2.x folders can be excluded with `<Compile Remove>`. The 2.x folders are `Requests/`, `Responses/`, `Models/`, `Enums/`, and the `Internal/` subfolders `Common`, `Converters`, `Extensions`, `Helpers`, `Models`, `Services`, `Validation`, plus the three root files.

### 7.2 JSON strategy
- `TaskSerializer.ToJsonObject(CaptchaTask)` works as follows:
  1. `typeInfo = AntiCaptchaJsonContext.Default.GetTypeInfo(task.GetType())`. If it is null, it throws `InvalidOperationException("… not registered in AntiCaptchaJsonContext")`.
  2. `JsonSerializer.SerializeToNode(task, typeInfo)`.
  3. Add `type` and the flattened proxy fields. `Proxy` is `[JsonIgnore]`, and `TypeBase`, `ProxyMode` and `FirstPollDelay` are non-public, so none of them is serialized by the context.
- Per-type constants such as GeeTest `version` are ordinary get-only `[JsonPropertyName]` properties.
- Registration is required: STJ source generation needs `[JsonSerializable(typeof(XTask))]` and `[JsonSerializable(typeof(XSolution))]`. This is a known second touch point per type, accepted because of the source-gen constraint. The discovery test guards it and names the missing attribute in its failure message.
- Enums:
  - `ProxyType`, `ImageToCoordinatesMode` and `AppStatsMode` use an internal `LowerCaseEnumConverter<T> : JsonStringEnumConverter<T>` with a camelCase policy.
  - `MinScore` uses `MinScoreJsonConverter`, which writes a number.
  - `NumericOption` serializes as a number.
- `cost` in `getTaskResult` arrives as a string. Parse it with `JsonNumberHandling.AllowReadingFromString`. `createTime` and `endTime` are unix seconds.
- Positional records need `[property: JsonPropertyName(...)]` on the solution parameters.

### 7.3 Coexistence during the track
- Phase 1 captures the 2.x wire contract and public API list as checked-in files. It then removes Newtonsoft and M.E.Http from the core project and excludes 2.x sources and 2.x tests with `<Compile Remove>`.
- This avoids type-name shadowing. For example, a new root-namespace `QueueType` would shadow `AntiCaptchaApi.Net.Enums.QueueType` for 2.x code. It also avoids nullable-warning noise.
- 2.x files stay on disk as reference until Phase 7.

### 7.4 Local toolchain
- Only the .NET 10 runtime is installed. The SDK builds `netstandard2.0` and `net8.0`, and tests run on `net10.0`.
- Use `DOTNET_ROLL_FORWARD=Major` as in workflow.md; it is harmless once tests target net10.0.
- In the Claude Code sandbox, `dotnet` needs the sandbox disabled (tech-stack.md).
- A console probe `tools/AntiCaptchaApi.Net.Probe` (net10.0, not packable) is used for each phase's manual verification. It runs offline by default. Its live commands need `ClientKey` and are run by the user only.

## 8. 2.x → 3.0 capability map (nothing lost)
| 2.x | 3.0 |
|---|---|
| `IAnticaptchaClient` / `AnticaptchaClient` | `IAntiCaptchaClient` / `AntiCaptchaClient` |
| `AnticaptchaClient(clientKey, ClientConfig?, HttpClient?)` | `AntiCaptchaClient(apiKey)`; `AntiCaptchaClient(apiKey, httpClient, options?)` |
| `AddAnticaptcha(services, key, cfg)` | removed from core; `AntiCaptchaApi.Net.DependencyInjection` (later track) |
| `ClientConfig.MaxWaitForTaskResultTimeMs` / `DelayTimeBetweenCheckingTaskResultMs` / `SolveAsyncRetries` / `MaxHttpRequestTimeMs` | `SolveOptions.Timeout` / `.PollInterval` / `.MaxAttempts` / `AntiCaptchaClientOptions.RequestTimeout` |
| `ClientConfig`, `Configure(ClientConfig)` | immutable `AntiCaptchaClientOptions.DefaultSolveOptions` + per-call `SolveOptions` |
| `ClientKey` (public property) | removed (secret) |
| `SolveCaptchaAsync<T>(request, languagePool, callbackUrl)` | `SolveAsync` / `TrySolveAsync` (`SolveOptions.LanguagePool`, `.CallbackUrl`) |
| `SolveProsopoAsync`, `SolveProsopoProxylessAsync`, `SolveFriendlyCaptchaAsync`, `SolveFriendlyCaptchaProxylessAsync` | `SolveAsync(new ProsopoTask(..) { Proxy = … })` / `SolveAsync(new FriendlyCaptchaTask(..))` |
| `CreateCaptchaTaskAsync<T>` → `CreateTaskResponse` | `CreateTaskAsync` → `int` |
| `GetTaskResultAsync<T>` → `TaskResultResponse<T>` | `GetTaskResultAsync<T>` → `TaskStatus<T>` |
| `WaitForTaskResultAsync<T>(taskId)` and `(CreateTaskResponse)` | `WaitForResultAsync<T>(taskId, options)` |
| `ReportTaskOutcomeAsync(taskId, ReportOutcome)` | `ReportAsync(taskId, Report)` |
| `PushAntiGateVariableAsync` → `ActionResponse` | `PushAntiGateVariableAsync` → `Task` |
| `GetBalanceAsync` → `BalanceResponse` | `GetBalanceAsync` → `decimal` |
| `GetQueueStatsAsync` → `GetQueueStatsResponse` | `GetQueueStatsAsync` → `QueueStats` |
| `GetSpendingStatsAsync(date, queue, softId, ip)` | `GetSpendingStatsAsync(SpendingQuery)` → `SpendingStats` |
| `GetAppStatsAsync(softId, mode)` | same → `AppStats` |
| `BaseResponse.ErrorId/ErrorCode/ErrorDescription/IsErrorResponse` | `AntiCaptchaException.Error` / `SolveResult.Error` |
| `BaseResponse.RawResponse/RawPayload` | removed (secret leak) |
| `ProxyConfig`, `ProxyTypeOption` | `Proxy`, `ProxyType` |
| `BaseSolution`, `BaseSolution.IsValid()` | `Solution` (the pipeline guarantees a non-null solution on success) |
| `TaskStatusType` | `TaskStatus<T>.IsReady`; errors throw |
| `ApiMethod` | internal |
| 22 request classes + ~40 `I*Request`/`I*Arg` interfaces | 13 task records (FR-3 table) |
| Public validators, converters, `ValidationResultExtensions` | internal / removed |
| `ReportOutcome.IncorrectHCaptcha` | removed (see Q-3) |

## 9. Documented wire and behaviour deviations from 2.x
The parity test allow-list contains exactly these items.
1. `ImageToCoordinatesTask.mode` is sent as `"points"`/`"rectangles"`. 2.x Newtonsoft serialized the enum as an integer, which is not what the anti-captcha docs specify.
2. `RecaptchaV3Task.minScore` is limited to 0.3/0.7/0.9 (2.x validated 0.3/0.5/0.7 against the docs). `isEnterprise` is always sent (default false).
3. The maximum proxy port is 65535 (2.x rejected 65535).
4. `getAppStats.mode` is sent lowercase (2.x sent `"Errors"`).
5. `getSpendingStats` is read from `data[]` (2.x mapped `AllDailySpendingStats`, which the API does not return). Skip this item if the hotfix already fixed it.
6. `FromFile` throws on a missing file (2.x silently kept an empty body).
7. The Turnstile `action`, `cData` and `chlPageData` fields are additive; they are new, not deviations.

If the Phase 1 capture shows that the hotfix already changed any 2.x behaviour above, update this list before Phase 2.

## 10. Out of Scope
- The `AntiCaptchaApi.Net.DependencyInjection` package, `ILogger`, `IAntiCaptchaClientFactory` and named clients.
- Release pipeline, CI and Dependabot changes, SourceLink/deterministic settings, package validation baselines, and a net8.0 test TFM in CI.
- The browser engine, Selenium/Playwright packages, and the Compat (obsolete shims) package.
- Publishing, tagging, pushing, and nuget.org metadata beyond the version/readme/release-notes fields.
- New captcha types beyond the 13 in FR-3 (hCaptcha, image-to-audio and others).

## 11. Open Questions / Resolved Ambiguities
Resolved in this spec; the owner can override:
- **R-1** The JSON context needs per-type `[JsonSerializable]` registration, a second touch point that design §1 wants to avoid. It is accepted because source generation requires it, and it is guarded by the discovery test with an actionable message.
- **R-2** UserAgent and Cookies are per-record `init` properties, not base properties ("where relevant"). Only `Proxy` is on the base.
- **R-3** `TrySolveAsync` still throws validation and cancellation exceptions (programmer errors, no spend). Only API and transport errors go into `SolveResult.Error`.
- **R-4** Per-attempt `SolveTimeout` is fatal, not retryable: the timed-out task may still be charged, and a new attempt would pay twice. Transient HTTP errors on `createTask` are retried as the design requires, which accepts a small risk of a duplicate task.
- **R-5** Time abstraction: an internal `ISystemClock` instead of `TimeProvider`, because `TimeProvider` needs `Microsoft.Bcl.TimeProvider` on netstandard2.0 and that would break "System.Text.Json only".
- **R-6** `MaxCostPerSolve` is a budget checked before each retry attempt. It never discards a received solution. It is P2 value, but it is kept small because the design lists it.
- **R-7** Defaults changed from 2.x: `Timeout` 180 s (so a paid slow reCAPTCHA is not abandoned at 120 s), `PollInterval` 3 s, `MaxAttempts` 3.
- **R-8** `ImageToCoordinatesTask` also uses a 3 s first-poll delay (it is an image task). The design mentions this only for ImageToText.
- **R-9** `CaptchaTask` is a closed hierarchy (`private protected` ctor and internal abstract members). Users cannot add custom task types, because unregistered types cannot be serialized.
- **R-10** `Report` is an enum with 3 values. hCaptcha reporting is dropped (see Q-3).

Open, to confirm during implementation (each has a default):
- **Q-1** AntiGate proxy fields: anti-captcha docs list `proxyAddress/Port/Login/Password` **without** `proxyType` for `AntiGateTask`. Default: match the hotfix `AntiGateRequest` legacy snapshot exactly. If that snapshot has no `proxyType`, add an internal `IncludesProxyType => false` override on `AntiGateTask`.
- **Q-2** The Amazon WAF solution shape (`token` in 2.x vs a possible `cookies["aws-waf-token"]`). Default: keep `token`. The owner can confirm with one live solve in the Phase 3 verification.
- **Q-3** `reportIncorrectHcaptcha`: anti-captcha discontinued hCaptcha. Default: drop it and document the removal in MIGRATION. If the owner wants parity, add `Report.IncorrectHCaptcha`.
- **Q-4** Whether `QueueType` should gain newer queue ids (Turnstile and others). Default: no new ids without a doc source; keep the 2.x set with the fixed names.
