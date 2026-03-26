# Shipyard History

## 2026-03-26 — Project Initialized

- **Action:** `/shipyard:init`
- **Settings:** Interactive mode, manual git, detailed review, all-Opus routing
- **Status:** Ready for planning

## 2026-03-26 — Project Definition & Roadmap

- **Action:** `/shipyard:brainstorm`
- **Scope:** Security & Stability Fixes — C-1, C-2, H-2, H-5, H-7
- **Decisions:**
  - C-1: Deny-list binder as default (non-breaking), optional allow-list binder for maximum lockdown
  - C-2: Wiki security page (documentation only, no runtime changes)
  - H-2: Per-transport queue name validation (not in base class)
  - H-5: Implement IAsyncDisposable on DashboardConsumerClient
  - H-7: Remove IntegrationTests.Metrics project (replace stub types with core NoOp types)
- **Roadmap:** 5 phases, Phases 1-4 parallel, Phase 5 depends on Phase 1
- **Status:** Roadmap approved, ready for Phase 1 planning

## 2026-03-26 — Phase 1 Planned

- **Action:** `/shipyard:plan 1`
- **Plans:** 3 plans across 2 waves
  - Wave 1: PLAN-1.1 (DenyListSerializationBinder + tests) | PLAN-1.2 (AllowListSerializationBinder + tests)
  - Wave 2: PLAN-2.1 (Wire into serializers, DI registration, integration tests)
- **Design Decisions:** Deserialization-only protection, method-based extensibility, leave test helper as-is
- **Critique Verdict:** READY — all file paths verified, API surface confirmed, baseline tests green
- **Status:** Ready for build
