# Phase 1 Context: Quick Wins -- Dead Code Cleanup and Trace Instrumentation

## Decisions

### ObjectPool Investigation
- Grep for all references to `ObjectPool` in the solution
- If dead code from dynamic LINQ removal: delete entirely
- If still used: add unit tests

### In-Memory Trace Exporter
- **Location:** Register `ActivityListener` in `DotNetWorkQueue.IntegrationTests.Shared` -- single registration, all transports pick it up automatically
- **Depth:** Collect recorded activities and add assertions verifying correct span names, attributes, etc. Not just passive coverage -- proper trace validation.
- **No external dependencies:** No Jaeger, no network calls. In-memory only.
- **Must not break existing tests or slow CI meaningfully**
