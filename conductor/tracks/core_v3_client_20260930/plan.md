# Implementation Plan: Core 3.0 client

Track: `core_v3_client_20260930`. Spec: `./spec.md`. Design: `conductor/design-v3.md` §4.

## Overview
There are 7 phases, in build order: foundation → task model and catalog → transport and errors → pipeline → account endpoints → observability → removal and docs. Each phase leaves the solution building with all offline tests green.

Conventions for every task (see `conductor/workflow.md`):
- Red → Green → Refactor. Run the listed tests red first and confirm they fail for the right reason. Then run the full offline suite:
  `DOTNET_ROLL_FORWARD=Major dotnet test AntiCaptchaApi.Net.sln --filter "FullyQualifiedName~UnitTests"`
- All new tests live under the namespace `AntiCaptchaApi.Net.Tests.UnitTests.*` so that the filter picks them up. Live tests live under `AntiCaptchaApi.Net.Tests.LiveTests.*` and are **never** run in task loops.
- The test project targets **net10.0** (the only local runtime). Tests exercise the core `net8.0` asset. `netstandard2.0` is verified by building only, and by the NFR-1 API rules.
- Commit per task: `<type>(<scope>): <description>`. The body is a summary, the files changed and the reason, plus the trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Then mark `[x] <sha7>` in this file and make a separate `conductor(plan): Mark task '<name>' as complete` commit.
- Quality gates: 0 new warnings (the core project has `WarningsAsErrors=nullable;CS1591`), XML docs on every public member, coverage ≥ 80 % on new code, no secrets in messages/`ToString`/telemetry.
- Paths: core = `AntiCaptchaApi.Net/`, tests = `AntiCaptchaApi.Net.Tests/`, probe = `tools/AntiCaptchaApi.Net.Probe/`.
- Never push, tag or publish.

---

## Phase 1: Foundation (branch, legacy capture, TFMs, JSON, test infra) [checkpoint: c751ad5]
Goal: branch `v3` exists and the 2.x wire contract and public API are frozen into checked-in files. After that, the core targets `netstandard2.0;net8.0` with STJ and no Newtonsoft/M.E.Http, 2.x code is excluded from compilation, and the test project (net10.0) has fakes, snapshot tooling and the probe.

Tasks:
- [x] Task 1.1: Create branch and record baseline (no red phase — setup) 1c2e398
  - Precondition: `hotfix_2x_critical_bugs_20260930` is complete. Run `git switch fix/2x-critical-bugs && git switch -c v3`.
  - Done 2026-09-30: branch `v3` was already created by the orchestrator from `fix/2x-critical-bugs` at 85cd2e2 (+ track-start commit 77c8966); only the baseline was recorded.
  - Run build and offline tests. Record the test count and warning count under "Baselines" at the end of this file.
  - Commit: none (only the plan update `conductor(plan): Record v3 baseline`).

- [x] Task 1.2: Capture 2.x wire contract and public API (TDD: characterization) fa71303
  - Add `AntiCaptchaApi.Net.Tests/UnitTests/Legacy/LegacyContractCapture.cs`. It is temporary and uses 2.x `CreateCaptchaRequestHelper.Build` and Newtonsoft.
  - `Capture_EveryLegacyRequestType_WritesWireSnapshot` (Theory over every concrete 2.x request type, **including the hotfix `AntiGateRequest`**) writes `Snapshots/Legacy2x/<RequestType>.<case>.json` in compact form with sorted keys:
    - Proxyless classes get the cases `minimal` and `full`. Proxy classes get `proxy`.
    - Use the canonical sample values that `TaskSamples` will reuse in Phase 2: url `https://example.com/login`, key `SITE_KEY`, proxy `Socks5 203.0.113.10:1080 proxyUser/proxyPass`, UA `Mozilla/5.0 (probe)`.
  - `Capture_LegacyPublicApi_WritesList` reflects over the 2.x assembly and writes `Snapshots/Legacy2x/public-api.txt`, one line per public type and per public member (`Type` and `Type.Member(params)`), sorted. Include enum member numeric values; Task 5.2 uses them.
  - Red: run the capture with the files absent. The test writes them and fails with "snapshot created — review". Green: rerun; the files match.
  - If a 2.x type throws during serialization (for example `ImageToCoordinatesRequest` duplicate `websiteURL`, unless the hotfix fixed it), write that file by hand from the 2.x properties and note it under "Deviations" at the end of this file.
  - Commit: `test(legacy): capture 2.x createTask wire contract and public API`

- [x] Task 1.3: Retarget core and drop 2.x dependencies (TDD) b2f8069
  - Red: `UnitTests/Packaging/CoreAssemblyTests.cs`:
    - `CoreAssembly_DoesNotReferenceNewtonsoftOrMicrosoftExtensions` (checks `GetReferencedAssemblies()` of `typeof(SoftId).Assembly`)
    - `SoftId_Value_Is1023`
  - Green, in `AntiCaptchaApi.Net/AntiCaptchaApi.Net.csproj`:
    - `TargetFrameworks=netstandard2.0;net8.0`, `LangVersion=latest`, `Nullable=enable`, `GenerateDocumentationFile=true`, `WarningsAsErrors=nullable;CS1591`.
    - Remove the `Newtonsoft.Json` and `Microsoft.Extensions.Http` references. Add `System.Text.Json 8.0.5` with `Condition="'$(TargetFramework)'=='netstandard2.0'"`.
    - Add `<Compile Remove>` for `Requests/**; Responses/**; Models/**; Enums/**; Internal/{Common,Converters,Extensions,Helpers,Models,Services,Validation}/**; AnticaptchaClient.cs; IAnticaptchaClient.cs; AnticaptchaServiceCollectionExtensions.cs`.
    - Keep `InternalsVisibleTo AntiCaptchaApi.Net.Tests`.
    - Add `Internal/SoftId.cs` (`internal static class SoftId { internal const int Value = 1023; }`).
    - Add `Internal/Polyfills/IsExternalInit.cs`, `CallerArgumentExpressionAttribute.cs` and `NullableAttributes.cs`, all internal and wrapped in `#if !NET5_0_OR_GREATER`.
  - Test project in the same task: `TargetFramework=net10.0`. Add `<Compile Remove>` for `UnitTests/RequestSerializationTests.cs; IntegrationTests/**; Helpers/**; Models/**; TestEnvironment.cs`. Delete `UnitTests/Legacy/LegacyContractCapture.cs`; its output is checked in.
  - Update `conductor/tech-stack.md` with a dated note: test TFM is net10.0 only for this track, and `ISystemClock` is used instead of `TimeProvider`.
  - Verify: `dotnet build` shows 0 warnings for both TFMs, and `dotnet pack` shows the nuspec dependencies for ns2.0 = STJ only and net8.0 = none.
  - Commit: `chore(core)!: target netstandard2.0/net8.0 with System.Text.Json; exclude 2.x sources`

- [x] Task 1.4: Test infrastructure (TDD) 4247183
  - Core: `Internal/Time/ISystemClock.cs` (`DateTimeOffset UtcNow`, `Task Delay(TimeSpan, CancellationToken)`) and `Internal/Time/SystemClock.cs`.
  - `UnitTests/Infrastructure/FakeHttpMessageHandler.cs`:
    - Scripted responses per API method (a queue of `Func<HttpRequestMessage, Task<HttpResponseMessage>>` or exceptions).
    - Records requests as (method name, body JSON).
    - Throws `InvalidOperationException` on an unscripted call.
  - `UnitTests/Infrastructure/FakeClock.cs`: `Delay` advances `UtcNow`, records every delay, returns immediately and honours cancellation.
  - `UnitTests/Infrastructure/SnapshotAssert.cs`:
    - Compares JSON semantically, independent of key order.
    - On a mismatch, prints missing, extra and changed keys.
    - With `UPDATE_SNAPSHOTS=1`, writes the source file located via `[CallerFilePath]` and still fails with "snapshot updated — review".
  - Test csproj: `<None Include="Snapshots/**;Fixtures/**" CopyToOutputDirectory="PreserveNewest" />`.
  - Red, then green:
    - `FakeHttpMessageHandlerTests.Send_UnscriptedMethod_Throws`
    - `FakeHttpMessageHandlerTests.Send_RecordsMethodAndBody`
    - `FakeClockTests.Delay_AdvancesUtcNow_AndRecordsDelay`
    - `FakeClockTests.Delay_CancelledToken_ThrowsOperationCanceled`
    - `SnapshotAssertTests.Match_DifferentKeyOrder_Passes`
    - `SnapshotAssertTests.Mismatch_ReportsMissingExtraChangedKeys`
    - `SystemClockTests.Delay_Zero_CompletesImmediately`
  - Commit: `test(infra): add fake HTTP handler, fake clock and snapshot assertions`

- [x] Task 1.5: JSON foundation (TDD) cb66a37
  - Add:
    - `Internal/Json/AntiCaptchaJsonContext.cs`: `[JsonSourceGenerationOptions(DefaultIgnoreCondition=WhenWritingNull, WriteIndented=false, PropertyNamingPolicy=CamelCase)]`.
    - `Internal/Json/LowerCaseEnumConverter.cs`: `JsonStringEnumConverter<T>` with camelCase.
    - `Internal/Json/Envelopes/ApiErrorEnvelope.cs`: `errorId`, `errorCode`, `errorDescription`.
    - `Internal/Json/Envelopes/CreateTaskRequest.cs`: `clientKey`, `task` (`JsonObject`), `softId`, `languagePool`, `callbackUrl`.
    - `CreateTaskResponse.cs`: `taskId`.
  - Red, then green:
    - `AntiCaptchaJsonContextTests.Serialize_CreateTaskRequest_IsCompactAndOmitsNulls`
    - `AntiCaptchaJsonContextTests.Deserialize_ErrorEnvelope_ReadsAllFields`
    - `AntiCaptchaJsonContextTests.Serialize_CreateTaskRequest_AlwaysContainsSoftId1023`: `CreateTaskRequest` gets `SoftId` from `SoftId.Value` and cannot be set by callers.
  - Commit: `feat(json): add source-generated JSON context and API envelopes`

- [x] Task 1.6: Console probe (no red phase — tooling) 9eb1698
  - Create `tools/AntiCaptchaApi.Net.Probe/AntiCaptchaApi.Net.Probe.csproj` (net10.0, `IsPackable=false`) and add it to `AntiCaptchaApi.Net.sln`.
  - `Program.cs` with the command `info`: prints the core assembly version, TFM and referenced assemblies. Later phases add commands. Live commands read `ClientKey` from the environment and exist only for the user to run.
  - Commit: `chore(probe): add console probe for manual verification`

- [x] Verification 1: Phase 1 checkpoint [c751ad5] — user confirmed 2026-09-30
  - Run the offline suite. Record the new baselines (tests, warnings) at the end of this file.
  - Manual:
    1. `dotnet build AntiCaptchaApi.Net.sln` shows 0 core warnings for both TFMs.
    2. `dotnet pack AntiCaptchaApi.Net -c Release -o <scratch>` then read the `.nuspec` inside the nupkg. The dependency groups are ns2.0 = System.Text.Json and net8.0 = empty.
    3. `dotnet run --project tools/AntiCaptchaApi.Net.Probe -- info` does not list Newtonsoft.
    4. Inspect `Snapshots/Legacy2x/` and confirm there is one file per 2.x request type and case, including AntiGate, plus `public-api.txt`.
  - Wait for user confirmation, then follow the checkpoint protocol.

---

## Phase 2: Task model, validation, catalog, snapshots [checkpoint: c35b617]
Goal: all 13 task records exist with validation. Their JSON is proven by reflection-discovered snapshot tests, and a parity test shows that no 2.x field is lost.

Tasks:
- [x] Task 2.1: `Proxy` and `ProxyType` (TDD) 5e67188
  - Files: `Proxy.cs`, `ProxyType.cs` (with `[JsonConverter(typeof(LowerCaseEnumConverter<ProxyType>))]`).
  - Tests `UnitTests/Tasks/ProxyTests.cs`:
    - `ToString_WithPassword_RedactsPassword`
    - `ToString_WithoutPassword_DoesNotPrintPlaceholder`
    - `Equality_SameValues_AreEqual`
    - `ProxyType_Serializes_Lowercase` (Theory: Http→`http`, Socks4→`socks4`, Socks5→`socks5`)
  - Commit: `feat(tasks): add Proxy record with redacted ToString`

- [x] Task 2.2: `Validator`, `ValidationError`, `AntiCaptchaValidationException` (TDD) 772ac44
  - Done 2026-09-30: signatures are `Url/NotEmpty/NotNull(value, [CallerArgumentExpression] field = "")`, `Range<T>(value, min, max, field)`, `OneOf<T>(value, IReadOnlyCollection<T> allowed, field)` and `Must(condition, field, message)` (the field is explicit because the condition expression is not a field name). Every check fails on `null`, so optional values are guarded by the caller. 3 extra tests: `NotEmpty_ThisQualifiedExpression_StripsThisPrefix`, `ThrowIfInvalid_WithErrors_ThrowsWithAllErrors`, `ThrowIfInvalid_NoErrors_DoesNotThrow`. The exception has a public ctor `(IEnumerable<ValidationError>)` so that users can throw it from mocks.
  - Files: `Tasks/Validator.cs`, `Errors/ValidationError.cs`, `Errors/AntiCaptchaValidationException.cs`.
  - Tests `UnitTests/Tasks/ValidatorTests.cs`:
    - `Url_Relative_AddsError`
    - `Url_FtpScheme_AddsError`
    - `Url_Https_NoError`
    - `NotEmpty_Whitespace_AddsErrorNamedByCallerExpression`, which asserts the field `"WebsiteKey"` when called as `v.NotEmpty(WebsiteKey)`
    - `Range_OutOfRange_AddsError`
    - `OneOf_NotInSet_AddsError`
    - `Chaining_CollectsAllErrors`
  - Tests `UnitTests/Errors/AntiCaptchaValidationExceptionTests.cs`:
    - `Message_ListsEveryFieldError`
    - `Errors_AreReadOnly`
  - Commit: `feat(validation): add fluent Validator and AntiCaptchaValidationException`

- [x] Task 2.3: `CaptchaTask`, `CaptchaTask<TSolution>`, `ProxyMode`, `Solution` (TDD) 4a77310
  - Done 2026-09-30: the test-only records and their `TestSolution` live in `UnitTests/Tasks/TestTasks.cs`. `RunValidation()` throws `AntiCaptchaValidationException`. Proxy error fields are `Proxy`, `Proxy.Address`, `Proxy.Port` and `Proxy.Type`. On a `None` type with a proxy only `Proxy: not supported by <Type>` is reported for the proxy (its fields are not checked). `Solution` also has a `private protected` ctor. 3 extra tests: `Validate_ValidTask_DoesNotThrow`, `Validate_ErrorMessage_DoesNotContainProxyPassword`, `ToString_WithProxy_RedactsPassword`.
  - Files:
    - `Tasks/CaptchaTask.cs`: `private protected` ctor; `Proxy`; internal abstract `ResolveTypeName()`, `RunValidation()`, `ResolvedFirstPollDelay`.
    - `Tasks/CaptchaTaskOfT.cs`: the design members, plus mode-based name resolution and proxy validation before the per-type `Validate`.
    - `Tasks/ProxyMode.cs`, `Tasks/Solution.cs`.
  - Tests `UnitTests/Tasks/CaptchaTaskTests.cs` use test-only records in the test assembly: `NoneTestTask`, `OptionalTestTask`, `RequiredTestTask`, `SameNameTestTask`. This is allowed through `InternalsVisibleTo` + `private protected`.
    - `TypeName_None_IsTypeBase`
    - `TypeName_OptionalWithoutProxy_AppendsProxyless`
    - `TypeName_OptionalWithProxy_IsTypeBase`
    - `TypeName_Required_IsTypeBase`
    - `TypeName_OptionalSameName_IgnoresProxy`
    - `Validate_ProxyOnNoneType_FailsWithProxyNotSupported`
    - `Validate_RequiredWithoutProxy_FailsWithProxyRequired`
    - `Validate_ProxyPortOutOfRange_Fails` (Theory: 0, 65536)
    - `Validate_ProxyPort65535_Passes`
    - `Validate_ProxyEmptyAddress_Fails`
    - `Validate_ProxyUndefinedType_Fails`
    - `Validate_PerTypeRulesAndProxyRules_AreAllReported`
    - `FirstPollDelay_Default_Is10Seconds`
  - Commit: `feat(tasks): add CaptchaTask base with ProxyMode type naming and proxy validation`

- [x] Task 2.4: `TaskSerializer` and the first catalog entry `TurnstileTask` (TDD) dd700c5
  - Done 2026-09-30: `type` is written as the first key (the other keys keep their declaration order, then the proxy fields). Solutions use the namespace `AntiCaptchaApi.Net.Tasks` (spec 7.1) although they live in `Tasks/Solutions/`. The UserAgent-with-proxy error is `UserAgent: required when Proxy is set`. 3 extra tests: `TaskSerializerTests.ToJsonObject_TypeIsFirstKey`, `TurnstileTaskTests.Validate_ProxyWithUserAgent_Passes`, `TypeName_WithoutProxy_IsTurnstileTaskProxyless`.
  - Files: `Internal/Json/TaskSerializer.cs`, `Tasks/TurnstileTask.cs`, `Tasks/Solutions/TurnstileSolution.cs`, and registration of both in the context.
  - Tests `UnitTests/Tasks/TaskSerializerTests.cs`:
    - `ToJsonObject_WithoutProxy_WritesTypeProxyless_AndNoProxyFields`
    - `ToJsonObject_WithProxy_FlattensAllFiveProxyFields`
    - `ToJsonObject_ProxyWithoutCredentials_OmitsLoginAndPassword`
    - `ToJsonObject_DoesNotEmitProxyObjectOrInternalMembers` (no `proxy`, `typeBase`, `proxyMode`, `firstPollDelay`)
    - `ToJsonObject_UnregisteredType_ThrowsInvalidOperationWithTypeName` (uses `OptionalTestTask`)
  - Tests `UnitTests/Tasks/TurnstileTaskTests.cs`:
    - `Validate_InvalidUrlAndEmptyKey_ReportsBoth`
    - `Validate_ProxyWithoutUserAgent_Fails`
    - `Serialize_OptionalFields_UseActionCDataChlPageDataNames`
  - Commit: `feat(tasks): add TaskSerializer and TurnstileTask`

- [x] Task 2.5: Reflection-discovered snapshot and parity tests (TDD) ecb4ce2
  - Done 2026-09-30: the map lists all 38 legacy files as `(LegacyFile, Target "<Type>.<case>", Mapped|Pending)`; only the 3 Turnstile entries are `Mapped`. `DocumentedDeviations` is empty (see Deviations). 3 extra tests: `TaskSnapshotTests.ConcreteTaskTypes_AreDiscovered`, `LegacyParityTests.LegacyParity_PendingEntries_HaveNoImplementedTaskType` (a `Pending` entry whose type exists fails, so catalog tasks must switch it to `Mapped`) and `DocumentedDeviations_EveryEntryIsNeeded` (an allow-list entry whose values no longer differ fails, so it cannot mask regressions). `EveryConcreteTaskType_HasMinimalFullAndProxySamples` also asserts that only the `proxy` sample sets a proxy. The `full` sample sets every optional property (for Turnstile this includes `UserAgent`, which 2.x allowed only with a proxy).
  - Files:
    - `UnitTests/Tasks/TaskSamples.cs`: `IReadOnlyDictionary<Type, IReadOnlyList<(string Case, CaptchaTask Task)>>` using the Task 1.2 canonical values.
    - `UnitTests/Tasks/TaskSnapshotTests.cs`
    - `UnitTests/Tasks/LegacyParityTests.cs`: a table mapping each legacy file to its 3.0 file, plus the `DocumentedDeviations` allow-list from spec §9.
  - Tests:
    - `EveryConcreteTaskType_HasMinimalFullAndProxySamples`: `proxy` is not required for `ProxyMode.None`. Types are discovered via `typeof(CaptchaTask).Assembly.GetTypes()`, non-abstract.
    - `EveryConcreteTaskType_AndItsSolution_AreRegisteredInJsonContext`. The failure message names the missing `[JsonSerializable]`.
    - `EverySample_HasSnapshotFile`
    - `Snapshot_MatchesExpectedJson` (Theory via `MemberData` over all samples)
    - `EverySample_PassesValidation`
    - `LegacyParity_EveryLegacyKeyPresentWithSameValue_ExceptDocumentedDeviations`
    - `LegacyParity_EveryLegacyFile_IsMapped`
  - Add `Snapshots/Tasks/TurnstileTask.{minimal,full,proxy}.json`.
  - Legacy files for types that do not exist yet go in the mapping table with an explicit `Pending` status. Parity skips them, and "IsMapped" accepts only files that are listed. Task 2.12 deletes `Pending` support, so from then on any unmapped legacy file fails. The suite stays green after every task.
  - Commit: `test(serialization): discover every task type and assert snapshots and 2.x parity`

  Catalog tasks 2.6–2.12 all follow the same TDD recipe:
  - **Red:**
    1. Add the samples to `TaskSamples`.
    2. Add the snapshot JSON by hand: start from the Legacy2x file and apply the spec §9 deviations and additions.
    3. Add a `<Type>Tests` class with the listed validation tests.
  - **Green:**
    1. Add the record and solution files (XML `<summary>` links the anti-captcha docs page).
    2. Register the task and its solution in the context.
    3. Switch the legacy parity entries from `Pending` to mapped.
  - **Solution deserialization** is tested in Phase 3 (Task 3.6).

- [x] Task 2.6: `ImageToTextTask`, `ImageToCoordinatesTask` (+ `NumericOption`, `ImageToCoordinatesMode`, solutions) 6e569f7
  - Done 2026-09-30: `NumericOption` keeps the 2.x member names (`NoRequirements`, `NumbersOnly`, `AnyLettersExceptNumbers` = 0/1/2). `ImageToCoordinatesTask.Mode` is non-nullable with default `Points` and is always sent (2.x parity). Body loading and validation are shared by the internal `Tasks/ImageBody.cs`. `ImageToTextSolution.Url` is nullable. The optional `WebsiteUrl` of both types is validated only when set. 4 extra tests: `ImageToTextTaskTests.Validate_NegativeLength_Fails`, `ImageToCoordinatesTaskTests.Mode_Default_IsPointsAndAlwaysSerialized`, `FromBytes_SetsBase64Body`; `Numeric_SerializesAsNumber` is a Theory over the 3 values. Both snapshots equal their 2.x files.
  - Tests `ImageToTextTaskTests`:
    - `FromFile_MissingFile_ThrowsFileNotFound`
    - `FromFile_ExistingFile_SetsBase64Body` (temp file)
    - `FromBytes_SetsBase64Body`
    - `Validate_EmptyBody_Fails`
    - `Validate_DataUriPrefix_Fails`
    - `Validate_MinGreaterThanMax_Fails`
    - `Validate_Proxy_FailsNotSupported`
    - `TypeName_IsImageToTextTask`
    - `FirstPollDelay_Is3Seconds`
    - `Numeric_SerializesAsNumber`
  - Tests `ImageToCoordinatesTaskTests`:
    - `Mode_SerializesLowercaseString` (points/rectangles)
    - `FromFile_MissingFile_ThrowsFileNotFound`
    - `Validate_EmptyBody_Fails`
    - `Validate_InvalidOptionalWebsiteUrl_Fails`
    - `FirstPollDelay_Is3Seconds`
  - Snapshots: `ImageToTextTask.{minimal,full}.json`, `ImageToCoordinatesTask.{minimal,full}.json`.
  - Commit: `feat(tasks): add ImageToText and ImageToCoordinates tasks`

- [x] Task 2.7: `RecaptchaV2Task`, `RecaptchaV2EnterpriseTask`, `RecaptchaSolution` 564a11e
  - Done 2026-09-30: `EnterprisePayload` is `IReadOnlyDictionary<string, string>?` (2.x `Dictionary<string, string>`), not a `JsonObject`, so the public surface keeps JSON types to AntiGate/AppStats (Task 5.7). `UserAgent` and `Cookies` are on both records and are sent without a proxy too; the `full` samples set them (like Turnstile), and they are 3.0 additions to the 2.x proxyless files. Refactor: internal `Validator.UserAgentWhenProxy(proxy, userAgent)` now holds the "UserAgent: required when Proxy is set" rule (Turnstile uses it), and internal `Tasks/RecaptchaApiDomain.cs` holds the `apiDomain` allow-list. 3 extra tests: `RecaptchaV2TaskTests.Validate_InvalidUrlAndEmptyKey_ReportsBoth`, `RecaptchaV2EnterpriseTaskTests.Validate_ProxyWithoutUserAgent_Fails`, `TypeName_ProxylessAndProxy`.
  - Tests:
    - `RecaptchaV2TaskTests.TypeName_ProxylessAndProxy`
    - `RecaptchaV2TaskTests.Validate_ProxyWithoutUserAgent_Fails`
    - `RecaptchaV2TaskTests.Serialize_CookiesAndIsInvisible`
    - `RecaptchaV2EnterpriseTaskTests.EnterprisePayload_SerializesAsObject`
    - `RecaptchaV2EnterpriseTaskTests.Validate_ApiDomainNotAllowed_Fails`
    - `RecaptchaV2EnterpriseTaskTests.Validate_ApiDomainRecaptchaNet_Passes`
  - Snapshots: `{minimal,full,proxy}` for both.
  - Commit: `feat(tasks): add RecaptchaV2 and RecaptchaV2Enterprise tasks`

- [x] Task 2.8: `RecaptchaV3Task` (+ `MinScore`, `MinScoreJsonConverter`) ed3f7c6
  - Done 2026-09-30: an extra sample case `enterprise` (`IsEnterprise = true`, default `Score03`) was added, with `Snapshots/Tasks/RecaptchaV3Task.enterprise.json`, and `RecaptchaV3EnterpriseRequest.minimal` maps to it exactly. Both 2.x `full` files are identical and map to `RecaptchaV3Task.full`. `TaskSamples.AddCase` appends extra cases. `DocumentedDeviations` stays **empty**: the only wire change of spec §9 item 2, `isEnterprise: false` in `minimal`, is an added key (2.x omitted it), and the parity test allows added keys (an allow-list entry for it would fail `DocumentedDeviations_EveryEntryIsNeeded`). The dropped 0.5 score is enforced by the enum type and is not used by any 2.x sample. `MinScore` members are 0/1/2, so `default(MinScore)` is `Score03`. The converter also reads 0.3/0.7/0.9 and throws `JsonException` for other values and for undefined enum values. 6 extra tests: `RecaptchaV3TaskTests.Validate_ApiDomainNotAllowed_Fails` and `MinScoreJsonConverterTests` (`Read_DocumentedNumber_MapsEnum` ×3, `Read_OtherValue_ThrowsJsonException` ×2, `Write_UndefinedValue_ThrowsJsonException`).
  - Tests:
    - `MinScore_SerializesAsNumber` (Theory: 0.3, 0.7, 0.9)
    - `MinScore_Default_Is03`
    - `IsEnterprise_Default_FalseAndAlwaysSerialized`
    - `TypeName_IsRecaptchaV3TaskProxyless`
    - `Validate_Proxy_FailsNotSupported`
    - `Validate_UndefinedMinScore_Fails`
  - Snapshots: `{minimal,full}`; `full` has `IsEnterprise = true`.
  - Parity: `RecaptchaV3Request.*` and `RecaptchaV3EnterpriseRequest.*` both map to `RecaptchaV3Task` samples.
  - Commit: `feat(tasks): add RecaptchaV3 task with MinScore enum`

- [x] Task 2.9: `FunCaptchaTask`, `FunCaptchaSolution` 191174e
  - Done 2026-09-30: `Data` stays a JSON string (2.x parity; the API expects a string). The `full` sample also sets `UserAgent` (a 3.0 addition to the 2.x proxyless file). 1 extra test: `FunCaptchaTaskTests.TypeName_ProxylessAndProxy`.
  - Tests:
    - `Serialize_ApiJsSubdomain_UsesFuncaptchaApiJSSubdomainName`
    - `Validate_EmptyPublicKey_Fails`
    - `Validate_ProxyWithoutUserAgent_Fails`
  - Snapshots: `{minimal,full,proxy}`.
  - Commit: `feat(tasks): add FunCaptcha task`

- [x] Task 2.10: `GeeTestV3Task`, `GeeTestV4Task`, solutions 8e62322
  - Done 2026-09-30: `Version` is a get-only `[JsonPropertyName("version")]` property (spec 7.2). `InitParameters` is `IReadOnlyDictionary<string, string>?` (2.x `Dictionary<string, string>`), like `EnterprisePayload`. The `full` samples also set `UserAgent`. 1 extra test: `GeeTestV4TaskTests.Validate_EmptyGt_Fails`; `Serialize_Version3_AndTypeGeeTestTask` also asserts the proxyless name.
  - Tests:
    - `GeeTestV3TaskTests.Serialize_Version3_AndTypeGeeTestTask`
    - `GeeTestV3TaskTests.Validate_EmptyChallenge_Fails`
    - `GeeTestV4TaskTests.Serialize_Version4_WithInitParametersObject`
    - `GeeTestV4TaskTests.Validate_ChallengeOptional_Passes`
    - `GeeTestV4TaskTests.Validate_ProxyWithoutUserAgent_Fails`
  - Snapshots: `{minimal,full,proxy}` for both.
  - Commit: `feat(tasks): add GeeTest v3 and v4 tasks`

- [x] Task 2.11: `AmazonWafTask`, `ProsopoTask`, `FriendlyCaptchaTask`, solutions 22f24e8
  - Done 2026-09-30: `AmazonWafTask` validates `Iv` and `Context` in both proxy modes (2.x `AmazonWafRequestValidator` skipped them with a proxy), and `Validate_EmptyIvOrContext_Fails` is a Theory over with/without proxy. AmazonWaf has no `UserAgent` (none in 2.x or spec FR-3), so its `full` sample equals the 2.x file. The Prosopo and FriendlyCaptcha `full` samples set `UserAgent` (the 2.x proxyless files have no optional fields). 3 extra tests: `AmazonWafTaskTests.Validate_ProxyWithoutUserAgent_Passes`, `ProsopoTaskTests.TypeName_ProxylessAndProxy`, `FriendlyCaptchaTaskTests.TypeName_ProxylessAndProxy`.
  - Tests:
    - `AmazonWafTaskTests.TypeName_AmazonTaskProxylessAndAmazonTask` (the 2.x `KeyNotFoundException` regression)
    - `AmazonWafTaskTests.Validate_EmptyIvOrContext_Fails`
    - `ProsopoTaskTests.Validate_ProxyWithoutUserAgent_Fails`
    - `FriendlyCaptchaTaskTests.Validate_ProxyWithoutUserAgent_Fails`
    - `FriendlyCaptchaTaskTests.Solution_IsFriendlyCaptchaSolution`
  - Snapshots: `{minimal,full,proxy}` for each.
  - Commit: `feat(tasks): add AmazonWaf, Prosopo and FriendlyCaptcha tasks`

- [x] Task 2.12: `AntiGateTask`, `AntiGateSolution` 4b08653
  - Done 2026-09-30: Q-1 resolved: `AntiGateRequest.proxy.json` has no `proxyType` (this matches the anti-captcha docs, checked 2026-09-30). So `CaptchaTask` has `internal virtual bool IncludesProxyType => true`, `AntiGateTask` overrides it to `false`, and `TaskSerializer` skips `proxyType` for it. The proxy `Type` is still validated. `DomainsOfInterest` is `IReadOnlyList<string>?`. `AntiGateSolution` has `Url` and `Domain` as non-nullable strings, and `Cookies`, `LocalStorage`, `SessionStorage`, `Fingerprint`, `HtmlsInBase64` (`HTMLsInBase64`), `Screenshots`, `RequestHeaders` and `ResponseHeaders` as `JsonObject?`, mirroring the 2.x `JObject`s. Only url/domain count as required; 2.x `IsValid` also demanded localStorage and fingerprint. `ParityStatus`/`Pending` is removed: `Map` is `(LegacyFile, Target)`, all 38 files are compared, and `LegacyParity_PendingEntries_HaveNoImplementedTaskType` is replaced by `LegacyParity_EveryConcreteTaskType_IsTargeted`. `ConcreteTaskTypes_AreDiscovered` also asserts 13 types. 1 extra test: `AntiGateTaskTests.Validate_ProxyIsOptional`.
  - Resolve spec Q-1 against `Snapshots/Legacy2x/AntiGateRequest.*.json` (proxyType present or absent). Implement the internal override only if it is needed.
  - Tests:
    - `TypeName_AlwaysAntiGateTask_WithAndWithoutProxy`
    - `Variables_SerializeAsNestedObject`
    - `Validate_EmptyTemplateName_Fails`
    - `Validate_NullVariables_Fails`
    - `Proxy_FieldsMatchLegacyAntiGateContract`
  - Snapshots: `{minimal,full,proxy}`.
  - Remove `Pending` support from `LegacyParityTests`. `LegacyParity_EveryLegacyFile_IsMapped` must be green with no pending entries.
  - Commit: `feat(tasks): add AntiGate task`

- [x] Verification 2: Phase 2 checkpoint [c35b617] — user confirmed 2026-09-30 (probe 'serialize' added in ae8e981)
  - Run the offline suite and coverage for `Tasks/**` and `Internal/Json/**` (≥ 80 %).
  - Add a probe command `serialize`: prints the compact JSON of every sample task, and prints the validation errors of `new TurnstileTask("not-a-url", "")`.
  - Manual:
    1. Run `dotnet run --project tools/AntiCaptchaApi.Net.Probe -- serialize`. The user eyeballs the JSON against the anti-captcha docs pages linked in the XML docs.
    2. Confirm that `proxyPassword` shows only in JSON and never in `ToString()` output (the probe prints both).
  - Wait for user confirmation, then follow the checkpoint protocol.

---

## Phase 3: Transport, errors, redaction, client skeleton [checkpoint: 158ccf0]
Goal: typed errors and a hardened HTTP transport, plus the `AntiCaptchaClient` constructors, `CreateTaskAsync` and `GetTaskResultAsync`, all with no secret leakage.

Tasks:
- [x] Task 3.1: `AntiCaptchaErrorCode`, `AntiCaptchaError`, parser and classifier (TDD) 804addd
  - Done 2026-09-30: the code list is the 35 codes on https://anti-captcha.com/apidoc/errors (fetched 2026-09-30; the 2.x sources have no code list). Each enum value equals its documented `errorId`, `Unknown = 0`, and the client codes are 1000–1004. `ERROR_NO_SUCH_CAPCHA_ID` → `NoSuchCaptchaId` and `ERROR_PROXY_CONNECT_*` → `ProxyConnection*`. `HttpError` keeps the HTTP status number as `RawCode` (null = network failure); this is how the classifier tells 400 from 503, with no extra member. `HttpTimeout` counts as "transient HTTP", so it is also retryable. `ErrorCodeParser.FromApi(errorId, raw, description)` builds the error and keeps the raw code. `ErrorClassifier.HttpError(status, description)` is the factory. 4 extra tests: `Parse_IsCaseSensitive_LowercaseIsUnknown`, `FromApi_KnownCode_KeepsRawAndErrorId`, `HttpError_KeepsStatusAsRawCode`, `HttpError_NetworkFailure_HasNoRawCode`.
  - Files: `Errors/AntiCaptchaErrorCode.cs` (every documented `ERROR_*` code plus the client codes from spec FR-6), `Errors/AntiCaptchaError.cs`, `Internal/Errors/ErrorCodeParser.cs`, `Internal/Errors/ErrorClassifier.cs`.
  - Tests:
    - `ErrorCodeParserTests.Parse_KnownCode_MapsEnum` (Theory, one row per documented code)
    - `Parse_UnknownCode_ReturnsUnknownAndKeepsRaw`
    - `Parse_Null_ReturnsUnknown`
    - `ErrorClassifierTests.IsRetryable` (Theory: NoSlotAvailable, CaptchaUnsolvable, ProxyConnectionRefused, HttpError-5xx → true; KeyDoesNotExist, ZeroBalance, IpBlocked, SolveTimeout, Unknown → false)
    - `IsTransient` (Theory: HttpError-network/408/429/5xx, HttpTimeout, InvalidResponse → true; HttpError-400 → false)
  - Commit: `feat(errors): add typed AntiCaptchaErrorCode with retry classification`

- [x] Task 3.2: `AntiCaptchaException` and `SecretRedactor` (TDD) 9d561f0
  - Done 2026-09-30: the ctor is `(AntiCaptchaError error, int? taskId, int attempts, Exception? innerException = null)` and is public, so users can throw it from mocks. Without a description the message is just `{Code}`. The exception does not redact: descriptions are redacted where the error is built (transport). `SecretRedactor.With(password)` returns an extended copy, for the per-task proxy password. 4 extra tests: `Redact_OverlappingSecrets_LongerIsReplacedFirst`, `With_AddsPasswords_WithoutChangingOriginal`, `Message_WithoutDescription_IsCode`, `Ctor_NullError_ThrowsArgumentNull`.
  - Files: `Errors/AntiCaptchaException.cs`, `Internal/Errors/SecretRedactor.cs` (constructed with the key and a set of proxy passwords; `Redact(string?)`).
  - Tests:
    - `SecretRedactorTests.Redact_ReplacesKeyAndPasswords`
    - `Redact_Null_ReturnsNull`
    - `Redact_EmptySecret_IsIgnored`
    - `AntiCaptchaExceptionTests.Message_ContainsCodeAndDescription`
    - `Properties_ExposeErrorTaskIdAttempts`
  - Commit: `feat(errors): add AntiCaptchaException and secret redaction`

- [x] Task 3.3: `ApiTransport` (TDD) e62a3c2
  - Done 2026-09-30: the ctor is `(HttpClient, Uri baseAddress, TimeSpan requestTimeout)`, and `TResponse : class`. A trailing `/` is added to the base address, so a base like `https://gw/anticaptcha` keeps its path. The error envelope is parsed first, then `TResponse`. Transport exceptions have `TaskId = null` and `Attempts = 0`; the client/pipeline adds those. The `HttpRequestException` is **not** attached as the inner exception (its message is redacted into the description instead), because exception `ToString()` would print an unredacted inner message. Non-2xx responses are always `HttpError`, even with a JSON body. Extra tests: a base-with-path row in `PostAsync_PostsJsonToBaseAddressPlusMethod`, `PostAsync_AlreadyCancelledToken_ThrowsOperationCanceled`, `PostAsync_HttpRequestExceptionEchoesSecret_IsRedacted`, and `InvalidJson` as a Theory (HTML, empty, `null`).
  - File: `Internal/Http/ApiTransport.cs`, with `PostAsync<TReq,TRes>(string method, TReq body, JsonTypeInfo<TReq>, JsonTypeInfo<TRes>, SecretRedactor, CancellationToken)`. Use `#if NET5_0_OR_GREATER` for `ReadAsStringAsync(ct)`.
  - Tests `UnitTests/Http/ApiTransportTests.cs` (fake handler):
    - `PostAsync_PostsJsonToBaseAddressPlusMethod`
    - `PostAsync_ErrorIdNonZero_ThrowsMappedAntiCaptchaException`
    - `PostAsync_HttpRequestException_ThrowsTransientHttpError`
    - `PostAsync_Status503_ThrowsTransientHttpError`
    - `PostAsync_Status400_ThrowsFatalHttpError`
    - `PostAsync_RequestTimeoutElapses_ThrowsHttpTimeout` (`RequestTimeout = 50 ms`, handler waits on ct)
    - `PostAsync_HttpClientTimeout_ThrowsHttpTimeout` (the handler throws `TaskCanceledException` while the user ct is not cancelled)
    - `PostAsync_UserCancels_ThrowsOperationCanceled_NotAntiCaptchaException`
    - `PostAsync_InvalidJson_ThrowsInvalidResponse_WithoutBody`
    - `PostAsync_ErrorDescriptionEchoesSecrets_IsRedacted`
  - Commit: `feat(http): add ApiTransport with timeout/cancellation separation and error mapping`

- [x] Task 3.4: `AntiCaptchaClient` constructors, options, shared handler (TDD) f83e453
  - Done 2026-09-30: `AntiCaptchaClientOptions` has only `BaseAddress` and `RequestTimeout` for now. `DefaultSolveOptions` is added in Task 4.1 together with `SolveOptions` (see Deviations). The key-only ctor creates one `HttpClient` per instance over `SharedHandler.Instance` (`disposeHandler: false`, `Timeout = Infinite`, because `ApiTransport` enforces `RequestTimeout`). The internal accessors are `SharedHandlerInUse` (null for the caller-`HttpClient` ctor), `Options`, `Clock`, `Random`, `Redactor` and `Transport`. The default random is a thread-local `Random`. Invalid options (null/relative `BaseAddress`, `RequestTimeout <= 0`) throw `ArgumentException`/`ArgumentOutOfRangeException` with `ParamName = options`. `ToString()` prints only the base address. The 2.x `AnticaptchaClient.cs`/`IAnticaptchaClient.cs` were renamed to `*.v2.cs` (see Deviations). 6 extra tests: `SharedHandler_OnNet8_IsSocketsHttpHandlerWithTwoMinuteLifetime`, `Ctor_NullOptions_UsesDefaults`, `Ctor_KeyOnly_UsesSystemClock`, `Ctor_Internal_UsesInjectedClockAndRandom`, `Ctor_InvalidOptions_ThrowsArgumentException` (Theory ×4), `Client_ImplementsInterface`.
  - Files: `IAntiCaptchaClient.cs` (declared empty here; methods are added as they are implemented), `AntiCaptchaClient.cs`, `AntiCaptchaClientOptions.cs`, `Internal/Http/SharedHandler.cs` (`#if NET8_0_OR_GREATER` `SocketsHttpHandler{PooledConnectionLifetime=2min}` `#else` `HttpClientHandler`). The internal ctor takes `ISystemClock` and `Func<double>`.
  - Tests `UnitTests/Client/AntiCaptchaClientCtorTests.cs`:
    - `Ctor_NullOrWhitespaceKey_ThrowsArgumentException` (Theory; the message never contains the value)
    - `Ctor_KeyOnly_TwoInstances_ShareHandler` (internal accessor)
    - `Ctor_WithHttpClient_DoesNotMutateTimeoutOrBaseAddress`
    - `Ctor_NullHttpClient_ThrowsArgumentNull`
    - `ToString_DoesNotContainKey`
    - `Options_Defaults_MatchSpec`
  - Commit: `feat(client): add AntiCaptchaClient constructors with shared handler`

- [x] Task 3.5: `CreateTaskAsync` and `CreateOptions` (TDD) 9fb0de2
  - Done 2026-09-30: argument checks come first (null task → `ArgumentNullException(task)`, relative `CallbackUrl` → `ArgumentException(o)`), then `RunValidation()`, then HTTP. `CallbackUrl` is sent as `Uri.AbsoluteUri`. The per-call redactor is `Redactor.With(task.Proxy?.Password)` (internal `RedactorFor`). `UnitTests/Client/ClientTestContext.cs` wires the client to the fake handler, `FakeClock` and random 0.5 for the client tests. 4 extra tests: `CreateTaskAsync_WithoutCreateOptions_OmitsLanguagePoolAndCallbackUrl`, `CreateTaskAsync_RelativeCallbackUrl_ThrowsArgument_AndSendsNoRequest`, `CreateTaskAsync_ApiErrorEchoesProxyPassword_IsRedacted`, `CreateTaskAsync_ThroughInterface_IsAvailable`.
  - Files: `CreateOptions.cs` (non-sealed record, because `SolveOptions` derives from it in Phase 4).
  - Tests `UnitTests/Client/CreateTaskTests.cs`:
    - `CreateTaskAsync_InvalidTask_ThrowsValidation_AndSendsNoRequest`
    - `CreateTaskAsync_SendsClientKeySoftId1023AndTask`: snapshot `Snapshots/Envelopes/createTask.turnstile.json` with key `TEST_KEY`
    - `CreateTaskAsync_WithCreateOptions_SendsLanguagePoolAndCallbackUrl`
    - `CreateTaskAsync_Success_ReturnsTaskId`
    - `CreateTaskAsync_ApiError_ThrowsAntiCaptchaException`
    - `CreateTaskAsync_NullTask_ThrowsArgumentNull`
  - Commit: `feat(client): add CreateTaskAsync`

- [x] Task 3.6: `GetTaskResultAsync` and `TaskStatus<T>`, plus solution deserialization for every type (TDD) 8e26730
  - Done 2026-09-30: `IsReady` guarantees a non-null `Solution`. A ready response with a missing, `null`, non-object or unparseable solution, or an unknown `status`, throws `InvalidResponse` (`TaskId` set). While processing, every other member is null. Transport errors are rethrown with `TaskId = taskId` (the transport exception is the inner). `solveCount` (a string in every doc example) and the unix times are also read from strings. The mapping lives in `Internal/Json/TaskResultMapper.cs`. The 11 fixtures are the anti-captcha doc examples (fetched 2026-09-30; the invalid `"Mozilla\5.0"` escape is fixed). GeeTestV3 uses the 2.x names, because the docs page shows only V4. The Amazon doc example shows `solution.token` (Q-2 default; spec note). **`AntiGateSolution` JSON members changed from `JsonObject?` to `JsonNode?`** (see Deviations). The lowercase 2.x fallback `htmlsInBase64` is not read. 11 extra tests: `GetTaskResult_SendsClientKeyAndTaskId`, `GetTaskResult_ReadyWithoutUsableSolutionOrUnknownStatus_ThrowsInvalidResponse` (Theory ×6), `AntiGateSolution_EmptyMapsAsArraysOrObjects_AreAccepted`, `AntiGateSolution_LowercaseHtmlsInBase64_IsNotRead`, `AmazonWafSolution_ReadsToken`, `GetTaskResult_ThroughInterface_IsAvailable`; `CostAsString` is a Theory (string and number).
  - Files: `TaskStatus.cs`, `Internal/Json/Envelopes/GetTaskResultRequest.cs` / `GetTaskResultResponse.cs` (solution as `JsonElement?`, then deserialized with `GetTypeInfo(typeof(TSolution))`; `cost` read from string; unix times).
  - Fixtures: `Fixtures/TaskResults/<SolutionType>.ready.json`, one per solution type. Build them from anti-captcha doc examples and 2.x converter expectations; AmazonWaf uses `token` (spec Q-2).
  - Tests `UnitTests/Client/GetTaskResultTests.cs`:
    - `GetTaskResult_Processing_ReturnsNotReady`
    - `GetTaskResult_Ready_MapsCostIpTimesSolveCount`
    - `GetTaskResult_CostAsString_ParsesDecimal`
    - `GetTaskResult_ApiError_Throws`
    - `EverySolutionType_HasReadyFixture` (reflection)
    - `EverySolutionType_DeserializesFromFixture` (Theory; asserts non-null required members)
    - `AntiGateSolution_ReadsCookiesLocalStorageFingerprint`
    - `ImageToCoordinatesSolution_ReadsNestedArrays`
  - Commit: `feat(client): add GetTaskResultAsync with typed solutions`

- [x] Task 3.7: Redaction sweep (TDD) e3a330a
  - Done 2026-09-30: 9 transport scenarios through `CreateTaskAsync`: API error echo, unknown-code echo, network failure (with inner), 503 with reason phrase, 400 with body, `RequestTimeout`, `HttpClient.Timeout` with message, invalid JSON, and wrong-shape JSON. Most echo both secrets. The checks cover `Message`, `ToString()`, the inner chain and every `AntiCaptchaError` member. The sweep found one leak, now fixed in `ApiTransport`: an **unknown API `errorCode` was kept unredacted as `RawCode`**. Codes are mapped from the original string, then `RawCode` and `Description` are both redacted. Mutation check: with `SecretRedactor.Redact` a no-op, 6 sweep tests fail. Limitation: `GetTaskResultAsync` knows only the key (no task, so no proxy password); its test asserts the key only. 3 extra tests: `GetTaskResult_ApiErrorEchoingKey_ContainsNoKey`, `Client_ToString_ContainsNoKey`, and `Task_ToString_WithProxy_ContainsNoPassword` as a Theory over all 37 samples. `ValidationException_...` is a Theory over 5 invalid proxy tasks.
  - Tests `UnitTests/Security/RedactionTests.cs`, with key `SECRET_KEY_123` and proxy password `PX_PASS_456`:
    - `AllTransportErrorPaths_ExceptionMessageAndToString_ContainNoSecrets` (Theory over the Task 3.3 scenarios through `CreateTaskAsync`)
    - `AntiCaptchaError_ToString_ContainsNoSecrets`
    - `Task_ToString_WithProxy_ContainsNoPassword` (every sample in `TaskSamples`)
    - `ValidationException_Message_ContainsNoProxyPassword`
  - Commit: `test(security): assert secrets never leak through errors or ToString`

- [x] Verification 3: Phase 3 checkpoint [158ccf0] — user confirmed 2026-09-30
  - Run the offline suite and coverage for `Errors/**`, `Internal/Http/**`, `Internal/Errors/**` and `AntiCaptchaClient.cs`.
  - Add probe commands:
    - `offline-create`: in-process scripted handler; shows the request JSON with the key masked, and runs an error scenario whose message is printed.
    - `live-create-image` (the **user runs this** with `ClientKey` set): creates one ImageToText task from `AntiCaptchaApi.Net.Tests/Resources/captchaexample.png` and prints the taskId.
    - Optional `live-amazon-shape` for Q-2 (user only).
  - Manual: run `offline-create` and confirm that no `SECRET` string appears. Live commands are optional and run by the user.
  - Wait for user confirmation, then follow the checkpoint protocol.

---

## Phase 4: Solving pipeline [checkpoint: 5acd8ee]
Goal: `WaitForResultAsync`, `TrySolveAsync` and `SolveAsync` with the per-type first-poll delay, poll interval, transient tolerance, retry with backoff and jitter, `MaxAttempts`, per-attempt `Timeout`, `MaxCostPerSolve` and correct cancellation, all proven with `FakeClock`.

Tasks:
- [x] Task 4.1: `SolveOptions` and `Backoff` (TDD) 9b44abc
  - Done 2026-09-30: `SolveOptions.Validate(paramName)` is internal and returns the options; it throws `ArgumentOutOfRangeException` (message prefixed `SolveOptions.<Option>`) for non-positive `Timeout`/`PollInterval`/`RetryBaseDelay`/`RetryMaxDelay`, a negative `FirstPollDelay` (zero is allowed), `MaxAttempts < 1`, a negative `MaxConsecutivePollErrors` (0 = fail on the first transient poll error) and a negative `MaxCostPerSolve` (0 is allowed). The client constructor also validates `DefaultSolveOptions` (null or invalid → `ParamName = options`). `Backoff.Delay` applies the jitter after the cap (spec formula), so the maximum is `RetryMaxDelay × 1.2`. Extra tests: `Validate_Defaults_DoesNotThrow`, `Validate_ZeroFirstPollDelay_IsAllowed`, `Validate_NegativeMaxConsecutivePollErrors_Throws`, `Validate_ZeroMaxCost_IsAllowed`, `BackoffTests.Delay_JitterAppliesAfterTheCap`, and 2 rows in `Ctor_InvalidOptions_ThrowsArgumentException`.
  - Also add `AntiCaptchaClientOptions.DefaultSolveOptions = new()` (FR-5), deferred from Task 3.4, and extend `Options_Defaults_MatchSpec`.
  - Files: `SolveOptions.cs` (sealed record deriving from `CreateOptions`), `Internal/Pipeline/Backoff.cs` (`Delay(int attempt, TimeSpan baseDelay, TimeSpan max, Func<double> random)`).
  - Tests:
    - `SolveOptionsTests.Defaults_MatchSpec`
    - `Validate_NonPositiveTimeoutOrPollInterval_Throws` (Theory)
    - `Validate_MaxAttemptsZero_Throws`
    - `Validate_NegativeMaxCost_Throws`
    - `BackoffTests.Delay_GrowsExponentially` (random = 0.5, so no jitter)
    - `Delay_CappedAtMax`
    - `Delay_JitterWithinPlusMinus20Percent` (random = 0 and 1)
  - Commit: `feat(pipeline): add SolveOptions and exponential backoff with jitter`

- [x] Task 4.2: `WaitForResultAsync` (TDD) 581614a
  - Decision 2026-09-30 (orchestrator, on the Task 3.6 note below): while polling, `InvalidResponse` (unparseable JSON **and** ready-without-usable-solution / unknown status) is **transient**. Polling is free and the task is already paid for, so it is re-asked up to `MaxConsecutivePollErrors`, then the call fails with that `InvalidResponse` and the `TaskId`. No internal distinction is needed. `Wait_ReadyWithoutSolution_ThrowsInvalidResponse` asserts the failure after the limit (6 polls with the default 5).
  - Done 2026-09-30: `SolvePipeline.PollAsync(taskId, options, firstPollDelay, deadline, redactor, attempts, ct)` returns the ready `TaskStatus<T>`; every exception is rethrown as `AntiCaptchaException(error, taskId, attempts, inner)`. Non-transient errors (fatal **and** retryable, e.g. `CaptchaUnsolvable`) are rethrown at once, since `WaitForResultAsync` never retries. The deadline is `UtcNow + Timeout` at the start of the wait; the last delay is shortened to end at the deadline, and a poll that is not ready (or a transient error) at or past it fails with `SolveTimeout` (so Timeout 10 s / PollInterval 3 s → polls at 0, 3, 6, 9, 10 s). A zero first delay still goes through `ISystemClock.Delay` (it is recorded by `FakeClock` and honours cancellation). `GetTaskResultAsync` delegates to internal `GetTaskResultCoreAsync(taskId, redactor, ct)` so that the solve pipeline can redact the task's proxy password while polling (closes the Task 3.7 limitation for solves). `UnitTests/Pipeline/PipelineResponses.cs` holds the scripted payloads. Extra tests: `Wait_NullOptions_UsesClientDefaultSolveOptions`, `Wait_TransientErrorAtTimeout_ThrowsSolveTimeout`, `Wait_Status400_ThrowsImmediately`, `Wait_ReadyWithoutSolutionThenReady_ReturnsSolution`, `Wait_InvalidOptions_ThrowsArgumentOutOfRange_AndSendsNoRequest`, `Wait_ThroughInterface_IsAvailable`; `Wait_FatalApiError_ThrowsImmediately` is a Theory (NoSuchCaptchaId, KeyDoesNotExist, CaptchaUnsolvable).
  - Note from Task 3.6: `GetTaskResultAsync` already throws `InvalidResponse` for "ready without a usable solution", so the same code arrives for unparseable JSON and for ready-without-solution. Decide there whether the pipeline treats both as transient (FR-6; it then fails after `MaxConsecutivePollErrors`) or needs an internal distinction for an immediate failure.
  - File: `Internal/Pipeline/SolvePipeline.cs` (poll loop). Add `WaitForResultAsync` to the interface and the client.
  - Tests `UnitTests/Pipeline/WaitForResultTests.cs` (`FakeClock` + fake handler):
    - `Wait_ProcessingTwiceThenReady_ReturnsSolution_AndPollsThreeTimes`
    - `Wait_DelaysArePollInterval` (asserts the `FakeClock.Delays` sequence)
    - `Wait_DefaultFirstPollDelay_IsZeroForExistingTask`
    - `Wait_TransientErrorsBelowLimit_KeepsPolling`
    - `Wait_ConsecutiveTransientErrorsExceedLimit_ThrowsWithTaskId`
    - `Wait_TransientCounterResetsAfterSuccessfulPoll`
    - `Wait_TimeoutElapsed_ThrowsSolveTimeout`
    - `Wait_FatalApiError_ThrowsImmediately`
    - `Wait_ReadyWithoutSolution_ThrowsInvalidResponse`
    - `Wait_CancelledDuringDelay_ThrowsOperationCanceled`
  - Commit: `feat(pipeline): add WaitForResultAsync polling with transient-error tolerance`

- [x] Task 4.3: `TrySolveAsync` and `SolveAsync` happy path and result fields (TDD) efef115
  - Done 2026-09-30: both methods share a private `RunSolveAsync` (null task → `ArgumentNullException(task)`, invalid options → `ArgumentOutOfRangeException(o)`, then `RunValidation()`; a relative `CallbackUrl` is rejected by `CreateTaskAsync` before its HTTP call) and `SolvePipeline.SolveAsync`, which returns an internal `SolveOutcome<T>` (the `SolveResult` plus the `AntiCaptchaException` that `SolveAsync` throws, keeping the inner chain). The per-attempt deadline is `UtcNow` just before `createTask` + `Timeout`. `SolveTime` is null when the API omits either time; a failed result has null `Cost`/`SolveTime`/`WorkerIp`. `SolveResult<T>` has `where TSolution : Solution` like `TaskStatus<T>`. Single attempt here; the retry loop is Task 4.4. Extra tests: `Solve_Success_ReturnsSolution`, `TrySolve_InvalidOptions_ThrowsArgumentOutOfRange_NoRequests`, `TrySolve_RelativeCallbackUrl_ThrowsArgument_NoRequests`, `TrySolve_NullTask_ThrowsArgumentNull`, `Solve_CreateTaskFails_ThrowsWithoutTaskId_Attempts1`, `TrySolve_Failure_ReturnsErrorWithTaskIdAndAttempts`, `TrySolve_ReadyWithoutTimes_HasNoSolveTime`, `TrySolveAndSolve_CancelledDuringPollDelay_ThrowOperationCanceled`, `SolveMethods_ThroughInterface_AreAvailable`, and `RedactionTests.Solve_PollErrorEchoingSecrets_ExceptionAndResultContainNoSecrets` (Theory ×3: API error, network failure, invalid JSON while polling a proxy task; checks the exception chain, `SolveResult.ToString()` and `Error.Description`).
  - Files: `SolveResult.cs`; add both methods to `IAntiCaptchaClient` and `AntiCaptchaClient`.
  - Tests `UnitTests/Pipeline/SolveTests.cs`:
    - `TrySolve_Success_ReturnsTaskIdSolutionCostSolveTimeWorkerIpAttempts1`
    - `TrySolve_FirstDelay_UsesTaskFirstPollDelay` (Theory: Turnstile 10 s, ImageToText 3 s)
    - `TrySolve_FirstPollDelayOverride_Wins`
    - `TrySolve_UsesClientDefaultSolveOptions_WhenNullPassed`
    - `TrySolve_PassesLanguagePoolAndCallbackUrl`
    - `TrySolve_InvalidTask_ThrowsValidation_NoRequests`
    - `Solve_Failure_ThrowsAntiCaptchaExceptionWithTaskIdAndAttempts`
    - `SolveResult_IsSuccess_Semantics`
  - Commit: `feat(client): add SolveAsync and TrySolveAsync`

- [x] Task 4.4: Retry and backoff behaviour (TDD) 3d45335
  - Done 2026-09-30: retry rules. A `createTask` error is retried when `IsRetryable`. A polling error is retried only when `IsRetryable && !IsTransient` (the API task errors NoSlot/CaptchaUnsolvable/ProxyConnectionRefused). Exceeding `MaxConsecutivePollErrors` is **not** retried, although its `HttpError`/`HttpTimeout` is `IsRetryable` in isolation: spec FR-7 calls it "a failure", and like `SolveTimeout` (R-4) the task may be charged. `TaskId` stays the last created task when a later `createTask` fails. The backoff goes through `ISystemClock.Delay` with the client's random. Extra tests: `Status400OnCreate_NoRetry`, `PollErrorLimitExceeded_NoRetry`, `MaxAttempts1_DoesNotRetry`, `RetryDelay_UsesOptionsAndClientRandom` (random 0 → 8 s then 12 s with base 10 s / max 15 s). `CancelledDuringBackoff_ThrowsOperationCanceled` covers both `TrySolveAsync` and `SolveAsync`.
  - Tests `UnitTests/Pipeline/RetryTests.cs`:
    - `NoSlotOnCreate_TwiceThenSuccess_Attempts3_WithGrowingDelays`
    - `CaptchaUnsolvableWhilePolling_RetriesWithNewTask`
    - `ProxyConnectionRefused_Retries`
    - `TransientHttpOnCreate_Retries`
    - `ZeroBalance_NoRetry_Attempts1`
    - `KeyDoesNotExist_NoRetry`
    - `SolveTimeout_NoRetry` (spec R-4)
    - `RetryableUntilMaxAttempts_FailsWithLastError`
    - `CancelledDuringBackoff_ThrowsOperationCanceled`
    - `TrySolve_ApiFailure_ReturnsErrorResult_NotThrow`
  - Commit: `feat(pipeline): retry retryable errors with backoff up to MaxAttempts`

- [x] Task 4.5: `MaxCostPerSolve` (TDD) 9f6b9ca
  - Done 2026-09-30: the check runs after a retryable failure, only when an attempt is left, and before the backoff delay (so no delay is spent). `CostLimitReached` carries the last attempt's `TaskId`/`Attempts`, the last failure as the inner exception, and a description with the budget, the reported spend and the last error **code**. Red: without the check both limit tests retried into an unscripted `createTask`. **Finding (see Deviations):** the API reports `cost` only with a ready result, which ends the solve, so the spend known before a retry is always 0 and is a `const` in `SolvePipeline`. `MaxCostPerSolve = 0` therefore means "no paid retries", and a positive budget cannot stop a retry yet. Extra tests: `CostLimitReached_TrySolve_ReturnsErrorResult`, `PositiveLimit_NoCostReportedYet_AllowsRetry`; `SolutionReceived_AlwaysReturned_EvenAboveLimit` is a Theory (0 and 0.001).
  - Tests `UnitTests/Pipeline/CostLimitTests.cs`:
    - `CostLimitReachedBeforeRetry_FailsWithCostLimitReached`
    - `SolutionReceived_AlwaysReturned_EvenAboveLimit`
    - `NoLimit_DoesNotCheck`
  - Commit: `feat(pipeline): add optional MaxCostPerSolve budget`

- [x] Task 4.6: Remove `MaxCostPerSolve` (owner decision 2026-09-30) 51b9329
  - Done 2026-09-30: `SolveOptions.MaxCostPerSolve`, its validation, the pipeline check, the spend `const` and the error builder are gone. `AntiCaptchaErrorCode.CostLimitReached` (1004) was removed because nothing else used it; `HttpError`..`SolveTimeout` keep 1000–1003. `CostLimitTests.cs` (6 cases) and `Validate_NegativeMaxCost_Throws`/`Validate_ZeroMaxCost_IsAllowed` were deleted, and the `Defaults_MatchSpec` assertion dropped. Red: `SolveOptions_HasNoMaxCostPerSolve` (`Assert.Empty` failed) and `AntiCaptchaErrorCode_HasNoCostLimitReached` (`Assert.DoesNotContain` failed) while the members existed. Offline suite 540 (546 − 8 + 2). spec.md FR-6/FR-7/R-6 and design-v3.md §4.4 carry the dated removal and the client-wide spend guard future-work note. The probe never referenced the option.
  - Why: anti-captcha reports and charges `cost` only for a ready (solved) task, and a ready task ends the solve. So every retry follows an unpaid attempt, and a limit checked before a retry always sees 0: `0` blocks all retries, any positive value never triggers (found in Task 4.5). The owner chose to remove it before 3.0 ships.
  - Red: add a test asserting `SolveOptions` has no `MaxCostPerSolve` member (reflection) and that `AntiCaptchaErrorCode` has no `CostLimitReached` value, unless something else uses it. Remove `CostLimitTests.cs` and every cost-limit assertion elsewhere.
  - Green: delete `SolveOptions.MaxCostPerSolve`, its validation, the pipeline check and the commented spend `const`, plus the `CostLimitReached` code if nothing else uses it. Update XML docs.
  - Docs: in spec.md, mark R-6 and the MaxCostPerSolve requirement as removed (dated). Add a future-work note: a client-wide spend guard (running total of the reported `cost` across solves, e.g. `AntiCaptchaClientOptions.MaxTotalCost`), which does work because solved tasks report cost. In design-v3.md §4.4, replace "optional MaxCostPerSolve" with that note.
  - Commit: `refactor(pipeline)!: remove MaxCostPerSolve (cannot trigger; cost is only reported on success)`
- [x] Verification 4: Phase 4 checkpoint [5acd8ee] — user confirmed 2026-09-30
  - Run the offline suite and confirm the full suite takes under 30 s. Run coverage for `Internal/Pipeline/**` (≥ 80 %).
  - Add probe commands:
    - `offline-solve`: scripted processing → transient error → ready. Prints the `SolveResult` and the simulated timeline.
    - `offline-retry`: NoSlot ×2 → success.
    - `live-solve-image` (user only): `SolveAsync(ImageToTextTask.FromFile(...))`, printing the text, cost and attempts.
  - Manual: run both offline commands. Optionally, the user runs `live-solve-image`.
  - Wait for user confirmation, then follow the checkpoint protocol.

---

## Phase 5: Account, report and stats endpoints [checkpoint: 9e65c4c]
Goal: every remaining 2.x API method has a 3.0 equivalent (spec FR-8), and the public surface is pinned.

Tasks:
- [x] Task 5.1: `GetBalanceAsync` (TDD) [32aa2dd]
  - Tests `UnitTests/Client/AccountTests.cs`:
    - `GetBalance_SendsClientKey_ReturnsDecimal`
    - `GetBalance_ApiError_Throws`
  - Commit: `feat(client): add GetBalanceAsync`

- [x] Task 5.2: `GetQueueStatsAsync`, `QueueType`, `QueueStats` (TDD) [809a988]
  - Tests:
    - `GetQueueStats_SendsQueueId` (Theory over every `QueueType` value)
    - `GetQueueStats_MapsAllFields`
    - `QueueType_Values_MatchLegacyNumbers` (asserts that the 2.x numeric ids from `public-api.txt` and the enum values are equal, honouring the `RecaptchaB2*` → `RecaptchaV2*` renames)
  - Commit: `feat(client): add GetQueueStatsAsync`

- [x] Task 5.3: `ReportAsync` and `Report` (TDD) [2903674]
  - Tests `UnitTests/Client/ReportTests.cs`:
    - `Report_MapsToEndpoint` (Theory: IncorrectImageCaptcha → `reportIncorrectImageCaptcha`, IncorrectRecaptcha → `reportIncorrectRecaptcha`, CorrectRecaptcha → `reportCorrectRecaptcha`)
    - `Report_SendsClientKeyAndTaskId`
    - `Report_UndefinedValue_ThrowsArgumentOutOfRange_NoRequest`
    - `Report_ApiError_Throws`
  - Commit: `feat(client): add ReportAsync`

- [x] Task 5.4: `PushAntiGateVariableAsync` (TDD) [e831e3d]
  - Tests:
    - `Push_SupportedValueTypes_SerializeCorrectly` (Theory: string, bool, int, long, double, decimal, `JsonNode`)
    - `Push_UnsupportedType_ThrowsArgumentException_NoRequest`
    - `Push_ApiError_Throws`
  - Commit: `feat(client): add PushAntiGateVariableAsync`

- [x] Task 5.5: `GetSpendingStatsAsync`, `SpendingQuery`, `SpendingStats`, `DailySpending` (TDD) [63eca17]
  - Tests:
    - `GetSpendingStats_NullQuery_SendsOnlyClientKey`
    - `GetSpendingStats_DateSentAsUnixSeconds`
    - `GetSpendingStats_SendsQueueSoftIdIp`
    - `GetSpendingStats_ReadsDataArray`
  - Commit: `feat(client): add GetSpendingStatsAsync`

- [x] Task 5.6: `GetAppStatsAsync`, `AppStatsMode`, `AppStats` (TDD) [f1decd8]
  - Tests:
    - `GetAppStats_SendsSoftIdAndLowercaseMode` (Theory over every mode)
    - `GetAppStats_NullMode_OmitsMode`
    - `GetAppStats_ReadsFromToAndChartData`
  - Commit: `feat(client): add GetAppStatsAsync`

- [x] Task 5.7: Public surface guard (TDD) [0dee005]
  - Tests `UnitTests/Packaging/PublicApiTests.cs`:
    - `IAntiCaptchaClient_Methods_MatchDesign`: expected signatures are hard-coded from spec FR-4, and every method has a trailing `CancellationToken ct = default`.
    - `EveryPublicType_HasXmlDocs`: redundant with CS1591, kept as a guard in case the warning setting is removed.
    - `PublicTypes_OnlyInAllowedNamespaces` (`AntiCaptchaApi.Net`, `AntiCaptchaApi.Net.Tasks`)
    - `NoPublicMember_ExposesInternalOrThirdPartyJsonTypes`, except the documented `JsonObject`/`JsonNode`/`JsonElement` on AntiGate and AppStats
  - Commit: `test(api): pin IAntiCaptchaClient surface and public namespaces`

- [x] Task 5.8: Hardening follow-ups from Phase 5 (owner approved 2026-09-30) (TDD) ffda88b
  - (a) `Internal/Json/TaskResultMapper.cs`: an out-of-range unix `createTime`/`endTime` must throw `AntiCaptchaException(InvalidResponse)` with `TaskId`, not a raw `ArgumentOutOfRangeException`. Reuse the spending-stats range guard as one shared internal helper. Red: `GetTaskResult_OutOfRangeUnixTime_ThrowsInvalidResponse` (Theory: createTime, endTime; huge and negative).
  - (b) Add `AntiCaptchaErrorCode.AccessDenied` for `ERROR_ACCESS_DENIED`. Pick a value that doesn't collide with the documented errorIds or the client codes 1000–1003, and record the choice. It is fatal (not retryable, not transient). Red: parser Theory row, classifier rows, and `GetAppStats_AccessDenied_MapsToAccessDenied`. Update the Task 5.6 test that pinned `Unknown`.
  - (c) `UnitTests/Security/RedactionTests.cs`: `AccountMethods_ErrorPaths_ContainNoSecrets` (Theory over GetBalance, GetQueueStats, Report, PushAntiGateVariable, GetSpendingStats, GetAppStats × API error echoing the key, network failure echoing the key, invalid JSON), asserting that the exception message, `ToString()` and `Error.ToString()` contain no key.
  - Commit: `fix(client): harden unix-time parsing, add AccessDenied, cover account methods in redaction tests`
- [x] Verification 5: Phase 5 checkpoint [9e65c4c] — user confirmed 2026-09-30
  - Run the offline suite and coverage.
  - Add probe commands (user runs the live ones with `ClientKey`): `live-balance`, `live-queue <QueueType>`, `live-appstats`, `live-spending`.
  - Manual: offline tests green. Optionally, the user runs the live read-only commands; they have no cost.
  - Wait for user confirmation, then follow the checkpoint protocol.

---

## Phase 6: Observability [checkpoint: 9882a4f]
Goal: `ActivitySource("AntiCaptcha")` and `Meter("AntiCaptcha")` telemetry, free of secrets.

Tasks:
- [x] Task 6.1: DiagnosticSource dependency and `AntiCaptchaDiagnostics` (TDD) [295f16d]
  - Add `System.Diagnostics.DiagnosticSource 8.0.0` to the csproj (netstandard2.0 only). Add `AntiCaptchaDiagnostics.cs` (public constants) and `Internal/Diagnostics/Telemetry.cs` (static `ActivitySource`, `Meter`, instruments).
  - Tests:
    - `AntiCaptchaDiagnosticsTests.Names_AreAntiCaptcha`
    - `Telemetry_Instruments_HaveSpecNamesAndUnits` (`MeterListener.InstrumentPublished`)
  - Commit: `feat(diagnostics): add AntiCaptcha ActivitySource and Meter`

- [x] Task 6.2: Activities (TDD) [640d9b5]
  - Tests `UnitTests/Diagnostics/ActivityTests.cs` (`ActivityListener`):
    - `Solve_Success_EmitsSolveActivityWithTypeTaskIdAttempts`
    - `Solve_Failure_SetsErrorStatusAndErrorCode`
    - `EachHttpCall_EmitsChildActivityNamedByMethod`
    - `NoListener_NoActivityCreated`
  - Commit: `feat(diagnostics): trace solves and API calls`

- [x] Task 6.3: Metrics (TDD) [2298d3e]
  - Tests `UnitTests/Diagnostics/MetricsTests.cs` (`MeterListener`):
    - `CreateTask_IncrementsTasksCreated`
    - `Solve_Success_RecordsSolvesDurationAndCost`
    - `Solve_Failure_IncrementsFailuresByErrorCode`
    - `Retry_CountsEachCreatedTask`
  - Commit: `feat(diagnostics): add task, failure, cost and duration metrics`

- [x] Task 6.4: Telemetry redaction (TDD) [f896645]
  - Tests:
    - `Telemetry_AllTagValuesAndEvents_ContainNoKeyOrProxyPassword`, covering the success, retry, transient and fatal scenarios.
  - Commit: `test(security): assert telemetry carries no secrets`

- [x] Verification 6: Phase 6 checkpoint [9882a4f] — user confirmed 2026-09-30
  - Run the offline suite and coverage.
  - Add a probe command `telemetry`: registers `ActivityListener` and `MeterListener`, runs the `offline-solve` scenario, and prints activities and measurements.
  - Manual:
    1. Run `telemetry` and confirm the names and tags, and that no secret appears.
    2. Run `dotnet pack` and check that the nuspec ns2.0 group = STJ + DiagnosticSource and net8.0 = empty.
  - Wait for user confirmation, then follow the checkpoint protocol.

---

## Phase 7: Live tests port, 2.x removal, docs, version
Goal: 2.x code is gone and the live tests are ported (opt-in). MIGRATION, README and CHANGELOG are complete and the version is `3.0.0-preview.1`.

Tasks:
- [x] Task 7.1: Opt-in live test infrastructure and port (TDD for the attribute only) [5d41522]
  - Files: `LiveTests/LiveFactAttribute.cs` and `LiveTheoryAttribute.cs` (set `Skip` when `ClientKey` is unset; `[Trait("Category","Live")]`), `LiveTests/LiveEnvironment.cs` (`ClientKey`, `ProxyAddress`, `ProxyPort`, `ProxyLogin`, `ProxyPassword`, same env names as 2.x).
  - Test `UnitTests/Live/LiveFactAttributeTests.cs`:
    - `Skip_WhenClientKeyMissing_IsSet`
  - Port each 2.x `IntegrationTests/*` class to `LiveTests/<Type>LiveTests.cs` using `SolveAsync`:
    - FriendlyCaptcha, FunCaptcha, GeeTest v3/v4 (merge the duplicate 2.x GeeTest classes), ImageToCoordinates (Theory over modes), ImageToText, Prosopo, RecaptchaV2, RecaptchaV2Enterprise, RecaptchaV3 (enterprise and non-enterprise), Turnstile.
    - Account: balance, queue stats, app stats.
    - Proxy variants run only when the proxy env vars are set.
    - Keep `Resources/captchaexample.png` and `Resources/cats.jpeg`, and the GeeTest helper models (ported to STJ).
  - Verify: `dotnet test --filter "FullyQualifiedName~UnitTests"` does not run them. `dotnet test --filter "Category=Live"` **without** `ClientKey` reports all of them as skipped. **Never run with a key in a task loop.**
  - Commit: `test(live): port 2.x integration tests to opt-in 3.0 live tests`

- [~] Task 7.2: Delete 2.x code (no red phase — removal; suite must stay green)
  - Delete `Requests/`, `Responses/`, `Models/`, `Enums/`, the `Internal/{Common,Converters,Extensions,Helpers,Models,Services,Validation}/` folders, `AnticaptchaClient.v2.cs`, `IAnticaptchaClient.v2.cs` (renamed in Task 3.4) and `AnticaptchaServiceCollectionExtensions.cs`. Also delete the 2.x tests: `UnitTests/RequestSerializationTests.cs`, `IntegrationTests/`, `Helpers/`, `Models/` (after the port) and `TestEnvironment.cs`.
  - Remove every `<Compile Remove>` entry from both csproj files.
  - Keep `Snapshots/Legacy2x/**` and `LegacyParityTests` as a permanent contract regression.
  - Verify: build with 0 warnings. Searching for `Newtonsoft` in `*.cs` and `*.csproj` returns nothing.
  - Commit: `refactor(core)!: remove 2.x API surface`

- [ ] Task 7.3: MIGRATION.md (TDD)
  - Red: `UnitTests/Docs/MigrationGuideTests.cs`:
    - `MigrationGuide_MentionsEveryLegacyPublicType`: every type in `Snapshots/Legacy2x/public-api.txt` appears in `MIGRATION.md`, located via `[CallerFilePath]` to the repo root.
    - `MigrationGuide_MentionsEveryLegacyClientMethod`
  - Green: `MIGRATION.md` containing:
    - Overview and minimum-change example.
    - Tables: client and ctor, config (`ClientConfig` → options), methods (spec §8), request class → task record (all 2.x request classes), responses and solutions, enums (`ProxyTypeOption` → `ProxyType`, `QueueType` renames, `ReportOutcome` → `Report` with IncorrectHCaptcha removed), errors (data → exceptions / `SolveResult`), removed items (`RawPayload`/`RawResponse`, `Configure`, `ClientKey`, public validators/converters/interfaces), DI ("coming in `AntiCaptchaApi.Net.DependencyInjection`; meanwhile use `new AntiCaptchaClient(key, httpClientFromFactory, options)`").
    - Wire deviations (spec §9) and changed defaults (spec R-7).
  - Commit: `docs(migration): add 2.x to 3.0 migration guide`

- [ ] Task 7.4: README rewrite (no red phase — docs)
  - `README.md` sections: badges; install; a 10-line quick start (`new AntiCaptchaClient(key)` + `SolveAsync(new TurnstileTask(...))`); `TrySolveAsync` example; one example per task type (13); proxy usage; `SolveOptions`/`AntiCaptchaClientOptions` table; errors and retry classification; cancellation vs timeout; `IHttpClientFactory` usage without the DI package; observability (source and meter names, OpenTelemetry snippet); troubleshooting; links to MIGRATION and CHANGELOG.
  - Verify: each code snippet compiles. Copy them into a probe command `readme-snippets` that compiles against the library; running it is not required.
  - Commit: `docs(readme): rewrite README for 3.0 core package`

- [ ] Task 7.5: Version, package metadata, CHANGELOG (no red phase — release prep; no publishing)
  - csproj:
    - `Version` and `PackageVersion` = `3.0.0-preview.1`.
    - `Title` = `AntiCaptchaApi.Net`.
    - `Description` updated.
    - `PackageReleaseNotes` = "See CHANGELOG.md".
    - `PackageReadmeFile` = `README.md` (packed).
    - Copyright year updated.
    - Keep `IncludeSymbols` and `snupkg`.
  - `CHANGELOG.md` (create or extend; the hotfix may have created it): a human-written `3.0.0-preview.1` entry with sections Added / Changed / Removed / Fixed, linking MIGRATION.md.
  - Verify: `dotnet pack -c Release -o <scratch>` produces `AntiCaptchaApi.Net.3.0.0-preview.1.nupkg` with the README included and the nuspec deps from NFR-2. **No `dotnet nuget push`, no tag, no push.**
  - Commit: `chore(release): prepare 3.0.0-preview.1`

- [ ] Verification 7: Final checkpoint [checkpoint marker]
  - Run the offline suite and a coverage report (≥ 80 % on new core code). Run `git diff --name-only <phase-6-checkpoint> HEAD` and confirm that every code change has tests.
  - Manual:
    1. `dotnet run --project tools/AntiCaptchaApi.Net.Probe -- info|serialize|offline-solve|telemetry`.
    2. Read `README.md` and `MIGRATION.md`.
    3. Inspect the nupkg contents.
    4. Optionally, the user runs `ClientKey=... DOTNET_ROLL_FORWARD=Major dotnet test --filter "Category=Live&FullyQualifiedName~ImageToText"` for one paid smoke solve.
  - Wait for user confirmation, then make the checkpoint commit. Updating `tracks.md` is left to the orchestrator.

---

## Baselines
- 2.x at branch creation (77c8966): tests: 126 offline unit tests passing (net9.0 test TFM, roll-forward to .NET 10), warnings: 15 (all in the test project; core 0) (Task 1.1)
- After Phase 1: tests: 16, core warnings: 0, test warnings: 0 (Verification 1, c751ad5)

## Deviations
_Record any departures from spec/tech-stack here with a date (workflow step 7)._
- 2026-09-30, Task 1.2: every 2.x type serialized without throwing (including `ImageToCoordinatesRequest`), so no snapshot was written by hand. `AntiGateRequest` is a single class for both proxy modes, so it got all three cases (`minimal`, `full`, `proxy`), not only `proxy`. The `proxy` case of each proxy class sets every settable field plus the proxy. This matters because some 2.x fields (for example RecaptchaV2 `cookies`) exist only on the proxy class. An extra guard fact `Capture_CoversEveryConcreteLegacyRequestType` asserts that the capture table lists every concrete 2.x request type. The capture changed spec §9: item 1 (`mode` is already a lowercase string in 2.x) and item 3 (the hotfix already accepts 65535) are no longer 2.x deviations. See the dated note in spec §9.
- 2026-09-30, Task 1.2: the `public-api.txt` format is `Namespace.Type`, `Namespace.Type..ctor(ParamTypes)`, `Namespace.Type.Method<T>(ParamTypes)`, `Namespace.Type.Property`, `Namespace.Type.Field` and `Namespace.EnumType.Member = N`. Parameter types are full names (generic types as `Name<Args>`, `Nullable<T>` as `T?`). Only declared public members are listed; accessors and other special-name methods are skipped. It has 505 lines.
- 2026-09-30, Task 1.3: the hotfix track added 14 more 2.x-dependent test files at the top level of `UnitTests/`, beside `RequestSerializationTests.cs` (AmazonWafClient, AntiGateClient, AntiGateRequestValidator, FakeHttpMessageHandler, PayloadRedactor, ProxyValidation, RawPayloadRedaction, RecaptchaV3RequestValidator, RegistryCompleteness, ReportOutcome, ServiceCollectionExtensions, SolveCaptchaAsync, TaskResultConverter, WaitForTaskResult). All 15 are listed explicitly in `<Compile Remove>` and stay on disk until Task 7.2. The offline count therefore drops from 166 to 2 (the new `CoreAssemblyTests`). `<GeneratePackageOnBuild>` was also removed from the core csproj (the orchestrator asked for this; it packed stale DLLs), and this is noted in tech-stack.md. The nullable polyfills are `AllowNull`, `DisallowNull`, `MaybeNull`, `NotNull`, `MaybeNullWhen`, `NotNullWhen`, `NotNullIfNotNull` and `DoesNotReturn`.
- 2026-09-30, Task 1.4: the test csproj uses `<None Update="Snapshots/**;Fixtures/**" …>` instead of `<None Include>`. The SDK already includes those files as `None` items, and a second `Include` would duplicate them. `FakeHttpMessageHandler` responders are `Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>` (a ct-less overload exists), so the Task 3.3 timeout tests can wait on the token. There are also `RespondJson`, `RespondStatus`, `Throw` and `RequestsFor(method)` helpers. `FakeClock` has an `OnDelay` hook (for cancelling mid-delay in Phase 4) and an `Advance` helper. `SnapshotAssert` also has `Equal(expected, actual)`, which never writes files, and `Normalize` (compact, keys sorted). Snapshot files are read from the test output directory and written to the source directory. `SystemClockTests` lives in `UnitTests/Time/`, and the fakes live in `UnitTests/Infrastructure/`.
- 2026-09-30, Task 1.5: `CreateTaskResponse.cs` lives in `Internal/Json/Envelopes/` beside the other envelopes. `CreateTaskRequest.CallbackUrl` is a `string?`; the client converts `CreateOptions.CallbackUrl` (`Uri`) when it builds the envelope. `SoftId` is a get-only property that returns `SoftId.Value`. Four tests were added beyond the plan so that the new code is covered: `Deserialize_CreateTaskResponse_ReadsTaskId`, plus `LowerCaseEnumConverterTests.Serialize_WritesCamelCaseString` (Theory) and `Deserialize_Integer_IsRejected`. They use reflection-based `JsonSerializerOptions`, which tests may use; the converter is not yet used by any context type.
- 2026-09-30, Task 1.6: the probe loads the core with `Assembly.Load("AntiCaptchaApi.Net")`, because the core has no public types yet. It sets `TreatWarningsAsErrors`, and is placed in a `tools` solution folder. The x64/x86 solution platforms that `dotnet sln add` generates were removed to keep the `.sln` diff minimal. `info` output: version 2.2.0.0, target framework `.NETCoreApp,Version=v8.0`, references only System.Runtime, System.Text.Encodings.Web and System.Text.Json (no Newtonsoft).
- 2026-09-30, Task 2.5: the `DocumentedDeviations` allow-list is **empty**, not the 7 items of spec §9. Per the dated spec §9 note, no §9 item changes a captured 2.x createTask key: item 1 is not a deviation, item 3 is validation-only, item 2 only drops the 0.5 score (the samples use 0.3/0.7) and adds `isEnterprise` (an added key, which parity allows), items 4–5 are not createTask fields, item 6 is behaviour and item 7 is additive. A guard test rejects entries that are not needed, so the list cannot hide regressions. The `RecaptchaV3EnterpriseRequest.minimal` target (`RecaptchaV3Task.minimal`) is provisional: that legacy file has `isEnterprise: true`, so Task 2.8 must map it to a sample with `IsEnterprise = true` and `minScore` 0.3.
- 2026-09-30, Tasks 2.6–2.12: after the catalog, `DocumentedDeviations` is still **empty**: every 2.x key of all 38 legacy files is present in 3.0 with the same value. The 3.0 wire changes are additions only: `isEnterprise: false` in `RecaptchaV3Task.minimal` (spec §9 item 2), and `userAgent`/`cookies` in the `full` samples of the proxy-capable types, where 2.x allowed them only on the proxy classes. `RecaptchaV3EnterpriseRequest.minimal` maps to a new `RecaptchaV3Task.enterprise` case (Task 2.8). Beyond the per-task tests the plan lists, internal helpers were added: `Validator.UserAgentWhenProxy`, `Tasks/ImageBody.cs` and `Tasks/RecaptchaApiDomain.cs`. `EnterprisePayload` and `InitParameters` are `IReadOnlyDictionary<string, string>` rather than JSON nodes, so public JSON types stay limited to AntiGate/AppStats (Task 5.7). AmazonWaf now validates `Iv`/`Context` with a proxy too (a 2.x bug). Q-1 is resolved with the internal `IncludesProxyType` override (Task 2.12; spec §11 note).
- 2026-09-30, Task 3.4: the 2.x `AnticaptchaClient.cs` and `IAnticaptchaClient.cs` differ from the new `AntiCaptchaClient.cs` and `IAntiCaptchaClient.cs` only by case. MSBuild's `<Compile Remove>` matches case-insensitively, so it also excluded the new files, and the pairs cannot coexist on case-insensitive filesystems (Windows, macOS). The 2.x files were renamed with `git mv` (content unchanged) to `AnticaptchaClient.v2.cs` and `IAnticaptchaClient.v2.cs`, and the `<Compile Remove>` entries and the Task 7.2 file list were updated. `AntiCaptchaClientOptions.DefaultSolveOptions` (spec FR-5) needs `SolveOptions`, which the plan introduces in Task 4.1, so it is added there (note under Task 4.1).
- 2026-09-30, Task 3.6: `AntiGateSolution.Cookies/LocalStorage/SessionStorage/Fingerprint/HtmlsInBase64/Screenshots/RequestHeaders/ResponseHeaders` are `JsonNode?`, not the Phase 2 `JsonObject?`. The AntiGate doc response example (fetched 2026-09-30) sends `screenshots`, `requestHeaders` and `responseHeaders` as `[]`, and the docs type `HTMLsInBase64` as an array. The API (PHP) encodes empty maps as `[]`, and 2.x accepted either shape for all of them (it wrapped arrays). With `JsonObject?`, a paid, ready AntiGate task would have failed as `InvalidResponse`. `JsonNode` stays within the AntiGate/AppStats JSON-type exception of Task 5.7. The doc example also carries a `domainsOfInterest` object in the solution; it is not mapped (no spec requirement) and is candidate follow-up.
- 2026-09-30, Task 4.2: decision by the orchestrator: `InvalidResponse` during polling (unparseable JSON and ready-without-usable-solution / unknown status alike) is transient and is re-asked up to `MaxConsecutivePollErrors` before failing with that error and the `TaskId`. This follows FR-6 ("`InvalidResponse` during polling" is transient); the task is already paid for, so re-asking is cheaper than failing at once.
- 2026-09-30, Task 4.4: after `MaxConsecutivePollErrors` is exceeded, the solve fails with that transient error without a new attempt, even though the error's `IsRetryable` is true (transient HTTP is retryable on `createTask`). Spec FR-7 step 2.2 lists it as "a failure", separate from "retryable task error: go to step 3", and a new attempt could pay twice for a task that may still be solved (the R-4 reasoning).
- 2026-09-30, Task 4.5: spec R-6 is implemented literally ("the sum of costs reported for tasks created in this call ≥ the limit" before a retry), but no API response reports a cost before a retry: `cost` comes only with `status: ready`, which ends the solve, and error envelopes carry no cost. The known spend before a retry is therefore always 0 (a `const` with a comment in `SolvePipeline`), so `MaxCostPerSolve = 0` stops every retry and a positive budget never triggers. It is pinned by `PositiveLimit_NoCostReportedYet_AllowsRetry`. Candidate follow-up for the owner: keep it as is, estimate the cost per attempt (for example from `getQueueStats` bids), or drop the option before 3.0. **Resolved by Task 4.6 (2026-09-30):** the owner chose to drop it; `MaxCostPerSolve` and `CostLimitReached` were removed (51b9329), and a client-wide spend guard is noted as future work in spec.md.
- 2026-09-30, Task 5.2: Q-4 is resolved from the docs (https://anti-captcha.com/apidoc/methods/getQueueStats, fetched 2026-09-30), which list more queues than 2.x: `QueueType` gains `TurnstileWithProxy = 26`, `TurnstileWithoutProxy = 27` and `ImageToCoordinates = 28`. Queue 29 (hCaptcha Enterprise) is not added, because 3.0 drops hCaptcha (Q-3, R-10). The other 2.x names stay (only the `RecaptchaB2*` typo is fixed). `getQueueStats` is sent as `{"queueId":N}` without `clientKey`, as documented and as in 2.x. The documented response has no `errorId` and sends `bid` as a string, so every number is also read from a string. `QueueStats` is `(int Waiting, double Load, decimal Bid, double Speed, int Total)`: `Bid` is money, so it is `decimal`; a missing field is `InvalidResponse`. `GetBalanceAsync` (Task 5.1) also reads a string `balance` and treats a missing one as `InvalidResponse`; `captchaCredits` is not mapped (FR-4 returns `decimal`).
- 2026-09-30, Task 5.4: `PushAntiGateVariableAsync` accepts every integral type (`sbyte` … `ulong`), `float`, `double`, `decimal`, `string`, `bool` and `JsonNode`, beyond the Theory list in the plan, per FR-8 ("integral and floating-point numbers"). Beyond the plan: NaN and ±Infinity throw `ArgumentException` (JSON cannot carry them), a null value throws `ArgumentNullException`, and a null/empty/whitespace `name` throws `ArgumentException`, all before any request. The unsupported-type message names the type but never the value, because a template variable can be a secret. Report and push errors carry `TaskId` (shared `PostForTaskAsync`, Task 5.3).
- 2026-09-30, Task 5.5: `getSpendingStats` follows the docs (fetched 2026-09-30): the records are hourly in the documented example, although the public type is named `DailySpending` as in the spec. `SpendingQuery.Date` is sent as `ToUnixTimeSeconds()` without rounding to the hour (the API documents "the hour from which we grab the 24 hour stats"). A missing or null `data`, a null record, a record without `dateFrom`/`dateTill`/`volume`/`money`, or a time outside the `DateTimeOffset` range is `InvalidResponse`; `data: []` gives no records. Candidate follow-up: `TaskResultMapper` converts `createTime`/`endTime` with `FromUnixTimeSeconds` without that range check, so an absurd value would escape as `ArgumentOutOfRangeException` rather than `InvalidResponse` (not changed here; outside Phase 5).
- 2026-09-30, Task 5.6: `AppStats.FromDate`/`ToDate` are `string`: the docs (fetched 2026-09-30) return them preformatted without a year (`"25 Jan 00:00"`), so they cannot be parsed safely into dates. `ChartData` is a `JsonElement` (the documented array; an object is kept too, as 2.x accepted either). A missing or null `chartData`, `fromDate` or `toDate` is `InvalidResponse`. `ERROR_ACCESS_DENIED`, which the getAppStats page names for non-owners, is not in the errors reference that `AntiCaptchaErrorCode` mirrors, so it maps to `Unknown` with `RawCode` kept (pinned by `GetAppStats_ApiError_Throws`). Candidate follow-up: add an `AccessDenied` code.
- 2026-09-30, Task 5.7: the guards passed against the finished surface on the first run, so red was shown with temporary, reverted mutations (ct without a default, an internal envelope made public, a `JsonNode` property on `QueueStats`, an undocumented member). Together they failed 5 of the 7 tests. The docs guard skips parameterless instance constructors (CS1591 ignores the implicit ones on `AntiCaptchaClientOptions`, `CreateOptions`, `SolveOptions` and `SpendingQuery`). The expected FR-4 lines add `where TSolution : Solution` to `GetTaskResultAsync` and `WaitForResultAsync`: the spec shows the constraint there, but design §4.2 leaves it out. Extra guards beyond the plan: `AntiCaptchaClient_IsSealedAndImplementsTheInterface` and `JsonTypeException_IsActuallyUsed`.
- 2026-09-30, Task 5.8: `AntiCaptchaErrorCode.AccessDenied = 900`. `ERROR_ACCESS_DENIED` has no documented `errorId` (the getAppStats page names it but the errors reference does not list it), so the enum cannot mirror one. 900 opens a block (900–999) for API codes without a documented `errorId`: it is clear of every documented id (1–57) and of the client codes (1000–1003), and the enum summary says so. The id the API actually sends stays in `AntiCaptchaError.ErrorId`. `AccessDenied` is fatal (the classifier default). Because `DocumentedCodes` asserts `(int)code == errorId`, the parser row is a separate Theory (`Parse_CodeWithoutDocumentedErrorId_MapsEnumOutsideDocumentedAndClientRanges`), which also asserts the 900–999 range and no collision. `GetAppStats_ApiError_Throws` now uses `ERROR_KEY_DOES_NOT_EXIST`. The shared unix-time guard is `Internal/Time/UnixTime.TryFromSeconds` (used by `TaskResultMapper` and `GetSpendingStatsAsync`); `UnixTimeTests` (7 rows, boundaries included) was added beyond the plan. The (c) Theory passed on the first run. A temporary, reverted mutation (`SecretRedactor.Redact` as a no-op) failed the 12 api-error and network-failure rows. The 6 invalid-json rows do not depend on redaction, because the response body never reaches the error.
- 2026-09-30, Task 6.1: System.Diagnostics.DiagnosticSource **8.0.0** is used as planned: `dotnet list package --vulnerable --include-transitive` reports no vulnerable package (direct or transitive) on nuget.org. The spec gives units only for `anticaptcha.solve.duration` (`s`) and `anticaptcha.cost` (`USD`); the three counters use the OpenTelemetry annotation units `{task}`, `{solve}` and `{failure}`. The source and meter carry the core assembly version. `AntiCaptchaDiagnostics` has only the two spec constants, so `PublicApiTests` needed no change (the root namespace is already allowed, and the docs guard covers the new type). Telemetry test classes share the xUnit collection `Telemetry` (`DisableParallelization = true`), because the source and meter are process-wide.
- 2026-09-30, Task 6.2: the solve activity `AntiCaptcha.Solve` (kind Internal) starts after validation, so an invalid task creates none. Its tags `anticaptcha.task.id` (the last created task; absent when none was created) and `anticaptcha.attempts` are set when the solve ends. On failure the status is Error, and both the status description and `anticaptcha.error.code` hold the error code **name** (for example `ZeroBalance`), never the API description. Success leaves the status Unset (OpenTelemetry guidance for instrumentation libraries). Cancellation escapes without a status. Each API call is a child `AntiCaptcha.<apiMethod>` activity (kind Client) from `ApiTransport`; a failed call also gets Error and `anticaptcha.error.code`, so transient poll errors that the solve survives are visible. The call name is built only when the source has listeners. Beyond the plan: `StandaloneCall_EmitsRootActivityNamedByMethod` (a direct `GetBalanceAsync` gives a root activity). `NoListener_NoActivityCreated` passed at red, because nothing emitted yet; a temporary, reverted mutation (a call activity started without listeners) makes it fail.
- 2026-09-30, Task 6.3: metric tags use the spec FR-10 names literally (`task.type`, `outcome`, `error.code`), while activity tags keep the `anticaptcha.` prefix of the spec's activity list. Semantics where the spec leaves room: `anticaptcha.tasks.created` counts every successful `createTask`, including direct `CreateTaskAsync` calls and each retry attempt (a failed `createTask` counts nothing). `anticaptcha.failures` counts **failed solves** (one per `SolveAsync`/`TrySolveAsync` that fails, by final error code), not failed attempts; the error of an attempt that a later retry recovers from is visible on its call activity instead (Task 6.2). `anticaptcha.solve.duration` is measured on the client clock from the start of the first attempt to the end of the solve, and is tagged with `task.type` and `outcome` as well. `anticaptcha.cost` adds the reported cost of a successful solve (none when the API sends no cost). Nothing is recorded when a solve is cancelled. `WaitForResultAsync` records no solve metrics (it does not know the task type). Beyond the plan: `CreateTask_ApiError_DoesNotIncrementTasksCreated`.
- 2026-09-30, Task 6.4: `TelemetryRedactionTests` (in `UnitTests/Security/`, `Telemetry` collection) checks more than the key and proxy password: the proxy address and login, the website URL and key, the user agent and the solution token must not appear in any activity name, display name, status description, trace state, tag, baggage, event or link, nor in any metric tag. Beyond the four planned scenarios (success, retry, transient, fatal) there is `fatal-unknown-code-echo` (an unknown `errorCode` that contains the key while polling). Each row also asserts the expected outcome, so every path runs as scripted. The Theory passed on the first valid run (the first run failed only on the test's own arrangement: a proxied Turnstile needs a `UserAgent`). Red was shown with two temporary, reverted mutations: tagging the request JSON on call activities failed all 5 rows, and tagging the solution on the solve activity failed the 3 successful rows.
- 2026-09-30, Task 7.1: `[LiveFact]`/`[LiveTheory]` compute `Skip` in an overridden getter (an explicit `Skip` wins, then the `ClientKey` check, then the proxy check when `RequiresProxy = true`), so a proxy variant is one attribute argument instead of the 2.x silent `return`. The reason names the missing variables, never their values. An internal constructor takes the variable lookup, so the attribute tests never touch the process environment. `[Trait("Category","Live")]` is on each live class (xUnit v2 `FactAttribute` cannot carry a trait); the guard `LiveTests_UseOnlyLiveAttributesAndCarryLiveCategory` fails when a class misses it or uses a plain `[Fact]`/`[Theory]`. 9 unit tests instead of 1 (key missing/whitespace/present, proxy required missing/defined, explicit reason, the two theory cases, the guard). Port choices: the three 2.x v3 GeeTest classes and two v4 classes are one `GeeTestLiveTests`; the v4 facts stay skipped with the 2.x "unsolvable" reason (2.x failed them with `Assert.False(true)`), and Prosopo keeps its 2.x skip ("Find captcha website"). The two identical 2.x RecaptchaV2Enterprise facts are merged, the 2.x RecaptchaV2 test (which sent an Enterprise request) now sends a `RecaptchaV2Task`, and the two RecaptchaV3 classes are one Theory over `IsEnterprise`. `ImageToTextLiveTests` uses `TrySolveAsync` so it also checks `TaskId`/`Cost`/`WorkerIp`/`SolveTime` (the 2.x `AssertHelper` checks). The 2.x "incorrect file path" test is not ported: `FromFile` now throws `FileNotFoundException`, which the offline tests cover. The GeeTest helper models live in `LiveTests/GeeTest/` (STJ). Live classes share the xUnit collection `Live` (sequential, as 2.x). Without `ClientKey`, `--filter "Category=Live"` reports 20 tests, all skipped.
- 2026-09-30, Task 7.2: 198 files were deleted (the 2.x core folders and root files, `IntegrationTests/`, `Helpers/`, `Models/`, `TestEnvironment.cs` and all 15 excluded 2.x unit-test files from the Task 1.3 list, not only `RequestSerializationTests.cs`), and both `<Compile Remove>` blocks are gone. The offline count stays at 707, because none of the deleted tests was compiled. The stale 2.2.0 `PackageReleaseNotes` (which described the Newtonsoft/M.E.Http package) became `See CHANGELOG.md` here rather than in Task 7.5, so the `Newtonsoft` search passes. That search now finds only `CoreAssemblyTests` (the guard that asserts the core does **not** reference `Newtonsoft.*`), which is kept on purpose. The probe still builds and `info` lists no Newtonsoft or Microsoft.Extensions reference.
