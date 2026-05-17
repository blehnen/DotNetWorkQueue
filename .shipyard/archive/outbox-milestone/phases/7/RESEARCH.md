# Research: Phase 7 — Documentation + Wiki Draft (Outbox Pattern)

## Context

Branch `feature/outbox-pattern`. Phases 2–6 are complete. Phase 7 closes PROJECT.md §SC #10
(ship-blocking): XML doc comments, `docs/outbox-pattern.md`, README pointer, Release-build
Source Link verification. This document enumerates the work scope for the architect.

---

## §1. Public-Type Inventory (Phases 2–4 Additions)

All types below are confirmed present on the branch by direct file read.

### Transport.Shared (Phase 2)

| FQ Name | File | Visibility | XML-doc status |
|---------|------|------------|----------------|
| `DotNetWorkQueue.Transport.Shared.Basic.Command.SendMessageCommand.ExternalTransaction` | `Source/DotNetWorkQueue.Transport.Shared/Basic/Command/SendMessageCommand.cs` | `public` property | **present** — full summary + remarks |

### Transport.RelationalDatabase (Phase 2)

| FQ Name | File | Visibility | XML-doc status |
|---------|------|------------|----------------|
| `DotNetWorkQueue.Transport.RelationalDatabase.IRetrySkippable` | `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRetrySkippable.cs` | `public interface` | **present** — type + `SkipRetry` member |
| `DotNetWorkQueue.Transport.RelationalDatabase.IExternalDbNameExtractor` | `Source/DotNetWorkQueue.Transport.RelationalDatabase/IExternalDbNameExtractor.cs` | `public interface` | **present** — type + `Extract` method with param/returns |
| `DotNetWorkQueue.Transport.RelationalDatabase.IRelationalProducerQueue<TMessage>` | `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRelationalProducerQueue.cs` | `public interface` | **present** — type + all 6 overloads with param/returns/remarks |
| `DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command.RelationalSendMessageCommand` | `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/Command/RelationalSendMessageCommand.cs` | `public class` | **present** — type + ctor params + `SkipRetry` |
| `DotNetWorkQueue.Transport.RelationalDatabase.Basic.ExternalTransactionValidator` | `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/ExternalTransactionValidator.cs` | `public sealed class` | **present** — type + ctor params + `Validate` with exceptions |
| `DotNetWorkQueue.Transport.RelationalDatabase.Basic.RelationalProducerQueue<T>` | `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalProducerQueue.cs` | `public class` | **present** — type + ctor + all 6 `public`/`protected virtual` methods with param/returns/exceptions |

### Transport.SqlServer (Phase 3)

| FQ Name | File | Visibility | XML-doc status |
|---------|------|------------|----------------|
| `DotNetWorkQueue.Transport.SqlServer.Basic.SqlServerExternalDbNameExtractor` | `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerExternalDbNameExtractor.cs` | `public sealed class` | **present** — type + `Extract` with param/returns/remarks (documents pass-through semantics + prior ToUpperInvariant history) |
| `DotNetWorkQueue.Transport.SqlServer.Basic.SqlServerRelationalProducerQueue<TMessage>` | `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalProducerQueue.cs` | `public sealed class` | **present** — type summary + 11-param ctor with all params; overrides use `/// <inheritdoc />` |

Internal fork methods `HandleExternalTransaction` / `HandleExternalTransactionAsync` in `SendMessageCommandHandler.cs` / `SendMessageCommandHandlerAsync.cs` are `private` — CS1591 does not apply; XML docs present as inline comments only. Not a gap.

### Transport.PostgreSQL (Phase 4)

| FQ Name | File | Visibility | XML-doc status |
|---------|------|------------|----------------|
| `DotNetWorkQueue.Transport.PostgreSQL.Basic.PostgreSqlExternalDbNameExtractor` | `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlExternalDbNameExtractor.cs` | `public sealed class` | **present** — type + `Extract` with param/returns/remarks (documents case-sensitive Ordinal semantics) |
| `DotNetWorkQueue.Transport.PostgreSQL.Basic.PostgreSqlRelationalProducerQueue<TMessage>` | `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalProducerQueue.cs` | `public sealed class` | **present** — type summary + 11-param ctor with all params; overrides use `/// <inheritdoc />` |

### Summary

All public types and members added in Phases 2–4 carry XML doc comments. No CS1591 gaps
found on a direct file-by-file read. The Release build is the authoritative gate (§2).

---

## §2. XML-Doc Gap Analysis

### GenerateDocumentationFile gating — critical finding

XML doc generation is gated **Release-only** via `<DocumentationFile>` inside
`Condition="'$(Configuration)|$(TargetFramework)|$(Platform)'=='Release|...|AnyCPU'"` blocks.
`Directory.Build.props` has NO `<GenerateDocumentationFile>` property at all. Therefore:

- **Debug builds** do NOT emit XML docs and do NOT surface CS1591.
- Only `dotnet build -c Release` (or `-c Release -p:CI=true`) triggers documentation
  generation and TreatWarningsAsErrors on the affected projects.

### Per-project Release condition coverage

| Project | net10.0 DocumentationFile | net8.0 DocumentationFile |
|---------|--------------------------|-------------------------|
| `Transport.Shared` | yes | yes |
| `Transport.RelationalDatabase` | yes (net10.0 only — **net8.0 condition block absent**) | **missing** |
| `Transport.SqlServer` | yes | yes |
| `Transport.PostgreSQL` | yes | yes |

**Gap:** `Transport.RelationalDatabase.csproj` has only a `Release|net10.0` condition block.
There is no `Release|net8.0` block with `DocumentationFile` or `TreatWarningsAsErrors`. This
means the net8.0 TFM of `Transport.RelationalDatabase` does NOT have XML-doc warnings enabled
in Release. If any CS1591 gaps exist in the new Phase 2 types and are only caught on net10.0,
the net8.0 artifact ships undocumented without a build break.

**Architect action required:** Add a `Release|net8.0` condition block to
`Transport.RelationalDatabase.csproj` mirroring the net10.0 block, with both
`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` and
`<DocumentationFile>DotNetWorkQueue.Transport.RelationalDatabase.xml</DocumentationFile>`.

### NoWarn / CS1591 suppression

No `<NoWarn>CS1591</NoWarn>` exists in any of the four affected production project files
(`Transport.Shared`, `Transport.RelationalDatabase`, `Transport.SqlServer`,
`Transport.PostgreSQL`). The `NoWarn` entries that exist are confined to test projects
(1701, 1702, 1705, NU1701, NU1603) and do not touch CS1591. The Release build IS the gate
and CS1591 will fail it if any public member is undocumented.

### Expected build output

Based on direct file inspection all public members carry XML docs. Expected outcome of
`dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release -p:CI=true`:

- **0 CS1591 warnings** (XML docs complete on all new public members).
- **14 NU1902 warnings** (pre-existing OpenTelemetry advisory — see §6 ISSUE-032).
- **1 NU1902 error** on `Transport.SQLite` (ISSUE-032 — pre-existing, blocks the full-solution
  Release build independently of Phase 7 work). This error exists today on master and is not
  introduced by Phase 7.

The architect must note that the full-solution Release build currently FAILS due to ISSUE-032
before Phase 7 even starts. Phase 7's success criterion is zero XML-doc warnings — not a
clean full-solution build. The architect should scope the build verification to the four
outbox projects individually, or document ISSUE-032 as the only remaining blocker after
XML-doc work is complete.

---

## §3. `docs/outbox-pattern.md` Content Blueprint

Decision 2 (CONTEXT-7): tutorial-first, one canonical SqlServer example, PG noted inline.
Style reference: `docs/jenkins-setup.md` (H2 sections, fenced code blocks, no emojis,
imperative voice).

### Proposed section structure

```
# Transactional Outbox Pattern

## Overview
[What the pattern solves: queue INSERT + business INSERT in one atomic transaction.
Why DNQ's default producer does not support it (owns its own connection/tx).
Capability-cast as the opt-in surface.]

## Quick Start

### Prerequisites
- SqlServer or PostgreSQL transport only.
- Queue tables must exist before the first Send call
  (`QueueCreationContainer<T>` + `CreateQueue()` at deploy time).

### Example: SqlServer
[Single fenced C# block — full vertical slice:]
  1. resolve IProducerQueue<T>, capability-cast to IRelationalProducerQueue<T>
  2. open SqlConnection, BeginTransaction
  3. business INSERT on same connection
  4. rp.Send(new MyEvent(...), transaction)   [or rp.SendAsync]
  5. transaction.Commit()

[PostgreSQL note inline (callout block): same pattern, NpgsqlConnection + NpgsqlTransaction]

## Reference

### Lifecycle Contract
- Caller opens connection, begins transaction, commits or rolls back.
- Producer never calls Commit / Rollback / Dispose / Close on the caller's tx or connection.
- ADO.NET transactions are not thread-safe: do not call Send(msg, tx) from multiple threads
  on the same transaction concurrently.
- Source: PROJECT.md §Ownership & Threading.

### Retry Contract
- The producer's Polly retry decorator is bypassed on the caller-tx path
  (IRetrySkippable.SkipRetry = true).
- To retry the whole business operation, wrap all writes + rp.Send in your own retry policy.
- Source: PROJECT.md §Functional Implementation, IRetrySkippable.

### Schema Deployment Prerequisite
- Run CreateQueue() once at deploy time against the same database as your business tables.
- The outbox pattern does not auto-create queue tables on first Send.
- Source: PROJECT.md §Non-Goals.

### Database-Name Comparison Semantics
| Transport | Extractor | Comparison |
|-----------|-----------|------------|
| SqlServer | SqlServerExternalDbNameExtractor | pass-through; StringComparison.Ordinal on both sides |
| PostgreSQL | PostgreSqlExternalDbNameExtractor | pass-through; StringComparison.Ordinal |
Note: both extractors are pass-through (no ToUpperInvariant). Users must configure
Database=<name> in connection string exactly as the catalog appears.

### Supported Transports
- Supported: SqlServer, PostgreSQL.
- Not supported: Memory, Redis, LiteDb, SQLite.
  These transports have no single-database-transaction model equivalent.
  Capability cast (producer is IRelationalProducerQueue<T>) returns false for all four.
```

### Source material to pull from

| Section | Primary source |
|---------|---------------|
| Overview | PROJECT.md §Description + §Goals |
| Example code shape | PROJECT.md §Functional New Public API (capability-cast snippet) |
| Lifecycle contract | PROJECT.md §Ownership & Threading Contract |
| Retry contract | PROJECT.md §Functional — Internal Implementation (Polly bypass) + IRetrySkippable.cs |
| Schema prerequisite | PROJECT.md §Non-Goals ("Auto-creation... wrong contract for outbox") |
| DB-name semantics | SqlServerExternalDbNameExtractor.cs remarks + PostgreSqlExternalDbNameExtractor.cs remarks |
| Supported transports | Phase 5 SUMMARY-1.1.md + Phase 5 SUMMARY-1.2.md |

---

## §4. README Integration Point

Decision 3 (CONTEXT-7): one bullet under "High-level features", after the cron job-scheduler
bullet.

### Exact surrounding context (verbatim, lines 10–16 of README.md)

```
**High-level features:**
- Queue / de-queue POCOs for distributed processing
- Queue / process compiled LINQ expressions
- Re-occurring job scheduler

See the [Wiki](https://github.com/blehnen/DotNetWorkQueue/wiki) for in-depth documentation.
```

### Insertion

The new bullet inserts after line 13 ("- Re-occurring job scheduler"):

```
- Transactional outbox pattern on SqlServer / PostgreSQL (see [`docs/outbox-pattern.md`](docs/outbox-pattern.md))
```

The blank line between the bullet list and the "See the Wiki" line must be preserved.
The builder agent Edit call: find `- Re-occurring job scheduler\n` and replace with
`- Re-occurring job scheduler\n- Transactional outbox pattern on SqlServer / PostgreSQL (see [\`docs/outbox-pattern.md\`](docs/outbox-pattern.md))\n`.

---

## §5. Source Link / Release Build Verification

### Command

```bash
dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release -p:CI=true
```

### Does `-p:CI=true` suppress XML-doc warnings?

No. `-p:CI=true` sets `ContinuousIntegrationBuild=true` in `Directory.Build.props` which
enables deterministic Source Link path embedding. It does NOT affect `TreatWarningsAsErrors`,
`DocumentationFile`, or any CS1591 handling. XML-doc warnings are surfaced identically with
and without `-p:CI=true`.

### Does the Release build already enable XML doc generation?

Yes — per per-project `.csproj` inspection (§2 above). `DocumentationFile` is set in the
Release condition blocks. No `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
needs to be added to any project.

### Gap to fix before Phase 7 build gate is valid

`Transport.RelationalDatabase.csproj` is missing the `Release|net8.0` condition block
(documented in §2). Without this fix the net8.0 TFM of the shared foundation layer is not
covered by the XML-doc gate. The build task must add this block before running the final
verification.

### Pre-existing blocker independent of Phase 7

ISSUE-032: `Transport.SQLite.csproj` escalates `NU1902` (OpenTelemetry advisory) to an error
under `TreatWarningsAsErrors` on Release builds. This causes the full-solution Release build
to fail before Phase 7 work is evaluated. The architect should document this as a pre-existing
blocker and plan the verification against the four outbox projects' individual Release builds,
or add `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` to `Transport.SQLite.csproj`
as a scoped fix.

---

## §6. ISSUE Intersections

| Issue | Status | Phase 7 relevance |
|-------|--------|-------------------|
| ISSUE-032 | Open | **Directly blocks** full-solution Release build (`-c Release -p:CI=true`). NU1902 on `Transport.SQLite` is a pre-existing error. XML-doc warnings will be mixed with this error in build output. Architect must either scope the verification to individual projects or add `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` to Transport.SQLite.csproj. This is NOT a Phase 7 root cause — it predates Phase 1. |
| ISSUE-033 | Open | Fork smoke-test end-bound overreaches into sibling helpers. Not Phase 7 scope; no doc impact. |
| ISSUE-034 | Open | Fragile relative path in fork smoke tests. Not Phase 7 scope. |
| ISSUE-035 | Open | Duplicated path-resolution block in smoke tests. Not Phase 7 scope. |
| ISSUE-036 | Resolved | `Tx → Transaction` rename complete (commit `9858f04f`). Doc and code are consistent; no carryover. |
| ISSUE-037 | Open | Priority round-trip coverage gap in AdditionalData tests. Not Phase 7 scope; deferred post-ship. |
| ISSUE-038 | Resolved | PG `tx` variable name fixed (commit `ef848165`). No carryover. |

No open issues touch documentation files, README, or release-build flow beyond ISSUE-032.

---

## §7. Risks and Pitfalls

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| `Transport.RelationalDatabase` net8.0 TFM has no Release XML-doc gate | High (confirmed missing) | Med — net8.0 artifact ships without CS1591 enforcement | Add `Release|net8.0` condition block to the csproj as Task 0 of the builder plan |
| ISSUE-032 makes full-solution Release build fail before Phase 7 work is evaluated | High (confirmed) | Med — obscures Phase 7 success signal | Either scope verification to per-project Release builds, or fix ISSUE-032 (`<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` in Transport.SQLite.csproj) as Phase 7 Task 0 |
| `-p:CI=true` mistakenly assumed to affect warning surfacing | Low | Low — CLAUDE.md lesson documented; researcher confirmed it does not suppress CS1591 | Builder plan must cite this research; use the same flag as publish.yml for Source Link fidelity |
| `<inheritdoc />` on override methods in `SqlServerRelationalProducerQueue` / `PostgreSqlRelationalProducerQueue` does not satisfy CS1591 when the base interface member IS documented | Low | Low — `<inheritdoc />` is accepted by the compiler as satisfying CS1591 on overrides | Confirmed: CS1591 is not raised for `<inheritdoc />` on overrides of documented members |
| WSL CRLF line-ending warnings during Edit on `.cs` files | High (CLAUDE.md lesson) | None — `.gitattributes` normalizes on commit | Builder should accept the cosmetic git warning; no action needed |
| Doc style divergence from `docs/jenkins-setup.md` | Low | Low | Builder agent must read `docs/jenkins-setup.md` before writing `docs/outbox-pattern.md` |

---

## Uncertainty Flags

- **Decision Required (ISSUE-032 scope):** The architect must decide whether Phase 7 fixes
  ISSUE-032 (`WarningsNotAsErrors` in Transport.SQLite) to enable a clean full-solution
  Release build, or whether Phase 7 verifies only the four outbox projects individually.
  Both are valid; the choice affects the plan task list.

- **`RelationalDatabase` net8.0 XML-doc coverage:** Confirmed missing from csproj. The
  builder must add the block. If there are any net8.0-only CS1591 gaps this is the only
  way to find them. The architect should make this an explicit task, not a footnote.

## Sources

1. `/mnt/f/git/dotnetworkqueue/.shipyard/ROADMAP.md` — Phase 7 description and success criteria
2. `/mnt/f/git/dotnetworkqueue/.shipyard/phases/7/CONTEXT-7.md` — locked decisions 1–3
3. `/mnt/f/git/dotnetworkqueue/.shipyard/PROJECT.md` — functional spec, ownership contract, success criteria
4. `/mnt/f/git/dotnetworkqueue/.shipyard/ISSUES.md` — ISSUE-032 through ISSUE-038
5. `/mnt/f/git/dotnetworkqueue/.shipyard/phases/2/results/SUMMARY-1.1.md` — Phase 2 Wave 1 (ExternalTransaction property, IRetrySkippable)
6. `/mnt/f/git/dotnetworkqueue/.shipyard/phases/2/results/SUMMARY-2.1.md` — Phase 2 Wave 2a (IExternalDbNameExtractor, ExternalTransactionValidator)
7. `/mnt/f/git/dotnetworkqueue/.shipyard/phases/2/results/SUMMARY-2.2.md` — Phase 2 Wave 2b (RelationalSendMessageCommand, IRelationalProducerQueue, RelationalProducerQueue)
8. `/mnt/f/git/dotnetworkqueue/.shipyard/phases/3/results/SUMMARY-1.1.md` — Phase 3 Wave 1 (SqlServerExternalDbNameExtractor, SqlServerRelationalProducerQueue, DI wiring)
9. `/mnt/f/git/dotnetworkqueue/.shipyard/phases/3/results/SUMMARY-2.1.md` — Phase 3 Wave 2 sync handler fork
10. `/mnt/f/git/dotnetworkqueue/.shipyard/phases/3/results/SUMMARY-2.2.md` — Phase 3 Wave 2 async handler fork
11. `/mnt/f/git/dotnetworkqueue/.shipyard/phases/4/results/SUMMARY-1.1.md` — Phase 4 Wave 1 (PostgreSqlExternalDbNameExtractor, PostgreSqlRelationalProducerQueue, DI wiring)
12. `/mnt/f/git/dotnetworkqueue/.shipyard/phases/4/results/SUMMARY-2.1.md` — Phase 4 Wave 2 sync handler fork
13. `/mnt/f/git/dotnetworkqueue/.shipyard/phases/4/results/SUMMARY-2.2.md` — Phase 4 Wave 2 async handler fork
14. Source files: `IRelationalProducerQueue.cs`, `IRetrySkippable.cs`, `IExternalDbNameExtractor.cs`, `RelationalSendMessageCommand.cs`, `ExternalTransactionValidator.cs`, `RelationalProducerQueue.cs`, `SqlServerExternalDbNameExtractor.cs`, `SqlServerRelationalProducerQueue.cs`, `PostgreSqlExternalDbNameExtractor.cs`, `PostgreSqlRelationalProducerQueue.cs`, `SendMessageCommand.cs` — direct read for XML-doc presence
15. `Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj` — DocumentationFile / TreatWarningsAsErrors gating
16. `Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj` — same
17. `Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj` — same
18. `Source/DotNetWorkQueue.Transport.Shared/DotNetWorkQueue.Transport.Shared.csproj` — same
19. `Source/Directory.Build.props` — CI flag, no global GenerateDocumentationFile
20. `README.md` lines 10–16 — exact insertion context for §4
21. `docs/jenkins-setup.md` — style reference for `docs/outbox-pattern.md`
