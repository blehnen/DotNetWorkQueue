# Security Audit: Phase 4

**Phase:** 4 — PostgreSQL Inbox Wiring + Unit Tests
**Date:** 2026-05-18
**Scope:** 5 file changes (3 production, 2 test). +371 lines. No new deps. No IaC. No config.
**Commit range:** `a3f9c14c..9f254fa3` (5 commits)

---

## Verdict: LOW_RISK (mirrors Phase 3 AUDIT-3 verdict)

No exploitable vulnerabilities. No secrets exposure. No dependency changes. The same two informational observations from Phase 3 carry over to this PG mirror — both are documented design decisions, not security defects.

---

## STRIDE Threat Model (PG-mirror pass)

| Threat | Surface | Assessment |
|---|---|---|
| Spoofing | `IWorkerNotification` resolution | No auth involved; container-internal. |
| Tampering | `DbTransaction` exposed to user handler | Accepted risk per PROJECT.md §Ownership & Threading Contract; documented in `IRelationalWorkerNotification` XML doc (Phase 2). Same posture as Phase 3 SqlServer. |
| Repudiation | No new logging added | Non-issue. |
| Information Disclosure | Bare `catch` in factory delegate; PG fake-credential test string | Catch: swallows exception type, falls back to safe non-relational path. Test string `Host=localhost;Username=postgres;Password=password;Database=test` is a fake credential matching the existing PG test-helper convention. |
| Denial of Service | No resource limits changed | N/A. |
| Elevation of Privilege | No new authz surface | N/A. |

---

## Findings

### Critical / High / Medium
- None.

### Low / Informational

- **L1 — Bare `catch` in factory delegate** (`PostgreSQLMessageQueueInit.cs`, factory-delegate block).
  Mirrors Phase 3 AUDIT-3 L1 finding verbatim. Consistent with `IBaseTransportOptions` precedent in the same file. Falls back to `EnableHoldTransactionUntilMessageCommitted = false` on any exception. Accepted per existing repo pattern.

- **L2 — `DbTransaction` ownership contract not compiler-enforced.**
  Mirrors Phase 3 AUDIT-3 L2 finding. User handlers could theoretically call `Commit()`/`Rollback()`/`Dispose()`/`Close()` on the exposed transaction. Documented in `IRelationalWorkerNotification.<remarks>` XML doc (Phase 2 deliverable). PROJECT.md §Ownership & Threading explicitly accepts this trade-off.

## Category review

### A. Code security
PG mirror of SqlServer; no new attack surface. Pattern-match in receive path is type-safe (no `InvalidCastException` risk on the option-false path). Factory delegate's try/catch swallows DB-load errors safely.

### B. Secrets
Fake connection string in `PostgreSqlRelationalWorkerNotificationRegistrationTests.cs` (`Host=localhost;Username=postgres;Password=password;Database=test`) matches the existing PG test-helper pattern and is not a real credential. Commit messages contain no secrets.

### C. Dependency vulnerabilities
No new `PackageReference` entries. NU1902 (OpenTelemetry.Api 1.15.2 advisory) carry-forward unchanged.

### D. IaC / configuration
N/A — no IaC, config, or shared-resource files touched.

### E. Cross-task coherence
DI registration override is option-driven and properly scoped. Receive-path setter only fires when the relational class is in scope. Tests use NSubstitute for interfaces only (no reflection-based escape hatches).

### F. Repo-specific posture
Transport security remains user's responsibility (CLAUDE.md / MEMORY.md). Phase 4 introduces no posture change beyond Phase 3's accepted design.

## Recommendation

**Ship.** LOW_RISK posture matches Phase 3. No remediation required. Both L1/L2 findings are documented accepted-risk items carried from Phase 2's interface design.
