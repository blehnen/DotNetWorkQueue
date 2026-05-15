# Phase 2 Security Audit

## Verdict: CLEAN

## Scope
- Diff range: `99003720..HEAD` (HEAD = `86a16287`)
- 15 Phase-2 `.cs` files audited (8 production, 4 test, 2 modified, 1 deleted PoC):
  - `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRetrySkippable.cs` (new)
  - `Source/DotNetWorkQueue.Transport.RelationalDatabase/IExternalDbNameExtractor.cs` (new)
  - `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRelationalProducerQueue.cs` (new)
  - `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/ExternalTransactionValidator.cs` (new)
  - `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalProducerQueue.cs` (new)
  - `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/Command/RelationalSendMessageCommand.cs` (new)
  - `Source/DotNetWorkQueue.Transport.Shared/Basic/Command/SendMessageCommand.cs` (+16 lines, additive `ExternalTransaction { get; init; }`)
  - `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecorator.cs` (+4 lines, bypass branch)
  - `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs` (+5 lines, bypass branch)
  - `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecorator.cs` (+5 lines, bypass branch)
  - `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs` (+5 lines, bypass branch)
  - `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/ExternalTransactionValidatorTests.cs` (new, 5 cases)
  - `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/RetryCommandHandlerOutputDecoratorBypassTests.cs` (new, 2 cases)
  - `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Decorator/RetryCommandHandlerOutputDecoratorBypassTests.cs` (new, 2 cases)
  - `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs` (deleted per Exit Criterion 8)

## STRIDE pre-scan
- **Spoofing / Elevation:** N/A — no auth surface in Phase 2.
- **Tampering:** Caller-supplied `DbTransaction` is the new trust boundary. Validator runs 4 ordered checks before any enlistment occurs. No SQL is constructed in Phase 2 (the SQL-emitting handlers land Phase 3/4).
- **Repudiation:** N/A — no new logging gaps; existing producer logger pipeline unchanged.
- **Info Disclosure:** Validator exception messages include the actual + expected DB names. Both are user-controlled configuration (queue connection `Container` and `DbConnection.Database`), not secrets. Per repo security model (`project_security_model`), transport security is the user's responsibility; surfacing the configured DB names in an `InvalidOperationException` is a desired diagnostic, not a leak.
- **DoS:** Validator is O(1); no unbounded allocation or loop.

## Findings

### Critical
None.

### Important
None.

### Informational

- **INFO-1 — `ExternalTransaction { get; init; }` is `init`-settable on the base `SendMessageCommand`.** `SendMessageCommand.cs:70` exposes the transaction as `init`-only, so non-derived call sites can construct a base `SendMessageCommand` with a transaction attached, but base `SendMessageCommand` does NOT implement `IRetrySkippable` — so such an instance would silently flow through the retry decorator with the caller's `DbTransaction` attached AND go through the Polly pipeline. This is a latent footgun for Phase 3/4 callers who forget to use `RelationalSendMessageCommand`. Phase 3 producer wiring already constructs `RelationalSendMessageCommand` (per CONTEXT-3), so no live exposure today. Recommendation for Phase 3 verification (not a Phase 2 blocker): add a single guard test that verifies the SqlServer producer's tx-aware path constructs `RelationalSendMessageCommand` specifically, not the base. CWE-841 (Improper Enforcement of Behavioral Workflow) — informational only.

- **INFO-2 — Validator database-name comparison uses `StringComparison.Ordinal` directly.** `ExternalTransactionValidator.cs:90` performs `StringComparison.Ordinal` (case-sensitive byte equality). XML doc states "per-provider case semantics are encoded by the extractor's normalization." Phase 2 ships no extractor implementations, so the case-sensitivity contract is unverifiable until Phase 3 (SqlServer extractor should lowercase or normalize via `OrdinalIgnoreCase`; PostgreSQL extractor preserves case). This is correctly documented as a Phase 3/4 obligation in `IExternalDbNameExtractor.cs:27-30`. Mention here only to flag the Phase 3 auditor: verify the SqlServer extractor's case normalization aligns with SQL Server's case-insensitive default before relying on `Ordinal` equality.

- **INFO-3 — `IRetrySkippable.SkipRetry` is queried via `command is IRetrySkippable` pattern.** `RetryCommandHandlerOutputDecorator.cs:54` (both transports, both sync/async) cleanly short-circuits before touching `_policies.Registry`. No TOCTOU: `SkipRetry` is a computed property derived from `ExternalTransaction != null` (an `init`-only field set at construction). Once a `RelationalSendMessageCommand` is built, `SkipRetry` is referentially stable. No smuggling path observed — the decorator pattern is correct.

- **INFO-4 — Decorator bypass branch has no `Guard.NotNull` on `command` before the `is` cast.** Actually it does: `Guard.NotNull(() => command, command);` precedes the `is`-cast on every branch (`RetryCommandHandlerOutputDecorator.cs:52-54` sync; `:53-55` async; mirrored on PostgreSQL). No null-deref risk.

## Dependency Audit

No NuGet package or csproj changes in Phase 2 (`git diff 99003720..HEAD -- '*.csproj' '*.props'` empty). The pre-existing `NU1902` advisory on `Microsoft.Data.Sqlite` is already tracked as ISSUE-032 (out of Phase 2 scope). OpenTelemetry-related tracked items (ISSUE-024, ISSUE-031) are likewise unrelated to Phase 2's additive plumbing.

No new transitive surface introduced; Phase 2 uses only types from `System.Data` / `System.Data.Common` (`DbTransaction`, `DbConnection`, `ConnectionState`) and existing project references.

## Secrets Scan

`grep -rEin "password|secret|api[._-]?key|connectionstring|token|bearer|pwd\s*=|private[._-]?key"` across all 15 Phase 2 files → 0 hits. No secrets, no test fixtures embedding credentials.

## Cross-task Coherence

- **Validator check ordering prevents partial state corruption.** The 4 checks in `ExternalTransactionValidator.Validate` (`null tx → null Connection → State != Open → DB name mismatch`) are ordered such that each subsequent check assumes the previous condition holds. No check has side effects. Failure on any check throws before the producer enlists commands; no partial state.

- **Marker-bypass branch does not open a TOCTOU.** `SkipRetry` is a computed-from-field property on an immutable construction (`init`-only). The decorator reads it once at the top of `Handle` / `HandleAsync`. Between the read and the inner handler invocation no concurrent mutation is possible — the command is a value-style DTO.

- **No smuggling path through base `SendMessageCommand`.** Adding `ExternalTransaction { get; init; }` to the base class without making the base implement `IRetrySkippable` is the correct layering choice (Phase 2 Decision 2, option B). A maliciously or accidentally constructed base `SendMessageCommand` with an `ExternalTransaction` set would still flow through Polly — degrading to slower behavior but NOT silently bypassing retry. The Polly path remains the safe default. Documented as INFO-1.

- **Layering invariant holds.** `Transport.RelationalDatabase` references only `Transport.Shared` + framework. No `Microsoft.Data.SqlClient` / `Npgsql` references introduced — confirmed by file inspection.

- **Spike PoC deletion (commit `49e587bf`) closes the throwaway-code invariant** per Exit Criterion 8. No `_SpikePollyBypassPoC.cs` remnant in the tree.

## Threat-model summary

The new trust boundary is the caller-supplied `DbTransaction`. The validator is the chokepoint and its 4 checks correctly cover: liveness (null/disposed), readiness (state), and locality (DB-name match). No exploitable surface in Phase 2 — the SQL-emitting handlers that would actually use the validated transaction are out of phase scope (Phase 3 SqlServer / Phase 4 PostgreSQL). Phase 3 auditor should focus on: (1) extractor case-normalization correctness, (2) the producer constructing `RelationalSendMessageCommand` (not base), (3) the `SendMessageCommandHandler` correctly skipping its internal `BeginTransaction`/`Commit`/`Dispose` when `ExternalTransaction != null`.
