# Plan Critique: Phase 3

## Verdict: CAUTION

## Coverage Matrix

| Criterion | Plan/Task | Status |
|-----------|-----------|--------|
| 1. Grouped connections under panels | PLAN-1.1 Task 2 | Covered |
| 2. Single source flat list | PLAN-1.1 Task 2 | Covered (preserved from Phase 2) |
| 3. Offline source warning + Retry | PLAN-1.1 Task 2 | Covered |
| 4. Per-source error, others unaffected | PLAN-1.1 Task 2 | Covered |
| 5. Integration: 2+ Memory instances | PLAN-1.2 Task 1 | Covered |
| 6. Integration: write routing | PLAN-1.2 Task 1 | Covered |
| 7. Integration: offline health transitions | PLAN-1.2 Task 2 | Covered |
| 8. Existing 38+ tests pass | PLAN-1.2 Task 3 | Covered |
| 9. Debug build 0 errors | Both plans verification | Covered |
| 10. Release build 0 errors 0 warnings | PLAN-1.1 verification | Covered |
| 11. No secrets in URLs/HTML | PLAN-1.1 (inherited from Phase 2) | Covered |

## Per-Plan Findings

### PLAN-1.1: SourceConnectionGroup + Home.razor
- **File paths:** Home.razor exists, Services/ directory exists for new model. PASS.
- **API surface:** `IMultiSourceDashboardApiClient.GetAllSources()`, `GetClientForSource()`, `ISourceHealthMonitor.GetHealth()` all confirmed. PASS.
- **No conflicts with PLAN-1.2:** PLAN-1.1 touches Home.razor + new model file. PLAN-1.2 touches new test files only. PASS.
- **Complexity:** PLAN-1.1 Task 2 (Home.razor rewrite) is large — the multi-source grouped display with parallel loading, error handling, retry, and expansion panels is significant. HIGH RISK but manageable.

### PLAN-1.2: Integration Tests
- **CAUTION: DashboardTestServer API** — Integration tests use `DashboardTestServer.CreateAsync(configure)` with Memory transport. The builder must verify the exact `DashboardOptions.AddConnection` API and Memory transport namespace/class names before writing test code. Existing tests (e.g., `MemoryEndpointTests.cs`) should be used as reference.
- **CAUTION: MessageQueueCreation** — The plan may reference queue creation APIs that need verification. Builder should read existing Memory tests first.
- **File paths:** `Dashboard.Api.Integration.Tests/Tests/` directory exists. PASS.

## Proceed with Awareness
- PLAN-1.1 Task 2 is the riskiest — large Home.razor rewrite with parallel async operations
- PLAN-1.2 needs careful reference to existing integration test patterns
