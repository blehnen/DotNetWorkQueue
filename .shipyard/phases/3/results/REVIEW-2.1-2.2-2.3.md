# Combined Review: Plans 2.1, 2.2, 2.3

## Verdict: PASS

All three plans correctly add unit tests for the `*JobSchema` classes. Assertions were cross-checked against the production schema source and match exactly (column names, types, lengths, nullability, PK name/unique/clustered flags). Transport-specific differences are correctly encoded.

---

## Plan 2.1 (SqlServerJobSchema) -- PASS

### Stage 1
- **GetSchema_ReturnsExactlyOneTable**: PASS -- asserts `tables.Count == 1`.
- **GetSchema_TableHasExpectedColumns**: PASS -- verified against `SqlServerJobSchema.cs:63-65`: `JobEventTime`/`JobScheduledTime` = `Datetimeoffset` NOT NULL, `JobName` = `Varchar(255)` NOT NULL. Also asserts column count == 3.
- **GetSchema_TableHasPrimaryKey**: PASS -- verified against production lines 68-70: `PK_{JobTableName}`, `Clustered=true`, `Unique=true`, column `JobName`. Strong test.
- **GetSchema_TableNameMatchesHelper**: PASS.
- **GetSchema_TableOwnerMatchesSchema**: PASS -- covers the `ISqlSchema.Schema` -> `Table.Owner` wiring that is unique to SqlServer. Good extra.

### Stage 2
- **Suggestions**:
  - `SqlServerJobSchemaTests.cs:98` -- `TestFixture.SqlSchema` property is assigned but never read by any test; it can be removed without losing coverage.
  - Constructor null-guard tests were skipped because the ctor has no `Guard.NotNull` -- acceptable, but consider adding a note/comment if coverage tooling flags the branch.

---

## Plan 2.2 (PostgreSqlJobSchema) -- PASS

### Stage 1
- **GetSchema_ReturnsExactlyOneTable**: PASS.
- **GetSchema_TableHasExpectedColumns**: PASS -- matches `PostgreSQLJobSchema.cs:62-64`: `JobEventTime`/`JobScheduledTime` = `Bigint` NOT NULL (Ticks storage, confirmed via `SetJobLastKnownEventCommandHandler.cs:67`), `JobName` = `Varchar(255)` NOT NULL.
- **GetSchema_TableHasPrimaryKey**: PASS -- walks `table.Constraints` and separately asserts `table.PrimaryKey.Unique`.
- **GetSchema_TableNameMatchesHelper**: PASS.

### Stage 2
- **Important**:
  - `PostgreSqlJobSchemaTests.cs:58` uses `pk.Columns.Contains("JobName")` but does not assert `pk.Columns.Count == 1`. If a regression added a second PK column the test would still pass. Consider tightening to `Assert.AreEqual(1, pk.Columns.Count)` to match the strictness of the SqlServer test.
- **Suggestions**:
  - Consider asserting `ConstraintType.PrimaryKey` on `table.PrimaryKey.Type` for symmetry with Plan 2.1 / 2.3.
  - No `Clustered` assertion -- correct for PostgreSQL (not applicable), but a short comment would aid future readers.
  - `IConnectionInformation` is referenced without a `using DotNetWorkQueue;` directive -- this compiles only because the test namespace (`DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic`) walks up to `DotNetWorkQueue`. Works, but an explicit using would be clearer.

---

## Plan 2.3 (SqliteJobSchema) -- PASS

### Stage 1
- **GetSchema_ReturnsExactlyOneTable**: PASS.
- **GetSchema_TableHasExpectedColumns**: PASS -- matches `SqliteJobSchema.cs:62-64`: all three columns `Text`, time fields length 35 (ISO 8601 `o` format), `JobName` length 255, all NOT NULL.
- **GetSchema_TableHasPrimaryKey**: PASS -- asserts name, type, `Unique=true`, membership of `JobName`. No `Clustered` assertion, which is correct (production doesn't set it for SQLite).
- **GetSchema_TableNameMatchesHelper**: PASS.

### Stage 2
- **Important**:
  - `SqliteJobSchemaTests.cs:81` uses `CollectionAssert.Contains` for the PK column list but does not assert count == 1 -- same loosening as Plan 2.2. Tighten for regression safety.
- **Suggestions**:
  - Uses `Substitute.For<ITableNameHelper>()` while Plans 2.1/2.2 use the concrete `new TableNameHelper(connection)`. Both are valid; the mock approach is slightly more isolated. No action required -- just note the inconsistency across the three test files.
  - Includes the LGPL license header (good); the other two new files omit it -- not a blocker but inconsistent with repo convention per `DotNetWorkQueue.licenseheader`.

---

## Cross-Plan Observations
- No prior `REVIEW-*.md` or `.shipyard/ISSUES.md` entries conflict with these plans.
- Production schemas were independently verified; no false positives from test-only mocks.
- All three tests are pure unit tests with no external dependencies -- fast (193 ms for Plan 2.1) and deterministic.
- Test smells: none critical. Mild inconsistencies in style (fixture class vs helper methods, mock vs concrete helper, license header presence) across the three files, but each file is internally consistent.

## Summary
**Verdict:** APPROVE
All three plans implement their spec correctly; production code behavior is faithfully captured. Two minor tightening suggestions (explicit PK column count) are non-blocking.
Critical: 0 | Important: 2 | Suggestions: 6
