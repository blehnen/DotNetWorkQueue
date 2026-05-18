# Security Audit: Phase 3

**Phase:** 3 — SqlServer Inbox Wiring + Unit Tests
**Date:** 2026-05-18
**Scope:** 5 file changes (3 production, 2 test). +372 lines. No new deps. No IaC. No config.
**Commit range:** `05325c50..151fbda8` (6 commits)

---

## Verdict: LOW_RISK

No exploitable vulnerabilities. No secrets exposure. No dependency changes. The two informational
observations below are documented design decisions, not security defects.

---

## STRIDE Threat Model (quick pass)

| Threat | Surface | Assessment |
|--------|---------|------------|
| Spoofing | IWorkerNotification resolution | No auth involved; container-internal |
| Tampering | DbTransaction exposed to user handler | Accepted risk per PROJECT.md §Ownership & Threading Contract; not compiler-enforced but XML-documented |
| Repudiation | No new logging added | Non-issue; existing WorkerNotification logging unchanged |
| Information Disclosure | Bare `catch` in factory delegate; Connection strings in tests | Catch: swallows exception type to log (no user-visible output). Test strings: fake credentials, not real |
| Denial of Service | No resource limits changed | N/A |
| Elevation of Privilege | No new authz surface | N/A |

---

## Findings

### Critical
None.

### High / Medium
None.

### Low / Informational

**[L1] Bare `catch` in IWorkerNotification factory delegate swallows all exception types**
- **Location:** `SQLServerMessageQueueInit.cs` — `Register<IWorkerNotification>` factory lambda
- **Description:** The `try/catch` block catches all exceptions (including `NullReferenceException`,
  `InvalidCastException`, programming errors) and silently falls back to
  `holdTransaction = false`. A misconfigured DI graph (e.g., missing `ITransportOptionsFactory`
  registration) would produce a silent, incorrect fallback rather than a visible error at startup.
- **Impact:** Not exploitable; the fallback is the *safe* non-relational path. Risk is
  operational: a configuration error could silently disable the inbox feature without any
  diagnostic signal. (OWASP A09:2021 — Security Logging / Monitoring Failures, advisory level)
- **Remediation:** Log the caught exception at `Warning` level before falling back, matching
  the precedent in `IBaseTransportOptions` exception handling elsewhere in the file. A single
  `_log.LogWarning(ex, "Failed to load transport options; defaulting to non-relational notification")` 
  would surface the issue without breaking the fallback contract.
- **Evidence:** Commit `ce6c79c3` — the `catch` block contains only `holdTransaction = false;`

**[L2] `ConnectionHolder` property is publicly settable on an `internal` class**
- **Location:** `SqlServerRelationalWorkerNotification.cs:74`
- **Description:** `ConnectionHolder` has a public setter. The class is `internal`, so external
  assemblies cannot set it — but any code in `DotNetWorkQueue.Transport.SqlServer` can inject an
  arbitrary `IConnectionHolder` after construction, potentially replacing the live dequeue holder
  mid-handler.
- **Impact:** Informational only. The setter is the intentional property-injection seam; `internal`
  visibility already limits the attack surface to within the assembly. No external exploit path.
- **Remediation:** Advisory: consider `internal set` to restrict the setter to assembly-internal
  callers only, making the boundary explicit and preventing accidental mutation from future
  assembly-internal code. Not a required fix.

---

## Category Review

### A. Code security (OWASP Top 10)
The new class intentionally exposes an active `DbTransaction` to user handlers via
`IRelationalWorkerNotification`. PROJECT.md §Ownership & Threading Contract documents the forbidden
operations (`Commit`, `Rollback`, `Dispose`, `Close`) and the no-stash, no-cross-thread rules.
This is identical in posture to the outbox milestone's `IRelationalProducerQueue<T>` and is an
accepted design trade-off. The receive-path pattern-match (`is SqlServerRelationalWorkerNotification`)
is a safe runtime check — no unsafe cast, no `ClassCastException` risk. No injection, XSS, or
deserialization surface introduced.

### B. Secrets
Test files contain `"Server=localhost;...Password=password"` — this is the same fake constant used
in `QueueCreatorTests.cs` (`GoodConnection` pattern) throughout the test suite. No real credentials.
Commit messages contain no embedded tokens or keys. No `.env` files added.

### C. Dependency vulnerabilities
No new package references introduced. Pre-existing `NU1902` (OpenTelemetry.Api 1.15.2 advisory) is
unchanged carry-forward from Phase 7; it is not introduced or worsened by this phase.

### D. IaC / configuration
N/A — no Terraform, Ansible, Docker, or config file changes.

### E. Cross-task coherence
- DI registration is option-driven and Transient-scoped (correct for per-message state).
- The `Register<WorkerNotification>` duplicate self-registration was correctly dropped in commit
  `ce6c79c3` — the core already registers it, avoiding an `AllowOverridingRegistrations` footgun.
- Receive-path setter fires only within the existing `GetConnectionAndSetOnContext` guard, after
  which the connection holder is live. No window where `Transaction` is accessed before injection.
- Tests use NSubstitute interfaces throughout; no reflection-based escape hatches. The `DbTransaction`
  mock (for the "returns underlying transaction" test) uses `Substitute.For<DbTransaction>()` not
  `SqlTransaction` — correct, because `SqlTransaction` is sealed (matches CLAUDE.md lesson on
  sealed transport types).

### F. Repo-specific posture
CLAUDE.md / MEMORY.md security model: "Transport security is user's responsibility;
TypeNameHandling/LINQ are accepted risks." The inbox feature adds no new attack surface beyond the
existing `IWorkerNotification` contract — it extends the notification with a transaction reference
the user already controlled (via their handler invocation context). The feature is consistent with
the documented posture.

---

## Recommendation

**Ship.** No blocking findings. L1 (bare catch) is worth a follow-up logging line in the same
phase or as a ISSUES.md carry-forward item; it does not gate delivery. L2 (`internal set`) is
advisory only.
