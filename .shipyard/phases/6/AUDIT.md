# Phase 6 Security Audit

**Date:** 2026-05-15
**Auditor:** Security & Compliance Agent
**Branch:** `feature/outbox-pattern`
**Scope:** SqlServer + PostgreSQL outbox integration tests; one production-code fix (commit 994e1404)
**Files audited:** 13
- `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/SqlServerOutboxIntegrationTestBase.cs`
- `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/SqlServerOutboxSendTests.cs`
- `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/SqlServerOutboxSendAsyncTests.cs`
- `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/SqlServerOutboxRetryBypassTests.cs`
- `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/SqlServerOutboxAdditionalDataTests.cs`
- `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/SqlServerOutboxValidationTests.cs`
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxIntegrationTestBase.cs`
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxSendTests.cs`
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxSendAsyncTests.cs`
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxRetryBypassTests.cs`
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxAdditionalDataTests.cs`
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxValidationTests.cs`
- `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerExternalDbNameExtractor.cs` (production)
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerExternalDbNameExtractorTests.cs`

---

## Verdict: CLEAN

**Risk Level:** Low

Phase 6 is test infrastructure (12 integration test files) plus a one-line production fix that removes a case-normalisation asymmetry in `SqlServerExternalDbNameExtractor`. No secrets, no exploitable injection surface, no new packages, no IaC changes. The production-code change is a security-neutral correctness fix; it does not alter any authorization boundary. Nothing blocks shipment.

---

## STRIDE Threat Model

Phase 6 introduces no new production attack surface. The only entry points are:
- SQL executed in test methods against a test CI database
- The single production method `Extract(DbConnection)` which performs a property read

| Threat | Relevance | Finding |
|--------|-----------|---------|
| Spoofing | None | No auth code changed |
| Tampering | Low | DDL identifiers are GUID-derived (see §2) |
| Repudiation | None | Test-only; no audit log changes |
| Information Disclosure | Low | Exception messages in validator include DB names (diagnostic, not a leak; see §3) |
| Denial of Service | None | No resource-limit changes |
| Elevation of Privilege | None | No authz code changed |

---

## §1. Secrets Scan

**Result: No findings.**

- `ConnectionInfo.ConnectionString` is the established project pattern (resolved at test runtime from `connectionstring.txt` which is not committed to version control). No hardcoded connection strings, passwords, API keys, tokens, or private keys appear in any new file.
- Grep surface checked: all string literals in 13 files. No `Password=`, `pwd=`, `Bearer`, `ApiKey`, or Base64-encoded credential patterns found.
- Test fixture data is limited to `FakeMessage` instances generated via `GenerateMessage.Create<FakeMessage>()` — no sensitive test data.

---

## §2. SQL Injection Audit (A03:2021, CWE-89)

**Result: No findings.**

All DDL and DML statements in both base classes interpolate identifiers, not values. Full analysis:

**Identifier construction** (`NewQueueName`, `NewBusinessTableName`):
- SqlServer: `"q" + Guid.NewGuid().ToString("N")` and `"OutboxBusiness_" + Guid.NewGuid().ToString("N")`
- PostgreSQL: `"q" + Guid.NewGuid().ToString("N")` and `"outboxbusiness_" + Guid.NewGuid().ToString("N")`

`Guid.ToString("N")` produces a 32-character lowercase hex string (`[0-9a-f]{32}`). No SQL metacharacters (quotes, semicolons, hyphens, spaces, comment markers) are present in this character set. The resulting identifiers are structurally safe for interpolation into DDL.

**Data values** in `INSERT` statements:
- SqlServer `InsertBusinessRow`: uses `cmd.Parameters.AddWithValue("@id", id)` and `cmd.Parameters.AddWithValue("@val", val)`. Parameterised. No injection surface.
- PostgreSQL `InsertBusinessRow`: same pattern with Npgsql parameters.

**`SELECT COUNT(*)` and `SELECT CorrelationID` queries** interpolate table names from the same GUID-derived namespace. No user-controlled data enters any query.

**`CountQueueMessages`** interpolates `helper.MetaDataName` which is derived from the queue name (also GUID-derived). Safe.

**Advisory note**: The test base classes use identifier interpolation rather than `QUOTENAME()` / `format_ident()` quoting. This is acceptable here because the identifiers are GUID-derived and the code lives entirely in the test assembly. Were this pattern copied into production code with any caller-supplied input, it would be a defect. No action required for test code.

---

## §3. OWASP Applicability to Test Code

**A01 (Broken Access Control):** N/A — tests exercise a producer; no consumer, no RBAC changes.

**A02 (Cryptographic Failures):** N/A — no encryption, no hashing, no token generation.

**A03 (Injection):** Audited in §2. No findings.

**A04 (Insecure Design):** N/A.

**A05 (Security Misconfiguration):**

- Exception messages in `ExternalTransactionValidator.Validate()` include database names (`actual` and `expected`). These appear in test failure output and potentially in CI logs. The values are DB catalog names (e.g., `"master"`, `"postgres"`, `"IntegrationTests"`) — not credentials. This is diagnostic intent, not a leak. The `Validation_CrossDatabaseMismatch_ThrowsBeforeInsert` tests deliberately assert on the presence of these names, confirming they are expected output.
- No debug modes, verbose stack traces, or CORS-equivalent settings are touched.

**A06–A10:** N/A for test code in this phase.

---

## §4. Production-Code Change Audit (commit 994e1404)

**File:** `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerExternalDbNameExtractor.cs`

**Change:** `connection.Database?.ToUpperInvariant() ?? string.Empty` → `connection.Database ?? string.Empty`

**Security analysis:**

The extractor feeds into `ExternalTransactionValidator.Validate()`:

```csharp
var actual = _extractor.Extract(connection);    // extractor output
var expected = _connectionInfo.Container;       // verbatim InitialCatalog from conn string
if (!string.Equals(actual, expected, StringComparison.Ordinal))
    throw new InvalidOperationException(…);
```

The comparison is `StringComparison.Ordinal` on both sides. Before this fix, `actual` was uppercased but `expected` (`IConnectionInformation.Container`) was verbatim — so the comparison would fail whenever the catalog name was mixed-case (e.g., `"MyDb"` in the connection string would produce `expected = "MyDb"` and `actual = "MYDB"`, yielding a false mismatch). This was a correctness bug, not a security bypass.

**Authorization / security boundary check:**

The `ExternalTransactionValidator` is a **correctness guard**, not an authorization enforcement point. It ensures the caller's transaction is on the right database (same database as the queue), preventing accidental data mixing in the outbox pattern. It does not:
- Gate access to privileged operations
- Enforce user identity or roles
- Control which callers may enqueue messages

Removing `ToUpperInvariant` does not relax any security boundary. It fixes a false-positive rejection that was blocking legitimate same-database transactions when catalog names contained lowercase letters. The PostgreSQL extractor (phase 4) has always been pass-through; this change aligns SqlServer to the same symmetric posture.

**Verdict for commit 994e1404:** Correctness fix, no security regression.

---

## §5. Dependency and IaC

**New NuGet packages added in Phase 6:** None. Confirmed — no `<PackageReference>` additions in any `.csproj` file.

**Pre-existing NU1902 OpenTelemetry advisory warnings:** Documented as ISSUE-032, carry-forward baseline, out of Phase 6 scope.

**IaC changes:** None. `Jenkinsfile` is unmodified. The new outbox test classes slot into existing parallel integration test stages.

**CI validation:** PR-138 build #3 (commit `ae7bfb66`) reported `continuous-integration/jenkins/pr-merge: SUCCESS` across the full 14-stage matrix.

---

## §6. Cross-Component Analysis

The two base classes (`SqlServerOutboxIntegrationTestBase`, `PostgreSqlOutboxIntegrationTestBase`) share the same structural pattern: GUID-derived identifiers, parameterised DML, `ConnectionInfo.ConnectionString` for credentials, and `IDisposable` scopes with `RemoveQueue()` teardown. Both are internally consistent with the established integration test conventions in the rest of the repository.

No cross-component trust boundary is introduced or weakened by Phase 6.

---

## Findings Summary

| Severity | Count |
|----------|-------|
| Critical | 0 |
| High | 0 |
| Medium | 0 |
| Low | 0 |
| Info | 1 |

**Info-1** — Test DDL uses GUID-derived identifier interpolation without `QUOTENAME()` / `format_ident()` quoting. Safe in context (identifiers are hex-only, test assembly, no user input). Pattern should not be copied into production query builders without adding identifier quoting. No remediation needed for test code.

---

## Analysis Coverage

| Area | Checked | Notes |
|------|---------|-------|
| Code Security (OWASP) | Yes | A03 injection full review; A05 spot-check on diagnostic exception messages |
| Secrets & Credentials | Yes | All 13 files; no hits |
| Dependencies | Yes | No new packages; NU1902 baseline carry-forward |
| IaC / Container | N/A | No infra files in diff |
| Configuration | N/A | No config files in diff |
