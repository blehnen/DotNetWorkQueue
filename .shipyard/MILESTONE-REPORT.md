# Milestone Report: Drop net48/netstandard2.0 (Issue #101)

**Completed:** 2026-04-07
**Version:** 0.9.19
**Phases:** 4/4 complete
**Branch:** issue-101-drop-net48

## Overview

Removed .NET Framework 4.8 and .NET Standard 2.0 targets from the entire DotNetWorkQueue solution. Deleted all `#if NETFULL` / `#if NETSTANDARD2_0` conditional compilation, removed vendored JpLabs.DynamicCode, cleaned up Schyntax net48/netstandard2.0 DLLs, updated CI, updated documentation. Breaking change: dynamic LINQ expressions are no longer supported.

## Phase Summaries

### Phase 1: Core Library, Transport Libraries, and Vendored DLL Cleanup
Removed net48/netstandard2.0 from DotNetWorkQueue.csproj and 8 transport library csproj files. Removed `#if NETFULL` blocks from 10 core .cs files. Deleted Lib/JpLabs.DynamicCode/, Lib/Schyntax/net48/, Lib/Schyntax/netstandard2.0/.

### Phase 2: Shared Test Infrastructure and Unit Tests
Removed net48/NETFULL from IntegrationTests.Shared (19 .cs + 1 csproj) and 13 test/integration csproj files. All 878 unit tests pass.

### Phase 3: Linq Integration Tests
Removed net48/NETFULL from all 6 Linq integration test projects: SqlServer, PostgreSQL, SQLite, Redis, LiteDB, Memory. 103 files changed, ~1500 lines deleted.

### Phase 4: CI, Documentation, and Version Bump
Updated GitHub Actions CI (ubuntu-latest, forward-slash paths), README.md, CLAUDE.md. Bumped version 0.9.18 -> 0.9.19. Added CHANGELOG entry. Resolved ISSUE-021 (7 empty shell files), ISSUE-022 (vestigial dynamic parameter), ISSUE-023 (cosmetic).

## Key Decisions
- Version bumped to 0.9.19 (not 0.9.3 as originally planned -- version was already 0.9.18)
- ISSUE-022 full cleanup: removed `bool dynamic` parameter entirely from shared JobSchedulerTests and all callers
- CI moved to ubuntu-latest since net48 no longer requires Windows
- Direct execution preferred over builder agents for bulk file edits (agents exhaust context)

## Metrics
- Files changed: 223
- Lines added: 2,813 (mostly .shipyard artifacts)
- Lines removed: 3,231
- Total commits: 28
- Unit tests: 878 passed
- Integration tests (Memory): 20 passed
