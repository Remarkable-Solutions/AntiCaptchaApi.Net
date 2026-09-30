# Changelog

All notable changes to AntiCaptchaApi.Net are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project follows [Semantic Versioning](https://semver.org/).

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

[2.2.0]: https://github.com/RemarkableSolutionsAdmin/AntiCaptchaApi.Net/compare/v2.1.18...HEAD
[2.1.6]: https://github.com/RemarkableSolutionsAdmin/AntiCaptchaApi.Net/compare/v2.1.5...v2.1.18
[2.1.18]: https://github.com/RemarkableSolutionsAdmin/AntiCaptchaApi.Net/compare/v2.1.5...v2.1.18
[2.1.5]: https://github.com/RemarkableSolutionsAdmin/AntiCaptchaApi.Net/compare/v2.1.4...v2.1.5
[2.1.4]: https://github.com/RemarkableSolutionsAdmin/AntiCaptchaApi.Net/compare/v2.1.3...v2.1.4
[2.1.3]: https://github.com/RemarkableSolutionsAdmin/AntiCaptchaApi.Net/compare/v2.1.2...v2.1.3
[2.1.2]: https://github.com/RemarkableSolutionsAdmin/AntiCaptchaApi.Net/compare/v2.1.1...v2.1.2
