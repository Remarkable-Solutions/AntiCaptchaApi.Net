# Tech Stack — AntiCaptchaApi.Net

## Current (2.x, as found 2026-09-30)
| Area | Choice |
|---|---|
| Language | C# (LangVersion default per TFM) |
| Targets | `net8.0; net9.0; netstandard2.1` |
| JSON | Newtonsoft.Json 13.0.4 |
| HTTP | `HttpClient` / `IHttpClientFactory` via Microsoft.Extensions.Http 10.0.12 (in core package) |
| Tests | xUnit 2.9, coverlet.collector; project `AntiCaptchaApi.Net.Tests` (net9.0). `UnitTests/` offline (27 tests); `IntegrationTests/` hit the live paid API |
| CI | GitHub Actions: `publish-nuget.yml` (main → GitHub Packages, `v*` tag → nuget.org). Dependabot (`.github/dependabot.yml`, weekly `nuget` + `github-actions`; the library's runtime dependencies are ignored so their floors stay low). *Changed 2026-09-30 (track `hotfix_2x_critical_bugs_20260930`): the daily `auto-dependency-update.yml` cron (auto-bump + tag) was removed.* |

## Target (3.0)
| Area | Choice | Notes |
|---|---|---|
| Targets | `netstandard2.0; net8.0` | netstandard2.0 reaches .NET Framework 4.6.2+ |
| Language | `LangVersion` latest, `Nullable` enable | records/init on netstandard2.0 via `IsExternalInit` polyfill (internal) |
| JSON | System.Text.Json (package ≥ 8.0.5 on netstandard2.0 only; inbox on net8.0) | source-generated `JsonSerializerContext` |
| HTTP | plain `HttpClient`; shared static `SocketsHttpHandler`/`HttpClientHandler` for the no-DI ctor | no Microsoft.Extensions.* in core |
| DI | separate package `AntiCaptchaApi.Net.DependencyInjection` (Microsoft.Extensions.Http ≥ 8.0.0, Options ≥ 8.0.0) | returns `IHttpClientBuilder` |
| Observability | `System.Diagnostics.ActivitySource` + `Meter` (DiagnosticSource ≥ 8.0.0 on netstandard2.0) | `ILogger` only in DI package |
| Tests | xUnit, fake `HttpMessageHandler`, JSON snapshot tests (checked-in expected JSON files), coverlet | tests target net10.0 (local SDK) and net8.0 |
| Test run (local) | `DOTNET_ROLL_FORWARD=Major dotnet test` | only .NET 10 runtime installed locally |
| Coverage | `dotnet test --collect:"XPlat Code Coverage"` + report | target ≥ 80% on new code |
| CI | build + offline tests on every PR; publish only on `v*` tag; Dependabot PRs instead of auto-release cron | |

## Constraints
- Do not push to `main` directly: pushes to `main` publish CI packages to GitHub Packages.
- Live integration tests (`IntegrationTests/`) require a paid API key — never run them in automated task loops; filter with `--filter "FullyQualifiedName~UnitTests"`.
- In the Claude Code sandbox, `dotnet` builds need the sandbox disabled (MSBuild node/pipe restrictions).
