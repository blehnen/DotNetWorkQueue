# Security Audit Report -- Phase 1 (Issue #101: Drop net48/netstandard2.0)

## Executive Summary

**Verdict:** PASS
**Risk Level:** Low

This phase removes net48/netstandard2.0 target frameworks, deletes the JpLabs.DynamicCode dynamic LINQ compiler (a significant attack surface reduction), and cleans up `#if NETFULL` conditional blocks. The changes are deletion-heavy and introduce no new code paths, no new dependencies, and no new configuration. The removal of dynamic LINQ string compilation eliminates a class of code injection risk. No critical or important findings.

### What to Do

| Priority | Finding | Location | Effort | Action |
|----------|---------|----------|--------|--------|
| 1 | Stale SECURITY.md references | Source/DotNetWorkQueue/SECURITY.md:89-90 | Trivial | Update docs to reflect DynamicCodeCompiler removal |
| 2 | Residual `#if NETFULL` in integration tests | Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/ (30+ occurrences) | Medium | Clean up in a later phase (out of scope for phase 1 core changes) |

### Themes
- **Attack surface reduction:** Removing DynamicCodeCompiler and JpLabs.DynamicCode eliminates runtime code compilation from untrusted LINQ strings -- a positive security change.
- **Clean fail-closed design:** `LinqCompiler.CompileAction()` now throws `NotSupportedException` rather than silently degrading, ensuring old `LinqExpressionToRun` messages cannot execute.

## STRIDE Threat Model Assessment

| Threat | Impact of Phase 1 Changes | Risk |
|--------|---------------------------|------|
| **Spoofing** | No auth changes | None |
| **Tampering** | Removed dynamic code compilation path -- reduces tampering surface | Positive |
| **Repudiation** | No logging changes | None |
| **Information Disclosure** | NotSupportedException message is descriptive but contains no sensitive data | None |
| **Denial of Service** | Old LinqExpressionToRun messages will throw on consumer -- expected behavior, not a DoS vector | None |
| **Elevation of Privilege** | Removed ability to compile arbitrary code strings at runtime -- significant privilege escalation vector eliminated | Positive |

## Detailed Findings

### Critical

No critical findings.

### Important

No important findings.

### Advisory

- **[A1] Stale SECURITY.md documentation** -- `Source/DotNetWorkQueue/SECURITY.md:89-90` still references `DynamicCodeCompiler` and `JpLabs.DynamicCode.Compiler` as active components. Update to reflect that dynamic LINQ compilation now throws `NotSupportedException`.

- **[A2] Residual `#if NETFULL` blocks in integration tests** -- Approximately 30+ occurrences of `#if NETFULL` remain in integration test projects (e.g., `Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/`, `Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/`). These are outside the scope of phase 1 (core + 8 transport csproj files) but should be tracked for cleanup.

- **[A3] `LinqExpressionToRun` class retained** -- `Source/DotNetWorkQueue/Messages/LinqExpressionToRun.cs` is still present and used by `MessageMethodHandling.cs:70-73` for the `ActionText` payload type. The consumer path correctly calls `_linqCompiler.CompileAction()` which now throws `NotSupportedException`. This is the correct fail-closed behavior. No action needed unless the class should be deprecated with an `[Obsolete]` attribute.

- **[A4] Test file references removed API** -- `Source/DotNetWorkQueue.Tests/Exceptions/CompileExceptionTests.cs:38-42` tests `GetObjectData` on `CompileException`. This is a pre-existing test, not changed in this phase, and `CompileException` still exists in the codebase. No action needed.

## Cross-Component Analysis

### LinqCompiler -> MessageMethodHandling Flow (Verified Safe)

The critical data flow for this audit is: consumer receives a message with `PayLoad == ActionText` -> `MessageMethodHandling.HandleExecution()` (line 69-73) -> deserializes `LinqExpressionToRun` -> calls `_linqCompiler.CompileAction()` -> which now throws `NotSupportedException`.

This is **fail-closed** behavior. An attacker who previously could have crafted a `LinqExpressionToRun` payload containing malicious LINQ strings (CWE-94: Improper Control of Generation of Code) will now get a hard exception rather than code execution. This is a security improvement.

### JobScheduler -> ScheduledJob Flow (Verified Safe)

The `LinqExpressionToRun` overloads of `AddUpdateJob` were removed from `JobScheduler.cs`. The `ScheduledJob` constructor that accepted `LinqExpressionToRun` was removed. The remaining code path only accepts `Expression<Action<...>>` (compiled expressions), which cannot be tampered with via string injection.

### IProducerMethodJobQueue Interface (Verified Clean)

The interface at `Source/DotNetWorkQueue/IProducerMethodJobQueue.cs` only has `SendAsync` with `Expression<Action<...>>` parameter. No `LinqExpressionToRun` overload exists. The `ProducerMethodJobQueueDecorator` tracing decorator had its `#if NETFULL` block with the `LinqExpressionToRun` overload cleanly removed.

### DI Registration (Verified Clean)

`ComponentRegistration.cs` removed the `IObjectPool<DynamicCodeCompiler>` registration. `LinqCompiler` now has a parameterless constructor. No dangling DI registrations that could cause runtime failures.

## Dependency Analysis

| Change | Security Impact |
|--------|----------------|
| **Removed: JpLabs.DynamicCode** (vendored DLL) | Positive -- removes opaque binary with lost source code from supply chain |
| **Removed: Schyntax net48/netstandard2.0** (vendored DLLs) | Neutral -- net8.0/net10.0 versions retained |
| **Removed: Microsoft.CSharp PackageReference** | Positive -- reduces dependency surface; was only needed for dynamic compilation |

No new dependencies were added. No CVE exposure changes.

## Secrets Scanning

No secrets were introduced or modified in this phase's changes. Pre-existing `connectionstring.txt` files with credentials were not part of the diff (these are a known pre-existing concern tracked separately).

## Analysis Coverage

| Area | Checked | Notes |
|------|---------|-------|
| Code Security (OWASP) | Yes | All 23 changed files reviewed; LinqCompiler fail-closed verified; cross-component data flow traced |
| Secrets & Credentials | Yes | No secrets in diff; connectionstring.txt files pre-existing and unchanged |
| Dependencies | Yes | 3 dependencies removed (positive); no additions; no new CVE exposure |
| IaC / Container | N/A | No IaC or container files changed |
| Configuration | Yes | TFM changes only; no debug flags, CORS, or security header changes |
