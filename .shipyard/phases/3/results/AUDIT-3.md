# Phase 3 Security Audit (SqlServer Implementation)

## Verdict: CLEAN

## Scope
- Files audited: 7 (4 new, 3 modified) — 2 transport sources (extractor, producer), 2 handler edits (sync/async fork), 1 DI registration delta, 2 test files (producer-subclass, fork smoke)
- Diff range: `4829176f..HEAD` (7 commits, +973 LOC, 0 deletions)
- STRIDE focus: Tampering (SQL injection in new fork), Elevation of Privilege (transaction crossing DB boundary), Information Disclosure (error messages with FQNs/DB names)

## Findings

### Critical
None.

### Important
None.

### Informational

**[A1] Error message exposes type FullName**
- Location: `SqlServerRelationalProducerQueue.cs:197` (`GuardSqlTransaction`)
- Detail: `InvalidOperationException` message includes `transaction.GetType().FullName`. In a production stack trace this is already visible — no marginal disclosure. Per repo security model ("transport security is user's responsibility"), in-bounds.
- Action: None.

**[A2] Database name surfaced in validator error messages (pre-existing, not Phase 3)**
- Location: `ExternalTransactionValidator` (Phase 2)
- Detail: Validator's `InvalidOperationException` carries both actual + expected DB names. DB names are non-secret config; necessary for diagnosis.
- Action: None.

**[A3] Test-fixture connection-string literals (informational, accepted-risk)**
- Detail: No connection-string literals in Phase 3 test files. All credentials mocked via NSubstitute.
- Action: None.

## OWASP Top 10 Walk

| Risk | Status | Notes |
|------|--------|-------|
| A01 Broken Access Control | N/A | Library code; no authz surface. |
| A02 Cryptographic Failures | N/A | No crypto in Phase 3. |
| A03 Injection | **CLEAN** | `HandleExternalTx[Async]` use exclusively parameterized SQL: `command.Parameters.Add("@body", SqlDbType.VarBinary, -1)` + `Parameters["@body"].Value = ...`. `CommandText` comes from `_commandCache.GetCommand(CommandStringTypes.InsertMessageBody)` — static lookup, not user input. Inherited `BuildMetaCommand`/`BuildStatusCommand` confirmed parameterized in Phase 2 RESEARCH §2. No string interpolation into SQL anywhere in the fork. |
| A04 Insecure Design | CLEAN | Validator-before-cast-guard ordering enforced and asserted (`Send_ValidatorRejectsDbMismatch_ThrowsBeforeCastGuard`). Lifecycle ownership contract enforced by source-text smoke test. |
| A05 Security Misconfig | CLEAN | DI uses `RegisterConditional`, singletons only — no captive dependency or accidental scope escalation. |
| A06 Vulnerable Components | CLEAN | Phase 3 added **zero** NuGet packages. ISSUE-032 (pre-existing NU1902) carried forward. |
| A07 Auth Failures | N/A | No auth surface. |
| A08 Software/Data Integrity | CLEAN | Cast guard (`is not SqlTransaction`) blocks cross-provider transaction misuse before any data write. |
| A09 Logging Failures | CLEAN | No new logging surface. |
| A10 SSRF | N/A | No outbound HTTP. |

## Dependency Audit
- 0 new NuGet packages added in Phase 3.
- ISSUE-032 (NU1902 pre-existing transitive) carried forward, not introduced.

## Cross-Task Coherence

1. **Validator-first ordering across all 4 producer entry points** — `SendWithExternalTransaction[Async]` and batch variants call `_validator.Validate(transaction)` BEFORE `GuardSqlTransaction(transaction)`. Order matters for diagnostics. Confirmed by tests.
2. **Batch validates once, not per item** — matches CONTEXT-3 Decision 3.
3. **Handler-side blind cast is safe by precondition** — `HandleExternalTx` casts `(SqlTransaction)commandSend.ExternalTransaction` without re-check. Sound because the producer subclass is the only public path that sets non-null `ExternalTransaction` on `RelationalSendMessageCommand`. Worst case (internal API misuse): `InvalidCastException` at the cast site, no SQL emitted, no integrity violation.
4. **Lifecycle ownership contract verified by source-text grep** — sync + async smoke tests assert no `.Commit/.Rollback/.Close/.Dispose` in the fork bodies.
5. **DI registration ordering** — `RegisterConditional` preempts the conditional fallback in `ComponentRegistration.RegisterFallbacks` without disturbing other transports.

## Coverage Table

| Area | Checked | Notes |
|------|---------|-------|
| Code Security (OWASP) | Yes | All 4 new public methods + 2 fork branches; parameterized SQL throughout. |
| Secrets & Credentials | Yes | Grep on 7 files: no `password`/`secret`/`api_key`/`token`/`bearer`/`pwd=`/`private_key`. Test fixtures use NSubstitute. |
| Dependencies | Yes | Zero new packages. |
| IaC / Container | N/A | No IaC files. |
| Configuration | Yes | DI deltas reviewed; singletons + RegisterConditional only. |

## Summary

Phase 3 ships pure additive SqlServer code with disciplined defense-in-depth: validator-before-cast ordering, parameterized SQL via inherited Phase 2 builders, explicit cast guards, and grep-enforced lifecycle ownership contracts. The handler-side blind cast is contractually safe because the producer surface is the only public entry to the fork. **PASS — no blockers for `/shipyard:ship`.**
