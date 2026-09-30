# Product Guidelines — AntiCaptchaApi.Net

## API design principles
1. **Pit of success.** The shortest code path must be the correct one: `await client.SolveAsync(new TurnstileTask(url, key))`. Required values are constructor parameters; optional ones are `init` properties with anti-captcha's defaults.
2. **Fail loudly and early.** Invalid input throws `AntiCaptchaValidationException` before any HTTP call (and before any money is spent). Never silently drop a value (e.g. a proxy on a proxyless-only type, a missing image file).
3. **Never waste a paid task.** A task that was created must not be abandoned because of a transient network error during polling.
4. **One place per concept.** Adding a captcha type = adding one file. No registries, dictionaries or switches that must be kept in sync by hand.
5. **Secrets are toxic.** The client key and proxy credentials never appear in `ToString()`, exception messages, logs, or exposed raw payloads.
6. **Cancellation is honored.** `CancellationToken` cancellation always surfaces as `OperationCanceledException`; an HTTP timeout is a distinct error.
7. **Minimal footprint.** Lowest-possible dependency floors; the core package depends on nothing beyond System.Text.Json (netstandard2.0 only).

## Naming
- Task records: `<Name>Task` matching anti-captcha's task type names without the `Proxyless` suffix (`RecaptchaV2Task`, `TurnstileTask`, `AmazonWafTask`).
- Solutions: `<Name>Solution`. Enums in singular (`QueueType`, `MinScore`).
- Async methods end in `Async` and accept `CancellationToken ct = default` last.

## Documentation voice
- Plain, direct English; lead with a runnable example.
- XML docs on every public member; link to the anti-captcha docs page for each task type.
- README per package: install → 10-line quick start → per-type examples → configuration → troubleshooting.
- Release notes are human-written per version (what changed for users), kept in `CHANGELOG.md`.

## Compatibility & versioning
- SemVer. Breaking changes only in majors, with a migration guide.
- 2.x maintained on a branch with bug fixes for ~6 months after 3.0.
- Package validation against the previous release baseline in CI (3.x onwards).
