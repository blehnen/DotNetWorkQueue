# Build Summary: Plan 1.2 (Phase 7 Wave 1 — docs/outbox-pattern.md)

## Status: complete

## Tasks Completed

- Task 1: Authored `docs/outbox-pattern.md` (204 lines) per CONTEXT-7 Decision 2 (tutorial + reference hybrid, ONE worked example). All 8 required headings present (Title/Intro + Tutorial + Reference §1-5 + section structure). Tutorial uses SqlServer commit path as the canonical worked example; PostgreSQL variation mentioned inline as prose (no duplicate code block).

## Commits

| SHA | Task | Subject |
|---|---|---|
| `b6c967ae` | 1 | `shipyard(phase-7): add docs/outbox-pattern.md (outbox tutorial + reference)` |

## Files Created

- `docs/outbox-pattern.md` — NEW (204 lines)

## Decisions Made

- **Single ` ```csharp ` fence preserved.** Initial draft had two C# code blocks (tutorial + schema-deployment section). Per CONTEXT-7 Decision 2's "ONE worked example" rule, the schema-deployment section was converted to numbered prose steps. Single C# fence retained for the canonical SqlServer commit tutorial.
- **`## Overview` heading added.** Initial draft had inline paragraphs after the title; added `## Overview` heading to match `docs/jenkins-setup.md` style reference (every major section gets H2).

## Issues Encountered

- **DOCUMENTATION CORRECTION (load-bearing):** PROJECT.md and CONTEXT-7 Decision 2 + RESEARCH.md §3 described "Per-provider DB-name comparison semantics (OrdinalIgnoreCase vs Ordinal)". **Reading the actual source code reveals BOTH transports use `StringComparer.Ordinal`** for the DB-name compare. The pass-through approach (Phase 3 commit `994e1404` for SqlServer; Phase 4 native for PostgreSQL) made the comparison symmetric: both extractors are now byte-verbatim pass-through, and both validators compare with `Ordinal`. The doc reflects the IMPLEMENTATION, not the outdated prompt language.
- **Implication:** PROJECT.md's §Diagnostics / §Functional Implementation may still describe the original (pre-Phase-3-fix) asymmetric design. **This is a separate ISSUE candidate** — track for Phase 7 close-out review or post-ship cleanup.

## Verification Results

| Check | Result |
|---|---|
| All 8 required headings present | yes |
| `IRelationalProducerQueue` mentioned in overview, code example, prerequisites, supported-transports | 4 occurrences |
| `SqlServerExternalDbNameExtractor` referenced in DB-name semantics section | yes |
| `PostgreSqlExternalDbNameExtractor` referenced in DB-name semantics section | yes |
| Exactly one ` ```csharp ` fence | yes (schema-deployment converted to prose) |
| Document free of emojis | yes (ASCII-only) |
| File size in 200-400 line range | 204 lines |

## Hand-off to PLAN-2.1 (Wave 2)

- The README pointer at `[`docs/outbox-pattern.md`](docs/outbox-pattern.md)` (committed in `af4fee60` by PLAN-1.3) now resolves to this real file. PLAN-2.1 Task 3 (link resolution check) will pass.
- **Follow-up ISSUE candidate:** PROJECT.md needs review for "OrdinalIgnoreCase vs Ordinal" language — Phase 3 pass-through fix made it Ordinal-on-both-sides. Suggest tracking as ISSUE-039 and bundling with future PROJECT.md maintenance.
