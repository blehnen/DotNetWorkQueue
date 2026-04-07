# Session Handoff — 2026-04-07

## Current Task

Issue #101 milestone: Drop net48/netstandard2.0. Phases 1 and 2 complete, Phase 3 next.

## Approach

10-phase roadmap across 4 waves. Working on branch `issue-101-drop-net48` off master.

Key decisions:
- Direct execution for mechanical edits (builder agents exhaust context on bulk file changes)
- Skip research (ROADMAP already exhaustive)
- Perl regex via write-to-/tmp + cp for bulk `#if NETFULL` removal (Perl `-i` fails on WSL due to temp file rename across mount points)
- Audit/simplifier/documenter skipped for Phase 2 (pure mechanical deletions, full pipeline runs at ship time)
- `LinqExpressionToRun` type preserved for serialization compatibility; `LinqCompiler` throws `NotSupportedException`
- `CompileException` class kept (public type, separate breaking change)

## Tried

- **Phase 1 (complete):** Core csproj + 8 transport csproj + 11 .cs conditional cleanup + vendored DLL deletion. 6 commits. Critique caught missing JpLabs dependency chain (DynamicCodeCompiler, LinqCompiler, ComponentRegistration). Also fixed NU1510 Microsoft.CSharp warning.
- **Phase 2 (complete):** IntegrationTests.Shared 19 .cs + 1 csproj, CompileExceptionTests.cs, 14 test/integration csproj. 2 commits. Full solution build has 23 NU1201 errors from Phase 3 Linq projects (expected — they still target net48).
- Builder agents ran out of context twice (Phase 1 PLAN-1.1, reviewer). Direct execution proved faster and more reliable.
- AppMetrics.Tests doesn't exist (ROADMAP said 8 unit test csproj, only 7 found).
- Redis integration test csproj filename was `Integration.Tests` not `IntegrationTests` — needed glob to find.

## Remaining

- **Phase 3a-3f** (Wave 3): Remove `#if NETFULL` from 6 Linq integration test projects (SqlServer, PostgreSQL, SQLite, Redis, LiteDB, Memory). Same pattern as Phase 2. All 6 can execute in parallel. ~76 .cs files + 6 csproj. Run `/shipyard:plan 3`.
- **Phase 4** (Wave 4): CI (.github/workflows/ci.yml), README.md, CLAUDE.md, SECURITY.md, version bump to 0.9.3. Run `/shipyard:plan 4` after Phase 3.
- **PR #108** already merged to master (confirmed at session start).
- Full solution build (`DotNetWorkQueue.sln`) blocked until Phase 3 completes.
- Currently on `issue-101-drop-net48` branch, 10 commits ahead of master.

## Open Questions

- None — design fully captured in PROJECT.md and ROADMAP.md.
