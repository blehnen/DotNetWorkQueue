## Current Task

Issue #96: Dashboard UI — Support Multiple API Sources. Brainstorming complete, PROJECT.md and ROADMAP.md committed and approved at `3aad6034`. No implementation work has started. Ready to plan Phase 1.

## Approach

**UI-side aggregation** — the Dashboard UI manages multiple `HttpClient` instances (one per API source) and merges results for display. Each Dashboard API instance is standalone and unmodified; transport logic stays in the API layer.

Key design decisions already made:
- **Source-aware URL routing**: `/source/{sourceSlug}/connection/{id}/queue/{queueId}` — slug derived from configured Name, never exposes BaseUrl or ApiKey
- **`IMultiSourceDashboardApiClient.GetClientForSource(slug)`** returns a per-source `IDashboardApiClient` — all 7 shared tab components stay unchanged, only 3 page components resolve source from URL
- **In-process API is just another source** — one code path, no special cases, registers as "Local" (or configurable name)
- **Background health polling** (30s interval, 5s timeout) — cached health state, page loads never block on unhealthy sources
- **Breaking config change** — old flat `DashboardApi:BaseUrl`/`ApiKey` removed, replaced by `DashboardApi:Sources[]` array. Startup validation detects old format and throws clear error with migration instructions
- **Home page grouping** — connections grouped under collapsible source headers when multiple sources; single source hides the group header entirely
- **Full read + write** across all sources from day one
- **Partial failure** — show data from healthy sources, inline warning for offline sources, retry button

## Tried

- Explored current Dashboard architecture: `DashboardApiClient` (26 methods, single `HttpClient`), `IDashboardApiClient` interface, 3 Blazor pages, 7 shared tab components, `DashboardTestServer` for integration tests
- Verified all 6 transports have Dashboard API integration test coverage (38 test classes total) — no gaps
- Created PROJECT.md and ROADMAP.md — both committed on master at `3aad6034`

## Remaining

1. **Run `/shipyard:plan 1`** — Plan Phase 1: Multi-Source Configuration and Client Infrastructure (config model, source registry, multi-source client wrapper, DI refactor, startup validation, unit tests)
2. Execute Phase 1 implementation
3. Plan and execute Phase 2: Health Monitoring and Source-Aware Page Routing
4. Plan and execute Phase 3: Home Page Grouping, Partial Failure UX, and Integration Tests

## Open Questions

- None — all design decisions are captured and approved in PROJECT.md and ROADMAP.md
