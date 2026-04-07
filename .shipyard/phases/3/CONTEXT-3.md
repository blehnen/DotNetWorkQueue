# Phase 3 Context — Linq Integration Test Cleanup

## Decisions

### Research: Skipped
ROADMAP already has exhaustive file lists for all 6 sub-phases. Pattern is identical to Phase 2 (remove `#if NETFULL` blocks containing `LinqMethodTypes.Dynamic` test cases, remove net48 from csproj TargetFrameworks).

### Execution Strategy: Direct execution
Builder agents exhaust context on bulk file edits (confirmed in Phase 1 and Phase 2). Direct execution via Perl regex + csproj edits is faster and more reliable. Same approach used successfully in Phase 2.

### Sub-phase parallelism
All 6 sub-phases (3a-3f) are independent and can be planned as a single wave. Each touches a different project directory with no shared files.
