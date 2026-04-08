# Session Handoff — 2026-04-07

## Current Task

Issue #101 milestone: Drop net48/netstandard2.0. Phases 1 and 2 complete, Phase 3 planned and ready for build.

## Approach

10-phase roadmap across 4 waves. Working on branch `issue-101-drop-net48` off master.

Key decisions:
- Direct execution for mechanical edits (builder agents exhaust context on bulk file changes — confirmed in Phases 1 and 2)
- Skip research for Phase 3 (ROADMAP already exhaustive, pattern identical to Phase 2)
- Perl regex via write-to-/tmp + cp for bulk `#if NETFULL` removal (Perl `-i` fails on WSL due to temp file rename across mount points)
- Audit/simplifier/documenter skipped for mechanical phases — full pipeline runs at ship time
- `LinqExpressionToRun` type preserved for serialization compatibility; `LinqCompiler` throws `NotSupportedException`
- `CompileException` class kept (public type, separate breaking change)

## Tried

- **Phase 1 (complete):** Core csproj + 8 transport csproj + 11 .cs conditional cleanup + vendored DLL deletion. 6 commits.
- **Phase 2 (complete):** IntegrationTests.Shared 19 .cs + 1 csproj, CompileExceptionTests.cs, 14 test/integration csproj. 2 commits.
- **Phase 3 (planned):** 2 plans, 1 wave, 6 tasks. PLAN-1.1 covers SqlServer/PostgreSQL/SQLite (53 .cs + 3 csproj). PLAN-1.2 covers Redis/LiteDB/Memory (44 .cs + 3 csproj). Verifier confirmed READY. Architect corrected ROADMAP file counts (e.g., SqlServer has 18 .cs files with NETFULL, not 13).
- Builder agents ran out of context twice (Phase 1 PLAN-1.1, reviewer). Direct execution proved faster and more reliable.
- AppMetrics.Tests doesn't exist (ROADMAP said 8 unit test csproj, only 7 found).

## Remaining

- **Uncommitted work:** 9 files have uncommitted changes (CLAUDE.md staged, 8 core .cs files unstaged with 220 lines of `#if NETFULL` deletions). Review and commit these before or during Phase 3 build.
- **Phase 3 build** (Wave 3): Run `/shipyard:build 3`. Remove `#if NETFULL` from 6 Linq integration test projects. 97 .cs files + 6 csproj. Direct execution, not builder agents.
- **Phase 4** (Wave 4): CI (.github/workflows/ci.yml), README.md, CLAUDE.md, version bump to 0.9.3. Run `/shipyard:plan 4` after Phase 3.
- **Full solution build** (`DotNetWorkQueue.sln`) currently has 23 NU1201 errors from Phase 3 Linq projects — these resolve once Phase 3 completes.

## Open Questions

- None — design fully captured in PROJECT.md and ROADMAP.md.
