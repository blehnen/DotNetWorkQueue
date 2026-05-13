# Phase 1 Verification

**Phase:** Polly Decorator Bypass Spike
**Date:** 2026-05-13
**Type:** build-verify

## Overall Status: PASS

## Requirements Coverage

### ROADMAP Phase 1 Success Criteria

- [x] **Documented resolution strategy for bare-handler access on both transports** — PASS. `.shipyard/notes/phase-1-polly-bypass-spike.md` "Chosen Mechanism" section (lines 39-87) specifies `IRetrySkippable` marker interface evaluated at the top of `RetryCommandHandlerOutputDecorator.Handle()`. Files-to-Touch section (lines 98-111) names both SqlServer (`Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecorator.cs` + `*Async.cs`) and PostgreSQL (`Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecorator.cs` + `*Async.cs`) decorator files for the production rollout. Mechanism category: "marker interface check inside existing decorator" (a fourth category beyond the three listed in ROADMAP — keyed registration / producer-side new / child scope). Memo explicitly closes per-transport divergence open question with "None found" at line 37.

- [x] **Enumerated decorator list per transport with keep/skip decisions** — PASS. Memo "Decorator Inventory" section (lines 5-33) tabulates 6 decorator slots: SqlServer sync (Retry line 154 + Trace line 182), SqlServer async (Retry line 160 + Trace line 186), PostgreSQL sync (Retry line 179 + Trace line 208), PostgreSQL async (Retry line 185 + Trace line 212). Implicit keep/skip decision: trace stays (line 96 "trace decorator sits outside the retry decorator, so the marker check fires after trace span creation — observability is preserved"); retry is bypassed by marker. All four handler classes (sync + async × 2 transports) addressed.

- [x] **PoC passes locally (in-memory SimpleInjector resolution test)** — PASS. `dotnet test ... --filter "FullyQualifiedName~_SpikePollyBypassPoC"` → Failed: 0, Passed: 2, Skipped: 0, Total: 2, Duration: 148 ms.

- [x] **Risk #1 closed or downgraded with concrete remediation path** — PASS. `.shipyard/PROJECT.md:136` reads `1. **Polly decorator bypass cleanness** (low — closed by Phase 1 spike) — Mechanism confirmed: ... See .shipyard/notes/phase-1-polly-bypass-spike.md.` Only occurrence of "Polly decorator bypass" in the file. Remediation path is the six-file enumeration in the memo's Files-to-Touch section.

### Plan Verification Commands (PLAN-1.1)

1. **Memo + section count.** `test -f .shipyard/notes/phase-1-polly-bypass-spike.md && grep -c "^##" ...` → file exists, **9 sections** (≥6 required). PASS.

2. **PoC test execution.** `dotnet test ... --filter "FullyQualifiedName~_SpikePollyBypassPoC" -c Debug` → **2 passed, 0 failed**, 148 ms, net10.0. PASS.

3. **No production-code change.** `git diff --name-only shipyard/pre-build-phase-1 -- ':!.shipyard/' ':!Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs'` → **empty output**. Diff-stat against `shipyard/pre-build-phase-1` confirms exactly 3 files changed: `.shipyard/PROJECT.md` (+1/-1), `.shipyard/notes/phase-1-polly-bypass-spike.md` (+119), `Source/.../_SpikePollyBypassPoC.cs` (+191). No production source touched. PASS.

4. **PROJECT.md Risk #1 downgrade.** `grep -A1 "Polly decorator bypass cleanness" .shipyard/PROJECT.md | head -2` → first line contains `(low — closed by Phase 1 spike)`. PASS.

### Additional Checks

5. **Broader SqlServer.Tests suite (regression).** `dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/..." -c Debug` → **141 passed, 0 failed, 0 skipped**, 16 s, net10.0. No regressions detected; pre-existing tests still pass alongside the 2 new PoC tests. PASS.

6. **SqlServer.Tests build cleanliness.** `dotnet build "Source/DotNetWorkQueue.Transport.SqlServer.Tests/..." -c Debug` → Build succeeded. **0 errors, 10 warnings**. All 10 warnings are pre-existing `NU1902` (OpenTelemetry.Api 1.15.2 transitive CVE — 5 projects × 2 TFMs); zero non-NU1902 warnings; filtered grep for non-NU1902 warnings returns empty. No new warnings introduced by Phase 1. PASS.

## Reviewer Findings Cross-Check

REVIEW-1.1 flagged three Minor findings, all non-blocking:
- Async path not exercised in PoC (in-spec; Phase 2 plan must cover async unit tests on production change).
- Negative-case test name slightly oversells (test name `RetryPath_Still_Used` only asserts the `Registry` getter was accessed, not that pipeline execution completed). Acceptable for throwaway.
- `_SpikeIRetrySkippable` declared `internal` but effective accessibility is `private` (nested in public class). Cosmetic; throwaway.

No critical findings. Reviewer verdict: PASS. Verifier concurs.

## Gaps

None. All four plan verifications, all four ROADMAP success criteria, and both additional regression checks pass.

## Infrastructure Validation

N/A — Phase 1 introduced no IaC files. No Terraform / Ansible / Docker changes.

## Recommendations for Phase 2

- **Async coverage in Phase 2 plan.** REVIEW-1.1 flagged that the PoC mirrors only the sync decorator. Phase 2's plan that ships the production `IRetrySkippable` branch must require **both** sync and async unit tests on `RetryCommandHandlerOutputDecorator` and `RetryCommandHandlerOutputDecoratorAsync`, on **both** SqlServer and PostgreSQL — four decorator classes total. Sync-only validation in Phase 1 is not sufficient evidence the async branch behaves correctly.
- **First task of Phase 2 deletes `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs`.** Plan, memo (line 119), and SUMMARY all flag this; verifier confirms the file is the only test artifact slated for removal.
- **`IRetrySkippable` placement is `Transport.Shared`.** Memo locks this at line 111: `RetryCommandHandlerOutputDecorator.cs` already imports `DotNetWorkQueue.Transport.Shared`, and `SendMessageCommand` lives in `Transport.Shared.Basic.Command`. Phase 2 should not relocate the interface to `Transport.RelationalDatabase` without revisiting the reference-graph reasoning.
- **NSubstitute can't proxy `ResiliencePipelineRegistry<string>`.** Builder pivoted (SUMMARY decisions §2) to property-getter call assertions on `IPolicies` itself. Phase 2 production tests should adopt the same pattern; do not attempt `policies.Registry.DidNotReceive().TryGetPipeline(...)`.
- **Pre-existing CVE NU1902 (OpenTelemetry.Api 1.15.2).** Not introduced by Phase 1, but surfaced in every Phase 1 build run. Out-of-scope here; flag for the next dependency-refresh milestone.

## Verdict

**PASS** — All four plan verifications green, all four ROADMAP success criteria met with concrete evidence, no regressions in the 141-test SqlServer.Tests suite, build clean with zero new warnings, production code untouched. Risk #1 successfully closed/downgraded with a documented six-file remediation path for Phase 2.
