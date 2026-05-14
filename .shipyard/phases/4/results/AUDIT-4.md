# Phase 4 Security Audit

## Verdict: CLEAN

## Executive Summary

**Risk Level:** Low

Phase 4 (PostgreSQL outbox-producer implementation) is a structural mirror of Phase 3 (SQL Server), which audited CLEAN. New surface area is small (3 source files + 4 test files; 0 new NuGet packages). No exploitable vulnerabilities, no secrets, no SQL-injection seams, and no resource-lifecycle violations identified. The caller-supplied-transaction fork enforces fail-fast validation at the API boundary before any handler dispatch, and the handlers never commit, rollback, close, or dispose caller-owned resources — verified by source-text smoke tests.

## Scope

- **Diff range:** `baf8a40c..HEAD`
- **Commits audited:** 7 (`4d04fc9a` … `21993800`)
- **Source files audited (3):**
  - `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlExternalDbNameExtractor.cs` (new, 50 LOC)
  - `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalProducerQueue.cs` (new, 202 LOC)
  - `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandler.cs` (HandleExternalTx fork, +101 LOC)
  - `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs` (HandleExternalTxAsync fork, +100 LOC)
  - `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs` (DI wiring, +12 LOC)
- **Test files audited (4):** smoke + unit tests under `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/`
- **Files NOT changed (in-scope for context):** PROJECT.md, CONTEXT-4.md, ISSUES.md, SUMMARY-{1.1,2.1,2.2}.md reviewed

## STRIDE Threat Model (Phase 4 surface)

| Threat | Surface | Assessment |
|--------|---------|------------|
| Spoofing | None — transport-layer; auth is user's responsibility per CLAUDE.md security model | N/A |
| Tampering | Producer command payloads (message bodies/headers serialized as bytea) | Parameterized via `command.Parameters.Add(...)`; no string concat |
| Repudiation | N/A (caller owns transaction; audit trail is caller's concern) | N/A |
| Information Disclosure | `GuardNpgsqlTransaction` error message includes `transaction.GetType().FullName` | Type FullName is not a secret; matches Phase 3 pattern; CLEAN |
| Denial of Service | Sequential batch loop (per CONTEXT-4 Decision 4) — `_validator.Validate` runs ONCE before loop, not per-message | Performance bounded by caller-supplied batch size; no unbounded recursion |
| Elevation of Privilege | None — handlers operate on caller's connection, no privilege escalation vector | N/A |

## Findings

### Critical
None.

### Important
None.

### Informational

1. **GuardNpgsqlTransaction exception message exposes runtime type FullName** (`PostgreSqlRelationalProducerQueue.cs:197`)
   - Identical to Phase 3 finding (SQL Server). Type FullName is not a secret and exposing it is required for actionable error diagnosis. Same accepted risk applies. No remediation needed.

2. **CA2100 suppression on `Handle`/`HandleExternalTx`** (`SendMessageCommandHandler.cs:98,200`)
   - Inherited from existing PG codebase pattern; all dynamic SQL composition flows through `PostgreSqlCommandStringCache.GetCommand(CommandStringTypes.X)` (canonical string keys → cached SQL templates, not user input) and `SendMessage.BuildMetaCommand`/`BuildStatusCommand` which parameterize all user values. CA2100 suppression is justified; CWE-89 risk is zero.

3. **Raw `(NpgsqlTransaction)` cast in handler fork** (`SendMessageCommandHandler.cs:206`, `SendMessageCommandHandlerAsync.cs:207`)
   - Pre-validated by `GuardNpgsqlTransaction` at producer surface; comments in both files document this invariant. If the producer guard were ever removed/refactored, the fork would throw `InvalidCastException` (loud failure, not silent data corruption) — acceptable defense-in-depth posture.

## OWASP Top 10 Walk

| Risk | Status | Notes |
|------|--------|-------|
| A01:2021 Broken Access Control | N/A | Transport layer; caller owns auth (per CLAUDE.md security model) |
| A02:2021 Cryptographic Failures | N/A | No new crypto introduced; serialization unchanged from pre-Phase-4 |
| A03:2021 Injection | CLEAN | All SQL parameterized via `NpgsqlDbType.Bytea` + named params (`@body`, `@headers`); `PostgreSqlCommandStringCache` uses fixed-template enum keys |
| A04:2021 Insecure Design | CLEAN | Validator-then-cast-guard order matches Phase 3; "fail-fast at API boundary" pattern preserved |
| A05:2021 Security Misconfiguration | CLEAN | DI uses `RegisterConditional` (Rule A) — does not alter security defaults |
| A06:2021 Vulnerable Components | CLEAN | 0 new NuGet packages added in Phase 4 |
| A07:2021 Identification & Auth Failures | N/A | Transport layer |
| A08:2021 Software/Data Integrity Failures | CLEAN | Handler fork never commits/rolls back caller's tx; serializer unchanged |
| A09:2021 Security Logging & Monitoring Failures | N/A | No new logging surface |
| A10:2021 SSRF | N/A | No outbound HTTP |

## Secrets Scan

Regex `(password\|secret\|api.?key\|connectionstring\|token\|bearer\|pwd\s*=\|private.?key)` (case-insensitive) on all 9 Phase 4 changed files: **0 matches** in source, **0 matches** in tests. No credentials, connection strings, or tokens introduced.

## Dependency Audit

- New NuGet packages: **0**
- `Npgsql` and `NpgsqlTypes` are pre-existing references; no version changes in Phase 4 diff.

## IaC / Container

N/A — Phase 4 has no IaC, Docker, or pipeline changes.

## Cross-Task Coherence

1. **Validator-before-cast-guard ordering** is consistent across all 4 producer override hooks (sync/async × single/batch). Eliminates the cross-DB transaction leak class identified in CONTEXT-4 Decision 4. Validator's `IConnectionInformation.Container` vs `connection.Database` comparison uses ordinal compare — matches `PostgreSqlExternalDbNameExtractor`'s no-normalization contract (PG is case-sensitive).
2. **No TOCTOU between validation and use:** the validator inspects the same `DbTransaction` instance that is subsequently passed to `SendOne`/`SendOneAsync` and ultimately the handler fork — no re-fetch, no mutation seam between check and use.
3. **Lifecycle invariant:** `HandleExternalTx` / `HandleExternalTxAsync` never call `Commit()`, `Rollback()`, `Close()`, or `Dispose()` on the caller's connection or transaction. Enforced by structural smoke tests (`SendMessageCommandHandler{Async}ForkSmokeTests.cs`) that grep the compiled fork body for these tokens.
4. **DI Rule A (`RegisterConditional` over `Register`)** correctly preempts the open-generic fallback in `ComponentRegistration.RegisterFallbacks` for `IProducerQueue<>` / `IRelationalProducerQueue<>` / `RelationalProducerQueue<>`. Matches Phase 3 SQL Server wiring shape.
5. **Carry-forward issue ISSUE-032** (from Phase 3) does not regress in Phase 4. Phase 4 minor issues (ISSUE-033/034/035 per scope brief) are non-blocking and tracked in `.shipyard/ISSUES.md`.

## Analysis Coverage

| Area | Checked | Notes |
|------|---------|-------|
| Code Security (OWASP) | Yes | Manual walk of 3 source files + 4 test files; all OWASP Top 10 categories assessed |
| Secrets & Credentials | Yes | Regex scan, 0 hits |
| Dependencies | Yes | 0 new packages |
| IaC / Container | N/A | No IaC changes in Phase 4 |
| Configuration | N/A | No config files changed |

## Conclusion

Phase 4 introduces no new attack surface beyond what Phase 3 already audited. The implementation faithfully mirrors the Phase 3 SQL Server outbox pattern with PostgreSQL-specific adaptations (`NpgsqlConnection`/`NpgsqlTransaction`/`NpgsqlCommand` types, `NpgsqlDbType.Bytea` for binary params, case-sensitive db-name extraction). No critical or important findings. Phase 4 is cleared to proceed to simplifier → documenter.
