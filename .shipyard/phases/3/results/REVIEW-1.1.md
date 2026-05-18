# Review: Phase 3 Plan 1.1 — SqlServerRelationalWorkerNotification + DI Registration

## Verdict: PASS (minor issues noted)

---

## Stage 1 — Spec Compliance

### Task 1: Create `SqlServerRelationalWorkerNotification.cs`
- **Status:** PASS
- **Evidence:** File exists at `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalWorkerNotification.cs` (91 lines).
  - 18-line LGPL-2.1 header present (lines 1-18).
  - Declared `internal class SqlServerRelationalWorkerNotification : WorkerNotification, IRelationalWorkerNotification` (line 50).
  - Constructor signature matches 6-parameter `WorkerNotification` ctor and forwards all params to `base(...)` (lines 63-71).
  - `ConnectionHolder` property: `IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand>` with `get; set;` (line 85).
  - `Transaction` getter: `=> ConnectionHolder?.Transaction;` returning `DbTransaction` (line 88).
  - XML doc: `<summary>` + `<remarks>` on class, `<summary>` + `<value>` on `ConnectionHolder`, `<inheritdoc/>` on `Transaction`. Constructor has `<summary>` + all 6 `<param>` entries (lines 53-70).
  - No `Tx`/`TX` tokens. No `(SqlConnection)` or `(SqlTransaction)` casts. Confirmed by builder's Gate 2 and Gate 3 grep results (exit 1, zero matches).
  - Using directives exactly match spec requirements: `System.Data.Common`, `System.Diagnostics`, `Microsoft.Data.SqlClient`, `Microsoft.Extensions.Logging`, `DotNetWorkQueue.Configuration`, `DotNetWorkQueue.Queue`, `DotNetWorkQueue.Transport.RelationalDatabase`.
- **Notes:** Implementation is byte-faithful to the spec's code shape on all measurable criteria.

### Task 2: Register option-driven factory delegate in `SQLServerMessageQueueInit`
- **Status:** PASS WITH DEVIATION (deviation is sound — see Notes)
- **Evidence:** `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs` lines 75-103 contain the inbox registration block.
  - `container.Register<SqlServerRelationalWorkerNotification>(LifeStyles.Transient)` — present (line 86). ✓
  - `container.Register<IWorkerNotification>(() => {...}, LifeStyles.Transient)` — present (lines 87-103). ✓
  - Factory delegate inspects `EnableHoldTransactionUntilMessageCommitted` — confirmed (line 94).
  - No unconditional `Register<IWorkerNotification, SqlServerRelationalWorkerNotification>` — confirmed by builder's Gate 4 (grep exit 1).
  - Exactly one `container.Register<IWorkerNotification>` line — confirmed by builder's Gate 3.
  - Builder's Gate 5 (`dotnet test ... 156/156 pass`) confirms the wiring resolves correctly at runtime.
- **Deviation:** The plan specified THREE registrations: relational concrete, `WorkerNotification` self-registration, and the factory delegate. The builder dropped the middle registration (`container.Register<WorkerNotification>(LifeStyles.Transient)`) and added a `try/catch` fallback inside the factory delegate instead.
  - **Dropped self-registration — sound.** `ComponentRegistration.cs:217` registers `container.Register<IWorkerNotification, WorkerNotification>(LifeStyles.Transient)`. In SimpleInjector, a concrete type that is already registered as an implementation target is auto-resolvable by concrete type via `GetInstance<WorkerNotification>()` without a separate self-registration. The 156/156 test pass confirms the wiring resolves. Avoids a duplicate-registration footgun if `AllowOverridingRegistrations` ever changes.
  - **Added try/catch — justified.** At `container.Verify()` / early-resolution time, `optionsFactory.Create()` attempts to load persisted options from a database. Without the guard, 6 pre-existing `QueueCreatorTests` that expect a bare `SqlException` receive a wrapped `InvalidOperationException`, breaking the test contract. The pattern exactly mirrors the existing `IBaseTransportOptions` factory delegate at lines 140-144 of the same file. This is not a new pattern; it is an existing codebase invariant the plan should have specified from the outset (SUMMARY's "lesson for plan authors" is apt).

### Task 3: Verification gates
- **Status:** PASS
- **Evidence:** All 5 gates passed per SUMMARY:
  - Gate 1 (Release build): `Build succeeded. 14 Warning(s) [all NU1902 pre-existing] 0 Error(s)`. Both net10.0 and net8.0. No CS1591.
  - Gate 2 (Tx grep): exit 1, zero matches.
  - Gate 3 (sealed-cast grep): exit 1, zero matches.
  - Gate 4 (factory delegate present): exit 0, one matching line.
  - Gate 5 (unit tests): `Failed: 0, Passed: 156, Skipped: 0, Total: 156`.

---

## Stage 2 — Code Quality

### Critical
None.

### Minor

- **Bare `catch` in factory delegate swallows all exception types** (`SQLServerMessageQueueInit.cs`, line 97).
  The factory delegate catches all exceptions to fall back to `holdTransaction = false`. While the intent is to trap options-load failures (DB connectivity errors), the bare `catch` will also silently swallow unrelated failures — including programming errors like `InvalidCastException` (e.g., if `optionsFactory.Create()` ever returns a type other than `SqlServerMessageQueueTransportOptions`), `NullReferenceException` from a null factory, or reflection errors. These would resolve to the `false` / non-relational path without any diagnostic signal, making them extremely hard to debug.
  - **Precedent:** The `IBaseTransportOptions` block at lines 140-144 also uses a bare `catch`. This review acknowledges the pattern is consistent with the codebase, but the absence of logging means silent misconfigurations are possible.
  - **Remediation:** Narrow to `catch (Exception ex) when (ex is SqlException || ex is InvalidOperationException)` to restrict the fallback to DB-origin failures, and add a log call at Warning level. Alternatively (minimal change): type the catch as `catch (Exception)` so it is at least explicit about swallowing everything. If the codebase elects to keep parity with the `IBaseTransportOptions` bare-catch as-is, document the accepted trade-off in the inline comment.

- **`Transaction` property returns `null` when `ConnectionHolder` is unset — mismatches interface contract** (`SqlServerRelationalWorkerNotification.cs`, line 88).
  `IRelationalWorkerNotification.Transaction`'s XML doc (Phase 2 deliverable) states "Never null when the containing interface is implemented." The `?.` null-conditional returns `null` when `ConnectionHolder` is not yet set (between construction and receive-path injection). The builder's SUMMARY correctly notes this is "defensive only" because option=true guarantees the holder is set before handler invocation. However:
  - NRT is disabled on this project (`CS8603` is suppressed), so the contract violation is not caught at compile time.
  - A future author implementing `IRelationalWorkerNotification` on a new transport could rely on the "never null" guarantee and receive a `NullReferenceException` if they pattern-match this implementation.
  - **Remediation:** Add an inline comment on the `Transaction` getter explicitly noting the null-when-unset window and why it is safe (i.e., the receive path always sets `ConnectionHolder` before handler invocation when this class is registered). This documents the invariant for future maintainers without changing behavior.

### Positive

- **Self-corrected when tests failed.** Six test failures from the initial commits were caught immediately, the root cause was correctly diagnosed as a `container.Verify()` eager-resolution issue, and the fix exactly matched the codebase's existing precedent. No guesswork, no workarounds.
- **SUMMARY documents the lesson clearly.** The "Lesson for plan authors" section in SUMMARY-1.1 is precise and actionable; future plans for similar factory-delegate registrations will benefit from it.
- **No sealed-type casts introduced.** `ConnectionHolder` property is typed as `IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand>` (the interface), not the concrete `ConnectionHolder` class. `Transaction` upcast to `DbTransaction` is implicit (no explicit cast). Discipline maintained.
- **XML doc is thorough.** Class, constructor, both properties all have meaningful documentation including the property-injection lifecycle explanation — not boilerplate.
