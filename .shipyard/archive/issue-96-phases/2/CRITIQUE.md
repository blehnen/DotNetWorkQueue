# Phase 2 Plan Critique
**Phase:** Phase 2 -- Health Monitoring and Source-Aware Page Routing
**Date:** 2026-04-09
**Type:** plan-review

## Part 1: Coverage Matrix

| # | Success Criterion (ROADMAP) | Covered By | Status |
|---|----------------------------|------------|--------|
| 1 | `ISourceHealthMonitor.GetHealth(slug)` returns `SourceHealthState` with status, timestamp, error | PLAN-1.1 Task 1-2 | COVERED |
| 2 | Background service polls all sources every 30s with 5s timeout | PLAN-1.1 Task 2 | COVERED |
| 3 | Health state transitions logged at Information level | PLAN-1.1 Task 2 (test 7: `PollAsync_Logs_State_Transitions`) | COVERED |
| 4 | All page routes include `{SourceSlug}` parameter | PLAN-2.1 Tasks 1-3 | COVERED |
| 5 | Single-source: `"/"` redirects to `"/source/{slug}"`, flat connection list shown | PLAN-2.1 Task 1 (Home.razor `OnParametersSetAsync`) | COVERED |
| 6 | All write operations route to correct source (verified by source slug in URL) | PLAN-2.1 Tasks 1-3 (per-source `IDashboardApiClient` passed to tabs) | COVERED |
| 7 | Source slug never exposes BaseUrl or ApiKey in any URL or rendered HTML | PLAN-2.1 (slugs derived from Name, not BaseUrl; no BaseUrl in template) | COVERED |
| 8 | Breadcrumbs include source name as first crumb (multi-source) or omit it (single-source) | PLAN-2.1 Tasks 1-3 (conditional breadcrumb logic in each page) | COVERED |
| 9 | All 7 shared tab components work unchanged (receive per-source `IDashboardApiClient` via `[Parameter]`) | PLAN-2.1 (pages resolve Api field, pass to tabs via `Api="Api"`) | COVERED |
| 10 | `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` succeeds with 0 errors | PLAN-1.1 Task 3, PLAN-2.1 Task 3 | COVERED |
| 11 | Unit tests pass for health transitions, source resolution, navigation URLs | PLAN-1.1 Task 2 (7 health tests) | PARTIAL -- see G1 |

| # | Additional Criterion (CONTEXT-2) | Covered By | Status |
|---|----------------------------------|------------|--------|
| A1 | Multi-source `/` shows basic source list with health indicators | PLAN-2.1 Task 1 (MudGrid with health icons) | COVERED |
| A2 | Invalid slug: MudAlert error on page | PLAN-2.1 Tasks 1-3 (all pages have `_sourceError` block) | COVERED |

## Part 2: Per-Plan Feasibility Analysis

### PLAN-1.1: Health Monitoring Service with Unit Tests (Wave 1)

**File paths exist:**
| File | Action | Exists | Verdict |
|------|--------|--------|---------|
| `Services/ISourceHealthMonitor.cs` | new | N/A | OK |
| `Services/SourceHealthMonitor.cs` | new | N/A | OK |
| `Services/SourceHealthState.cs` | new | N/A | OK |
| `Program.cs` | modify | YES (168 lines) | OK |
| `Ui.Tests/Services/SourceHealthMonitorTests.cs` | new | N/A | OK |

**API surface matches:**
- `IMultiSourceDashboardApiClient.GetClientForSource(slug)` -- EXISTS at `MultiSourceDashboardApiClient.cs:55`. Returns `IDashboardApiClient`. Throws `KeyNotFoundException` on unknown slug (line 70). MATCH.
- `ISourceRegistry.GetAll()` -- EXISTS at `SourceRegistry.cs:72`. Returns `IReadOnlyList<DashboardApiSourceConfig>`. MATCH.
- `IDashboardApiClient.GetSettingsAsync()` -- EXISTS at `IDashboardApiClient.cs:29`. Returns `Task<DashboardSettingsResponse?>`. MATCH.
- Test project `DotNetWorkQueue.Dashboard.Ui.Tests` -- EXISTS with 5 test files and 40 passing tests (net10.0 + net8.0). MATCH.

**Verify commands runnable:**
- `dotnet build "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj"` -- valid, confirmed builds (0 errors).
- `dotnet test "Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj" --filter "FullyQualifiedName~SourceHealthMonitorTests"` -- valid filter syntax.
- `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` -- valid.

**DI registration pattern (Task 3):**
- Plan registers as `AddSingleton<ISourceHealthMonitor, SourceHealthMonitor>()` then `AddHostedService(sp => (SourceHealthMonitor)sp.GetRequiredService<ISourceHealthMonitor>())`. This is a standard pattern. `BackgroundService` inherits `IHostedService`. CORRECT.
- Insertion point: "after the existing `IMultiSourceDashboardApiClient` singleton registration (around line 79)". Actual line 79 in Program.cs: `builder.Services.AddSingleton<IMultiSourceDashboardApiClient, MultiSourceDashboardApiClient>();`. MATCH.

**Complexity:** 5 files, 2 directories. Well within bounds.

**Issues:**
- PLAN-1.1 uses `GetSettingsAsync()` as the health probe. This is a lightweight endpoint returning dashboard settings. REASONABLE. However, the ROADMAP says "polls each source's `GET /api/v1/dashboard/connections`" (line 84 of ROADMAP.md). The plan deviates by using `GetSettingsAsync` instead of `GetConnectionsAsync`. This is a LOW-impact discrepancy -- `GetSettingsAsync` is actually lighter weight and more appropriate. The builder should note this deviation.

**Verdict: READY**

---

### PLAN-2.1: Source-Aware Page Routing and Navigation (Wave 2)

**File paths exist:**
| File | Action | Exists | Verdict |
|------|--------|--------|---------|
| `Components/Pages/Home.razor` | modify | YES (85 lines) | OK |
| `Components/Pages/ConnectionDetail.razor` | modify | YES (157 lines) | OK |
| `Components/Pages/QueueDetail.razor` | modify | YES (272 lines) | OK |

**API surface matches:**
- `IMultiSourceDashboardApiClient` -- EXISTS. `GetClientForSource(slug)` at line 55, throws `KeyNotFoundException`. MATCH with plan's `catch (KeyNotFoundException)` blocks.
- `ISourceRegistry.GetAll()` -- EXISTS, returns `IReadOnlyList<DashboardApiSourceConfig>`. Plan accesses `.Count` and index `[0].Slug` -- `IReadOnlyList` supports both. MATCH.
- `ISourceRegistry.GetBySlug(slug)` -- EXISTS, returns `DashboardApiSourceConfig?`. Plan accesses `?.Name`. MATCH.
- `ISourceHealthMonitor` -- DOES NOT EXIST YET (created by PLAN-1.1, Wave 1). Dependency correctly declared: `dependencies: ["1.1"]`. OK.
- `SourceHealthStatus` enum -- DOES NOT EXIST YET (created by PLAN-1.1). Used in Home.razor template. OK given wave ordering.

**Current page structure vs. plan assumptions:**

*Home.razor:*
- Plan says "replace `@inject IDashboardApiClient Api`" -- current line 2: `@inject IDashboardApiClient Api`. MATCH.
- Plan says "replace `OnInitializedAsync`" -- current line 58: `protected override async Task OnInitializedAsync()`. MATCH.
- Plan says remove `[Inject] private NavigationManager Navigation` from code block -- current line 52: `[Inject] private NavigationManager Navigation { get; set; } = default!;`. MATCH.
- `ConnectionResponse` type used in plan -- imported via `_Imports.razor` line 14: `@using DotNetWorkQueue.Dashboard.Ui.Models`. MATCH.

*ConnectionDetail.razor:*
- Plan says "replace `@inject IDashboardApiClient Api`" -- current line 2. MATCH.
- Plan says "replace `OnInitializedAsync`" -- current line 110. MATCH.
- Plan references `_breadcrumbs` field -- current line 104. MATCH.
- Plan references `NavigateToQueue` method -- current line 151. MATCH.
- Plan says update breadcrumb `href: "/"` to `href: $"/source/{SourceSlug}"` -- current line 106-107 uses `href: "/"`. MATCH.

*QueueDetail.razor:*
- Plan says "replace `@inject IDashboardApiClient Api`" -- current line 2. MATCH.
- Plan says "replace `OnInitializedAsync`" -- current line 141. MATCH.
- Plan references `_connectionId` field -- current line 129. MATCH.
- Plan references `_initialLoading` field -- current line 114. MATCH.
- Plan references connection link `Href="@($"/connections/{_connectionId}")"` at "around line 15" -- current line 15. MATCH.

**Forward references:** PLAN-2.1 depends on PLAN-1.1. Different waves (Wave 2 depends on Wave 1). No intra-wave dependencies. CORRECT.

**Hidden dependencies / shared files:**
- `Program.cs` is touched by both PLAN-1.1 (Task 3: add hosted service) and PLAN-2.1 -- but PLAN-2.1 does NOT touch Program.cs (it's not in files_touched). Only PLAN-1.1 modifies Program.cs. NO CONFLICT.

**Verify commands runnable:**
- `dotnet build "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj"` -- valid.
- `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` -- valid.
- `dotnet test "Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj"` -- valid.
- `dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" --filter "FullyQualifiedName~Memory"` -- valid project path.

**Complexity:** 3 files, 1 directory. Low complexity. However, the 3 Blazor pages are substantial rewrites (Home: 85 -> ~150+ lines, QueueDetail: 272 -> ~300+ lines). The rewrites are well-specified with exact code snippets. ACCEPTABLE.

**Issues found:**

1. **MEDIUM -- No unit tests for source resolution or navigation URLs in PLAN-2.1.** Success criterion #11 requires "Unit tests pass for health transitions, source resolution, navigation URLs." PLAN-1.1 covers health transition tests (7 tests). But PLAN-2.1 has `tdd: false` and creates NO test files. Source resolution logic (slug validation, redirect behavior) and navigation URL generation (including `/source/{slug}` prefix) have zero test coverage in either plan. These are pure Blazor component concerns that are harder to unit test without bUnit, but the criterion explicitly requires them.

2. **LOW -- `_Imports.razor` does not need updates.** The `DotNetWorkQueue.Dashboard.Ui.Services` namespace (line 16 of `_Imports.razor`) is already imported, so `IMultiSourceDashboardApiClient`, `ISourceRegistry`, `ISourceHealthMonitor`, and `SourceHealthStatus` will all be in scope for Razor pages. No action needed. CONFIRMED.

3. **LOW -- ConnectionDetail.razor guard for `ConnectionId` parameter change.** The plan replaces `OnInitializedAsync` with `OnParametersSetAsync` and guards on `_lastSourceSlug`. But `ConnectionId` could also change via navigation without changing `SourceSlug`. When navigating from one connection to another within the same source, the slug guard would skip re-resolution (correct -- same client) but `LoadData()` would NOT be called (incorrect -- different connection). The plan does not address this. Current code uses `OnInitializedAsync` which only fires once, so this is not an existing regression -- but the switch to `OnParametersSetAsync` introduces a new code path that should handle both `SourceSlug` and `ConnectionId` changes.

4. **LOW -- QueueDetail.razor same guard issue with `QueueId`.** Same pattern: the `_lastSourceSlug` guard would skip re-loading when `QueueId` changes but `SourceSlug` stays the same. The plan's `OnParametersSetAsync` only guards on slug, not on `QueueId`. A user navigating between queues in the same source would see stale data.

5. **LOW -- Health probe endpoint discrepancy.** ROADMAP says health check uses `GET /api/v1/dashboard/connections`. PLAN-1.1 uses `GetSettingsAsync()` (GET api/v1/dashboard/settings). `GetSettingsAsync` is lighter weight but deviates from the spec. Either update the roadmap or update the plan.

## Gaps

- **G1 (MEDIUM): No unit tests for source resolution or navigation URL generation.** Success criterion #11 explicitly requires "source resolution, navigation URLs" tests. Neither plan creates them. PLAN-2.1 is `tdd: false` with zero test files. Recommend adding a test file (e.g., `SourceResolutionTests.cs` or `NavigationUrlTests.cs`) that validates: (a) slug extraction from route parameter, (b) invalid slug produces error state, (c) navigation URLs contain `/source/{slug}` prefix. These could be plain C# tests that exercise helper methods extracted from the pages, or bUnit tests.

- **G2 (MEDIUM): `OnParametersSetAsync` guard only checks slug, not entity IDs.** ConnectionDetail and QueueDetail pages guard re-execution on `_lastSourceSlug` but do not guard on `ConnectionId`/`QueueId` changing. If a user navigates from one connection to another within the same source, `OnParametersSetAsync` fires with a new `ConnectionId` but the same `SourceSlug`, and the guard skips `LoadData()`. This would show stale data. Fix: guard on both slug AND entity ID changes.

- **G3 (LOW): Health probe endpoint mismatch.** ROADMAP specifies `connections` endpoint; plan uses `settings` endpoint.

## Recommendations

1. **(MUST)** Add unit tests for source resolution and navigation URL generation to satisfy criterion #11. Either add a test task to PLAN-2.1 or create a separate PLAN-2.2 for test coverage.

2. **(MUST)** Fix the `OnParametersSetAsync` guard in ConnectionDetail.razor (PLAN-2.1 Task 2) and QueueDetail.razor (PLAN-2.1 Task 3) to also check for entity ID changes. Example fix for ConnectionDetail:
   ```csharp
   private Guid _lastConnectionId;
   // In OnParametersSetAsync:
   if (string.Equals(SourceSlug, _lastSourceSlug, StringComparison.Ordinal)
       && ConnectionId == _lastConnectionId)
       return;
   _lastSourceSlug = SourceSlug;
   _lastConnectionId = ConnectionId;
   ```
   Same pattern for QueueDetail with `_lastQueueId`.

3. **(SHOULD)** Resolve the health probe endpoint discrepancy (G3). Either update the plan to use `GetConnectionsAsync()` per the roadmap, or update the roadmap to reflect `GetSettingsAsync()` as the lighter-weight choice. Both work; they just need to agree.

## Phase 1 Regression Check

| # | Phase 1 Criterion | Status | Evidence |
|---|-------------------|--------|----------|
| R1 | Dashboard UI builds | PASS | `dotnet build` succeeded with 0 errors, 0 warnings. |
| R2 | Phase 1 unit tests pass | PASS | 40/40 tests pass on both net10.0 and net8.0. |
| R3 | Phase 1 services exist | PASS | All 8 service files confirmed via glob: `DashboardApiSourceConfig.cs`, `ISourceRegistry.cs`, `SourceRegistry.cs`, `IMultiSourceDashboardApiClient.cs`, `MultiSourceDashboardApiClient.cs`, `DashboardConfigParser.cs`, `LocalSourceHostedService.cs`, `DashboardApiClient.cs`. |

## Deferred Issues Check

Reviewed `.shipyard/ISSUES.md`. Open issues ISSUE-016/017/018 (Redis PurgeMessageHistory), ISSUE-019 (missing summary), ISSUE-020 (double-dispose) are all unrelated to Phase 2 scope. No deferred items require attention for this phase.

## Verdict

**CAUTION** -- Plans provide complete coverage of 11/11 success criteria and 2/2 additional criteria from CONTEXT-2. All file paths verified against the real codebase. Phase 1 API surfaces match plan assumptions exactly. Wave ordering is correct with no intra-wave dependencies or file conflicts. Two actionable gaps must be addressed: (1) missing unit tests for source resolution/navigation URLs (criterion #11), and (2) `OnParametersSetAsync` guards that only check slug but not entity IDs, which will cause stale data when navigating between connections/queues within the same source. Neither gap requires plan restructuring -- both can be fixed by adding a test task and adjusting 4 lines in Tasks 2-3 of PLAN-2.1.
