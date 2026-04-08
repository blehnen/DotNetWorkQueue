# Phase 4 Context — CI, Documentation, and Version Bump

## Decisions

### Open Issues: Include both
Include ISSUE-021 (delete 7 empty shell files from NETFULL removal) and ISSUE-022 (fix no-op dynamic test parameter) as part of this phase. This is the final cleanup phase.

### Version Bump: Version bump + CHANGELOG entry
Update version to 0.9.3 in csproj AND add a CHANGELOG.md entry summarizing the net48/netstandard2.0 removal breaking change.

### Unstaged Changes: Sweep and commit
Check for any uncommitted changes from prior phases (core .cs files like ASendJobToQueue, CompileException, etc.) and include them in this phase.

### Execution Strategy: Direct execution preferred
Builder agents exhaust context on bulk edits (confirmed phases 1-3). Direct execution is faster and more reliable for the small number of files in this phase.
