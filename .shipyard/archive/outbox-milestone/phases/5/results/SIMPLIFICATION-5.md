# Phase 5 Simplification Review

**Date:** 2026-05-14
**Files analyzed:** 4 new test files + 2 csproj edits

## Overall: LOW_FINDINGS

## Findings

### High Priority
None.

### Medium Priority

#### Near-duplicate reflection-scan block across 4 transports
- **Type:** Consolidate (potential)
- **Effort:** Moderate
- **Locations:**
  - `Source/DotNetWorkQueue.Transport.Memory.Tests/Basic/MemoryProducerDoesNotImplementRelationalTests.cs:56-65`
  - `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/LiteDbProducerDoesNotImplementRelationalTests.cs:56-65`
  - `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/RedisProducerDoesNotImplementRelationalTests.cs:56-65`
  - `Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/SqliteProducerDoesNotImplementRelationalTests.cs:58-67`
- **Description:** All 4 files share an identical 10-line reflection scan that differs only in the anchor type (`MemoryDashboardInit`, `LiteDbMessageQueueInit`, `RedisQueueInit`, `SqLiteMessageQueueInit`) and the assertion-message prefix. The Decision-1 `IsAssignableFrom` line is also duplicated. Total duplication: ~25 lines × 4 = ~100 lines that could collapse to a single shared helper + 4 thin call sites.
- **Suggestion:** Defer. Extracting `AssertTransportDoesNotImplementRelational(Type anchor, string transportName)` into a shared test utility (e.g., a new `DotNetWorkQueue.Transport.Tests.Shared` project, or as `internal static` duplicated via `<Link>` from a single source) would centralize the invariant. Tradeoff: a new shared test project for ~25 lines × 4 trivially-mechanical assertions is high ceremony. No test churn is expected here — these tests are write-once regression guards.
- **Impact:** Net negative if a new project is created (NuGet/build cost > saved lines). Net positive ONLY if the same pattern returns in Phase 6+ (e.g., a new non-relational transport requires the same negative test).
- **Recommendation:** **Do not extract now.** Rule of Three is satisfied numerically (N=4) but the duplication is closed-form (no future variation expected) and per-file readability is high. Revisit if Phase 6+ adds a 5th transport requiring the same shape.

### Low Priority

- **Decision-4 SQLite extra assertion is paranoid but self-documenting.** `Sqlite...Tests.cs:69-83` — the assertion is true by construction today (`RelationalProducerQueue<T>` derives from `ProducerQueue<T>`, not the other way around), and the 9-line comment explicitly admits this. The Decision-2 assembly scan already covers the regression case the extra assertion guards against. Defensible as "explicit SQLite-specific intent marker" but adds zero detection capability beyond Decision 2. Keep — the comment makes the intent clear, and removing it would require re-litigating Decision 4.
- **Commit-prefix deviation** (`test(...)` vs `shipyard(phase-5):`) — cosmetic, already flagged in both SUMMARYs. No action.
- **TestMessage POCO duplicated in each of 4 files** (lines 35-38 each). Trivial — would not extract; co-location with the test improves readability.

## Recommendations
- Ship as-is. The duplication is closed-form, the tests are write-once regression guards, and a shared helper would cost more (new project / `<Link>` infrastructure) than it saves.
- If a 5th non-relational transport is added in Phase 6+, revisit consolidation at that point.
- No dead code, no AI bloat, no unnecessary abstraction. Reflection scan correctly handles both closed- and open-generic forms (verified in Decision 2 design).
