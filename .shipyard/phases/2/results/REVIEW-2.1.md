# Review: Plan 2.1

## Verdict: PASS

## Stage 1: Spec Compliance — PASS

### Task 1: `IExternalDbNameExtractor` interface — PASS
- File `Source/DotNetWorkQueue.Transport.RelationalDatabase/IExternalDbNameExtractor.cs` exists at project root (alongside `IRetrySkippable` from PLAN-1.1).
- LGPL header bytes 1-18 match the repo standard verbatim.
- Namespace `DotNetWorkQueue.Transport.RelationalDatabase`. Single `public interface IExternalDbNameExtractor` with exactly one method: `string Extract(DbConnection connection)` (line 43).
- XML doc covers both type and method per Release-build XML requirement.
- Layering grep on the file returns no `Microsoft.Data.SqlClient` / `using Npgsql` matches.

### Task 2: `ExternalTransactionValidator` class — PASS
- File `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/ExternalTransactionValidator.cs` exists in `Basic/` per convention.
- `public sealed class` in namespace `DotNetWorkQueue.Transport.RelationalDatabase.Basic` (line 42).
- Constructor accepts `IExternalDbNameExtractor` + `IConnectionInformation` and applies `Guard.NotNull` to both (lines 58-59).
- `Validate(DbTransaction)` runs the 4 checks in plan order:
  1. null tx → `ArgumentNullException` (lines 73-74)
  2. null `transaction.Connection` → `InvalidOperationException` ("null Connection") (lines 76-80)
  3. `connection.State != Open` → `InvalidOperationException` (state name interpolated) (lines 82-86)
  4. `!string.Equals(actual, expected, StringComparison.Ordinal)` against `_connectionInfo.Container` → `InvalidOperationException` containing both `'{actual}'` and `'{expected}'` (lines 88-95)
- Confirmed `IConnectionInformation.Container` is defined at `Source/DotNetWorkQueue/IConnectionInformation.cs:65` — validator stays transport-agnostic as planned.
- Layering grep returns clean.

### Task 3: 5 unit tests — PASS
- File `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/ExternalTransactionValidatorTests.cs` exists.
- Exactly 5 `[TestMethod]` methods, names verbatim per plan:
  - `Validate_WhenTransactionIsNull_ThrowsArgumentNullException`
  - `Validate_WhenConnectionIsNull_ThrowsInvalidOperationException`
  - `Validate_WhenConnectionNotOpen_ThrowsInvalidOperationException`
  - `Validate_WhenDatabaseNameMismatch_ThrowsInvalidOperationExceptionWithBothNames`
  - `Validate_WhenAllChecksPass_DoesNotThrow`
- All exception assertions use `Assert.ThrowsExactly<T>` (MSTest 4.x). Repo-wide grep for `Assert.ThrowsException` in `Transport.RelationalDatabase.Tests` returns zero hits.
- NSubstitute mocks are against abstract bases: `Substitute.For<DbTransaction>()` (line 46), `Substitute.For<DbConnection>()` (line 43) — per CLAUDE.md mocking lesson.
- DB-name-mismatch test asserts BOTH names in message (lines 83-84).
- Per SUMMARY: filtered run = 5/5 pass; full suite = 221 pass / 0 fail (216 baseline + 5 new), no regressions.

## Stage 2: Code Quality — PASS

### Critical
- None.

### Important
- None.

### Suggestions
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/ExternalTransactionValidator.cs:74` — `throw new ArgumentNullException(nameof(transaction))` is hand-rolled while the constructor uses `Guard.NotNull(...)`. Both are correct; switching the method-arg null check to `Guard.NotNull(() => transaction, transaction)` would be stylistically consistent with the rest of the class (and with the repo's `DotNetWorkQueue.Validation.Guard` convention) but emits a slightly less canonical exception. Not blocking; either form is acceptable. No code change required.
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/ExternalTransactionValidator.cs:88` — `_extractor.Extract(connection)` is called before the database-name comparison. If a Phase 3/4 extractor implementation ever throws on a transient/odd state (e.g., `connection.Database` returning `""` and the extractor electing to throw), the exception surface for Validate becomes wider than the 2 documented exception types in the XML doc. The current contract says "via `connection.Database`" which is non-throwing on both providers — so this is theoretical. No change required for Phase 2; flag for the architect when wiring per-provider extractors in Phase 3/4 to keep Extract total/non-throwing.
- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/ExternalTransactionValidatorTests.cs:65,73,81` — the `_` discards from `BuildSut` are unused in three tests; the helper's tuple shape (`(sut, tx, conn)`) is wider than callers need for those cases. Reasonable for a 5-test file with a shared builder; refactoring to per-test inline construction would add noise. No change required.

### Positive
- Three atomic commits, one per task, with `shipyard(phase-2):` prefix. History bisects cleanly.
- Wave 2 parallelism worked as designed: PLAN-2.2's two commits landed between Task 1 and Task 2 of this plan without content overlap. The HEAD-ref race was handled correctly via retry — no merge, no rebase, no content collision (the SUMMARY documents this explicitly).
- Builder correctly identified and resolved the chicken-and-egg between Task 1's XML cref to `Basic.ExternalTransactionValidator` and Task 2's file existence; the Task 1 commit landed only after Task 2's file existed in working tree so the Release build with `TreatWarningsAsErrors` would succeed at every commit on the final tree.
- `Guard.NotNull(() => extractor, extractor)` pattern in the constructor matches existing repo convention (`DotNetWorkQueue.Validation.Guard` with expression-tree lambda for parameter-name extraction).
- Layering invariant holds: project-wide grep for `Microsoft.Data.SqlClient` / `using Npgsql` over `Source/DotNetWorkQueue.Transport.RelationalDatabase/` returns no matches. Validator stays transport-agnostic via `IConnectionInformation.Container`.
- Test helper `BuildSut` with default parameters keeps the 5 tests readable and DRY without over-abstracting; the failure-mode flags (`actualDbFromExtractor`, `connState`, `nullConnectionOnTx`) map 1:1 to the four checks.
- DB-name-mismatch test asserts BOTH names appear in the error message, directly satisfying PROJECT.md §Non-Functional Diagnostics.
- 4th-check error message explicitly explains the outbox-pattern rationale ("queue tables and the caller's business data to live in the same database") — high-value diagnostic for end users hitting this guard.

## Summary
Verdict: APPROVE. Three atomic commits implement the Wave 2 validator + extractor surface exactly as planned: verbatim XML doc and LGPL headers, exact ordering and exception types for the 4 checks, MSTest 4.x `ThrowsExactly` assertions throughout, abstract-base NSubstitute mocking per CLAUDE.md, layering invariant intact (no transport-specific types), and 221/0 regression-gate green. No blocking findings; suggestions are stylistic only.
Critical: 0 | Important: 0 | Suggestions: 3
