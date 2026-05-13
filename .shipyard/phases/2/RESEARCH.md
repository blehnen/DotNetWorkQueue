# Phase 2 Research: Foundation Layer (RelationalDatabase + Marker + Decorator Branches)

---

## Section 1: `SendMessageCommand` — Current Shape and Reference Graph

**Files inspected:**
- `Source/DotNetWorkQueue.Transport.Shared/Basic/Command/SendMessageCommand.cs` (lines 1–56)
- `Source/DotNetWorkQueue.Transport.Shared/Basic/SendMessages.cs` (lines 62–152)
- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/Command/SendMessageCommandTests.cs`

**Findings:**
- Namespace: `DotNetWorkQueue.Transport.Shared.Basic.Command`
- Class: `public class SendMessageCommand` — NOT sealed, NOT abstract.
- Constructor: `public SendMessageCommand(IMessage messageToSend, IAdditionalMessageData messageData)`
- Properties: `IMessage MessageToSend { get; }` and `IAdditionalMessageData MessageData { get; }` — both init-only, set in constructor.
- No existing `ExternalTransaction`, `SkipRetry`, or any other properties.
- Files constructing `new SendMessageCommand(...)`:
  - `Source/DotNetWorkQueue.Transport.Shared/Basic/SendMessages.cs` — 4 call sites (lines 69, 89, 115, 135). These are inside the standard `ISendMessages` implementation. No transport-specific files construct this class directly.
  - `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/Command/SendMessageCommandTests.cs` — test only.
  - `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/Command/SendMessageCommandTests.cs` — test only.

**Plan implications:**
- Class is not sealed — derivation (option B) is structurally available.
- The Phase 1 spike memo proposed placing `IRetrySkippable` in `Transport.Shared` and having `SendMessageCommand` implement it. CONTEXT-2 revised this to put `IRetrySkippable` in `Transport.RelationalDatabase` with a derived command class in `Transport.RelationalDatabase`. That is clean because `SendMessageCommand` is not sealed.
- `SendMessages<T>` (the standard producer helper) constructs `SendMessageCommand` directly. `RelationalProducerQueue<T>` will construct `RelationalSendMessageCommand` (the derived class) instead for its tx-aware overloads. The standard path is unaffected.

---

## Section 2: `Transport.RelationalDatabase` Current Structure

**Files inspected:**
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj`

**Findings:**
- Project file: `Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj`
- Targets: `net10.0;net8.0` (confirmed, line 4)
- Existing project references (lines 34–37):
  - `DotNetWorkQueue.Transport.Shared.csproj` — YES, references Shared
  - `DotNetWorkQueue.csproj` — YES, references core
  - No reference to SqlServer, PostgreSQL, or any transport-specific project — correct layering
- `TreatWarningsAsErrors=true` on Release with `DocumentationFile` set — XML doc required on all new public types
- `GeneratePackageOnBuild=true` — this project is packable
- No existing `Validator`, `Validation`, or `ProducerQueue` types in the project root (the source tree contains only query/command/handler infrastructure for relational DB operations — no producer types yet)

**Plan implications:**
- No new project references needed in `Transport.RelationalDatabase.csproj` — it already references both `Transport.Shared` and core `DotNetWorkQueue`.
- SqlServer and PostgreSQL will need to add a project reference to `Transport.RelationalDatabase` if not already present (verify in Section 10).

---

## Section 3: `IProducerQueue<T>` and Producer Factory Wiring

**Files inspected:**
- `Source/DotNetWorkQueue/IProducerQueue.cs`
- `Source/DotNetWorkQueue/Queue/ProducerQueue.cs`
- `Source/DotNetWorkQueue/QueueContainer.cs` (lines 330–349)
- `Source/DotNetWorkQueue/IQueueContainer.cs`

**Findings:**
- `IProducerQueue<TMessage>` defined in namespace `DotNetWorkQueue`, file `Source/DotNetWorkQueue/IProducerQueue.cs`. Inherits `IProducerBaseQueue`. Has 6 overloads: `Send(T)`, `Send(List<T>)`, `Send(List<QueueMessage<T, IAdditionalMessageData>>)` + async equivalents.
- `ProducerQueue<T>` (concrete): `Source/DotNetWorkQueue/Queue/ProducerQueue.cs`, namespace `DotNetWorkQueue.Queue`. Public, non-sealed. Uses `ISendMessages` for the actual transport call — does NOT call `SendMessageCommand` directly.
- Factory: `QueueContainer<TTransportInit>.CreateProducer<TMessage>(QueueConnection)` at line 338 calls `container.GetInstance<IProducerQueue<TMessage>>()`. Return type is `IProducerQueue<TMessage>`.
- Transports register `IProducerQueue<T>` → `ProducerQueue<T>` in their init class via `IContainer.Register<IProducerQueue<T>, ProducerQueue<T>>()`. The factory returns whatever the container resolves for `IProducerQueue<TMessage>`.

**Plan implications:**
- `IRelationalProducerQueue<T> : IProducerQueue<T>` can be returned by `CreateProducer` only if the transport registers `IProducerQueue<T>` → `RelationalProducerQueue<T>` (where `RelationalProducerQueue<T> : ProducerQueue<T>, IRelationalProducerQueue<T>`). The caller then capability-casts.
- `QueueContainer.CreateProducer` returns `IProducerQueue<T>` — the factory itself does NOT need to change. Capability cast `as IRelationalProducerQueue<T>` happens at the call site.
- `RelationalProducerQueue<T>` must inherit `ProducerQueue<T>` to be registered as `IProducerQueue<T>` (SimpleInjector requires the concrete to implement the service). `ProducerQueue<T>` is public + non-sealed — inheritance is clean.

---

## Section 4: Retry-Decorator File Structures

**Files inspected (all 4 read in full):**
- `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecorator.cs`
- `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs`
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecorator.cs`
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs`

**Findings — SqlServer sync (`RetryCommandHandlerOutputDecorator.cs`):**
- Namespace: `DotNetWorkQueue.Transport.SqlServer.Decorator`
- Class: `internal class RetryCommandHandlerOutputDecorator<TCommand, TOutput>`
- `Handle(TCommand command)` body (lines 49–66): `Guard.NotNull`, then `TryGetPipeline(TransportPolicyDefinitions.RetryCommandHandler, ...)` wrapped in `try/catch(ObjectDisposedException)`, then `if (pipeline != null) pipeline.Execute(...) else _decorated.Handle(command)`.
- **Bypass branch insertion point: after `Guard.NotNull(() => command, command);` at line 51, before the `ResiliencePipeline pipeline = null;` at line 52.**
- Uses `using DotNetWorkQueue.Transport.Shared` (for `IPolicies`) — line 21.

**Findings — SqlServer async (`RetryCommandHandlerOutputDecoratorAsync.cs`):**
- Identical structure, `TransportPolicyDefinitions.RetryCommandHandlerAsync` key, `await _decorated.HandleAsync(command)`.
- Bypass insertion point: same — after `Guard.NotNull` (line 52), before pipeline var at line 53.

**Findings — PostgreSQL sync and async:**
- Byte-for-byte equivalent structure to SqlServer counterparts. Uses `DotNetWorkQueue.Transport.PostgreSQL.Basic` instead of `DotNetWorkQueue.Transport.SqlServer.Basic` for the policy key import. Same `Handle()` / `HandleAsync()` body. Same insertion point.

**PHASE 1 CLAIM VERIFIED:** PostgreSQL retry decorators are structurally identical to SqlServer. No per-transport variation.

**Plan implications:**
- 4 files, each receives the same 3-line early-return branch after `Guard.NotNull`:
  ```csharp
  if (command is IRetrySkippable s && s.SkipRetry)
      return _decorated.Handle(command); // or await _decorated.HandleAsync(command) for async
  ```
- `IRetrySkippable` must be visible to all 4 decorators. Currently each imports `DotNetWorkQueue.Transport.Shared` already (line 21 in SqlServer files). If `IRetrySkippable` is placed in `Transport.RelationalDatabase` (per CONTEXT-2 Decision 2), then SqlServer and PostgreSQL decorator projects need a new project reference to `Transport.RelationalDatabase`. See Section 10 for whether that reference already exists.

---

## Section 5: `IPolicies` and Policy Resolution

**Files inspected:**
- `Source/DotNetWorkQueue/IPolicies.cs`

**Findings:**
- Namespace: `DotNetWorkQueue`
- `Registry` property returns `Polly.Registry.ResiliencePipelineRegistry<string>` — sealed Polly type (confirmed: `using Polly.Registry` at top).
- Two other properties: `Definition` (`PolicyDefinitions`) and `TransportDefinition` (`ConcurrentDictionary<string, TransportPolicyDefinition>`).
- `EnableChaos` bool property.

**Plan implications:**
- Phase 2 bypass unit tests assert "no `_policies.Registry` access". The test pattern: substitute `IPolicies` with NSubstitute, pass a command where `SkipRetry == true`, verify `_policies.Registry` getter was never called. The Phase 1 PoC used this exact approach — valid for all 4 decorator tests.

---

## Section 6: `IConnectionInformation` for Validator Construction

**Files inspected:**
- `Source/DotNetWorkQueue/IConnectionInformation.cs`
- `Source/DotNetWorkQueue.Transport.SqlServer/SQLConnectionInformation.cs`
- `Source/DotNetWorkQueue.Transport.PostgreSQL/SQLConnectionInformation.cs`

**Findings — `IConnectionInformation` interface:**
- Namespace: `DotNetWorkQueue`
- Public surface: `QueueName`, `ConnectionString`, `AdditionalConnectionSettings`, `Server`, `Container`, `ToString()`, `Equals()`, `GetHashCode()`, `Clone()`.
- **No `Database` property on the base interface** — database name is transport-specific.

**Findings — SqlServer `SqlConnectionInformation`:**
- Namespace: `DotNetWorkQueue.Transport.SqlServer`
- `Container` property returns `_catalog` which is populated from `SqlConnectionStringBuilder(value).InitialCatalog` in `ValidateConnection()`.
- `Server` returns `_server` = `builder.DataSource`.
- So for SqlServer: `IConnectionInformation.Container == database name (catalog)`.

**Findings — PostgreSQL `SqlConnectionInformation`:**
- Namespace: `DotNetWorkQueue.Transport.PostgreSQL`
- `Container` property returns `Server` which is populated from `NpgsqlConnectionStringBuilder(value).Database` in `ValidateConnection()`.
- So for PostgreSQL: `IConnectionInformation.Container == database name` (stored in the `_server`/`Server` field — same underlying value, just named oddly).
- **Both transports expose the configured database name via `IConnectionInformation.Container`.**

**Plan implications:**
- `ExternalTransactionValidator` can use `_connectionInfo.Container` to get the configured database name without downcasting to transport-specific types. This avoids sealed-type casts and keeps `Transport.RelationalDatabase` transport-agnostic.
- `IExternalDbNameExtractor.Extract(DbConnection)` returns the database name from the live connection. Phase 3/4 implementations will call `connection.Database` (both `SqlConnection` and `NpgsqlConnection` expose `.Database`).
- The validator comparison: `string.Equals(extractor.Extract(tx.Connection), _connectionInfo.Container, StringComparison)` — comparison semantics injected by each transport's extractor implementation.

---

## Section 7: SimpleInjector Container Abstraction (`IContainer`)

**Files inspected:**
- `Source/DotNetWorkQueue/IContainer.cs`

**Findings:**
- Namespace: `DotNetWorkQueue`
- Registration methods available: `Register<TService, TImpl>(LifeStyles)`, `Register(Type, Type, LifeStyles)`, `Register<TConcrete>(LifeStyles)`, `Register<TService>(Func<TService>, LifeStyles)`, `Register(Type, LifeStyles, Assembly[])`, `Register(Type, IEnumerable<Type>, LifeStyles)`, `RegisterNonScopedSingleton<T>(T)`, `RegisterDecorator(Type, Type, LifeStyles)`, `RegisterDecorator<TService, TDecorator>(LifeStyles)`, `Register(Type, Func<object>, LifeStyles)`, `RegisterConditional(Type, Type, LifeStyles)`, `RegisterConditional<TService, TImpl>(LifeStyles)`, `RegisterCollection<T>(IEnumerable<Type>)`.
- No `RegisterValidator` helper — validators are registered as regular services.
- `LifeStyles` enum: `Transient = 0`, `Singleton = 1` — these are the only two options.

**Plan implications — PARTIAL, architect must verify:**
- `ExternalTransactionValidator` registered via `Register<ExternalTransactionValidator>(LifeStyles.Transient)` in whatever DI registration helper `Transport.RelationalDatabase` uses. Need to confirm whether `Transport.RelationalDatabase` has a shared init/registration helper that SqlServer/PostgreSQL call (likely it does — grep for `RegisterRelationalDatabase` or similar in SqlServer init). Not investigated — architect must locate the registration hook.
- `IExternalDbNameExtractor` is an interface — `Register<IExternalDbNameExtractor, SqlServerExternalDbNameExtractor>(LifeStyles.Transient)` in each transport's init class (Phase 3/4 work).

---

## Section 8: Existing Test Conventions for `Transport.RelationalDatabase.Tests`

**Files inspected:**
- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/Command/SendMessageCommandTests.cs`
- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/Command/DeleteMessageCommandTests.cs`
- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/TestHelpers/AdoNetMockFixture.cs` (lines 1–60)

**Findings:**
- Namespace pattern: `DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.[subfolder]`
- File naming: `[ClassName]Tests.cs` in `Basic/Command/`, `Basic/Query/`, `Basic/QueryHandler/`, etc.
- Test framework: `[TestClass]` + `[TestMethod]` (MSTest 4.x), `NSubstitute.Substitute.For<T>()`
- `AdoNetMockFixture` (sync tests): wraps `IDbConnectionFactory`, `IDbConnection`, `IDbCommand`, `IDataReader`, `IReadColumn`, `ITransactionFactory`, `ITransactionWrapper`, `IDbTransaction` — all via NSubstitute on interfaces.
- `AdoNetAsyncMockFixture` also exists (not read) — presumably uses abstract base classes per CLAUDE.md lesson.
- Command tests (e.g., `SendMessageCommandTests`, `DeleteMessageCommandTests`) are simple property-round-trip tests. No `IDbConnectionFactory` injection in command tests — only in handler tests.

**Plan implications:**
- New `ExternalTransactionValidator` tests: standalone class, no DB connection needed. NSubstitute mocks for `IExternalDbNameExtractor`, and `DbTransaction`/`DbConnection` abstract base classes (not interfaces — per CLAUDE.md async mock lesson, and `DbTransaction.Connection` is on the abstract base).
- Follow namespace `DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic` for new test files.
- Retry decorator tests live in their transport test projects (`Transport.SqlServer.Tests/Decorator/`, `Transport.PostgreSQL.Tests/Decorator/`).

---

## Section 9: `SendMessageCommand` Consumers — Handlers Touch List

**Files inspected (grep output for `SendMessageCommandHandler` across Source):**
- `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandler.cs`
- `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs`
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandler.cs`
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs`

**Findings:**
- Phase 2 does NOT modify any of these 4 handlers. They receive `SendMessageCommand` (or `RelationalSendMessageCommand` which is a subclass of it) and call `messageToSend`/`messageData` — unchanged in Phase 2.
- The handlers are NOT in `Transport.RelationalDatabase` — they are in each transport's project. Phase 3/4 add `HandleExternalTx` forks to them.
- Also present: SQLite, LiteDB, Redis equivalents — these are NOT modified in Phase 2–4 for this feature.

---

## Section 10: Layering Audit — Circular-Reference Risk

**Files inspected:**
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj` (confirmed in Section 2)
- For SqlServer and PostgreSQL project references: PARTIAL — not directly read. Inferring from Phase 1 RESEARCH.md which shows SqlServer decorator files already import `DotNetWorkQueue.Transport.Shared` and the transports build against `Transport.RelationalDatabase` for query/command types.

**Findings (confirmed):**
- `Transport.Shared` → does NOT reference `Transport.RelationalDatabase`. (Shared is the lower layer; RelationalDatabase references Shared.)
- `Transport.RelationalDatabase` → references `Transport.Shared` (confirmed, csproj line 35) and `DotNetWorkQueue` core (csproj line 36).
- `Transport.SqlServer` and `Transport.PostgreSQL` → almost certainly reference `Transport.RelationalDatabase` (they use command/query types from it). **PARTIAL — architect must verify by reading their .csproj files.**

**Plan implications for Decision 2:**
- **Option (B) Derived class is layering-safe:** `RelationalSendMessageCommand` lives in `Transport.RelationalDatabase`, which already references `Transport.Shared` (where `SendMessageCommand` lives). No circular dependency.
- **Option (A) is also safe:** adding `ExternalTransaction` and `SkipRetry` to `SendMessageCommand` in `Transport.Shared` without an interface marker — decorator branch tests `SkipRetry` as a plain bool property. `IRetrySkippable` in `Transport.RelationalDatabase` remains a capability contract not implemented by base.
- **The chosen approach (B) requires SqlServer/PostgreSQL decorators to reference `Transport.RelationalDatabase`** to see `IRetrySkippable`. If those projects don't already have that project reference, it must be added. Verify before writing the plan — if the reference already exists, option B costs nothing extra; if not, it's 2 new .csproj edits.

---

## Section 11: `IAdditionalMessageData` and `QueueMessage<,>`

**Files inspected:**
- `Source/DotNetWorkQueue/IAdditionalMessageData.cs`
- `Source/DotNetWorkQueue/Messages/QueueMessage.cs`

**Findings:**
- `IAdditionalMessageData`: namespace `DotNetWorkQueue`, file `Source/DotNetWorkQueue/IAdditionalMessageData.cs`.
- `QueueMessage<TMessage, TMessageData>`: namespace `DotNetWorkQueue.Messages`, file `Source/DotNetWorkQueue/Messages/QueueMessage.cs`. Generic constraint: `where TMessageData : IAdditionalMessageData`.

**Plan implications:**
- `IRelationalProducerQueue<T>` overloads use `List<QueueMessage<TMessage, IAdditionalMessageData>>` — both types live in `DotNetWorkQueue` (core), which `Transport.RelationalDatabase` already references. No new references needed.
- Fully qualified type refs in the interface file: `using DotNetWorkQueue; using DotNetWorkQueue.Messages;`.

---

## Section 12: Spike PoC Removal — Scope Confirmation

**Files inspected:**
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs` — confirmed present by grep (file appears in `SendMessageCommandHandler` search results and Phase 1 memo)

**Findings:**
- Exactly one file to delete: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs`.
- No other spike artifacts found (no spike folders, no ref files, no other `_Spike` prefixed files in the grep results).
- Phase 1 memo explicitly states: "Phase 2's first task deletes this file."

---

## Summary

### Decision 2 Answer: Option (B) — Derived Class

**Recommended: Option (B) — `RelationalSendMessageCommand : SendMessageCommand, IRetrySkippable`.**

Rationale:
1. `SendMessageCommand` is public, non-sealed — derivation requires zero changes to the base class signature beyond adding `ExternalTransaction { get; }` as a new property.
2. `Transport.RelationalDatabase` already references `Transport.Shared` — the derived class can inherit cleanly without any new project references within the foundation layer.
3. The decorator branch (`if (command is IRetrySkippable s && s.SkipRetry)`) preserves the `is`-cast pattern from the spike PoC.
4. The only cost of (B) vs. (A): SqlServer and PostgreSQL decorator projects must reference `Transport.RelationalDatabase` (to see `IRetrySkippable`). If they already do (likely — they use RelationalDatabase query/command types), this is free. **Architect must verify the 2 transport .csproj files.**

If those projects do NOT reference `Transport.RelationalDatabase`, fall back to Option (A): add `ExternalTransaction` and `SkipRetry` directly to `SendMessageCommand` in `Transport.Shared`, no interface marker on the base — the decorator branch tests `command.SkipRetry` via a direct cast to `SendMessageCommand` (since the decorator is open-generic over `TCommand`, a cast is needed anyway). `IRetrySkippable` in `Transport.RelationalDatabase` becomes a standalone interface not implemented by the base — still useful as future capability contract.

### Full File-Touch List for Phase 2

| Task | Action | File |
|------|--------|------|
| 1 | DELETE | `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs` |
| 2 | MODIFY | `Source/DotNetWorkQueue.Transport.Shared/Basic/Command/SendMessageCommand.cs` — add `ExternalTransaction { get; }` property (and `SkipRetry` if option A) |
| 3 | NEW | `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRetrySkippable.cs` — marker interface |
| 4 | NEW | `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/Command/RelationalSendMessageCommand.cs` — derived command (if option B) |
| 5 | NEW | `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRelationalProducerQueue.cs` — new interface with 6 overloads |
| 6 | NEW | `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalProducerQueue.cs` — concrete (inherits `ProducerQueue<T>`) |
| 7 | NEW | `Source/DotNetWorkQueue.Transport.RelationalDatabase/IExternalDbNameExtractor.cs` — validation interface |
| 8 | NEW | `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/ExternalTransactionValidator.cs` — standalone validator |
| 9 | MODIFY | `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecorator.cs` — add bypass branch |
| 10 | MODIFY | `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs` — add bypass branch |
| 11 | MODIFY | `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecorator.cs` — add bypass branch |
| 12 | MODIFY | `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs` — add bypass branch |
| 13 | POSSIBLY MODIFY | `Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj` — add reference to Transport.RelationalDatabase if not present (VERIFY) |
| 14 | POSSIBLY MODIFY | `Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj` — same (VERIFY) |
| 15 | NEW | `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/ExternalTransactionValidatorTests.cs` — 5 test cases |
| 16 | NEW | `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/RetryCommandHandlerOutputDecoratorBypassTests.cs` — 2 test cases (sync + bypass) |
| 17 | NEW | `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/RetryCommandHandlerOutputDecoratorAsyncBypassTests.cs` — 2 test cases (async + bypass) |
| 18 | NEW | `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Decorator/RetryCommandHandlerOutputDecoratorBypassTests.cs` — mirror SqlServer |
| 19 | NEW | `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Decorator/RetryCommandHandlerOutputDecoratorAsyncBypassTests.cs` — mirror SqlServer |

### Surprises vs Phase 1 RESEARCH.md

1. **`IRetrySkippable` placement changed.** Phase 1 memo placed the marker in `Transport.Shared`. CONTEXT-2 moved it to `Transport.RelationalDatabase`. The derived-command approach (option B) makes this work cleanly without new circular references. Phase 1's code snippet in the memo is superseded by CONTEXT-2 Decision 2.

2. **`IConnectionInformation.Container` is the portable DB-name accessor.** Both SqlServer and PostgreSQL populate `Container` with the database name (from `InitialCatalog` and `Database` fields of their respective connection string builders respectively). The validator can use `_connectionInfo.Container` without downcasting. Phase 1 did not surface this — it assumed `IExternalDbNameExtractor` would need to bridge from `IConnectionInformation` to the DB name. It still bridges from `DbConnection.Database` to a string, but the configured-DB side is available directly via `Container`.

3. **PostgreSQL `Container` stores DB name in the `_server` field** (named confusingly — `Container => Server => _server => builder.Database`). This is correct but warrants a comment in the validator implementation.

4. **`ProducerQueue<T>` is non-sealed** — `RelationalProducerQueue<T>` can inherit cleanly without any unsealing.

5. **Decorator line numbers match Phase 1:** Guard.NotNull is line 51 in SqlServer sync, bypass insertion is after that line. No drift.

### Gaps That Would Block Plan Creation

1. **SqlServer + PostgreSQL .csproj reference to `Transport.RelationalDatabase`** — not verified. Architect must read those two .csproj files before deciding option A vs B. If the reference is absent, option B adds 2 .csproj edits (acceptable). If present, option B is zero-cost.

2. **`Transport.RelationalDatabase` DI registration hook** — does the project have a shared `RegisterRelationalDatabase(IContainer)` method that SqlServer/PostgreSQL init classes call? If yes, `ExternalTransactionValidator` can be registered there. If no, architect must decide where registration goes (likely each transport's init class in Phase 3/4 for the validator, since it needs `IExternalDbNameExtractor` which is transport-specific anyway). Not a blocker for Phase 2 — the validator class can be written without DI registration; DI wiring is Phase 3/4 anyway.

3. **`RelationalProducerQueue<T>` constructor signature** — inheriting `ProducerQueue<T>` requires calling the base constructor with all 6 parameters (`QueueProducerConfiguration`, `ISendMessages`, `IMessageFactory`, `ILogger`, `GenerateMessageHeaders`, `AddStandardMessageHeaders`). The architect must verify SimpleInjector can resolve `RelationalProducerQueue<T>` with those constructor params when registered as `IProducerQueue<T>`. The existing `ProducerQueue<T>` registration pattern is the model.

---

## Sources

All findings from direct file inspection of repository at commit `HEAD` (branch `master`, 2026-05-13). No external URLs consulted — all data is in-repo.

Files read:
1. `Source/DotNetWorkQueue.Transport.Shared/Basic/Command/SendMessageCommand.cs`
2. `Source/DotNetWorkQueue.Transport.Shared/Basic/SendMessages.cs`
3. `Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj`
4. `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecorator.cs`
5. `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs`
6. `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecorator.cs`
7. `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs`
8. `Source/DotNetWorkQueue/IProducerQueue.cs`
9. `Source/DotNetWorkQueue/Queue/ProducerQueue.cs`
10. `Source/DotNetWorkQueue/QueueContainer.cs`
11. `Source/DotNetWorkQueue/IPolicies.cs`
12. `Source/DotNetWorkQueue/IConnectionInformation.cs`
13. `Source/DotNetWorkQueue.Transport.SqlServer/SQLConnectionInformation.cs`
14. `Source/DotNetWorkQueue.Transport.PostgreSQL/SQLConnectionInformation.cs`
15. `Source/DotNetWorkQueue/IContainer.cs`
16. `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/Command/SendMessageCommandTests.cs`
17. `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/Command/DeleteMessageCommandTests.cs`
18. `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/TestHelpers/AdoNetMockFixture.cs`
19. `Source/DotNetWorkQueue/IAdditionalMessageData.cs`
20. `Source/DotNetWorkQueue/Messages/QueueMessage.cs`
21. `.shipyard/phases/2/CONTEXT-2.md`
22. `.shipyard/ROADMAP.md`
23. `.shipyard/phases/1/RESEARCH.md`
24. `.shipyard/notes/phase-1-polly-bypass-spike.md`
