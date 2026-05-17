# Review: Plan 2.2 (Phase 4 Wave 2 — PG Async Fork)

## Verdict: PASS

## Stage 1 — Spec Compliance

### Task 1: HandleExternalTxAsync fork + early-branch dispatch
- Status: PASS
- Evidence:
  - `SendMessageCommandHandlerAsync.cs:108-109` — early-branch `if (commandSend.ExternalTransaction != null) return await HandleExternalTxAsync(commandSend).ConfigureAwait(false);` placed immediately after the lazy-init block (lines 103-106), before `var jobName = ...` continuation. Matches plan insertion point exactly.
  - `SendMessageCommandHandlerAsync.cs:204` — `private async Task<long> HandleExternalTxAsync(SendMessageCommand commandSend)` declared with correct signature and async modifier.
  - `SendMessageCommandHandlerAsync.cs:207-208` — casts `(NpgsqlTransaction)commandSend.ExternalTransaction` then `(NpgsqlConnection)npgsqlTx.Connection`.
  - `SendMessageCommandHandlerAsync.cs:223` — uses `DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>` (correct generic args).
  - `SendMessageCommandHandlerAsync.cs:239,245` — `NpgsqlDbType.Bytea` used for both `@body` and `@headers`. Zero `SqlDbType` occurrences in fork.
  - `SendMessageCommandHandlerAsync.cs:249` — `await command.ExecuteScalarAsync().ConfigureAwait(false)` (async ADO API; matches plan and existing self-managed-tx path style).
  - `SendMessageCommandHandlerAsync.cs:267-269` — `await CreateMetaDataRecordAsync(..., _getTime.GetCurrentUtcDate()).ConfigureAwait(false)` — 8th arg materialized synchronously (IGetTime is sync; no rogue `await` on it).
  - `SendMessageCommandHandlerAsync.cs:273-274` — `await CreateStatusRecordAsync(...).ConfigureAwait(false)` invoked only when `EnableStatusTable` is set.
  - `SendMessageCommandHandlerAsync.cs:279` — `_sendJobStatus.Handle(new SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>(...))` invoked sync (matches existing self-managed path line 170; no async overload available per RESEARCH §3).
  - `SendMessageCommandHandlerAsync.cs:283` — exact CONTEXT-4 Rule B wording present.
  - Self-managed-tx async path (lines 111-188) preserved unchanged.

### Task 2: 3 structural smoke tests
- Status: PASS
- Evidence:
  - `SendMessageCommandHandlerAsyncForkSmokeTests.cs:36-49` — reflection test asserts `Task<long>` return type.
  - `SendMessageCommandHandlerAsyncForkSmokeTests.cs:51-72` — source-grep test confirms early-branch, dispatch, declaration, and `.ConfigureAwait(false)` substrings.
  - `SendMessageCommandHandlerAsyncForkSmokeTests.cs:74-104` — 8-pattern fork-body grep covers both sync (`.Commit()`, `.Rollback()`, `.Close()`, `.Dispose()`) and async (`.CommitAsync`, `.RollbackAsync`, `.CloseAsync`, `.DisposeAsync`) lifecycle calls.
  - LGPL header present (lines 1-18).
  - SUMMARY confirms 3 pass, 0 fail; full PG.Tests suite 143/0.

## Stage 2 — Code Quality

### Critical
None.

### Important
None.

### Minor
- `SendMessageCommandHandlerAsync.cs:204-285` — fork is ~80 lines of near-mechanical duplication against the self-managed-tx async path (lines 111-188). Same observation made in REVIEW-2.1 for the sync fork; the duplication is intentional per CONTEXT-4 Decision 3 (early-branch inside existing handler, no helper extraction) and Phase 6 integration tests will pin runtime behavior, so this is a deliberate trade-off. Document only.

### Positive
- Three PG-specific compile-gate hazards (sync `_getTime.GetCurrentUtcDate()` materialization, `NpgsqlDbType.Bytea`, typed query generics) all landed correctly on first commit per SUMMARY. Builder applied PG-vs-SqlServer deviations from RESEARCH §11 without rediscovery.
- Sync sub-handler invocation pattern (`_jobExistsHandler.Handle`, `_sendJobStatus.Handle`) matches the existing self-managed-tx async path (lines 125, 170), so no async-overload landmines.
- `.ConfigureAwait(false)` discipline maintained on every `await` in the fork (lines 109, 249, 267, 273).
- Phase 3 PLAN-2.2 lesson (source-side comment rephrase) applied from the start — no comment-stripping preprocessing needed in the smoke test.
- 14 NU1902 OpenTelemetry advisory warnings correctly identified as pre-existing (ISSUE-032), not introduced by this plan.

## CONTEXT-4 Rule B audit
- Lifecycle-invariant comment at `SendMessageCommandHandlerAsync.cs:283` uses EXACT mandated wording: `// Caller owns lifecycle: no Commit, Rollback, Close, or Dispose performed here.`
- No method-call substrings (`.Commit()`, `.Rollback()`, `.Close()`, `.Dispose()`) appear anywhere in the fork body (lines 204-285).
- No async variants (`.CommitAsync`, `.RollbackAsync`, `.CloseAsync`, `.DisposeAsync`) appear in the fork body.
- Smoke test grep gate (`HandleExternalTxAsync_DoesNotCommitOrRollbackOrCloseOrDispose`) plain-text-passes without preprocessing — confirms source-side rephrase strategy is sound.

## PG-specific deviation check
- `_getTime.GetCurrentUtcDate()` correctly materialized: **yes** (line 269; passed as 8th arg to `CreateMetaDataRecordAsync`; invoked sync, no rogue `await`)
- `NpgsqlDbType.Bytea` used: **yes** (lines 239, 245; 2 occurrences in fork body; zero `SqlDbType` references)
- `_jobExistsHandler`/`_sendJobStatus` invoked sync: **yes** (lines 223, 279; matches existing self-managed-tx async path pattern at lines 125, 170)
- `ExecuteScalarAsync`/`ExecuteNonQueryAsync` used (async pattern): **yes** — `ExecuteScalarAsync` at line 249 for the body insert; `CreateMetaDataRecordAsync`/`CreateStatusRecordAsync` helpers internally use `ExecuteNonQueryAsync` (lines 303, 329, unchanged from baseline)

## Cross-plan consistency with PLAN-2.1
- Both forks use the same Rule B wording: **yes** — sync fork at `SendMessageCommandHandler.cs` (per SUMMARY-2.1 verification table) and async fork at `SendMessageCommandHandlerAsync.cs:283` both use the EXACT string `// Caller owns lifecycle: no Commit, Rollback, Close, or Dispose performed here.`
- Both forks use the same PG-specific deviations: **yes** — both materialize `_getTime.GetCurrentUtcDate()` as 8th arg, both use `NpgsqlDbType.Bytea` (×2), both use `DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>` + `SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>` generic args.
- Files are disjoint (sync: `SendMessageCommandHandler.cs` + sync smoke tests; async: `SendMessageCommandHandlerAsync.cs` + async smoke tests). No parallel-edit conflict.
- Test count math checks out: 130 baseline + 7 Wave 1 + 3 sync + 3 async = 143, matches SUMMARY-2.2.
