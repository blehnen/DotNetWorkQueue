# Security Audit Report — Milestone: Outbox Pattern

**Date:** 2026-05-15
**Branch:** `feature/outbox-pattern` (HEAD: `24a1e3d7`)
**Commit range:** `origin/master...HEAD` — 77 commits across 7 phases
**Prior phase audits relied on:** Phase 6 AUDIT.md (CLEAN), Phase 7 AUDIT.md (CLEAN)
**New production source files in scope (Phases 1-5, no prior AUDIT.md):** 19 files

---

## Executive Summary

**Verdict: CLEAN**
**Risk Level: Low**

No exploitable vulnerability identified. Single standing risk is a pre-existing OpenTelemetry advisory (`GHSA-g94r-2vxg-569j`) explicitly accepted and documented via ISSUE-032 closure (Phase 7 / commit `88ff8996`). Cross-phase coherence is sound: retry-bypass path correctly gated, validator runs at every API boundary before any handler dispatch, no transaction-lifecycle ownership leakage. Two advisory items noted (no action required for ship gate).

---

## §1. OWASP Top 10 Mapping

### A01 — Broken Access Control: NO FINDINGS

The outbox surface adds no new authorization boundary. `IRelationalProducerQueue<T>` is resolved from the same DI container as the existing `IProducerQueue<T>`. The caller must already hold a valid open `DbTransaction`. `ExternalTransactionValidator.Validate()` enforces 4 checks at the API surface before any handler dispatch:
1. Null check on transaction
2. Null check on `transaction.Connection` (catches disposed/completed)
3. `ConnectionState.Open` check
4. Database name equality (`StringComparer.Ordinal`) between `connection.Database` and `IConnectionInformation.Container`

Plus per-transport type guards (`GuardSqlTransaction` / `GuardNpgsqlTransaction`) prevent cross-transport transaction misuse.

### A02 — Cryptographic Failures: NO FINDINGS

No connection strings in production code. Integration tests resolve from `connectionstring.txt` (gitignored). Tutorial uses `Password=...` (literal placeholder) per Phase 7 AUDIT.md.

### A03 — Injection (SQL): NO FINDINGS

Both `HandleExternalTransaction` + `HandleExternalTransactionAsync` handler forks (SqlServer + PostgreSQL) use `IDbCommand.CreateParameter()` with typed `SqlDbType`/`NpgsqlDbType` enum parameters. Command text from pre-built `_commandCache.GetCommand(CommandStringTypes.InsertMessageBody)` — the same cache used by the pre-existing standard path (production-proven). `[SuppressMessage("Microsoft.Security", "CA2100")]` attributes match the existing pattern. No string interpolation of untrusted values into SQL text anywhere in the new code.

### A05 — Security Misconfiguration: NO FINDINGS

`<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` (Phase 7 / commit `88ff8996`) demotes one specific advisory from error to warning. Advisory still surfaces as warning on every Release build — not suppressed. `<DocumentationFile>` addition on Transport.RelationalDatabase Release|net8.0 block tightens the build, not loosens it.

### A06 — Vulnerable Components: NO NEW INTRODUCTIONS

`Source/Directory.Packages.props` zero diff. No new `<PackageReference>` lines in any csproj across 77 commits. The 20 NU1902 carry-forward advisories predate Phase 1.

### A09 — Security Logging Failures: NO FINDINGS

`ExternalTransactionValidator` exception messages include DB names (diagnostic intent; no credentials/tokens/connection strings). No new logging statements in handler forks.

---

## §2. Secrets Scan

All 54 non-shipyard changed files checked for hardcoded credentials, API keys, tokens, passwords, private keys, Base64-encoded secrets, `.env` files. **No secrets found.** Integration tests resolve `ConnectionInfo.ConnectionString` at runtime from gitignored `connectionstring.txt`.

---

## §3. Dependency Surface

**No new dependencies.** `Directory.Packages.props` unchanged. No new `<PackageReference>` entries. Transport.RelationalDatabase + Transport.SQLite csproj diffs are build-tooling only.

---

## §4. Cross-Phase Coherence

### §4.1 Auth + Authz coherence — PASS

Validator runs at the public API surface before any handler dispatch. Handler forks contain no independent validation — single-responsibility split (boundary validation in producer, business logic in handler). Correct design.

### §4.2 ExternalTransaction base property visibility — ADVISORY (filed as ISSUE-042)

`SendMessageCommand.ExternalTransaction` is declared `public { get; init; }`. A future transport author writing a custom `SendMessageCommand` subclass (not inheriting from `RelationalSendMessageCommand`) could set `ExternalTransaction` via object-initializer syntax. The handler fork checks `commandSend.ExternalTransaction != null` to branch into the caller-tx path — so the custom subclass would enter the fork WITHOUT the retry-decorator bypass firing (since `IRetrySkippable` is implemented only on `RelationalSendMessageCommand`).

**Impact assessment:** No exploitable risk for the current two registered transports (SqlServer, PostgreSQL). Both go through `RelationalSendMessageCommand` which correctly implements `IRetrySkippable`. The advisory only matters if a future transport author writes a non-standard `SendMessageCommand` subclass.

**Filed as ISSUE-042** (Low severity, future-proofing) — narrow the visibility to `internal init` or move the property entirely to `RelationalSendMessageCommand`. Not a ship blocker.

### §4.3 IRetrySkippable bypass correctness — PASS

Retry decorators check `if (command is IRetrySkippable skippable && skippable.SkipRetry)` before entering the Polly pipeline. `SkipRetry` on `RelationalSendMessageCommand` returns `ExternalTransaction != null`. Batch path validates once before the loop; transaction reference stable across iterations.

### §4.4 Phase 3 extractor pass-through fix (commit `994e1404`) — STRENGTHENS

Original `SqlServerExternalDbNameExtractor` used `ToUpperInvariant()` without symmetric `IConnectionInformation.Container` normalization, creating a symmetry gap → false mismatch in integration tests. Fix is pass-through (`connection.Database ?? string.Empty`) matching PG's extractor. **Strengthens** the correctness contract; does NOT weaken any security boundary. Validator still enforces DB name equality before any INSERT.

### §4.5 Non-relational transport capability-cast correctness (Phase 5) — PASS

Memory, Redis, LiteDb, SQLite producers do NOT inherit from `RelationalProducerQueue<T>` and do NOT implement `IRelationalProducerQueue<T>`. Phase 5 negative-path tests assert this explicitly. DI registrations are container-scoped — no cross-transport leakage possible.

---

## §5. Outstanding Advisories

### §5.1 GHSA-g94r-2vxg-569j — OpenTelemetry.Api 1.15.2 (ISSUE-032 accepted risk)

**Status:** Documented risk acceptance, not a security gap.

`OpenTelemetry.Api 1.15.2` in `Directory.Packages.props` carries advisory `GHSA-g94r-2vxg-569j` (moderate severity). Predates this branch. Phase 7 PLAN-1.1 / commit `88ff8996` demoted NU1902 from error to warning on `Transport.SQLite.csproj` to unblock the Release build path. The advisory still surfaces as a visible NU1902 warning on all Release builds.

**Impact:** Advisory relates to OpenTelemetry instrumentation libraries. DotNetWorkQueue uses OpenTelemetry for distributed tracing decorators; the advisory does not affect queue core or outbox feature. Long-term remediation: bump to patched OpenTelemetry version when GA-compatible with net8.0.

### §5.2 ExternalTransaction base visibility — see §4.2 (filed as ISSUE-042)

---

## Analysis Coverage

| Area | Checked | Notes |
|------|---------|-------|
| Code Security (OWASP) | YES | A01/A02/A03/A05/A06/A09 mapped above |
| Secrets / Credentials | YES | 54 files; no findings |
| Dependencies | YES | Directory.Packages.props unchanged |
| IaC / Container | N/A | Jenkinsfile, Dockerfile, GH Actions unchanged |
| Configuration | YES | 2 csproj edits; both tighten or advisory-neutral |
| Cross-phase coherence | YES | 5 dimensions checked (§4.1-§4.5) |

---

## Verdict + Tally

| Severity | Count |
|----------|-------|
| Critical | 0 |
| High | 0 |
| Medium | 0 |
| Low | 0 |
| Info/Advisory | 2 (ExternalTransaction base visibility filed as ISSUE-042; GHSA-g94r-2vxg-569j accepted risk via ISSUE-032 closure) |

**CLEAN. No findings block shipment.**
