# C# Style Guide — AntiCaptchaApi.Net

Conductor ships no C# template; this guide is project-specific (public NuGet library).

## General
- File-scoped namespaces for new files; one public type per file; file name = type name.
- `Nullable` enabled in new code; no `!` suppression without a comment explaining why.
- Prefer `sealed` records for data (tasks, solutions, results); classes for services.
- `internal` by default. Anything `public` is API surface: it needs XML docs and is covered by package validation.
- No `dynamic`, no reflection-based dispatch at runtime in hot paths; trimming/AOT-safe code.
- `ConfigureAwait(false)` on every `await` in library code.
- Every async public method takes `CancellationToken ct = default` as the last parameter and passes it down (including `ReadAsStringAsync`/`ReadAsStreamAsync` where available).

## Errors
- Validation → `AntiCaptchaValidationException` (field-level errors). API errors → `AntiCaptchaException` with `AntiCaptchaErrorCode`.
- Never `catch (Exception)` to return a default; never swallow `OperationCanceledException`.
- Exception messages must not contain the client key or proxy credentials.

## Serialization
- Property names via `[JsonPropertyName]` matching anti-captcha docs exactly (e.g. `websiteURL`).
- Null values omitted; compact (non-indented) output.

## Tests
- xUnit. Test class per production type: `<Type>Tests`. Method names: `Method_Condition_ExpectedResult`.
- Offline only in task loops: HTTP via a fake `HttpMessageHandler`; no network, no real API key.
- JSON contracts asserted against checked-in expected files under `Snapshots/`.
- Arrange/Act/Assert separated by blank lines; one behavior per test.

## Formatting
- 4 spaces, Allman braces (matches existing code), `var` when the type is obvious.
- Keep the existing `AntiCaptchaApi.Net.sln.DotSettings` conventions.
