# Documentation Report
**Phase:** 1 — Coverage Test Infrastructure (ActivityListener + cleanup)

## Summary
- API/Code docs: 1 file reviewed (`SharedSetup.cs`) — 0 XML doc additions needed
- Architecture updates: 0 (no `docs/` architecture sections exist for test infra)
- User-facing docs: 0 required
- CLAUDE.md lessons learned: 2 proposed additions (both Priority: Medium)

## 1. Public API Doc Updates (Priority: Low — non-blocking)

**File:** `Source/DotNetWorkQueue.IntegrationTests.Shared/SharedSetup.cs`
**Type:** Reference
**Status:** No action required.

### Findings
- `SharedSetup` is in the `DotNetWorkQueue.IntegrationTests.Shared` assembly — an **integration test support library**, not a shipped public API. The visibility change (`internal` -> `public`) is cosmetic from a consumer standpoint; this assembly is not NuGet-published and has no XML doc generation enabled.
- `ActivitySourceWrapper` was already `public`. The new members are:
  - `ConcurrentBag<Activity> CollectedActivities { get; }` — self-describing property name.
  - Private `_listener` field + constructor wiring — implementation detail, not part of the surface.
- Project-wide convention: test-support assemblies in this repo do not carry XML doc comments (spot-checked — no existing `///` blocks on `SharedSetup` members). Adding them here would break convention without value.

**Recommendation:** None. Leave as-is.

## 2. Architecture Documentation (Priority: Low)

**Scope checked:** `docs/` contains only `docs/jenkins-setup.md`. No architecture or testing-guide documents exist.

### Findings
- There is no existing "How to write integration tests" or "Tracing in tests" document to update.
- Creating a brand-new architecture doc for a single test-infrastructure pattern would violate the "update rather than duplicate" rule and create a parallel file with nothing to anchor to.

**Recommendation:** Defer. If/when a `docs/testing.md` or `docs/contributing.md` is created later in the coverage milestone, add a short "Verifying traces in tests" How-to section that references `ActivitySourceWrapper.CollectedActivities` and the `RunWithTraceVerification` pattern in `Memory/SimpleProducer.cs`. Not worth a standalone file today.

## 3. CLAUDE.md Lessons Learned (Priority: Medium — recommended)

Both discoveries match the style and value of existing entries in the "Lessons Learned" section. Proposed additions:

### Lesson A — ActivityListener required for trace-code coverage
> `TraceExtensions` and other `Activity`-producing code will report 0% coverage in tests unless an `ActivityListener` is registered with `ActivitySource.AddActivityListener` and returns `ActivitySamplingResult.AllDataAndRecorded`. Without a listener, .NET short-circuits `ActivitySource.StartActivity` and returns `null`, so any `using var activity = source.StartActivity(...)` block is skipped entirely. Integration tests that need to exercise tracing code should use `ActivitySourceWrapper` (in `DotNetWorkQueue.IntegrationTests.Shared.SharedSetup`), which installs a listener and exposes `CollectedActivities` for assertions.

**Why add:** Non-obvious runtime behavior; exactly the class of gotcha CLAUDE.md already documents (cf. the `RedisValue.Null` cast lesson).

### Lesson B — `Metrics.Metrics` namespace walk-up shadowing
> The nested type `DotNetWorkQueue.Metrics.Metrics` shadows the parent `DotNetWorkQueue.Metrics` namespace in any code under `DotNetWorkQueue.*`. C# resolves unqualified `Metrics` via namespace walk-up before `using` directives, so `new Metrics.NoOpMetrics()` can resolve to `Metrics.Metrics.NoOpMetrics` (wrong) instead of `DotNetWorkQueue.Metrics.NoOpMetrics`. Same failure mode as the existing `IConfiguration` lesson. Use `global::DotNetWorkQueue.Metrics.NoOpMetrics` or add an explicit `using` alias when referencing metrics types from inside the `DotNetWorkQueue.*` namespace hierarchy.

**Why add:** Direct analog to the already-documented `IConfiguration` shadowing lesson; future bug reports will benefit from cross-reference.

**Recommendation:** Add both entries to the "Lessons Learned" section of `CLAUDE.md`. I have not edited `CLAUDE.md` — that is a convention file that should be updated by the maintainer or explicitly requested.

## Gaps (informational)
- No contributor/testing guide exists at repo root or under `docs/`. A future milestone could add one; not blocking Phase 1.
- No XML docs on `IntegrationTests.Shared` public surface generally — consistent across the assembly, so not a Phase 1 regression.

## Verification
- Source inspection of `SharedSetup.cs` (lines 1–248) confirmed member visibility and absence of existing XML doc comments.
- `docs/` directory enumerated: only `jenkins-setup.md` present.
- No code examples included in this report (findings are meta-documentation), so no Bash verification step required.
