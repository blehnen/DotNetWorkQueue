# Review: Plan 1.1

## Verdict: PASS

## Stage 1: Spec Compliance — PASS

### Task 1: Delete Phase 1 throwaway PoC — PASS
- Evidence: `Glob` for `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs` returns "No files found". Commit `49e587bf` listed in SUMMARY-1.1.md.
- Acceptance criterion `test ! -f ...` satisfied. Per SUMMARY verification table, `RetryCommandHandlerOutputDecoratorTests` baseline still 3/3 pass after deletion.

### Task 2: Add `ExternalTransaction` property to `SendMessageCommand` — PASS
- File: `Source/DotNetWorkQueue.Transport.Shared/Basic/Command/SendMessageCommand.cs`
- Evidence:
  - Line 19 adds `using System.Data.Common;` (alphabetical with existing `using DotNetWorkQueue.Validation;` on line 20). Single new using as required.
  - Lines 34–41: constructor `SendMessageCommand(IMessage messageToSend, IAdditionalMessageData messageData)` — signature and body unchanged.
  - Lines 48 / 55: existing `MessageToSend` and `MessageData` get-only properties unchanged.
  - Lines 57–70: new property `public DbTransaction ExternalTransaction { get; init; }` added after `MessageData`, with XML `<summary>` + `<remarks>` block matching the plan wording verbatim.
  - License header (lines 1–18) preserved verbatim.
- Plan's "exactly one new property + exactly one new using + no other lines change" criterion is satisfied.
- The 4 existing `new SendMessageCommand(messageToSend, data)` call sites in `Source/DotNetWorkQueue.Transport.Shared/Basic/SendMessages.cs` (lines 69, 89, 115, 135) are unmodified — they continue to compile because the new property is `init`-only, not a constructor parameter. Per SUMMARY, `DotNetWorkQueueNoTests.sln` Debug = 0 errors and `SendMessageCommandTests.Create_Default` still passes 1/1.
- Release-config build of `Transport.Shared` reported 0 errors per SUMMARY, confirming XML doc satisfies `TreatWarningsAsErrors`.

### Task 3: Create `IRetrySkippable` marker interface — PASS
- File: `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRetrySkippable.cs` (40 lines per SUMMARY; 40 lines confirmed by Read).
- Evidence:
  - LGPL header lines 1–18 match the repo's standard header verbatim.
  - Namespace `DotNetWorkQueue.Transport.RelationalDatabase` (root, not under `Basic/`). Matches plan and existing project layout.
  - Interface `IRetrySkippable` is `public` and contains exactly one member: `bool SkipRetry { get; }`.
  - XML `<summary>` + `<remarks>` on the interface; XML `<summary>` on the `SkipRetry` property. Per SUMMARY, Release build of `Transport.RelationalDatabase` = 0 errors (XML doc requirement satisfied).
  - Layering grep for `Microsoft.Data.SqlClient`/`using Npgsql` returns no matches per SUMMARY (file has no such references — only the namespace declaration and interface body).
- All four acceptance criteria for Task 3 satisfied.

## Stage 2: Code Quality — PASS

### Critical
- None.

### Important
- None.

### Suggestions
- `Source/DotNetWorkQueue.Transport.Shared/Basic/Command/SendMessageCommand.cs:70` — `ExternalTransaction` is a non-nullable reference type with no NRT annotation. The XML doc says "When null (the default)" but the property type `DbTransaction` is not declared as `DbTransaction?`. The Transport.Shared project does not enable nullable reference types in `Directory.Build.props`, so this is consistent with neighboring code and emits no warning — but Wave 2's `RelationalSendMessageCommand` derived class and the validator in `Transport.RelationalDatabase` will need to handle the "may be null" semantics explicitly. Remediation: Wave 2 plans should document the null-default contract (no code change here).
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRetrySkippable.cs:30` — the `<remarks>` says "Implemented by `RelationalSendMessageCommand`" but that class does not yet exist (Wave 2 deliverable). The cref is `<c>` (plain), not `<see cref>`, so no doc warning is emitted. Remediation: none required; the forward reference is acceptable in the marker interface for the phase, and Wave 2 will land the implementer.
- Builder summary's "Issues Encountered" notes pre-existing NU1902 warnings on `OpenTelemetry.Api 1.15.2` (GHSA-g94r-2vxg-569j). Tracked as an existing concern — not introduced by Plan 1.1. No action for this plan.

### Positive
- Three atomic commits, one per task, with `shipyard(phase-2):` prefix — clean history bisects cleanly if a regression appears later in Phase 2.
- The plan's "additive-only" contract is honored to the letter: no constructor signature change, no existing property mutation, no call-site edits. The `init`-only property choice preserves all 4 existing `new SendMessageCommand(...)` sites without touching them.
- LGPL header on `IRetrySkippable.cs` is byte-identical to the repo's other top-level interface files. Namespace placement (project root, not `Basic/`) matches the existing convention for top-level interfaces.
- Both new public types carry full XML doc on the type AND every member, satisfying `TreatWarningsAsErrors` on the Release config gate per SUMMARY.
- Wave 2 hand-off block in SUMMARY-1.1.md correctly identifies the three downstream consumers (derived command class, marker implementation, no new `<ProjectReference>` needed) — matches CONTEXT-2 Decision 2 Option B.

## Summary
Verdict: APPROVE. Three atomic commits cleanly implement the Wave 1 deliverables; spec compliance is exact (verbatim plan wording in the new XML docs and LGPL header), no regressions reported, and the additive `init`-only property design preserves all existing call sites unchanged. No blocking findings.
Critical: 0 | Important: 0 | Suggestions: 3
