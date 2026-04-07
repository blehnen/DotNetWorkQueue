# Phase 1 Context: Dashboard API History Tests

## Decisions

- **Pattern:** Follow MemoryHistoryTests.cs exactly (both Disabled + Enabled classes per transport)
- **Scope:** Redis + LiteDb only (other transports already have history tests)
- **CI:** Redis tests run in Jenkins only (connection string gated). LiteDb runs everywhere.
- **No production changes:** Test-only files
- **Skip research:** Existing test patterns already read during brainstorming
