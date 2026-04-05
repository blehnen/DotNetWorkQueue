# Security Audit Report — Phase 2

## Executive Summary

**Verdict:** PASS
**Risk Level:** Low

Phase 2 adds queue name validation to all 6 transport connection info classes, using static compiled regexes to restrict names to safe ASCII alphanumeric characters, underscores, dots, and (for Redis) hyphens. This is the correct mitigation for the SQL injection risk created by the pattern of interpolating queue names directly into SQL statements as table identifiers across SqlServer, PostgreSQL, and SQLite transports. The validation is placed at construction time of each connection info object, which is early enough to prevent any use of unvalidated names. The regex patterns are sound, not vulnerable to ReDoS, and cannot be bypassed by Unicode tricks or null bytes in .NET's Regex engine. Two advisory-level findings are noted: an unvalidated SQL Server schema name that was a pre-existing issue (not introduced by this phase), and the allowance of empty queue names for backward compatibility in relational transports.

### What to Do

| Priority | Finding | Location | Effort | Action |
|----------|---------|----------|--------|--------|
| 1 | SQL Server schema name not validated | ConfigurationExtensions.cs:168 | Small | Add same regex validation to `GetSchema()`/`SetSchema()` |
| 2 | Empty queue names allowed in relational transports | SqlServer/SQLConnectionInformation.cs:92, PostgreSQL/SQLConnectionInformation.cs:71, SQLite/SqliteConnectionInformation.cs:69 | Trivial | Document or log a warning when empty name is used |

### Themes
- Validation is consistently applied across all 6 transports at the correct enforcement point (constructor)
- The regex allowlist approach is the right pattern for identifier injection prevention
- One pre-existing adjacent attack surface (schema name) remains unaddressed but is out of scope for this phase

## Detailed Findings

### Critical

No critical findings.

### Important

No important findings.

### Advisory

- **[A1] SQL Server schema name (`AdditionalConnectionSettings["SqlSchema"]`) is interpolated into SQL without validation** at `Source/DotNetWorkQueue.Transport.SqlServer/ConfigurationExtensions.cs:168` and used in `SqlServerTableNameHelper.cs:82`, `SqlServerCommandStringCache.cs:100`. The `GetSchema()` method returns the raw user-supplied string or default `"dbo"`. A malicious schema value like `dbo]; DROP TABLE --` would be interpolated into queries like `_schema.Schema + "." + _connectionInformation.QueueName`. This is a pre-existing issue not introduced by Phase 2, but it represents the same class of vulnerability that Phase 2 addresses for queue names. **Remediation:** Apply the same `^[a-zA-Z0-9_.]+$` regex validation to the schema value in `SetSchema()` or `GetSchema()`, or validate it in the `SqlSchema` constructor.

- **[A2] Empty queue names are silently accepted in relational transports** (`SqlServer/SQLConnectionInformation.cs:92`, `PostgreSQL/SQLConnectionInformation.cs:71`, `SQLite/SqliteConnectionInformation.cs:69`). The validation returns early with `if (string.IsNullOrEmpty(name)) return;` for backward compatibility. While the `TableNameHelper` handles empty names by substituting `"Error-Name-Not-Set"`, this could lead to confusing behavior. This is explicitly documented as a backward-compatibility choice and is not a security risk since the fallback string is also safe. **Remediation:** Consider logging a deprecation warning when an empty queue name is used, to encourage migration.

- **[A3] SQLite has no maximum length constraint** (`Source/DotNetWorkQueue.Transport.SQLite/SqliteConnectionInformation.cs:69-71`). While SQLite does not have a formal identifier length limit, extremely long names could cause memory or display issues. The regex validation still prevents injection regardless of length. **Remediation:** Consider adding a reasonable upper bound (e.g., 256 characters) for defense in depth.

## Cross-Component Analysis

**Regex pattern consistency:** All 6 transports use the same core pattern `^[a-zA-Z0-9_.]+$` (Redis adds `\-`). This is an effective allowlist that blocks all SQL injection metacharacters (single quotes, semicolons, brackets, parentheses, spaces, comment markers). The pattern:
- Uses `^` and `$` anchors, preventing partial match bypass
- Is applied to the full string, not line-by-line (no `RegexOptions.Multiline`)
- Only allows ASCII characters — .NET's `Regex` class does not match Unicode word characters against `a-zA-Z0-9` ranges
- Has no backtracking risk (linear-time evaluation, no nested quantifiers) — ReDoS is not possible

**Null byte handling:** In .NET, `\0` (null byte) is not matched by `[a-zA-Z0-9_.]`, so a name containing embedded nulls will be correctly rejected. The `string.IsNullOrEmpty()` check handles the null reference case.

**Validation placement:** Validation occurs in each transport's `ConnectionInformation` constructor, which is the earliest possible point. The `QueueConnection` class (a simple DTO) does not validate, but every transport that consumes it does. The `BaseConnectionInformation` base class also does not validate, but since it is never used directly for SQL operations (only concrete transport subclasses are), this is acceptable. The `Clone()` method on each class re-invokes the constructor, ensuring cloned instances are also validated.

**Data flow integrity:** Queue names flow from `QueueConnection.Queue` -> `BaseConnectionInformation._queueName` -> `IConnectionInformation.QueueName` -> `TableNameHelper` -> SQL string interpolation. Validation at the constructor ensures the name is safe before it enters the immutable `_queueName` field (the backing field is `readonly`-equivalent via property getter). There is no setter or mutation path that could introduce an unvalidated name after construction.

**SQL Server schema name gap:** The `SqlSchema` class reads the schema from `AdditionalConnectionSettings` without validation. This value is concatenated into table names (`_schema.Schema + "." + _connectionInformation.QueueName`) and directly interpolated into a WHERE clause (`TABLE_SCHEMA = '{_schema.Schema}'`). While this is a pre-existing issue and out of Phase 2's explicit scope, it represents the same injection vector that Phase 2 mitigates for queue names. It should be addressed in a follow-up.

## Analysis Coverage

| Area | Checked | Notes |
|------|---------|-------|
| Code Security (OWASP) | Yes | Regex validation patterns verified against SQL injection (CWE-89), ReDoS (CWE-1333), Unicode bypass |
| Secrets & Credentials | Yes | No secrets found in changed files |
| Dependencies | N/A | No dependency changes in this phase |
| Infrastructure as Code | N/A | No IaC changes |
| Docker/Container | N/A | No Docker changes |
| Configuration | Yes | Checked `AdditionalConnectionSettings` schema flow |

## Dependency Status

No dependency changes in Phase 2.

## IaC Findings

Not applicable — no infrastructure changes in this phase.
