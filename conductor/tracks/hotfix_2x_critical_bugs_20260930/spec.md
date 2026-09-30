# Spec: 2.x critical hotfix → release 2.2.0

- **Track ID:** `hotfix_2x_critical_bugs_20260930`
- **Type:** bug
- **Branch:** `fix/2x-critical-bugs`
- **Target version:** `2.2.0` (SemVer minor: bug fixes + one additive feature; no breaking changes)
- **Baseline:** version 2.1.18; 27 offline unit tests passing; 15 build warnings.
- **Test command:** `DOTNET_ROLL_FORWARD=Major dotnet test AntiCaptchaApi.Net.sln --filter "FullyQualifiedName~UnitTests"` (never run `IntegrationTests/`: they call the paid live API).

## 1. Overview

Fix the verified defects in the current 2.x codebase, working within its existing architecture: hand-kept registries, Newtonsoft.Json, and `TaskResultResponse`-as-data. Ship them as `AntiCaptchaApi.Net` 2.2.0. The goal is that every advertised task type works, a paid task is never abandoned because of one network hiccup, secrets never leak through `RawPayload`, and releases are intentional. This track does **not** start the v3 redesign (`conductor/design-v3.md`). It fixes what users of 2.x hit today.

## 2. Background

Revenue depends on successful solves through the library (see `product.md`). A code review and a runtime probe program confirmed these defects in 2.1.18:

| # | Defect | Location (verified) |
|---|---|---|
| B1 | Amazon WAF throws `KeyNotFoundException`. No task-name entry for `AmazonWafProxylessRequest` / `AmazonWafRequest` | `AntiCaptchaApi.Net/Internal/Helpers/RequestTaskNameHelper.cs:17-38` (dictionary ends at `FriendlyCaptchaRequest`; the validator registry does have WAF entries at `CreateCaptchaRequestHelper.cs:40-41`) |
| B2 | Default `RecaptchaV3Request` fails validation with "IsEnterprise must not be null!". `RecaptchaV3EnterpriseRequest` also fails even though its docs say IsEnterprise is set automatically (`Build()` sets it *after* validation, `CreateCaptchaRequestHelper.cs:79-81`). The MinScore whitelist is `0.3/0.5/0.7`, but anti-captcha's queues are `0.3/0.7/0.9` | `Internal/Validation/Validators/RecaptchaV3RequestValidator.cs:13-14`; `Requests/RecaptchaV3Request.cs:18,29,49` |
| B3 | `TurnstileCaptchaRequest` is validated with the *proxyless* validator after a cast to `TurnstileCaptchaProxylessRequest`, so the proxy is never validated | `Internal/Helpers/CreateCaptchaRequestHelper.cs:34` |
| B4 | No `TaskResultConverter` for `ProsopoSolution` or `AmazonWafSolution`, so `CreateTimeUtc`/`EndTimeUtc` are never set and API errors are not mapped to `Status = Error`. `FriendlyCaptcha*Request` also uses `ProsopoSolution`, so it has the same problem | `Internal/Helpers/HttpHelper.cs:19-29` (BaseSolution subclasses: AmazonWaf, AntiGate, FunCaptcha, GeeTestV3, GeeTestV4, ImageToCoordinates, ImageToText, Prosopo, Recaptcha, Turnstile. Only Prosopo and AmazonWaf are missing) |
| B5 | Thread-safety race. `TaskResultConverter<T>` stores the parsed `JObject` in an instance field (`protected JObject jObject;`). Converter instances are static and shared (`HttpHelper.Converters`), and `AntiGateTaskResultConverter` reads the field after `base.ReadJson`, so concurrent solves can read another response's JSON | `Internal/Converters/TaskResultConverter.cs:13,23`; `Internal/Converters/AntiGateTaskResultConverter.cs:20-25` |
| B6 | Port 65535 is rejected (off-by-one: `>= 65535`) | `Internal/Extensions/ValidationResultExtensions.cs:65-67` and `:89-91` |
| B7 | `BaseResponse.RawPayload` exposes `clientKey` and `proxyPassword` in plain text | `Responses/Abstractions/BaseResponse.cs:11`; assigned in `HttpHelper.cs:75,87,96,106,115` |
| B8 | `WaitForTaskResultAsync` gives up on the first transient HTTP error (status `null` → return), abandoning a paid, still-running task | `AnticaptchaClient.cs:286-293` |
| B9 | `SolveCaptchaAsync` retries local validation/build failures (`ErrorCode == "BadRequest"`), which cannot succeed on retry | `AnticaptchaClient.cs:162-174` (with `CreateCaptchaTaskAsync` at `:224-231`) |
| B10 | DI ignores `ClientConfig.MaxHttpRequestTimeMs`. `AddAnticaptcha` registers the default unnamed client, and `HttpHelper` calls `CreateClient()` with no name | `AnticaptchaServiceCollectionExtensions.cs:38,46-47`; `HttpHelper.cs:49` |
| B11 | AntiGate is unusable. `AntiGateSolution`, `AntiGateTaskResultConverter`, `PushAntiGateVariableAsync`, `QueueType.AntiGateTask` and `IAntiGateRequest` exist, but there is no concrete request class | `Requests/Abstractions/Interfaces/IAntiGateRequest.cs` (`TemplateName`, `JObject Variables`, `List<string> DomainsOfInterest`, plus `IProxyConfigArg`) |
| B12 | No automated guard against registry drift. B1 and B4 would have been caught | tests |
| B13 | Release hygiene. A daily cron auto-bumps dependency floors to the latest version and auto-tags a release (2.1.5 to 2.1.18 are all dependency bumps). Unit tests are commented out in the publish workflow. Floors are `Microsoft.Extensions.Http 10.0.12` / `Newtonsoft.Json 13.0.4` | `.github/workflows/auto-dependency-update.yml:4-6,145-212`; `.github/workflows/publish-nuget.yml:32-34`; `AntiCaptchaApi.Net.csproj:59-60` |
| B14 | Docs are inaccurate. The README says ".NET 9 library", lists HCaptcha (no hCaptcha request exists), and contradicts itself on DI ("only way", README:86-88, vs. the public constructor). `ReportOutcome.IncorrectHCaptcha` points to a type the library does not support. `PackageReleaseNotes` is a list of repetitive auto-bump entries | `README.md:3,9,86-88,258-261`; `Enums/ReportOutcome.cs:26-30`; `AntiCaptchaApi.Net.csproj:20-56` |

## 3. Functional Requirements

### FR-1 Amazon WAF task names (B1), Priority: P0
`RequestTaskNameHelper` maps `AmazonWafProxylessRequest` → `"AmazonTaskProxyless"` and `AmazonWafRequest` → `"AmazonTask"` (names from the 2.1.3 release notes).
**Acceptance:**
- `CreateCaptchaRequestHelper.Build(new AmazonWafProxylessRequest{...})` yields `type = "AmazonTaskProxyless"`, `websiteURL`, `websiteKey`, `iv`, `context` (plus `captchaScript`/`challengeScript` when set), and no `proxyConfig` key.
- `AmazonWafRequest` with a proxy yields `type = "AmazonTask"` with flattened `proxyType/proxyAddress/proxyPort/proxyLogin/proxyPassword`.
- `CreateCaptchaTaskAsync` for either type sends the request (verified with a fake `HttpMessageHandler`) and does not throw.

### FR-2 reCAPTCHA v3 validation (B2), Priority: P0
- `IsEnterprise == null` is valid and means `false`. No `isEnterprise` key is serialized for a plain `RecaptchaV3Request` with `null`; the API default is `false`.
- `RecaptchaV3EnterpriseRequest` always serializes `isEnterprise: true`, whatever the property value (current `Build()` behavior, kept).
- Accepted `MinScore` values: `0.3`, `0.7`, `0.9` (the anti-captcha queues), plus `0.5` **for backward compatibility only**. 2.x accepted 0.5, and rejecting it in a minor release would break callers. The XML docs mark 0.5 as not a real queue and discouraged. Removal is deferred to 3.0 (`MinScore` enum).
- Any other value (e.g. `0.4`, `1.0`) fails validation with a `MinScore` error listing the accepted values.
**Acceptance:** Validate a default `RecaptchaV3Request` (URL+key only) → valid; a default `RecaptchaV3EnterpriseRequest` → valid; `MinScore = 0.9m` → valid; `0.5m` → valid; `0.4m` → invalid. The existing serialization tests stay green.

### FR-3 Turnstile proxy validation (B3), Priority: P0
- `TurnstileCaptchaRequest` is validated by `TurnstileRequestValidator` (via `request as TurnstileCaptchaRequest`).
- `TurnstileRequestValidator` requires website URL, website key and a valid proxy (`ValidateProxy`). It must **not** require `UserAgent`: anti-captcha documents that a custom UA is not needed for Turnstile, and requiring it would newly break working callers. `UserAgent` stays optional and is serialized when set.
- The XML doc on `TurnstileCaptchaRequest.UserAgent` changes from "[Required] … Google …" to "[Optional]".
**Acceptance:** A `TurnstileCaptchaRequest` without `ProxyConfig` → validation error on `ProxyConfig`. One with a valid proxy and no UA → valid. `ProxyPort = 0` → error. Every proxy-required request type rejects a null `ProxyConfig` (table-driven test).

### FR-4 Complete result converters (B4), Priority: P0
`HttpHelper.Converters` contains a `TaskResultConverter<T>` for **every** concrete `BaseSolution` subclass: add `ProsopoSolution` and `AmazonWafSolution`. The list becomes `internal` (read-only) so tests can inspect it.
**Acceptance:** A ready `getTaskResult` JSON for Prosopo/AmazonWaf (through a fake handler) → `CreateTimeUtc`/`EndTimeUtc` set. An `errorId: 12` JSON → `Status == TaskStatusType.Error`.

### FR-5 Stateless converters (B5), Priority: P0
- `TaskResultConverter<T>.ReadJson` keeps no per-call state in instance fields. It passes the loaded `JObject` to a new `protected virtual void OnResultParsed(JObject source, TaskResultResponse<T> response)` hook (name may be adjusted), which `AntiGateTaskResultConverter` overrides instead of reading a field.
- Binary compatibility: `TaskResultConverter<T>` is a public type, so its `protected JObject jObject` field is kept, marked `[Obsolete("Not thread-safe; no longer populated. Override OnResultParsed instead.")]` and `[EditorBrowsable(Never)]`, and is no longer written. (No known external subclasses. Removal is planned for 3.0.)
**Acceptance:** Parsing 200 distinct AntiGate ready responses in parallel with one shared converter set gives each result its own `cookies`/`fingerprint` values. A reflection test asserts that no non-obsolete instance fields exist on the converter types.

### FR-6 Proxy port range (B6), Priority: P1
`ValidateProxy` and `ValidateOptionalProxy` accept `1..65535` inclusive. The error message reads "must have value between 1 and 65535".
**Acceptance:** Port `65535` valid; `65536`, `0` and `null` invalid.

### FR-7 Redacted RawPayload (B7), Priority: P0
- Every `RawPayload` assignment in `HttpHelper` stores a redacted copy. The top-level `clientKey` is replaced with `"***"`, and so is every `proxyPassword` property at any depth (e.g. `task.proxyPassword`). The actual HTTP request body is unchanged.
- Implemented as an internal `PayloadRedactor.Redact(string json)` that parses with `JObject`. If parsing fails it returns a fixed placeholder (`"[payload redacted: unparseable]"`) and never the raw text.
- `RawResponse` is unchanged (anti-captcha responses do not echo the key).
**Acceptance:** A fake-handler `createTask` call has `RawPayload` without the client key or proxy password and with `"clientKey":"***"`. The request body the handler receives still has the real values. This holds on success and on each error path (`JsonError`, `HttpError`, `Cancelled`, `UnexpectedError`).

### FR-8 Resilient polling (B8), Priority: P0
- In `WaitForTaskResultAsync(CreateTaskResponse, …)`, a poll result whose `ErrorCode` is a transient transport code from `HttpHelper` (`"HttpError"`, `"JsonError"`, `"UnexpectedError"`, or `"Cancelled"` **only when the caller's `cancellationToken` is not cancelled**, i.e. an `HttpClient` timeout) does not end the wait. Polling continues after the normal delay.
- After `MaxConsecutiveTransientPollErrors` consecutive transient failures (internal constant, **3**), the last error response is returned as today (Status `null`, ErrorCode/Description of the last failure). One successful poll resets the counter.
- `ClientConfig.MaxWaitForTaskResultTimeMs` is still honored. When it is exceeded, the existing timeout result is returned.
- Real API errors (`errorId > 0` → `Status == Error`) still return immediately. Caller cancellation still ends the wait (`Task.Delay` throws `OperationCanceledException`, as today).
- The transient-code strings become internal constants shared by `HttpHelper` and the client, so they cannot drift.
**Acceptance (fake handler, 1 ms delay):**
- `[HttpRequestException, processing, HttpRequestException, ready]` → Ready with the solution.
- 3 consecutive `HttpRequestException` → returns `ErrorCode == "HttpError"` after exactly 3 `getTaskResult` calls.
- `errorId: 16` → returns Error after 1 call.
- A cancelled token → no further polls.

### FR-9 No retry of local failures (B9), Priority: P1
`SolveCaptchaAsync` stops at once, without another attempt, when `CreateCaptchaTaskAsync` returns the local `BadRequest` error (validation failure or null payload). The returned `TaskResultResponse` carries that error code and description as today. API-side create errors keep their current retry behavior (out of scope; see §7).
**Acceptance:** With `SolveAsyncRetries = 3` and an invalid request, the fake handler receives 0 requests and the result has `ErrorCode == "BadRequest"`. The loop body runs once.

### FR-10 DI honors HTTP timeout (B10), Priority: P1
- `AddAnticaptcha` registers a **named** client (internal const name, e.g. `"AntiCaptchaApi.Net"`) configured with `Timeout = TimeSpan.FromMilliseconds(clientConfig.MaxHttpRequestTimeMs)`. `HttpHelper` (factory constructor) calls `CreateClient(<name>)`.
- The public signature and return type of `AddAnticaptcha` are unchanged. The unnamed `services.AddHttpClient()` call may stay for backward compatibility.
**Acceptance:** After `AddAnticaptcha(key, c => c.MaxHttpRequestTimeMs = 12345)`, `IHttpClientFactory.CreateClient(<name>).Timeout == 12345 ms`, and the resolved `IAnticaptchaClient` sends requests through that named client (verified with `ConfigurePrimaryHttpMessageHandler` on the named client in the test).

### FR-11 AntiGate request (B11), Priority: P1
- A new public `AntiCaptchaApi.Net.Requests.AntiGateRequest : CaptchaRequest<AntiGateSolution>, IAntiGateRequest`:
  - `WebsiteUrl` (`websiteURL`, required, inherited)
  - `TemplateName` (`templateName`, required)
  - `Variables` (`variables`, required, `JObject` per the existing `IAntiGateRequest` contract. Callers can pass `JObject.FromObject(new { ... })`.)
  - `DomainsOfInterest` (`domainsOfInterest`, optional `List<string>`)
  - `ProxyConfig` (optional). Flattened to `proxyAddress/proxyPort/proxyLogin/proxyPassword` (+ `proxyType`, see Open Questions) by the existing `Build()` logic.
  - A parameterless constructor and a copy constructor `AntiGateRequest(IAntiGateRequest)`, matching the other request classes, with XML docs linking `https://anti-captcha.com/apidoc/task-types/AntiGateTask`.
- Task type is **always** `"AntiGateTask"`, with or without a proxy (anti-captcha uses the same type name).
- An internal `AntiGateRequestValidator` requires `WebsiteUrl`, `TemplateName` and `Variables` to be non-empty. The proxy is truly optional: `null` → no error; non-null → `ValidateProxy`. (The existing `ValidateOptionalProxy` reports an error for `null`, so it is not used here.)
- It is wired into the validator registry (`CreateCaptchaRequestHelper`) and the name registry (`RequestTaskNameHelper`). The `AntiGateSolution` converter is already registered.
**Acceptance:** Snapshot tests with and without a proxy. Validation errors for missing template/variables. End-to-end fake-handler `SolveCaptchaAsync(new AntiGateRequest{...})` returns Ready with `Url`, `Domain`, `Cookies`, `LocalStorage`, `Fingerprint` populated. `PushAntiGateVariableAsync` sends `taskId`, `name` and `value`.

### FR-12 Registry-completeness regression test (B12), Priority: P0
An offline test discovers, by reflection over the `AntiCaptchaApi.Net` assembly, every concrete (non-abstract, non-generic-definition) class implementing `ICaptchaRequest<T>`. For each one it asserts:
1. a validator is registered: `CreateCaptchaRequestHelper.Validate` does not throw `KeyNotFoundException`;
2. a task name is registered: `Build()` returns a non-empty `type`;
3. `HttpHelper.Converters` contains a converter whose `CanConvert(typeof(TaskResultResponse<T>))` is true.

It also asserts that every concrete `BaseSolution` subclass has a converter. Failures name the offending type.
**Acceptance:** The test is green after FR-1/FR-4/FR-11, and it fails (named type in the message) when any single registry entry is removed locally. That red state is shown during implementation and not committed.

### FR-13 Release hygiene (B13), Priority: P1
- **No automatic releases:** delete `.github/workflows/auto-dependency-update.yml`. Its daily cron raises dependency floors to the latest version and pushes a release tag, which conflicts with lowered floors and with changelog-driven releases. Dependabot replaces it.
- Add **`.github/dependabot.yml`**: ecosystems `nuget` (directory `/`) and `github-actions` (directory `/`), `interval: weekly`. The library's runtime dependencies (`Microsoft.Extensions.Http`, `Newtonsoft.Json`) are listed under `ignore` for version updates so the floors stay low on purpose. Security advisories still show up as Dependabot alerts.
- **`publish-nuget.yml`**: enable a test step that runs **only offline unit tests** after Build: `dotnet test AntiCaptchaApi.Net.sln --configuration Release --no-build --filter "FullyQualifiedName~UnitTests"`. A test failure must fail the job before pack/publish.
- **Lower dependency floors** in `AntiCaptchaApi.Net.csproj`:
  - `Microsoft.Extensions.Http` `10.0.12` → `8.0.0` (supports netstandard2.0+, so it covers all current TFMs `netstandard2.1; net8.0; net9.0`). If `dotnet list package --vulnerable --include-transitive` flags 8.0.0 or its transitive graph, use the lowest clean 8.0.x patch.
  - `Newtonsoft.Json` `13.0.4` → `13.0.1`, the first version without GHSA-5crp-9r3c-p9vr / CVE-2024-21907 (uncontrolled recursion / DoS on deeply nested JSON).
- **Why (minimum-version semantics):** a NuGet `PackageReference Version="X"` means **≥ X** for consumers, and NuGet resolves the *lowest* version that satisfies every constraint in the graph. A library floor of `10.0.12` forces every consumer, including a .NET 8 LTS app pinned to 8.x `Microsoft.Extensions.*`, to upgrade to 10.x. That can cause downgrade errors (NU1605) or pull 10.x assemblies into an 8.0 app. A low floor forces nothing: apps that already use newer versions keep them. The floor should be the lowest version that compiles, works and has no known vulnerability. Raising a floor is a deliberate, changelog-worthy decision, not a daily automation.
**Acceptance:** `dotnet build` passes for all TFMs with no new warnings. `dotnet list package --vulnerable --include-transitive` reports nothing for the library. The workflow YAML is valid (checked with `actionlint` if available, otherwise by review). No push, tag or publish.

### FR-14 Docs, changelog and version (B14), Priority: P1
- README: "A .NET 9 library" → "A .NET library (netstandard2.1, net8.0, net9.0)". The feature list drops HCaptcha and lists the supported types (ImageToText, ImageToCoordinates, reCAPTCHA v2/v2 Enterprise/v3/v3 Enterprise, FunCaptcha, GeeTest v3/v4, Turnstile, Amazon WAF, Prosopo, Friendly Captcha, AntiGate). Remove the contradictory "DI (Required) / only way" wording. Add an AntiGate and an Amazon WAF example. The "Changelog" section links to `CHANGELOG.md`.
- `ReportOutcome.IncorrectHCaptcha` gets `[Obsolete("hCaptcha is not supported by this library/anti-captcha; this value will be removed in 3.0.")]`, and the `switch` usage in `AnticaptchaClient.ReportTaskOutcomeAsync` is wrapped in `#pragma warning disable/restore CS0618`, so the build adds no new warnings.
- New `CHANGELOG.md` (Keep a Changelog style): a human-written `2.2.0` section (Fixed / Added / Changed / Deprecated / Security), with 2.1.6 to 2.1.18 collapsed into one "dependency updates" line and 2.1.2 to 2.1.5 preserved.
- `AntiCaptchaApi.Net.csproj`: `Version` and `PackageVersion` → `2.2.0`. `PackageReleaseNotes` becomes a short 2.2.0 summary plus "Full history: CHANGELOG.md" and a link.
**Acceptance:** `dotnet pack` produces `AntiCaptchaApi.Net.2.2.0.nupkg` locally, and the nuspec release notes are the short form. The README has no "HCaptcha" and no ".NET 9 library". There are no new build warnings. Nothing is published, tagged or pushed.

## 4. Non-Functional Requirements

- **NFR-1 Compatibility:** no public member is removed or has its signature changed. New public API is `AntiGateRequest` plus the protected `OnResultParsed` hook. Behavior changes are limited to the bug fixes above and are listed in CHANGELOG. Validation that is stricter than before (Turnstile proxy) only rejects requests the API would have rejected anyway.
- **NFR-2 Security:** the client key and proxy password never appear in `RawPayload`, in exception messages, or in new log/diagnostic strings.
- **NFR-3 Reliability:** under concurrent solves the result for task A is never built from task B's JSON. Polling survives up to 2 consecutive transient failures and gives up on the 3rd.
- **NFR-4 Performance:** the redaction parse runs once per request (payloads are small, well under 1 ms). Registry dictionaries may become `static readonly` (optional refactor; they are currently rebuilt per call).
- **NFR-5 Quality gates (workflow.md):** offline tests only; coverage ≥ 80% on new/changed code; no build warnings beyond the baseline of 15 (the goal is to lower it, not raise it); XML docs on all new public members; per-task conventional commits.
- **NFR-6 Test style:** xUnit, `Method_Condition_ExpectedResult` names, Arrange/Act/Assert, HTTP via a fake `HttpMessageHandler` injected through the public `AnticaptchaClient(string, ClientConfig, HttpClient)` constructor. No network and no real key.

## 5. User Stories

- **US-1 (Amazon WAF):** As a scraper developer, I want `SolveCaptchaAsync(new AmazonWafProxylessRequest{...})` to create a task, so that I can get an `aws-waf-token`.
  *Given* a valid WAF request, *when* I call `SolveCaptchaAsync`, *then* a `createTask` with `type: AmazonTaskProxyless` is sent and no `KeyNotFoundException` is thrown.
- **US-2 (reCAPTCHA v3):** As a developer, I want a minimal v3 request (URL, key) to work, so that I don't have to set `IsEnterprise = false` by hand.
  *Given* `new RecaptchaV3Request{WebsiteUrl, WebsiteKey}`, *when* I solve it, *then* validation passes and the task is created.
- **US-3 (Never waste a paid task):** As a service owner, I want polling to survive a brief network blip, so that I am not charged for a solve I never receive.
  *Given* a created task and one failed `getTaskResult` HTTP call, *when* the next poll succeeds with `ready`, *then* I receive the solution.
- **US-4 (Secrets):** As a developer who logs `RawPayload` for debugging, I want the key and proxy password masked, so that my logs don't leak credentials.
  *Given* any API call, *when* I read `response.RawPayload`, *then* `clientKey` is `"***"`.
- **US-5 (AntiGate):** As an automation developer, I want to run an AntiGate template, so that I can use anti-captcha's AntiGate feature from .NET.
  *Given* `new AntiGateRequest{WebsiteUrl, TemplateName, Variables}`, *when* I solve it, *then* an `AntiGateTask` is created and I get cookies/fingerprint in the solution.
- **US-6 (DI timeout):** As an ASP.NET Core developer, I want `MaxHttpRequestTimeMs` to take effect when I use `AddAnticaptcha`, so that slow calls time out as configured.
- **US-7 (Concurrency):** As a service running parallel solves, I want each result to be parsed from its own response, so that I never get another task's cookies.

## 6. Technical Considerations

- **Fake HTTP:** a test helper `FakeHttpMessageHandler` (queue of responses or exceptions, captured request bodies and URIs) in `AntiCaptchaApi.Net.Tests/Helpers/`. It is injected through `new AnticaptchaClient("test-key", config, new HttpClient(handler))`. `InternalsVisibleTo(AntiCaptchaApi.Net.Tests)` already exists, so internal helpers (`CreateCaptchaRequestHelper`, `HttpHelper`, `PayloadRedactor`) can be tested directly.
- **Registry test:** `CreateCaptchaRequestHelper.Validate<TSolution>` / `Build<TSolution>` are generic. The test calls them through `MethodInfo.MakeGenericMethod(solutionType)` and unwraps `TargetInvocationException` to report `KeyNotFoundException` for the named type.
- **DI test:** `ServiceCollection` comes from `Microsoft.Extensions.DependencyInjection`, which is transitively available through `Microsoft.Extensions.Http` → `Microsoft.Extensions.Logging`. If not, add it as a test-only package reference.
- **Obsolete enum member:** marking `IncorrectHCaptcha` obsolete triggers CS0618 at `AnticaptchaClient.cs:126`. Suppress it locally with a pragma.
- **Warnings baseline:** record the build warning count before and after each phase. It must be ≤ 15.
- **Behavior of `ValidateOptionalProxy`:** it currently adds an error when `ProxyConfig == null` (`ValidationResultExtensions.cs:79-82`). This affects `FunCaptchaRequest` and `AmazonWafRequest`. Changing it is out of scope for this track (see §7). AntiGate uses its own null-tolerant check.
- **Local runtime:** only the .NET 10 runtime is installed locally, hence `DOTNET_ROLL_FORWARD=Major`. In the Claude Code sandbox, `dotnet` needs the sandbox disabled.

## 7. Out of Scope

- Everything breaking from `design-v3.md`: record-based task model, `SolveAsync`/`TrySolveAsync`, exceptions instead of error data, System.Text.Json, netstandard2.0 retarget, a `MinScore` enum, removing `0.5`, the separate DI package, `IHttpClientBuilder`, `ILogger`/`ActivitySource`/`Meter`, the per-type first-poll delay, backoff/jitter, retryable-vs-fatal classification of API error codes, a static shared handler, and removing public `ClientKey` or the `protected jObject` field.
- Changing `ValidateOptionalProxy` null semantics (FunCaptcha/AmazonWaf) or FriendlyCaptcha's use of `ProsopoSolution`.
- Adding an hCaptcha request type.
- Package validation / API-compat tooling, SourceLink, deterministic builds, and a package icon (3.0 release pipeline).
- Any push, tag, publish, or change to `conductor/tracks.md`.
- Running `IntegrationTests/`.

## 8. Open Questions (defaults chosen; confirm or override)

1. **AntiGate `proxyType`:** the AntiGateTask docs may list only `proxyAddress/proxyPort/proxyLogin/proxyPassword`. *Default:* check `https://anti-captcha.com/apidoc/task-types/AntiGateTask` before writing the snapshot. If `proxyType` is not documented, `Build()` removes `proxyType` for `AntiGateRequest`; otherwise keep the generic flattening.
2. **Transient-poll limit as public config?** *Default:* internal constant `3` (minimal 2.x surface; v3 exposes `SolveOptions`). Alternative: a new `ClientConfig.MaxConsecutivePollingErrors` (non-breaking).
3. **`auto-dependency-update.yml`:** *Default:* delete it. Alternative: keep it as `workflow_dispatch`-only with `dry_run` defaulting to `true`. Note that a manual run would still raise floors back to latest.
4. **Microsoft.Extensions.Http floor:** *Default:* `8.0.0`, or the lowest vulnerability-free 8.0.x.

### Resolution of Open Question 1 (2026-09-30, checked https://anti-captcha.com/apidoc/task-types/AntiGateTask)
- Task fields: `type` = `AntiGateTask` (same name with or without proxy), `websiteURL`, `templateName`, `variables` (object), optional `domainsOfInterest` (array), and proxy fields `proxyAddress`, `proxyPort`, `proxyLogin`, `proxyPassword`.
- **`proxyType` is not documented** → `Build()` must remove `proxyType` for `AntiGateRequest`.
- The page says: "Note that there are optional proxy parameters. Only HTTPS proxies are accepted, and 'proxyAddress' must be an IP address." The table marks the proxy fields "Yes" (required when a proxy is used). Decision: the proxy stays optional as a whole; when `ProxyConfig` is set, validate it with the existing `ValidateProxy` (address + port). Login/password stay optional, as for all other types, to avoid over-rejecting.
- Documented solution fields: `cookies`, `localStorage`, `sessionStorage`, `fingerprint`, `url`, `domain`, `HTMLsInBase64`, `screenshots`, `requestHeaders`, `responseHeaders`. `AntiGateSolution` currently lacks `SessionStorage`, `Screenshots`, `RequestHeaders`, `ResponseHeaders` → add them (additive, parsed like the existing `JObject` fields; arrays wrapped as today by `ParseSolutionJObject`) in Task 3.3.
- `AntiGateSolution.IsValid()` requires `LocalStorage` and `Fingerprint`, which a template may legitimately not return. Out of scope for 2.x (behavior change); revisit in 3.0.
