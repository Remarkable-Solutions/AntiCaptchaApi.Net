# Changelog

All notable changes to AntiCaptchaApi.Net are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project follows [Semantic Versioning](https://semver.org/).

## [3.0.0-preview.1] - 2026-09-30 (unreleased)

3.0 is a rewrite. Each captcha type is one record, one call solves it, and the package no longer depends on Newtonsoft.Json or Microsoft.Extensions.*. This is a breaking release: the whole 2.x API is replaced. [MIGRATION.md](MIGRATION.md) maps every 2.x type and method to its replacement.

### Added

- **One record per task type** in `AntiCaptchaApi.Net.Tasks`: `ImageToTextTask`, `ImageToCoordinatesTask`, `RecaptchaV2Task`, `RecaptchaV2EnterpriseTask`, `RecaptchaV3Task`, `TurnstileTask`, `FunCaptchaTask`, `GeeTestV3Task`, `GeeTestV4Task`, `AmazonWafTask`, `ProsopoTask`, `FriendlyCaptchaTask` and `AntiGateTask`. Required values are constructor parameters. Setting `Proxy` switches to the proxy variant.
- **`AntiCaptchaClient` / `IAntiCaptchaClient`** with `SolveAsync` (returns the solution or throws) and `TrySolveAsync` (returns a `SolveResult` with `TaskId`, `Cost`, `SolveTime`, `WorkerIp` and `Attempts`). Also `CreateTaskAsync`, `GetTaskResultAsync`, `WaitForResultAsync`, `ReportAsync`, `PushAntiGateVariableAsync`, `GetBalanceAsync`, `GetQueueStatsAsync`, `GetSpendingStatsAsync` and `GetAppStatsAsync`. Every method takes a `CancellationToken`.
- **A solving pipeline that protects paid tasks:**
  - Network errors, HTTP 408/429/5xx, timeouts and unparseable responses while polling are retried on the same task, up to `MaxConsecutivePollErrors`.
  - `NoSlotAvailable`, `CaptchaUnsolvable` and `ProxyConnectionRefused` start a new attempt with exponential backoff and jitter.
  - Everything else fails at once.
- **Typed errors:** `AntiCaptchaException` with an `AntiCaptchaError` (`Code`, `RawCode`, `Description`, `ErrorId`, `IsRetryable`, `IsTransient`), and `AntiCaptchaValidationException` with field-level `ValidationError`s. `AntiCaptchaErrorCode` covers every documented API error, plus `AccessDenied` and the client-side `HttpError`, `HttpTimeout`, `InvalidResponse` and `SolveTimeout`.
- **Validation before any request**, so a mistake costs nothing. It checks absolute http(s) URLs, non-empty keys, image bodies, length ranges, the reCAPTCHA `ApiDomain`, the proxy address and port, and `UserAgent` when a proxy is set.
- **Options:** `AntiCaptchaClientOptions` (`BaseAddress`, `RequestTimeout`, `DefaultSolveOptions`) and per-call `SolveOptions` (`Timeout`, `PollInterval`, `FirstPollDelay`, `MaxAttempts`, `MaxConsecutivePollErrors`, `RetryBaseDelay`, `RetryMaxDelay`, `LanguagePool`, `CallbackUrl`).
- **Observability:** an `ActivitySource` and a `Meter` named `AntiCaptcha` (`AntiCaptchaDiagnostics`). There is an `AntiCaptcha.Solve` activity per solve and an `AntiCaptcha.<apiMethod>` activity per call. The instruments are `anticaptcha.tasks.created`, `anticaptcha.solves`, `anticaptcha.failures`, `anticaptcha.solve.duration` and `anticaptcha.cost`. Telemetry never carries the key, proxy details or solution content.
- **Turnstile** `Action`, `CData` and `ChlPageData`. **`QueueType`** `TurnstileWithProxy` (26), `TurnstileWithoutProxy` (27) and `ImageToCoordinates` (28).
- **`FriendlyCaptchaSolution`**, a separate type (2.x returned a `ProsopoSolution`).
- `ImageToTextTask.FromBytes` and `ImageToCoordinatesTask.FromBytes`.

### Changed

- **Targets** `netstandard2.0` (now .NET Framework 4.6.2+ too) and `net8.0`, instead of netstandard2.1, net8.0 and net9.0.
- **Dependencies:** System.Text.Json ≥ 8.0.5 and System.Diagnostics.DiagnosticSource ≥ 8.0.0, on netstandard2.0 only. There are none on net8.0.
- **Defaults:**
  - The wait per attempt is 180 s (was 120 s) and the poll interval 3 s (was 1 s).
  - The first poll comes after 3 s for image tasks and 10 s for the others.
  - Up to 3 attempts (was 1).
  - The HTTP request timeout is 30 s (was 60 s).
- `new AntiCaptchaClient(apiKey)` shares one pooled handler across instances. `new AntiCaptchaClient(apiKey, httpClient, options)` never disposes your `HttpClient` and never changes its `Timeout` or `BaseAddress`.
- reCAPTCHA v3 `MinScore` is the enum `MinScore.Score03` / `Score07` / `Score09`, and `isEnterprise` is always sent (default `false`).
- `QueueType.RecaptchaB2WithProxy` / `RecaptchaB2WithoutProxy` are renamed to `RecaptchaV2WithProxy` / `RecaptchaV2WithoutProxy`.
- `ReportTaskOutcomeAsync(taskId, ReportOutcome)` is now `ReportAsync(taskId, Report)`.
- Results are plain values: `GetBalanceAsync` returns `decimal`, and `GetQueueStatsAsync` returns `QueueStats` (with a `decimal` bid). `GetSpendingStatsAsync` takes a `SpendingQuery` and returns `SpendingStats` with `DateTimeOffset` times.
- `AntiGateSolution` data members are `JsonNode?` (the API sends an empty map as `[]`). `AppStats.ChartData` is a `JsonElement`.
- `userAgent` and `cookies` can be sent without a proxy on the types that support them.
- Cancelling your `CancellationToken` always ends a call with `OperationCanceledException`. It is never wrapped or returned as an error. An HTTP timeout is a separate `HttpTimeout` error.

### Removed

- The 2.x API: `AnticaptchaClient`, `IAnticaptchaClient`, `ClientConfig`, `Configure`, the public `ClientKey`, all `*Request` / `*ProxylessRequest` classes and their interfaces, the `*Response` classes, `BaseSolution.IsValid()`, `ProxyConfig`, `ProxyTypeOption`, `TaskStatusType`, `ApiMethod`, and the public validators and Newtonsoft converters.
- `RawPayload` and `RawResponse`: they could expose the API key and proxy password.
- `AddAnticaptcha`: dependency injection moves to a separate `AntiCaptchaApi.Net.DependencyInjection` package. Until it ships, pass an `HttpClient` from `IHttpClientFactory` to the constructor (see the README).
- `ReportOutcome.IncorrectHCaptcha` and reCAPTCHA v3 `MinScore = 0.5`, deprecated in 2.2.0.
- The Newtonsoft.Json and Microsoft.Extensions.Http dependencies.

### Fixed

- A missing image file now throws `FileNotFoundException` from `FromFile`. 2.x silently sent an empty body.
- `getSpendingStats` results are read from the documented `data` array. In 2.x they were always empty.
- `getAppStats` sends `mode` lowercase, as documented.
- Amazon WAF validates `Iv` and `Context` with a proxy too.

## [2.2.0] - 2026-09-30 (unreleased)

A bug-fix release for 2.x. Every advertised task type now works, polling survives network blips, and secrets no longer show up in `RawPayload`. There are no breaking API changes.

### Fixed

- **Amazon WAF:** `AmazonWafProxylessRequest` and `AmazonWafRequest` threw `KeyNotFoundException` when the task was created. They now send `AmazonTaskProxyless` and `AmazonTask`.
- **reCAPTCHA v3:** a request without `IsEnterprise` failed validation with "IsEnterprise must not be null!". That included `RecaptchaV3EnterpriseRequest`, which sets the flag itself. Unset now means `false`. `MinScore = 0.9` is now accepted (valid values: 0.3, 0.7, 0.9, plus 0.5 for compatibility).
- **Turnstile with proxy:** `TurnstileCaptchaRequest` was validated as if it were proxyless, so a missing or invalid proxy was only rejected by the API. The proxy is now validated locally. `UserAgent` stays optional.
- **Prosopo, Friendly Captcha and Amazon WAF results:** `CreateTimeUtc` / `EndTimeUtc` were never set, and API errors such as `ERROR_CAPTCHA_UNSOLVABLE` did not set `Status = Error`.
- **Concurrent solves:** results parsed at the same time could read each other's JSON. For example, an AntiGate result could get another task's cookies. Result parsing no longer shares state.
- **Proxy port 65535** was rejected. The valid range is now 1-65535.
- **Polling:** one failed `getTaskResult` call (network error, invalid JSON or HTTP timeout) ended the wait and abandoned a task you had already paid for. Polling now continues. It gives up only after 3 consecutive failed polls and then returns the last error. API errors and your own cancellation still end the wait immediately.
- **Retries:** `SolveCaptchaAsync` retried requests that had failed local validation, which cannot succeed. It now returns the `BadRequest` error at once.
- **Dependency injection:** `AddAnticaptcha` ignored `ClientConfig.MaxHttpRequestTimeMs`. The client now uses a named `HttpClient` with that timeout.
- **AntiGate:** `AntiGateSolution.HTMLsInBase64` was never populated because the documented key is `HTMLsInBase64`, not `htmlsInBase64`. Both spellings are now read.

### Added

- `AntiGateRequest` for [AntiGate](https://anti-captcha.com/apidoc/task-types/AntiGateTask) templates (`AntiGateTask`): `TemplateName`, `Variables`, optional `DomainsOfInterest` and optional `ProxyConfig`. It works with `SolveCaptchaAsync`, `CreateCaptchaTaskAsync` and `PushAntiGateVariableAsync`.
- `AntiGateSolution.SessionStorage`, `Screenshots`, `RequestHeaders` and `ResponseHeaders`.
- `TaskResultConverter<T>.OnResultParsed(JObject, TaskResultResponse<T>)`, a protected hook for custom converters.

### Changed

- **Lower dependency floors:** `Microsoft.Extensions.Http` ≥ 8.0.0 (was 10.0.12) and `Newtonsoft.Json` ≥ 13.0.1 (was 13.0.4). NuGet treats these as minimum versions. The old 10.x floor forced every app, including .NET 8 LTS apps, onto Microsoft.Extensions 10.x. Apps that already use newer versions keep them.
- **Releases are intentional:** the daily workflow that bumped dependencies and tagged a release automatically has been removed, and Dependabot now proposes updates weekly. The publish workflow runs the offline unit tests before packing.
- README rewritten: correct target frameworks (netstandard2.1, net8.0, net9.0), the actual list of supported types, and Amazon WAF and AntiGate examples.

### Deprecated

These still work in 2.x and will be removed in 3.0:

- `ReportOutcome.IncorrectHCaptcha`: the library has no hCaptcha request type.
- `RecaptchaV3Request.MinScore = 0.5`: accepted for backward compatibility, but it is not an anti-captcha score queue. Use 0.3, 0.7 or 0.9.
- The protected field `TaskResultConverter<T>.jObject`: it is no longer populated because it was not thread-safe. Override `OnResultParsed` instead.

### Security

- `RawPayload` no longer contains secrets: `clientKey` and every `proxyPassword` are replaced with `"***"`. The request sent to anti-captcha is unchanged.
- The `Newtonsoft.Json` floor, 13.0.1, is the first release without [GHSA-5crp-9r3c-p9vr](https://github.com/advisories/GHSA-5crp-9r3c-p9vr) (CVE-2024-21907, denial of service on deeply nested JSON).

## [2.1.6] - [2.1.18] - 2025-12-08 to 2026-09-09

- Dependency updates only, released automatically: `Microsoft.Extensions.Http` 10.0.0 through 10.0.12. No library code changes.

## [2.1.5] - 2025-10-21

- Updated dependencies (`Microsoft.Extensions.Http` 9.0.10, `Newtonsoft.Json` 13.0.4).

## [2.1.4] - 2025-06-21

- Fixed a dependency injection ambiguity in how the `HttpHelper` constructor was selected.

## [2.1.3] - 2025-05-24

- Added Amazon WAF captcha support (`AmazonTaskProxyless` and `AmazonTask`).

## [2.1.2] - 2025-05-19

- Added a public constructor to `AnticaptchaClient`, so it can be created without dependency injection.
- `HttpHelper` accepts an `HttpClient` in its constructor to support manual instantiation.

[3.0.0-preview.1]: https://github.com/Remarkable-Solutions/AntiCaptchaApi.Net/compare/v2.2.0...HEAD
[2.2.0]: https://github.com/Remarkable-Solutions/AntiCaptchaApi.Net/compare/v2.1.18...HEAD
[2.1.6]: https://github.com/Remarkable-Solutions/AntiCaptchaApi.Net/compare/v2.1.5...v2.1.18
[2.1.18]: https://github.com/Remarkable-Solutions/AntiCaptchaApi.Net/compare/v2.1.5...v2.1.18
[2.1.5]: https://github.com/Remarkable-Solutions/AntiCaptchaApi.Net/compare/v2.1.4...v2.1.5
[2.1.4]: https://github.com/Remarkable-Solutions/AntiCaptchaApi.Net/compare/v2.1.3...v2.1.4
[2.1.3]: https://github.com/Remarkable-Solutions/AntiCaptchaApi.Net/compare/v2.1.2...v2.1.3
[2.1.2]: https://github.com/Remarkable-Solutions/AntiCaptchaApi.Net/compare/v2.1.1...v2.1.2
