# Phase 7 Security Audit

**Date:** 2026-05-15
**Auditor:** Security & Compliance Agent
**Branch:** `feature/outbox-pattern`
**Scope:** Documentation phase — 1 new markdown tutorial, 1 README line, 2 csproj edits
**Files audited:** 4
- `docs/outbox-pattern.md` (new, ~205 lines)
- `README.md` (1-line addition)
- `Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj` (3 PropertyGroup edits)
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj` (1 PropertyGroup addition)

---

## Verdict: CLEAN

**Risk Level:** Low

Phase 7 is a documentation-only milestone. The tutorial doc contains clearly-marked placeholder credentials, demonstrates correct parameterized SQL, and correctly models `using`/`DbTransaction` ownership. The csproj edits are build-tooling changes only: one adds `<DocumentationFile>` for XML doc generation, the other demotes a pre-existing NU1902 vulnerability warning from error to warning to unblock the Release build — with the advisory still visible as a warning. Nothing blocks shipment.

---

## STRIDE Threat Model (abbreviated)

Phase 7 introduces no new production code. The only attack-surface consideration is the tutorial's **educational impact** — whether the example it teaches would be insecure if copy-pasted.

| Threat | Relevance | Finding |
|--------|-----------|---------|
| Spoofing | None | No auth code added |
| Tampering | None | No production logic changed |
| Repudiation | None | No logging changes |
| Information Disclosure | Low | Tutorial shows `User Id=sa;Password=...` — assessed §1 |
| Denial of Service | None | No resource-limit changes |
| Elevation of Privilege | None | No authz code changed |

---

## §1. Secrets Scan

**Result: No findings.**

The tutorial contains two connection string literals:

```
"Server=localhost;Database=AppDb;User Id=sa;Password=...;TrustServerCertificate=true"
```

Both are identical placeholders. Assessment:
- `Password=...` is a literal three-dot ellipsis — not a real credential. No entropy, not parseable by any secrets scanner.
- `User Id=sa` is the conventional SQL Server example account name, present in Microsoft's own documentation samples.
- `Server=localhost` is a loopback-only address. The combination cannot connect to any real or internet-accessible server.
- The values appear twice (once in the `QueueConnection` setup block, once in the per-request `SqlConnection` block). Both are clearly labeled as setup/example code in surrounding prose.

No API keys, tokens, private keys, Base64-encoded credentials, or `.env`-equivalent files appear anywhere in the Phase 7 diff.

---

## §2. Tutorial Code Review

### 2a. Connection String Handling (Education Risk)

The tutorial shows `User Id=sa;Password=...` rather than advising integrated authentication or a secrets-manager pattern. This is a **documentation advisory**, not a code defect:

- The placeholder is visually obvious (`...` is not a real password).
- The tutorial's purpose is to demonstrate the outbox API surface, not production connection-string management. Adding a "use a secrets manager" note would improve the doc without blocking ship.
- `TrustServerCertificate=true` is shown without comment. In a production-facing tutorial this is worth noting — it disables TLS certificate validation and is appropriate only for local/dev environments.

Neither concern is a Critical or Important finding given the clear example context, but both are captured as advisory items below.

### 2b. SQL Injection (A03:2021, CWE-89)

The business INSERT in the tutorial:

```csharp
cmd.CommandText = "INSERT INTO Orders (OrderId, Status) VALUES (@id, @status)";
cmd.Parameters.AddWithValue("@id", 42);
cmd.Parameters.AddWithValue("@status", "Pending");
cmd.ExecuteNonQuery();
```

This is a correct parameterized query. No string concatenation, no interpolation of user-supplied values. `AddWithValue` is the standard ADO.NET parameterization path. No injection surface is present or taught.

**Result: No findings.**

### 2c. Resource Leak Patterns (CWE-772)

`using` discipline in the example:

| Resource | Disposal |
|----------|----------|
| `producerContainer` | `using var` — correct |
| `producer` | `using var` — correct |
| `sqlConn` | `using var` — correct |
| `transaction` | `using var` — correct (also committed before scope exit) |
| `cmd` | `using (var cmd = ...)` — correct |

The pattern follows the "caller owns connection and transaction" contract described in the Lifecycle Contract section. All disposable resources are properly scoped. No leaks taught.

**Result: No findings.**

### 2d. Retry Semantics (CWE-755)

The Retry Contract section explicitly states the transport bypasses its internal Polly retry on the caller-transaction path and directs callers to wrap the entire connection+transaction block in their own policy. The prose example shows the correct layering:

```
Polly retry policy
  └─ open connection + begin transaction
  └─ business INSERT
  └─ relationalProducer.Send(msg, transaction)
  └─ transaction.Commit()
```

This is correct: retrying only the `Send` call inside an active transaction after a transient error would be unsafe. The tutorial teaches the right pattern.

**Result: No findings.**

### 2e. TrustServerCertificate Advisory

`TrustServerCertificate=true` in both connection strings disables server certificate validation. It is appropriate for localhost dev but should not be copy-pasted into production without understanding the implication. The doc provides no warning. See Advisory A-1 below.

---

## §3. csproj Security Review

### 3a. `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` — Transport.SQLite

**Status of advisory:** `GHSA-g94r-2vxg-569j` (OpenTelemetry.Api 1.15.2 moderate severity) remains present as a **warning** on all projects that reference OpenTelemetry. The `<WarningsNotAsErrors>` element demotes the warning from a build-breaking error to a non-fatal warning **on Transport.SQLite Release builds only**. It does not suppress the advisory from appearing in build output.

Verification: the element used is `<WarningsNotAsErrors>`, not `<NoWarn>`. `<NoWarn>` would suppress the diagnostic entirely; `<WarningsNotAsErrors>` only prevents escalation to error. The advisory is still visible.

**Decision traceability:** ISSUE-032 in `.shipyard/ISSUES.md` documents the full context: pre-existing advisory, non-introduction by Phase 2, the two remediation options, and the Phase 7 resolution. Long-term remediation (bump OpenTelemetry to patched version when the patched line is GA) is explicitly captured.

**Result: No findings.** The advisory is visible, the decision is documented, and the override is scoped correctly.

### 3b. `<DocumentationFile>` addition — Transport.RelationalDatabase (Release|net8.0)

Adds XML documentation generation for the net8.0 Release target, matching the existing net10.0 Release block. This is a build-tooling change only — it enables IntelliSense XML output for the NuGet package. No security implication.

**Result: No findings.**

---

## §4. README Change

Single bullet added under "High-level features":

> Transactional outbox pattern (SqlServer / PostgreSQL) — link to docs/outbox-pattern.md

No credentials, no security-sensitive content, no broken links to sensitive targets. Trivial change.

**Result: No findings.**

---

## §5. Dependencies and IaC

**New packages:** None. The Phase 7 diff contains no `<PackageReference>` additions.

**NU1902 carry-forward:** `OpenTelemetry.Api 1.15.2` advisory (`GHSA-g94r-2vxg-569j`) is pre-existing. Phase 7 neutralized the build-escalation on Transport.SQLite (§3a). Advisory status unchanged everywhere else.

**IaC / Jenkinsfile:** Unmodified. The outbox documentation requires no CI stage changes.

**Result: No findings.**

---

## §6. Cross-Component Analysis

Phase 7 is documentation-only. No cross-component trust boundaries are modified. The tutorial accurately describes the API contract implemented in Phases 3-5 (IRelationalProducerQueue, ExternalTransactionValidator, SkipRetry path). The Lifecycle Contract, Retry Contract, and Database-Name Comparison Semantics sections are consistent with the production code audited in Phases 3-6.

No systemic patterns spanning multiple components are introduced.

---

## Findings Summary

| Severity | Count |
|----------|-------|
| Critical | 0 |
| High | 0 |
| Important | 0 |
| Advisory | 2 |

**Advisory A-1** — `TrustServerCertificate=true` in tutorial connection strings shown without a dev-only caveat (`docs/outbox-pattern.md` lines 52, 63). Disabling certificate validation in production is a TLS misconfiguration (CWE-295). Add a one-sentence note: "Set `TrustServerCertificate=true` for local development only; remove it in production."

**Advisory A-2** — Tutorial uses SQL Server `sa` account with a placeholder password rather than recommending integrated authentication or a secrets manager. Low education risk given the obvious placeholder, but a one-line note ("use a secrets manager or integrated authentication in production") would improve posture for readers who copy-paste. No immediate action required.

---

## Analysis Coverage

| Area | Checked | Notes |
|------|---------|-------|
| Code Security (OWASP) | Yes | A03 injection (parameterized SQL confirmed); A05 TLS flag (advisory A-1) |
| Secrets & Credentials | Yes | All 4 files; placeholder confirmed non-real |
| Dependencies | Yes | No new packages; NU1902 baseline carry-forward |
| IaC / Container | N/A | No infra files in diff |
| Configuration | Yes | csproj WarningsNotAsErrors scoped and documented |
