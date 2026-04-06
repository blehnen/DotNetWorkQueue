# Security Audit Report — Phase 1

## Executive Summary

**Verdict:** PASS
**Risk Level:** Low

This phase fixes two Redis history bugs: guarding against null/missing hash fields when recording completion/errors, and restricting the purge handler to terminal-only message states. All four changed files (two production, two test) are display and maintenance logic only — no authentication, no user-facing endpoints, no data ingress paths, and no new dependencies were introduced. No exploitable vulnerabilities, secrets, or supply-chain risks were found. The changes are safe to ship.

### What to Do

| Priority | Finding | Location | Effort | Action |
|----------|---------|----------|--------|--------|
| 1 | ExceptionText stores raw exception string with no length cap | `WriteMessageHistoryHandler.cs:94` | Small | Add a `StringSizeLimit` trim or document the max-length expectation |
| 2 | `body`/`headers` parameters accepted but silently dropped | `WriteMessageHistoryHandler.cs:47-60` | Trivial | Remove unused parameters or document intentional omission |
| 3 | `GetDb()` seam is `protected virtual` on production classes | `PurgeMessageHistoryHandler.cs:44`, `WriteMessageHistoryHandler.cs:44` | Small | Consider `internal protected` + `[InternalsVisibleTo]` to tighten surface area |

### Themes
- No security themes: all findings are defensive hardening advisories, not exploitable issues.
- The fix correctly moves from unsafe cast-on-null to `HasValue` guarded reads throughout both handlers.

---

## Detailed Findings

### Critical

None.

### Important

None.

### Advisory

- **Unbounded ExceptionText stored in Redis** (`WriteMessageHistoryHandler.cs:94`) — `exception ?? ""` is written directly to the hash field with no length limit. A very large exception message (e.g. a stack trace from a loop failure) could consume unexpected Redis memory. Consider capping at a reasonable limit (e.g. 4096 chars) before storage. Not a security vulnerability in the threat model (internal infrastructure), but a resource-exhaustion defensive measure.

- **Unused `body`/`headers` parameters** (`WriteMessageHistoryHandler.cs:47`) — `RecordEnqueue` accepts `byte[] body` and `byte[] headers` but neither is used or stored. This is not a vulnerability, but if either parameter ever contained sensitive payload data, the signature creates a false expectation that the data is persisted. Remove the parameters or document explicitly that they are intentionally ignored (CWE-561 — Dead Code).

- **`GetDb()` seam is `protected virtual` on production classes** (`PurgeMessageHistoryHandler.cs:44`, `WriteMessageHistoryHandler.cs:44`) — The virtual override exists solely for test injection (correctly documented). It does not create an exploitable attack surface in a library context, but the seam slightly expands the inheritance surface of internal classes. Applying `[assembly: InternalsVisibleTo("...Tests")]` and marking the method `internal protected` would confine the seam to test assemblies only.

- **`orphaned index` path increments `count`** (`PurgeMessageHistoryHandler.cs:63-64`) — When a hash is missing (orphaned index entry), the handler removes the sorted-set entry and increments the purge count. This is correct behavior, but the count semantics are now slightly overloaded (deleted hash vs. cleaned-up orphan). No security issue; worth a comment for future maintainers.

---

## Cross-Component Analysis

The phase touches only the Redis transport's history subsystem. History is an opt-in diagnostic feature (`EnableHistory` flag); it is disabled by default and isolated from the message delivery path. No cross-component trust-boundary issues were identified:

- **Authentication/Authorization coherence:** History writes/reads are gated only by `EnableHistory`. No user-facing authorization is involved — these are internal lifecycle callbacks called by the consumer/producer pipeline after authentication has already been established at the transport layer. Consistent with project security model.
- **Data flow — ExceptionText:** Exception text from failed message processing is written to Redis. This text originates from application code running inside the consumer, not from untrusted external input. The data flow is: consumer exception → `RecordError(queueId, exception.ToString())` → Redis hash field. The exception string is written verbatim with no sanitization; this is acceptable given the closed trust model (internal infrastructure only) but the length advisory above applies.
- **Error handling consistency:** Both handlers now consistently apply `HasValue` guards before casting `RedisValue` to `long` or `int`. This is the fix under review and it is applied symmetrically across `RecordComplete`, `RecordError`, and `Purge`. No inconsistencies found.
- **Logging:** No logging changes in this phase. The handlers do not log; observability is via metrics at the caller level. Consistent with existing patterns.

---

## Analysis Coverage

| Area | Checked | Notes |
|------|---------|-------|
| Code Security (OWASP) | Yes | No injection, no auth, no deserialization, no XSS surfaces in scope |
| Secrets & Credentials | Yes | No hardcoded secrets, tokens, or connection strings in any changed file |
| Dependencies | Yes | No new dependencies added; existing StackExchange.Redis and NSubstitute unchanged |
| Infrastructure as Code | N/A | No IaC files changed |
| Docker/Container | N/A | No Dockerfile changes |
| Configuration | Yes | No configuration files changed; `EnableHistory` flag handling reviewed |

---

## Dependency Status

No dependencies were added or changed in this phase.

| Package | Version | Known CVEs | Status |
|---------|---------|-----------|--------|
| StackExchange.Redis | (pinned in Directory.Packages.props, unchanged) | None in scope of this diff | OK |
| NSubstitute | (test-only, unchanged) | None | OK |

---

## IaC Findings

Not applicable — no infrastructure files were changed in this phase.
