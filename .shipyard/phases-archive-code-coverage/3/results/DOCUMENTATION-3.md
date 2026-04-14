# Documentation Report
**Phase:** 3 -- Relational Transport Job Handler Tests + Refactors

## Summary
- API/Code docs: 2 handlers reviewed for public API impact
- Architecture updates: 1 proposed pattern doc for transport handler unit testing
- User-facing docs: 0 (internal refactor, no user surface change)
- CLAUDE.md lessons learned: 2 proposed additions

## 1. Public API Impact Analysis

### 1.1 SqlServer SetJobLastKnownEventCommandHandler
- **File:** `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SetJobLastKnownEventCommandHandler.cs`
- **Type:** Reference
- **Change:** Constructor signature changed from `(SqlServerCommandStringCache, IConnectionInformation)` to `(SqlServerCommandStringCache, IDbConnectionFactory)`.
- **Public API break?** **Technically yes, practically no.**
  - The class is `public` and so is the constructor, so binary compatibility is broken.
  - However, this handler is only ever instantiated by the SimpleInjector container inside `SqlServerMessageQueueInit`. It is not listed in any public factory surface, not exposed through `IProducerQueue<T>` / `IConsumerQueue`, and there is no documented downstream extensibility scenario where a consumer would `new` it up directly.
  - `IDbConnectionFactory` was already registered in the SqlServer DI module, so container-resolved instances continue to work without any registration change.
- **Recommendation:** No API reference doc update needed. The XML `<summary>` on the constructor already documents the new parameter (`<param name="dbConnectionFactory">...</param>`). No release-notes entry required beyond a short internal note.

### 1.2 PostgreSQL SetJobLastKnownEventCommandHandler
- **File:** `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SetJobLastKnownEventCommandHandler.cs`
- **Type:** Reference
- **Change:** Same constructor signature change as SqlServer. Additionally, the Wave 1 version briefly shipped with a `(NpgsqlConnection)` cast inside `Handle()`; the final version drops the cast and operates on `IDbConnection` directly. The class still implements `ICommandHandler<SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>>`, so the **generic signature is unchanged** and any type-level consumers are unaffected.
- **Public API break?** Same as SqlServer -- technical break on a container-resolved class, no practical downstream impact.
- **Recommendation:** No API reference doc update needed. XML doc already uses `<inheritdoc />` and documents the new `dbConnectionFactory` parameter.

### Cross-cutting note (Priority: LOW)
If the project ever publishes a formal "what is binary-compatible" contract, these DI-internal handlers should be explicitly listed as **not** part of the public contract (candidates for `internal` in a future cleanup). Several similar handlers across the relational transports have the same shape and would benefit from the same re-classification.

## 2. Proposed CLAUDE.md Lessons Learned

**Priority: HIGH** -- both are non-obvious and will bite the next person who touches a transport-specific handler.

### Lesson A -- Sealed transport connection types break mocking
Propose adding to the `## Lessons Learned` bullet list:

> - Casting `IDbConnection` to a sealed transport-specific type (e.g., `NpgsqlConnection`, `SqliteConnection`) inside a handler's `Handle()` method makes the handler effectively unmockable with NSubstitute/Castle DynamicProxy -- you cannot proxy a sealed class, so any test that tries to pass a substitute `IDbConnection` into the factory will hit `TypeLoadException` at the cast site. Pattern: keep handlers operating on `IDbConnection` / `IDbCommand` with generic `DbType` enum values + `IDbCommand.CreateParameter()`. The transport-specific generic args on `ICommandHandler<SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>>` are fine -- the cast inside `Handle()` is what kills testability. PostgreSQL `SetJobLastKnownEventCommandHandler` had to be re-refactored mid-phase to drop this cast (phase 3, commit `9c77537d`).

### Lesson B -- Test seam pattern for transport handlers
Propose adding to the `## Lessons Learned` bullet list:

> - For unit-testing transport handlers that own their own `using` block around an `IDbConnection`, the preferred seam is constructor-injected `IDbConnectionFactory` (already registered in every relational transport DI module). Mock chain: `IDbConnectionFactory.Create()` -> `IDbConnection.CreateCommand()` -> `IDbCommand.Parameters` (as an `IDataParameterCollection`). Capture parameters via `Arg.Do<IDbDataParameter>(p => list.Add(p))` on `Parameters.Add` -- do **not** try to mock `IDataParameterCollection` indexers, and do **not** use `System.Reflection` or `Testable*` subclasses to reach protected members (neither is needed once the factory seam is in place).

## 3. Proposed Architecture Doc (Priority: MEDIUM)

### Unit Testing Transport Command Handlers
- **Type:** How-to guide
- **Proposed location:** `docs/testing-transport-handlers.md` (new file) OR a new `## Testing Command Handlers` section inside an existing architecture doc if one covers the Command/Query pattern.
- **Rationale:** Phase 3 established a repeatable pattern across SqlServer, PostgreSQL, and SQLite. Several dozen other handlers under `Transport.*/Basic/CommandHandler/` share the same shape and are currently uncovered. A short how-to will pay for itself on the very next coverage phase.
- **Proposed outline** (kept deliberately short -- one good example beats three paragraphs):
  1. **When to use this pattern** -- handlers that own a `using (var conn = ...)` block around an `IDbConnection` and execute a single SQL command.
  2. **Refactor checklist** -- inject `IDbConnectionFactory` (not `IConnectionInformation` + `new XxxConnection(...)`); operate on `IDbConnection` interfaces; avoid any cast to the sealed transport connection type inside `Handle()`.
  3. **Mock wiring recipe** -- NSubstitute chain from `IDbConnectionFactory` down to parameter capture via `Arg.Do<>`. Link to `SetJobLastKnownEventCommandHandlerTests.cs` in the SqlServer and PostgreSQL test projects as canonical examples.
  4. **What to assert** -- constructor null-guards, `CommandText` value (catches silent SQL typos -- see the pre-existing "SQL UPDATE silent no-op" lesson in CLAUDE.md), parameter names / `DbType` / values, and `Open()` + `ExecuteNonQuery()` call counts.
  5. **Pitfalls** -- sealed connection types (Lesson A above), `IDataParameterCollection` indexer mocking, `SetJobLastKnownEventCommand<TConn, TTxn>` requires non-null-typed generic args but accepts `null` values for the `connection`/`transaction` constructor params when the handler ignores them.

## 4. Gaps
- No architecture doc currently describes the Command/Query handler pattern under `Transport.Shared`. This is a pre-existing gap, not introduced by phase 3, but the phase 3 pattern doc above would be the natural anchor for a future expansion.
- The `IDbConnectionFactory` abstraction itself is undocumented at the architecture level -- worth a one-paragraph "why it exists" note the next time architecture docs are touched (supports DI, supports unit testing, isolates connection-string handling from handlers).

## 5. Priority Summary
| Priority | Item | Action |
|---|---|---|
| HIGH | Lesson A (sealed connection cast) | Add to CLAUDE.md Lessons Learned |
| HIGH | Lesson B (factory seam pattern) | Add to CLAUDE.md Lessons Learned |
| MEDIUM | Transport handler testing how-to | New `docs/testing-transport-handlers.md` |
| LOW | Public-vs-internal classification of DI-resolved handlers | Track for a future API hygiene pass |
| LOW | `IDbConnectionFactory` architecture note | Fold into next architecture doc update |

All findings are **non-blocking**. No source code or CLAUDE.md edits were made -- wording is proposed only.
