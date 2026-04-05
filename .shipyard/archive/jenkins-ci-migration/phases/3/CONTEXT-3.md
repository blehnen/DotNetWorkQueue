# Phase 3: Async Dispose Fix — Design Decisions

## Decisions Captured (from brainstorm)

### 1. Implementation
- Implement `IAsyncDisposable` on `DashboardConsumerClient`
- `DisposeAsync()` properly awaits the HTTP DELETE call
- Keep synchronous `Dispose()` as fallback using fire-and-forget with documented rationale
- Remove silent exception swallowing — log or propagate appropriately

### 2. Target
- Dashboard.Client project targets only net10.0 and net8.0
- `IAsyncDisposable` is fully available without conditional compilation

### 3. Testing
- Unit tests required for DisposeAsync behavior, double-dispose safety, Dispose not blocking
