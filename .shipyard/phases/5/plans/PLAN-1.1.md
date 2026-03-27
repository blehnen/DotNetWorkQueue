---
phase: security-documentation
plan: "1.1"
wave: 1
dependencies: []
must_haves:
  - SECURITY.md exists covering Dynamic LINQ risks, serialization binder protections, deployment recommendations
  - Document references specific classes (DynamicCodeCompiler, LinqCompiler, DenyListSerializationBinder)
  - Document explains Dynamic LINQ is net48-only via JpLabs.DynamicCode
  - Document provides actionable guidance (not just "be careful")
  - No code changes
files_touched:
  - Source/DotNetWorkQueue/SECURITY.md (new)
tdd: false
---

# Plan 1.1: Create Security Considerations Document

## Context

The Dynamic LINQ compilation feature compiles arbitrary code strings into executable delegates with no sandboxing. Combined with the deserialization risk (now mitigated by the deny-list binder from Phase 1), this needs clear documentation so users understand the threat model and mitigations available.

## Tasks

### Task 1: Create SECURITY.md
**Files:** `Source/DotNetWorkQueue/SECURITY.md` (new)
**Action:** create
**Description:**

Create a security considerations document at `Source/DotNetWorkQueue/SECURITY.md` with these sections:

1. **Overview** — Brief statement that DotNetWorkQueue processes user-defined message payloads and supports dynamic code compilation, requiring security awareness.

2. **Deserialization Security** — Explain the `TypeNameHandling.Auto` usage with Newtonsoft.Json, the `DenyListSerializationBinder` default protection (29 blocked gadget types), how to extend the deny-list, how to use the `AllowListSerializationBinder` for maximum lockdown, and how to replace binders via DI. Reference `DotNetWorkQueue.Serialization.DenyListSerializationBinder` and `AllowListSerializationBinder`.

3. **Dynamic LINQ Compilation** — Explain that `DynamicCodeCompiler` and `LinqCompiler` compile arbitrary LINQ expression strings into executable delegates. The `References` and `Usings` properties allow callers to inject assembly references and namespaces. This is a net48-only feature via the vendored `JpLabs.DynamicCode.dll` (2019 binary). On .NET 8+ and .NET 10, compiled LINQ expressions are used instead (safe). Clearly state: if an attacker can enqueue a crafted LINQ message, they achieve code execution on the consumer.

4. **Queue Backend Access Control** — Recommend securing the queue backend (SQL Server, PostgreSQL, Redis, SQLite, LiteDB) with proper authentication, network ACLs, and TLS. The queue store is the trust boundary — if an attacker can write to the queue, they can exploit both deserialization and dynamic LINQ.

5. **Dashboard API Security** — Note that the Dashboard API supports optional API key authentication and read-only mode. Recommend HTTPS, network restrictions, and API key rotation for production deployments.

6. **Recommended Deployment Patterns** — Provide actionable guidance:
   - Use `DenyListSerializationBinder` (default) or `AllowListSerializationBinder` for maximum security
   - Restrict queue backend access to trusted producers only
   - Use network-level ACLs to limit who can write to the queue
   - If Dynamic LINQ is not needed, don't use the method queue variants
   - Monitor queue message patterns for anomalies
   - Keep DotNetWorkQueue updated for security fixes

7. **Reporting Security Issues** — Standard responsible disclosure section pointing to GitHub issues (private security advisories if available).

**Style:** Clear, concise, actionable. Use markdown headers, bullet points, and code examples where helpful. Target audience is a developer deploying DotNetWorkQueue in production.

**Acceptance Criteria:**
- File exists at `Source/DotNetWorkQueue/SECURITY.md`
- All 7 sections present
- References DenyListSerializationBinder, AllowListSerializationBinder, DynamicCodeCompiler, LinqCompiler by name
- States Dynamic LINQ is net48-only via JpLabs.DynamicCode
- Provides at least 6 actionable recommendations
- No code changes anywhere

## Verification
```bash
# File exists and has content
test -f "Source/DotNetWorkQueue/SECURITY.md" && wc -l "Source/DotNetWorkQueue/SECURITY.md"
# No code files modified
git diff --name-only HEAD | grep -v "SECURITY.md" | grep -v ".shipyard" | wc -l
```

Success: SECURITY.md exists with substantial content. Zero code files modified.
