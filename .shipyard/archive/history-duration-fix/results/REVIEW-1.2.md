# Review: Plan 1.2

## Verdict: PASS

---

## Stage 1: Spec Compliance

### Task 1: Redis transport — read-side fix + write-side regression coverage

- **Status: PASS**
- **Evidence:**
  - Production fix: `Source/DotNetWorkQueue.Transport.Redis/Basic/QueryMessageHistoryHandler.cs` line 127 now reads `DurationMs = completedTicks > 0 ? durationMs : (long?)null,` (was `durationMs > 0`). The correct discriminator is in place.
  - Read-side TDD: Builder reports RED run `Failed: 1 (LoadRecord_CompletedStatus_DurationZero_PreservesZero), Passed: 33`; GREEN run `Failed: 0, Passed: 34`. Full RED→GREEN cycle confirmed.
  - Test assertions verified in `QueryMessageHistoryHandlerTests.cs` line 225: `Assert.AreEqual(0L, record.DurationMs, "DurationMs=0 on a completed row must be preserved as 0, not converted to null")` and line 259: `Assert.IsNull(record.DurationMs, "DurationMs must be null when CompletedUtc=0")`.
  - Write-side regression: `WriteMessageHistoryHandlerTests.cs` lines 261-263 and 277-279 assert `db.Received().HashSet(…, Arg.Is<HashEntry[]>(entries => ContainsEntry(entries, "DurationMs", 0L)), …)` for both `RecordComplete_WithoutStartedUtc_WritesDurationZero` and `RecordError_WithoutStartedUtc_WritesDurationZero`. Write-side tests pass immediately (lock-in, not RED→GREEN) — anticipated and correctly documented.
  - `protected virtual GetDb()` seam added to both `QueryMessageHistoryHandler` and `WriteMessageHistoryHandler`. Plan explicitly permitted this (`"introduce protected virtual GetDb() seam if required — no behavioral change"`). All `_connection.Connection.GetDatabase()` call sites are replaced with `GetDb()`. Behavioral change is zero.
- **Notes:** The NSubstitute extension-method limitation was a genuine constraint. Using the 3-argument interface methods (`HashGet(RedisKey, RedisValue, CommandFlags)`, `HashGetAll(RedisKey, CommandFlags)`, `HashSet(RedisKey, HashEntry[], CommandFlags)`) is the correct workaround — the 2-arg extensions delegate to these, so the mock configuration is effective. This is not a test-quality issue.

---

### Task 2: LiteDb QueryMessageHistoryHandler — stop converting DurationMs 0 to null

- **Status: PASS**
- **Evidence:**
  - Production fix: `Source/DotNetWorkQueue.Transport.LiteDB/Basic/QueryMessageHistoryHandler.cs` line 100 now reads `DurationMs = h.CompletedUtc > 0 ? h.DurationMs : (long?)null,` (was `h.DurationMs > 0`). Correct discriminator confirmed.
  - TDD: Builder reports RED run `Failed: 1 (Query_CompletedRow_DurationZero_PreservesZero), Passed: 1`; GREEN run `Failed: 0, Passed: 2`. Full RED→GREEN cycle confirmed.
  - Test assertions verified in `QueryMessageHistoryHandlerTests.cs`: `record.DurationMs.Should().Be(0L, "DurationMs=0 on a completed row must be preserved as 0, not converted to null")` and `record.DurationMs.Should().BeNull("DurationMs must be null when CompletedUtc=0 (row never completed)")`.
  - Tests use a real in-memory LiteDB instance via `CreateHandler()` / `InsertRow()` — no mocking of the database layer. This is stronger than a mock-based approach for a data-mapping bug.
- **Notes:** None. Implementation exactly matches the plan's specified one-line change.

---

### Task 3: Dashboard UI — render "< 1 ms" for DurationMs==0, preserve "-" for null

- **Status: PASS**
- **Evidence:**
  - `Source/DotNetWorkQueue.Dashboard.Ui/Components/Shared/HistoryTab.razor` line 154: `if (ms == 0) return "< 1 ms";` is present between the null-check and the positive-value branch.
  - Confirmed via grep: `FormatDuration(long? ms)` at line 151; null path is unchanged (returns `"-"`); zero path returns `"< 1 ms"`; positive path returns the existing formatted string.
  - tdd="false" task — verified by build check only, as specified.
- **Notes:** The null-rendering contract from CONTEXT-1 is honored exactly: `null` → `"-"` (unchanged), `0` → `"< 1 ms"` (new), `>0` → formatted string (unchanged).

---

### CONTEXT-1 Decision Compliance

- **Decision 1 (RecordError scope):** Write-side regression tests cover both `RecordComplete` and `RecordError` paths for Redis. LiteDb read-side fix applies to the single `MapRecord` call site, which covers all statuses including Error.
- **Decision 2 (TDD):** Tasks 1 and 2 both executed full RED→GREEN cycles with documented run output. Task 3 is `tdd="false"` by design.
- **Decision 3 (null UI behavior preserved):** `FormatDuration(null)` path is unchanged. Confirmed.
- **Wave 1 coordination:** The `completedTicks > 0` / `h.CompletedUtc > 0` discriminator correctly round-trips the Wave 1 write-side fix. A row written with `DurationMs=0L` and `CompletedUtc=<ticks>` is now read back as `DurationMs=0L` rather than `null` on both transports.

---

## Stage 2: Code Quality

### Critical

None.

---

### Important

- **`GetDb()` seam is `protected virtual` on sealed-capable classes with no other subclassing use in production code** (`Source/DotNetWorkQueue.Transport.Redis/Basic/QueryMessageHistoryHandler.cs` line 46, `WriteMessageHistoryHandler.cs` line 44).
  - This is a test-only hook exposed on production classes. The classes are not `sealed`, so any consumer can subclass and override `GetDb()` to inject an arbitrary database, potentially bypassing connection management. The risk is low in practice (these are internal transport classes, not part of the public API), but the seam is wider than necessary.
  - Remediation: Add `internal` visibility to the `GetDb()` method (change `protected virtual` to `protected internal virtual`) or document in the XML summary that overriding is for test use only. Alternatively, if the project follows the pattern of marking test-seam classes `internal`, verify the classes themselves are `internal` — if so, the exposure scope is already contained. The existing XML summary comment (`Returns the Redis database to use. Protected virtual to allow test seam injection`) is good; confirm it is preserved in the release build's XML documentation output.

- **Redis `QueryMessageHistoryHandlerTests` mixes assertion libraries** (`Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/QueryMessageHistoryHandlerTests.cs` lines 225/259 use `Assert.AreEqual`/`Assert.IsNull` for the new tests while the LiteDb tests use `FluentAssertions`).
  - This is inconsistent within the test suite added in this wave. It is not a bug, but it reduces readability — a reader must context-switch between assertion styles in the same project.
  - Remediation: Standardize the Redis query handler tests to use MSTest assertions exclusively (matching the existing disabled-path tests in the same file), or adopt FluentAssertions throughout. Given the project's plan to replace FluentAssertions with MSTest assertions (per MEMORY.md), using `Assert.AreEqual`/`Assert.IsNull` is actually the correct direction for new tests.

---

### Suggestions

- **LiteDb `QueryMessageHistoryHandlerTests` disposes `connectionManager` inside `using(cm)` but the handler holds a reference to the same manager** (`Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/QueryMessageHistoryHandlerTests.cs`). The test calls `handler.GetByQueueId("q1")` inside the `using(cm)` block, so this is safe. However, if a future test calls the handler after the `using` block closes, a use-after-dispose defect would occur silently. The pattern is correct as written; it is worth adding a comment to make the dependency explicit.

- **No test for `FormatDuration` edge cases** (negative values, `long.MaxValue`). The spec does not require these, and the Razor component has no unit test framework, so this is a known gap noted by the plan itself. It is recorded here for future reference when a test harness for Blazor components is available.

- **Write-side regression tests for Redis assert `HashSet` was called but do not assert the full entry set** — only that the `HashEntry[]` array contains an entry with key `"DurationMs"` and value `0L`. This is intentional (minimal lock-in per the plan) but does not prevent a regression where `DurationMs` is written correctly while other fields (e.g., `Status`, `CompletedUtc`) are silently dropped.

---

## Positive Observations

- The `completedTicks > 0` discriminator is the correct semantic: it distinguishes "this row has been completed" from "this row has a measured non-zero duration." This distinction matters for the Deleted, Expired, and Rollback states, which can have `CompletedUtc > 0` but `DurationMs = 0` if processing was sub-millisecond — the fix handles them correctly without special-casing.
- The LiteDb tests using a real in-memory database instance are the highest-confidence approach for a data-mapping bug. This is stronger than mocking and directly exercises the full read path.
- The builder's disclosure of the NSubstitute extension-method constraint and the 3-argument workaround is thorough and accurate. The approach is idiomatic.
- Commit messages follow the project's conventional commit format and accurately describe the behavioral change and test strategy.
- The `ISSUES.md` file was correctly modified (status `M`) to record non-blocking findings carried forward from this wave.
