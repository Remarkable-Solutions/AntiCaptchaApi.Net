# Project Workflow — AntiCaptchaApi.Net

Customized from the Conductor default template during setup (2026-09-30).

| Setting | Value |
|---|---|
| Coverage target | ≥ 80% for new/changed code |
| Commit strategy | Per task |
| Git notes | **Disabled** — task summaries go in the commit body and `plan.md` |
| Mobile testing | Skip (library) |
| Parallel agents | worktree |

## Guiding Principles
1. **The Plan is the Source of Truth:** all work is tracked in the track's `plan.md`.
2. **The Tech Stack is Deliberate:** changes to `tech-stack.md` are documented *before* implementation.
3. **Test-Driven Development:** failing test first, then implementation.
4. **Offline tests only in task loops:** never run `IntegrationTests/` (paid live API) automatically.
5. **Never push, tag or publish** without explicit user approval. `main` pushes publish CI packages; `v*` tags publish to nuget.org.

## Commands
```bash
# build (0 new warnings allowed vs. baseline)
dotnet build AntiCaptchaApi.Net.sln
# offline tests (local machine has only the .NET 10 runtime)
DOTNET_ROLL_FORWARD=Major dotnet test AntiCaptchaApi.Net.sln --filter "FullyQualifiedName~UnitTests"
# coverage
DOTNET_ROLL_FORWARD=Major dotnet test AntiCaptchaApi.Net.sln --filter "FullyQualifiedName~UnitTests" --collect:"XPlat Code Coverage"
```
Baseline at setup: 27 unit tests passing, 15 build warnings.

## Task Workflow
1. **Select Task:** next `[ ]` task in `plan.md`, in order.
2. **Mark In Progress:** change `[ ]` → `[~]`.
3. **Red:** write tests that define the expected behavior; run them and confirm they fail for the right reason. (For pure refactors/infrastructure tasks with no behavior change, record "no red phase — refactor" in the plan.)
4. **Green:** minimum code to pass; run the full offline suite.
5. **Refactor:** clean up with tests green.
6. **Verify Coverage:** ≥ 80% on new code.
7. **Document Deviations:** if implementation deviates from `tech-stack.md` or the spec, stop, update the doc with a dated note, then continue.
8. **Commit:** conventional message `<type>(<scope>): <description>`, body = short task summary + files changed + why. Trailer: `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
9. **Record SHA:** mark the task `[x]` in `plan.md` with the 7-char SHA.
10. **Commit Plan Update:** separate commit `conductor(plan): Mark task '<name>' as complete`.

## Phase Completion Verification and Checkpointing
1. Announce phase completion.
2. `git diff --name-only <previous_checkpoint> HEAD`; ensure every changed code file has corresponding tests.
3. Announce and run the offline test command; on failure, at most two fix attempts, then stop and ask.
4. Present a manual verification plan (e.g. a small console probe against the built library; optional live check with the user's API key, run by the user).
5. **Wait for explicit user confirmation.**
6. Checkpoint commit `conductor(checkpoint): Checkpoint end of Phase X` (empty commit allowed).
7. Append `[checkpoint: <sha>]` to the phase heading in `plan.md`; commit `conductor(plan): Mark phase '<name>' as complete`.

## Quality Gates (every task)
- [ ] All offline tests pass
- [ ] Coverage ≥ 80% on new code
- [ ] No new build warnings
- [ ] Follows `code_styleguides/csharp.md`
- [ ] All new public members have XML docs
- [ ] No secrets in logs/messages/raw payloads
- [ ] README/CHANGELOG updated when user-visible behavior changes

## Commit Guidelines
Types: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`, `conductor`.
Examples:
```
fix(amazon-waf): register AmazonTask/AmazonTaskProxyless task names
test(serialization): snapshot every request type
```

## Definition of Done
Implemented to spec, tests written and passing offline, coverage met, docs updated, committed with a proper message, SHA recorded in `plan.md`.
