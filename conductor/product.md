# Product Guide — AntiCaptchaApi.Net

## What it is
A .NET client library (NuGet: `AntiCaptchaApi.Net`) for the [anti-captcha.com](https://anti-captcha.com/apidoc) API. It is the foundation of a package family by Remarkable Solutions:

| Package | Repo | Role |
|---|---|---|
| `AntiCaptchaApi.Net` | this repo | API client (task model, solving, account/stats endpoints) |
| `AntiCaptchaApi.Net.DependencyInjection` (v3, new) | this repo | `IServiceCollection` integration, `IHttpClientBuilder` |
| `AntiCaptcha.Browser` (v3, new) | Selenium.AntiCaptcha repo | Browser-agnostic detect → solve → inject engine |
| `Selenium.AntiCaptcha` | Selenium.AntiCaptcha repo | Selenium adapter |
| `Playwright.Anticaptcha` (v3, new) | Selenium.AntiCaptcha repo | Playwright adapter |

## Business model
Every `createTask` call carries the Remarkable Solutions developer `softId` (currently `1023`, internal constant). anti-captcha.com pays the developer a share of spend attributed to that softId. **Revenue = number of successful solves made through these packages.** Therefore adoption, reliability and "it just works" matter more than anything else.

- softId stays internal and non-overridable (decision 2026-09-30).
- Only anti-captcha.com is supported; no multi-provider abstraction for now (decision 2026-09-30).

## Target users
- .NET developers automating web workflows (scraping, QA of own sites, form automation) who need captcha tokens.
- Users of Selenium and Playwright in .NET (via the browser packages).
- Range from hobbyists (console apps, `new AnticaptchaClient(key)`) to services (ASP.NET Core, DI, resilience).

## Problems solved
- Typed, validated requests for every anti-captcha task type — no hand-built JSON.
- Create-and-poll orchestration with sane timeouts, retries and error reporting.
- (Browser packages) automatic detection of the captcha on a page, solving, and injecting the token.

## Key features (v3 target)
- One self-describing record per captcha task type; proxy/proxyless handled by one optional `Proxy` property.
- `SolveAsync` (throws typed `AntiCaptchaException`) and `TrySolveAsync` (`SolveResult` with `IsSuccess`).
- Resilient polling: survives transient HTTP errors, per-type first-poll delay, retryable-vs-fatal error classification, backoff.
- Secrets (client key, proxy password) never exposed in raw payloads, logs or exception messages.
- netstandard2.0 + net8.0; System.Text.Json; minimal dependency floors.
- Optional DI package, `ILogger`/`ActivitySource`/`Meter` observability.

## Success metrics
- Zero known "feature advertised but broken" defects (e.g. Amazon WAF name lookup).
- Every task type covered by an offline serialization snapshot test.
- NuGet downloads and softId-attributed spend trend up after the 3.0 launch.
- Releases are intentional (changelog-driven), not dependency-bump churn.

## Roadmap (tracks)
1. **2.x critical hotfix** — fix verified bugs in the current codebase, release 2.2.0.
2. **Core 3.0** — task model, client, polling, errors, System.Text.Json, netstandard2.0.
3. **DI package + observability + release pipeline** (3.0).
4. (Selenium.AntiCaptcha repo, separate Conductor) browser engine, Selenium 3.0, `Playwright.Anticaptcha`.

Full design: `conductor/design-v3.md`.
