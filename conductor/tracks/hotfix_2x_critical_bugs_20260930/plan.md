# Implementation Plan: 2.x critical hotfix → release 2.2.0

Track: `hotfix_2x_critical_bugs_20260930` · Spec: `./spec.md` · Branch: `fix/2x-critical-bugs`

## Overview

Four phases. Each fixes one area and ends in a user-confirmed checkpoint (see `workflow.md`, "Phase Completion Verification and Checkpointing").

1. **Correctness:** registries and validation (B1, B2, B3, B4, B6, B12).
2. **Resilience and security:** converter thread-safety, redaction, polling, retries, DI timeout (B5, B7, B8, B9, B10).
3. **AntiGate feature** (B11).
4. **Release hygiene and docs:** CI, Dependabot, dependency floors, obsolete, README, CHANGELOG, 2.2.0 (B13, B14).

Rules for every task:
- Red → Green → Refactor. Run the offline suite after Green: `DOTNET_ROLL_FORWARD=Major dotnet test AntiCaptchaApi.Net.sln --filter "FullyQualifiedName~UnitTests"`.
- Never run `IntegrationTests/`.
- The build warning count must be ≤ 15 (baseline) after every task.
- Commit per task with the suggested conventional message. The body is a short summary, the changed files, and why. Trailer: `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Then record the SHA here and make a separate `conductor(plan): …` commit.
- No push, tag or publish.

Manual verification probe (used at every checkpoint): a throwaway console app in the scratchpad (not committed) that references `AntiCaptchaApi.Net/bin/Debug/net8.0/AntiCaptchaApi.Net.dll`, or a `ProjectReference` to the csproj. It uses `new AnticaptchaClient("probe-key", config, new HttpClient(new StubHandler()))`, where `StubHandler` returns canned anti-captcha JSON and prints each request body. It needs no real key and spends no money. The optional live check is run **by the user** with their own key and is noted per phase.

---

## Phase 1: Correctness (registries and validation)
Goal: every existing request type can be validated, named and parsed. Validation matches anti-captcha's rules. A guard test stops registry drift.

Tasks:
- [x] Task 1.1: Add the `FakeHttpMessageHandler` test helper (no red phase: test infrastructure). [5c15351]
  - `AntiCaptchaApi.Net.Tests/Helpers/FakeHttpMessageHandler.cs`: FIFO queue of `Func<HttpRequestMessage, HttpResponseMessage>` or an exception to throw. Captures `RequestUri` and body strings. Exposes `RequestCount`.
  - Add a self-test `UnitTests/FakeHttpMessageHandlerTests.cs` (returns queued JSON, throws a queued `HttpRequestException`, records the body).
  - Commit: `test(infra): add FakeHttpMessageHandler for offline client tests`
- [x] Task 1.2: Register Amazon WAF task names (TDD, B1 / FR-1). [a60b49d]
  - Red: in `UnitTests/RequestSerializationTests.cs`, add `AmazonWafProxylessRequestSerializationTest` (`type = AmazonTaskProxyless`, `websiteURL`, `websiteKey`, `iv`, `context`, optional scripts) and `AmazonWafRequestSerializationTest` (`type = AmazonTask` + flattened proxy). Add `UnitTests/AmazonWafClientTests.cs`: `CreateCaptchaTaskAsync_AmazonWafProxyless_SendsAmazonTaskProxyless` using the fake handler. It currently fails with `KeyNotFoundException` from `RequestTaskNameHelper.cs:39`.
  - Green: add `{ typeof(AmazonWafProxylessRequest), "AmazonTaskProxyless" }` and `{ typeof(AmazonWafRequest), "AmazonTask" }` to `Internal/Helpers/RequestTaskNameHelper.cs`.
  - Commit: `fix(amazon-waf): register AmazonTask/AmazonTaskProxyless task names`
  - Note: also added `CreateCaptchaTaskAsync_AmazonWafWithProxy_SendsAmazonTask` (FR-1 covers both types). Red: all 4 new tests failed with `KeyNotFoundException` for the WAF request types.
- [x] Task 1.3: Add missing result converters for Prosopo and Amazon WAF (TDD, B4 / FR-4). [8a10bd1]
  - Red: `UnitTests/TaskResultConverterTests.cs`. Through the fake handler, call `GetTaskResultAsync<ProsopoSolution>` / `<AmazonWafSolution>` with ready JSON (`createTime`, `endTime`) → assert `CreateTimeUtc`/`EndTimeUtc` are set. With `{"errorId":12,"errorCode":"ERROR_CAPTCHA_UNSOLVABLE"}` → assert `Status == TaskStatusType.Error`. Include one existing type (Turnstile) as a control.
  - Green: add `new TaskResultConverter<ProsopoSolution>()` and `new TaskResultConverter<AmazonWafSolution>()` to `Internal/Helpers/HttpHelper.cs:19-29`. Change the list to `internal static readonly IReadOnlyList<JsonConverter> Converters` (it is also passed as an array to `DeserializeObject`; cache the array).
  - Commit: `fix(converters): register TaskResultConverter for Prosopo and AmazonWaf solutions`
  - Red: Prosopo/WAF ready → `CreateTimeUtc` null; `errorId 12` → `Status` null (Turnstile control green). Converters exposed as `internal static readonly IReadOnlyList<JsonConverter> Converters` wrapping a cached `ConverterArray` passed to `DeserializeObject`.
- [x] Task 1.4: reCAPTCHA v3: `IsEnterprise` null means false, and MinScore accepts 0.9 (TDD, B2 / FR-2). [ff51d9e]
  - Red: `UnitTests/RecaptchaV3RequestValidatorTests.cs`:
    - `Validate_DefaultRequest_IsValid`
    - `Validate_EnterpriseRequestWithoutIsEnterprise_IsValid` (through `CreateCaptchaRequestHelper.Validate`)
    - `[Theory] Validate_MinScore_Accepted(0.3, 0.5, 0.7, 0.9)`
    - `[Theory] Validate_MinScore_Rejected(0.4, 1.0, 0)`
  - Green: in `Internal/Validation/Validators/RecaptchaV3RequestValidator.cs`, remove `.ValidateIsNotNull(IsEnterprise)` and change the whitelist to `{0.3m, 0.5m, 0.7m, 0.9m}`. Update the XML doc of `RecaptchaV3Request.MinScore` ("0.3, 0.7 or 0.9; 0.5 is accepted for backward compatibility but is not an anti-captcha queue") and of `IsEnterprise` ("null = false").
  - Verify that the existing V3 serialization tests stay green (no `isEnterprise` key when null).
  - Commit: `fix(recaptcha-v3): treat null IsEnterprise as false and accept MinScore 0.9`
  - Red: default V3 and V3 Enterprise failed with "IsEnterprise must not be null!"; 0.9 rejected ("Correct values: 0.3,0.5,0.7"). `Validate_MinScore_Rejected` also asserts the error is on `MinScore` and lists all 4 accepted values. `RecaptchaV3RequestSerializationTest_OnlyRequiredArguments` (no `isEnterprise` key) stays green.
- [x] Task 1.5: Validate the Turnstile proxy with the proxy validator, without newly requiring a UA (TDD, B3 / FR-3). [6f7d560]
  - Red: `UnitTests/ProxyValidationTests.cs`:
    - `Validate_TurnstileRequestWithoutProxy_ReturnsProxyConfigError`
    - `Validate_TurnstileRequestWithProxyNoUserAgent_IsValid`
    - Table-driven `[Theory]` over the proxy-required types (`RecaptchaV2Request`, `RecaptchaV2EnterpriseRequest`, `GeeTestV3Request`, `GeeTestV4Request`, `TurnstileCaptchaRequest`, `ProsopoRequest`, `FriendlyCaptchaRequest`). With all other required fields set and `ProxyConfig = null`, assert an error on `ProxyConfig`.
  - Green: `CreateCaptchaRequestHelper.cs:34` → `new TurnstileRequestValidator().Validate(request as TurnstileCaptchaRequest)`. Change `TurnstileRequestValidator` to take a `TurnstileCaptchaRequest` overload, or keep the override with a safe `as` cast. It validates base + `ValidateProxy` only; the UA requirement at line 14 is removed. Fix the `TurnstileCaptchaRequest.UserAgent` XML doc to say "[Optional]".
  - Commit: `fix(turnstile): validate proxy for TurnstileTask and keep UserAgent optional`
  - Red: Turnstile without proxy / with `ProxyPort = 0` → no errors; the theory failed only for `TurnstileCaptchaRequest`. Kept the existing override with a safe `as` cast. Added `Validate_TurnstileRequestWithProxyPortZero_ReturnsProxyPortError` (FR-3 acceptance). The theory is `Validate_ProxyRequiredRequestWithoutProxy_ReturnsProxyConfigError(string requestType)`.
- [ ] Task 1.6: Accept proxy port 65535 (TDD, B6 / FR-6).
  - Red: in `UnitTests/ProxyValidationTests.cs`, add `[Theory] ValidateProxy_Port(1 ok, 65535 ok, 0 err, 65536 err, null err)`, and the same for `ValidateOptionalProxy`.
  - Green: `Internal/Extensions/ValidationResultExtensions.cs:65,89`: `>= 65535` → `> 65535`, and the message becomes "between 1 and 65535".
  - Commit: `fix(validation): accept proxy port 65535`
- [ ] Task 1.7: Registry-completeness regression test (TDD guard, B12 / FR-12).
  - Red: write `UnitTests/RegistryCompletenessTests.cs`:
    - `EveryRequestType_HasValidator`
    - `EveryRequestType_HasTaskName`
    - `EveryRequestType_SolutionHasConverter`
    - `EverySolutionType_HasConverter`

    Use reflection over `typeof(AnticaptchaClient).Assembly`, generic invocation via `MakeGenericMethod`, and unwrap `TargetInvocationException`. The messages name the type. To show the red state, temporarily remove the WAF name entry and the Prosopo converter, confirm that the failures name those types, then restore them. Record "red verified locally" here and do not commit the red state.
  - Green: already green after 1.2 and 1.3. Optional refactor: make the registry dictionaries `static readonly` (no behavior change).
  - Commit: `test(registry): guard that every request type has validator, task name and converter`
- [ ] Verification: Phase 1 manual checks [checkpoint marker]
  - Run the offline suite (expect 27 + new tests, all green) and `dotnet build` (warnings ≤ 15).
  - Probe: create `AmazonWafProxylessRequest`, `RecaptchaV3Request` (URL+key only), `RecaptchaV3EnterpriseRequest`, and `TurnstileCaptchaRequest` without a proxy. Print each `createTask` body, or the returned validation error for Turnstile. Parse canned Prosopo/WAF `getTaskResult` JSON and print `CreateTimeUtc`.
  - Optional live check (user, own key): solve one `RecaptchaV3Request` without `IsEnterprise`, and one Amazon WAF on a known test page.
  - Wait for user confirmation → checkpoint commit `conductor(checkpoint): Checkpoint end of Phase 1`.

## Phase 2: Resilience and security
Goal: concurrent solves are safe, secrets never leak through `RawPayload`, a paid task survives transient errors, pointless retries stop, and DI honors the timeout.

Tasks:
- [ ] Task 2.1: Make `TaskResultConverter` stateless (TDD, B5 / FR-5).
  - Red: in `UnitTests/TaskResultConverterTests.cs`, add:
    - `ReadJson_ParallelAntiGateResponses_EachResultUsesOwnJson`. 200 distinct ready AntiGate JSONs are deserialized with `Parallel.For` using the shared `HttpHelper.Converters`; assert that `Solution.Cookies["id"]` equals its own index.
    - `Converters_HaveNoMutableInstanceFields`: reflection over `TaskResultConverter<>` and `AntiGateTaskResultConverter`, ignoring `[Obsolete]` members.

    The race test is probabilistic, so the reflection test is the deterministic red.
  - Green: in `Internal/Converters/TaskResultConverter.cs`, use a local `var source = JObject.Load(reader)` and add `protected virtual void OnResultParsed(JObject source, TaskResultResponse<T> response) { }`, called before returning. Keep `protected JObject jObject` with `[Obsolete(...)]` and `[EditorBrowsable(EditorBrowsableState.Never)]`, never assigned (binary compatibility). `AntiGateTaskResultConverter` overrides `OnResultParsed` instead of overriding `ReadJson` and reading the field.
  - Commit: `fix(converters): make TaskResultConverter stateless to fix concurrent-solve race`
- [ ] Task 2.2: Redact secrets in `RawPayload` (TDD, B7 / FR-7).
  - Red: `UnitTests/PayloadRedactorTests.cs`:
    - `Redact_ClientKey_Masked`
    - `Redact_NestedProxyPassword_Masked`
    - `Redact_OtherFields_Unchanged`
    - `Redact_InvalidJson_ReturnsPlaceholder`

    `UnitTests/RawPayloadRedactionTests.cs` (fake handler): for success, `HttpRequestException`, invalid-JSON response, and cancellation, `RawPayload` contains neither `"test-key"` nor the proxy password. The handler-captured body still contains both.
  - Green: new `Internal/Helpers/PayloadRedactor.cs` (internal static). `HttpHelper.cs`: compute `var redacted = PayloadRedactor.Redact(payload)` once and assign it at all 5 `RawPayload` sites (lines 75, 87, 96, 106, 115).
  - Commit: `fix(security): redact clientKey and proxyPassword from RawPayload`
- [ ] Task 2.3: Keep polling on transient errors (TDD, B8 / FR-8).
  - Red: `UnitTests/WaitForTaskResultTests.cs`, with `ClientConfig { DelayTimeBetweenCheckingTaskResultMs = 1 }`:
    - `Wait_TransientErrorThenReady_ReturnsSolution` (`[throw, processing, throw, ready]`)
    - `Wait_ThreeConsecutiveTransientErrors_ReturnsLastErrorAfterThreePolls`
    - `Wait_TransientErrorsNotConsecutive_DoesNotGiveUp` (`[throw, throw, processing, throw, throw, ready]`)
    - `Wait_ApiError_ReturnsImmediately` (`errorId 16`, 1 call)
    - `Wait_HttpTimeoutWithoutCallerCancellation_IsTransient` (the handler throws `TaskCanceledException`)
    - `Wait_CallerCancelled_StopsPolling`
    - `Wait_MaxWaitExceeded_ReturnsTimeout` (always processing, small `MaxWaitForTaskResultTimeMs`)
  - Green: new `Internal/Common/TransportErrorCodes.cs` (`HttpError`, `JsonError`, `UnexpectedError`, `Cancelled`), used by `HttpHelper`. In `AnticaptchaClient.WaitForTaskResultAsync` (lines 279-306), a `consecutiveTransientErrors` counter with an internal const limit of 3. A transient error with `!cancellationToken.IsCancellationRequested` → `continue`; any valid status resets the counter.
  - Commit: `fix(polling): keep polling a paid task through transient HTTP errors`
- [ ] Task 2.4: Do not retry local validation failures (TDD, B9 / FR-9).
  - Red: `UnitTests/SolveCaptchaAsyncTests.cs`:
    - `Solve_InvalidRequest_DoesNotRetryAndSendsNoHttp` (`SolveAsyncRetries = 3`; handler `RequestCount == 0`; `ErrorCode == "BadRequest"`)
    - `Solve_TaskErrorStatus_RetriesUpToConfiguredCount` (current behavior kept)
  - Green: in `AnticaptchaClient.SolveCaptchaAsync` (lines 162-174), `break` when `createTaskResponse.ErrorCode == HttpStatusCode.BadRequest.ToString()`, before the next iteration. The existing error-copy tail stays.
  - Commit: `fix(solve): stop retrying requests that fail local validation`
- [ ] Task 2.5: DI uses a named client with `MaxHttpRequestTimeMs` (TDD, B10 / FR-10).
  - Red: `UnitTests/ServiceCollectionExtensionsTests.cs`:
    - `AddAnticaptcha_ConfiguresNamedClientTimeout` (12345 ms)
    - `AddAnticaptcha_ClientUsesNamedHttpClient`: register `ConfigurePrimaryHttpMessageHandler(() => fake)` on the named client in the test, resolve `IAnticaptchaClient`, call `GetBalanceAsync`, and assert that the fake received the request.

    Add a `Microsoft.Extensions.DependencyInjection` test package reference only if `ServiceCollection` is not transitively available.
  - Green: internal const `HttpClientName = "AntiCaptchaApi.Net"`. `services.AddHttpClient(HttpClientName, c => c.Timeout = TimeSpan.FromMilliseconds(clientConfig.MaxHttpRequestTimeMs))`. `HttpHelper(IHttpClientFactory)` → `CreateClient(HttpClientName)` at `HttpHelper.cs:49`. The `AddAnticaptcha` signature is unchanged.
  - Commit: `fix(di): use named HttpClient honoring ClientConfig.MaxHttpRequestTimeMs`
- [ ] Verification: Phase 2 manual checks [checkpoint marker]
  - Run the offline suite (all green) and `dotnet build` (warnings ≤ 15). Coverage run: new and changed code ≥ 80%.
  - Probe:
    - A `StubHandler` that fails the 1st and 3rd `getTaskResult` → the solve still returns Ready.
    - Print `RawPayload` → shows `"clientKey": "***"`.
    - An invalid request with `SolveAsyncRetries = 3` → the stub reports 0 HTTP calls.
    - 50 parallel AntiGate parses → no cross-talk.
    - A DI container with `MaxHttpRequestTimeMs = 2000` → print the named client's `Timeout`.
  - Optional live check (user): one `ImageToTextRequest` solve with the network briefly toggled during polling; inspect `RawPayload` in the output.
  - Wait for user confirmation → `conductor(checkpoint): Checkpoint end of Phase 2`.

## Phase 3: AntiGate feature
Goal: AntiGate templates can be solved end to end, using the existing solution, converter and `PushAntiGateVariableAsync`.

Tasks:
- [ ] Task 3.1: Add `AntiGateRequest` with serialization (TDD, B11 / FR-11).
  - Pre-step: check `https://anti-captcha.com/apidoc/task-types/AntiGateTask` for the exact field names and whether `proxyType` is accepted (spec Open Question 1). Record the finding in the spec with a dated note.
  - Red: in `RequestSerializationTests.cs`, add:
    - `AntiGateRequestSerializationTest` (`type = AntiGateTask`, `websiteURL`, `templateName`, `variables` object, `domainsOfInterest`, flattened proxy)
    - `AntiGateRequestSerializationTest_OnlyRequiredArguments` (same type name, no proxy keys)
  - Green: new `AntiCaptchaApi.Net/Requests/AntiGateRequest.cs`, `public class AntiGateRequest : CaptchaRequest<AntiGateSolution>, IAntiGateRequest`, with `TemplateName`, `JObject Variables`, `List<string> DomainsOfInterest`, `ProxyConfig`, a parameterless constructor and a copy constructor. It has full XML docs with a docs link. Add a `RequestTaskNameHelper` entry `{ typeof(AntiGateRequest), "AntiGateTask" }`. In `Build()`, strip `proxyType` for AntiGate only if the pre-step says so.
  - Commit: `feat(antigate): add AntiGateRequest for AntiGateTask`
- [ ] Task 3.2: `AntiGateRequestValidator` and registry wiring (TDD).
  - Red: `UnitTests/AntiGateRequestValidatorTests.cs`:
    - `Validate_Minimal_IsValid`
    - `Validate_MissingTemplateName_Error`
    - `Validate_NullVariables_Error`
    - `Validate_MissingWebsiteUrl_Error`
    - `Validate_NullProxy_IsValid`
    - `Validate_InvalidProxyPort_Error`

    Run them through `CreateCaptchaRequestHelper.Validate`. `RegistryCompletenessTests` goes red until wired.
  - Green: new internal `Internal/Validation/Validators/AntiGateRequestValidator.cs` (`CaptchaRequestValidator<AntiGateRequest, AntiGateSolution>`) with a null-tolerant proxy check (`ProxyConfig == null ? result : result.ValidateProxy(...)`). Register it in `CreateCaptchaRequestHelper`.
  - Commit: `feat(antigate): validate AntiGateRequest and register it`
- [ ] Task 3.3: End-to-end AntiGate solve and push variable (TDD).
  - Red: `UnitTests/AntiGateClientTests.cs`:
    - `SolveCaptchaAsync_AntiGate_ReturnsParsedSolution` (fake: createTask `{taskId}`, then ready JSON with `url`, `domain`, `cookies`, `localStorage`, `fingerprint`); assert all fields.
    - `PushAntiGateVariableAsync_SendsTaskIdNameValue` (inspect the captured body; `clientKey` is present in the wire body but `RawPayload` is redacted).
  - Green: any fixes they uncover (expected: none beyond 3.1/3.2).
  - Commit: `test(antigate): cover end-to-end AntiGate solve and pushAntiGateVariable`
- [ ] Verification: Phase 3 manual checks [checkpoint marker]
  - Run the offline suite (all green, including `RegistryCompletenessTests` with `AntiGateRequest` discovered) and `dotnet build` (warnings ≤ 15).
  - Probe: build an `AntiGateRequest` with `JObject.FromObject(new { css_selector = "#x" })` and a proxy. Print the `createTask` body (type `AntiGateTask`, redacted `RawPayload`). Feed a canned ready response and print the cookies.
  - Optional live check (user): run one inexpensive AntiGate template (e.g. "Wait for control appearance") against a test page.
  - Wait for user confirmation → `conductor(checkpoint): Checkpoint end of Phase 3`.

## Phase 4: Release hygiene and docs
Goal: releases are intentional, CI runs the offline tests, dependency floors are minimal, the docs are accurate, and the package is versioned 2.2.0 locally (not published).

Tasks:
- [ ] Task 4.1: Remove the auto-release cron and add Dependabot (no red phase: CI config).
  - Delete `.github/workflows/auto-dependency-update.yml` (spec Open Question 3; the alternative is `workflow_dispatch` only with `dry_run: true` by default).
  - Add `.github/dependabot.yml` (`version: 2`):
    - `nuget` at `/` weekly, with `ignore` entries for `Microsoft.Extensions.Http` and `Newtonsoft.Json` (floors are intentional)
    - `github-actions` at `/` weekly
  - Validate the YAML (`actionlint` / `yamllint` if installed, otherwise review).
  - Commit: `chore(ci): replace daily auto-release cron with weekly Dependabot`
- [ ] Task 4.2: Run offline unit tests in `publish-nuget.yml` (no red phase: CI config).
  - Replace the commented-out step (lines 32-34) with `- name: Test (offline unit tests)` running `dotnet test AntiCaptchaApi.Net.sln --configuration Release --no-build --filter "FullyQualifiedName~UnitTests"`, placed before "Determine Package Version" so that a failure blocks pack/publish.
  - Locally, run the same command with `DOTNET_ROLL_FORWARD=Major` in Release.
  - Commit: `chore(ci): run offline unit tests before packing`
- [ ] Task 4.3: Lower dependency floors (verification-driven; red = vulnerability/compat check).
  - `AntiCaptchaApi.Net.csproj:59-60`: `Microsoft.Extensions.Http` → `8.0.0`, `Newtonsoft.Json` → `13.0.1`.
  - Run `dotnet restore`, `dotnet build` (all 3 TFMs, warnings ≤ 15), `dotnet list AntiCaptchaApi.Net/AntiCaptchaApi.Net.csproj package --vulnerable --include-transitive`. If anything is flagged, raise to the lowest clean patch and note it in the spec. Run the offline suite.
  - Update the `tech-stack.md` "Current (2.x)" rows with a dated note, as workflow.md requires tech-stack changes to be documented first.
  - Commit: `chore(deps): lower dependency floors to Microsoft.Extensions.Http 8.0.0 and Newtonsoft.Json 13.0.1`
- [ ] Task 4.4: Mark `ReportOutcome.IncorrectHCaptcha` obsolete (TDD).
  - Red: `UnitTests/ReportOutcomeTests.cs`, `IncorrectHCaptcha_IsObsolete` (reflection: `ObsoleteAttribute` present, not an error-level obsolete). `ReportTaskOutcomeAsync_IncorrectHCaptcha_StillCallsApi` (fake handler, URI ends with `reportIncorrectHCaptcha`) so existing callers keep working.
  - Green: add `[Obsolete("...removed in 3.0")]` on `Enums/ReportOutcome.cs:30` and `#pragma warning disable/restore CS0618` around `AnticaptchaClient.cs:126` (and in the test). The warning count must not grow.
  - Commit: `refactor(report): mark ReportOutcome.IncorrectHCaptcha obsolete`
- [ ] Task 4.5: Fix the README (no red phase: docs).
  - Line 3: ".NET 9 library" → ".NET library (netstandard2.1, net8.0, net9.0)".
  - Line 9: the accurate type list (no HCaptcha; add Amazon WAF, Prosopo, Friendly Captcha, AntiGate).
  - Lines 86-88: remove "DI (Required) / only way".
  - Add Amazon WAF and AntiGate examples and a short note on resilient polling and redacted `RawPayload`.
  - The Changelog section → a link to `CHANGELOG.md`.
  - Commit: `docs(readme): correct supported frameworks and captcha types, add AntiGate/WAF examples`
- [ ] Task 4.6: Add CHANGELOG.md, collapse PackageReleaseNotes, bump to 2.2.0 (no red phase: release metadata).
  - New `CHANGELOG.md`, with a human-written 2.2.0 section:
    - Fixed: B1-B10
    - Added: AntiGateRequest
    - Changed: floors, CI
    - Deprecated: IncorrectHCaptcha, MinScore 0.5
    - Security: RawPayload redaction, Newtonsoft floor
    - Older history: 2.1.6-2.1.18 collapsed to one "dependency updates" entry; 2.1.2-2.1.5 kept.
  - `AntiCaptchaApi.Net.csproj`: `Version`/`PackageVersion` → `2.2.0`. `PackageReleaseNotes` → a short 2.2.0 summary plus the CHANGELOG link.
  - Check: `dotnet pack AntiCaptchaApi.Net/AntiCaptchaApi.Net.csproj -c Release -o <scratchpad>` produces `AntiCaptchaApi.Net.2.2.0.nupkg`. Inspect the nuspec release notes and dependency ranges (`>= 8.0.0`, `>= 13.0.1`).
  - Commit: `chore(release): prepare 2.2.0 with CHANGELOG.md`
- [ ] Verification: Phase 4 manual checks and release readiness [checkpoint marker]
  - Full offline suite (Release and Debug), `dotnet build` warnings ≤ 15 (report the final count), and coverage report ≥ 80% on changed code.
  - Probe against the **packed** 2.2.0 nupkg: a local feed in the scratchpad, `dotnet add package AntiCaptchaApi.Net --version 2.2.0 --source <scratchpad feed>` in a net8.0 console app. Re-run the Phase 1-3 probe scenarios and confirm that NuGet resolves `Microsoft.Extensions.Http` 8.0.x when the app has no other reference.
  - Review `git diff --name-only <phase-1 base> HEAD`: every changed code file has tests. Confirm that `tracks.md` is untouched and nothing was pushed or tagged.
  - Optional live check (user): the Phase 1-3 live checks against the packed build.
  - Wait for user confirmation → `conductor(checkpoint): Checkpoint end of Phase 4`. Publishing (tag `v2.2.0`) is a separate, explicit user decision outside this track.
