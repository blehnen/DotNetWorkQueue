# Phase 2 Context: Foundation Layer (RelationalDatabase + Marker + Decorator Branches)

## Phase Scope (from ROADMAP.md + user decision below)

**Phase 2 = vertical slice.** Foundation plumbing in `Transport.RelationalDatabase` PLUS the `IRetrySkippable` marker and the four retry-decorator branches in SqlServer + PostgreSQL. End-of-Phase-2 deliverable: the bypass mechanism actually works (a `SendMessageCommand` with `ExternalTransaction != null` will skip the Polly pipeline), even though no caller-tx handler fork exists yet. Phase 3 (SqlServer) and Phase 4 (PostgreSQL) add the handler forks and producer wiring that make the full feature end-to-end.

**Phase risk classification:** low (additive plumbing) plus the four decorator branches, which are 3-line, RetryCommandHandlerOutputDecorator-internal additions. Risk #1 (Polly bypass cleanness) was closed by Phase 1; this phase is the first cash-in on that closure.

**Phase size:** M (4–6 hours) per ROADMAP, ticking up slightly to account for the extra 4 decorator-branch additions and their tests. Realistic: 5–7 hours.

## User Decisions

### Decision 1: Phase 2 scope — VERTICAL SLICE

Phase 2 ships all of:

1. `IRelationalProducerQueue<TMessage> : IProducerQueue<TMessage>` interface in `Transport.RelationalDatabase` (six overload signatures per PROJECT.md §Functional New Public API; sync + async; single + batch + with/without `IAdditionalMessageData`).
2. `RelationalProducerQueue<TMessage>` concrete in `Transport.RelationalDatabase` (see Decision 3 for hook shape).
3. `SendMessageCommand.ExternalTransaction { get; }` optional property added to the existing class in `Transport.Shared` (defaults to null, preserves existing self-managed path).
4. `IExternalDbNameExtractor` interface in `Transport.RelationalDatabase` (validation contract per PROJECT.md §Validation). No implementations in Phase 2 — those live with each transport in Phase 3/4.
5. `ExternalTransactionValidator` standalone class in `Transport.RelationalDatabase` (see Decision 4).
6. `IRetrySkippable` marker interface in `Transport.RelationalDatabase` (see Decision 2 + layering note).
7. `IRetrySkippable` branch added to both SqlServer retry decorators (`RetryCommandHandlerOutputDecorator.cs` sync + `RetryCommandHandlerOutputDecoratorAsync.cs` async).
8. `IRetrySkippable` branch added to both PostgreSQL retry decorators (sync + async).
9. XML doc comments on every new public type.
10. Unit tests for the validator (5 cases per PROJECT.md §Success Criteria), the marker-bypass branch in each retry decorator (sync + async on both transports = 4 tests), and a smoke test confirming the `SendMessageCommand.ExternalTransaction` property dispatch works.

**Why vertical slice:** Phase 1's spike already proved the marker mechanism works in a `_SpikePatchedRetryDecorator`. The production decorator branches are mechanical (3 lines + a Guard.NotNull check). Splitting marker addition across Phases 2/3/4 makes the bypass mechanism land in three different review cycles. Vertical slice keeps the cross-cutting "bypass works" concern in one phase and makes Phase 3/4 narrowly focused on transport-specific handler forks + DI wiring.

**Rejected:** Strict-ROADMAP scope (foundation-only). Pushes 4 decorator-branch additions across Phases 3 + 4, doubling the cross-phase coordination surface.

### Decision 2: `IRetrySkippable` marker location — `Transport.RelationalDatabase`

**Marker interface lives in `DotNetWorkQueue.Transport.RelationalDatabase`** (project namespace `DotNetWorkQueue.Transport.RelationalDatabase`). The four retry decorators in `Transport.SqlServer/Decorator/` and `Transport.PostgreSQL/Decorator/` add a project reference to `Transport.RelationalDatabase` (if not already present — both transports' main projects almost certainly already reference RelationalDatabase since they sit one floor up).

**Layering note (architect must resolve):**

The `SendMessageCommand` class lives in `Transport.Shared`. The Phase 1 spike memo proposed having `SendMessageCommand` directly implement `IRetrySkippable`, which would require Transport.Shared to reference Transport.RelationalDatabase — a circular layering violation (RelationalDatabase already depends on Shared).

The architect must choose one of:

- **(A) Property-on-base, no marker on base:** Add `ExternalTransaction` and a virtual `SkipRetry { get => ExternalTransaction != null; }` property directly to `SendMessageCommand` in Transport.Shared without an interface. The decorator branch tests `command.SkipRetry` directly. The `IRetrySkippable` interface in Transport.RelationalDatabase becomes an explicit public *capability* contract for any future relational-only command but is not implemented by the base. **Loses the "marker is checked via `is`-cast" pattern from the spike** but is the simplest layering-safe option.

- **(B) Derived command class:** Add `ExternalTransaction` to base `SendMessageCommand` (Transport.Shared). Add `RelationalSendMessageCommand : SendMessageCommand, IRetrySkippable` (Transport.RelationalDatabase) that overrides `SkipRetry`. `RelationalProducerQueue<T>` constructs the derived class. Decorator branch: `if (command is IRetrySkippable s && s.SkipRetry) return _decorated.Handle(command);`. Preserves the spike's `is`-cast pattern; adds one new public type. **Recommended if the architect can satisfy the layering with a clean derived class.**

- **(C) Move marker to Transport.Shared anyway:** Re-evaluate Decision 2 against the layering constraint. Only do this if (A) and (B) are both judged worse than violating the user's stated preference — flag back to the user before committing to it.

Default architect preference: **(B) Derived command class** unless it forces non-trivial changes to the producer factory or DI wiring. Fall back to (A) if (B) creates surprise.

### Decision 3: Caller-tx hook on `RelationalProducerQueue<T>` — VIRTUAL METHOD

`RelationalProducerQueue<TMessage>` in `Transport.RelationalDatabase` exposes a `virtual` (or `protected virtual`) method that performs the caller-tx send:

```csharp
protected virtual IQueueOutputMessage SendWithExternalTransaction(
    TMessage message,
    IAdditionalMessageData? data,
    DbTransaction transaction)
{
    throw new InvalidOperationException(
        "Caller-supplied-transaction send is not implemented for this transport. " +
        "Override SendWithExternalTransaction or use a SqlServer/PostgreSQL producer.");
}
```

- Async equivalent: `protected virtual Task<IQueueOutputMessage> SendWithExternalTransactionAsync(...)`.
- Batch equivalent: `protected virtual IQueueOutputMessages SendWithExternalTransactionBatch(...)` and async.
- Phase 3 SqlServer derives from `RelationalProducerQueue<T>` (or sets a per-transport override path — architect's call) and overrides these four virtual methods to invoke the SqlServer `SendMessageCommandHandler` directly.
- Phase 4 PostgreSQL mirrors.
- Default implementation throws `InvalidOperationException` (NOT `NotImplementedException` — per CLAUDE.md "compile errors over runtime errors"; `InvalidOperationException` with a clear message is appropriate for a misregistered transport).

**Rejected:** Constructor delegate (harder to debug, DI-trace). Abstract base (forces every relational transport to provide an impl, including any future SQLite outbox support which is currently out of scope but planned-extensible per PROJECT.md non-goals).

### Decision 4: Validator surface — STANDALONE CLASS

`ExternalTransactionValidator` is a standalone class in `Transport.RelationalDatabase`:

```csharp
public sealed class ExternalTransactionValidator
{
    private readonly IExternalDbNameExtractor _extractor;
    private readonly IConnectionInformation _connectionInfo; // queue's configured DB

    public ExternalTransactionValidator(IExternalDbNameExtractor extractor, IConnectionInformation connectionInfo) { ... }

    public void Validate(DbTransaction transaction)
    {
        // 4 checks per PROJECT.md §Validation:
        // 1. transaction != null  -> ArgumentNullException
        // 2. transaction.Connection != null  -> InvalidOperationException ("transaction disposed/completed")
        // 3. transaction.Connection.State == ConnectionState.Open  -> InvalidOperationException
        // 4. extractor.Extract(transaction.Connection) equals _connectionInfo's DB  -> InvalidOperationException with both names
    }
}
```

- Registered in `Transport.RelationalDatabase` DI registration helper as transient or scoped — architect decides per CONVENTIONS.md.
- Per-provider `IExternalDbNameExtractor` registered by each transport's init class in Phase 3/4. Phase 2 ships only the interface, NOT the implementations.
- Validator unit tests (5 cases): each of the 4 negative paths + the happy-path. Uses NSubstitute for `DbTransaction`/`DbConnection`/`IExternalDbNameExtractor` mocks. CLAUDE.md lesson: mock `DbTransaction`/`DbConnection` (abstract base classes), not `IDbTransaction`/`IDbConnection` interfaces.

**Rejected:** Methods on the producer. Harder to unit-test in isolation; tests would need to construct the full producer with DI dependencies.

## Hard Rules / Cross-Cutting Constraints

These come from PROJECT.md, CLAUDE.md, and Phase 1 lessons. The architect must encode them in plan task acceptance criteria:

- **Layering purity:** `Transport.RelationalDatabase` MUST NOT reference `Microsoft.Data.SqlClient` or `Npgsql`. ROADMAP §Phase 2 invariant.
- **No sealed-type casts in handlers:** Per CLAUDE.md. Handlers operate on `IDbConnection`/`IDbCommand` — except `tx.Connection` which is `DbConnection` (fine, never cast further).
- **`IDbConnectionFactory` injection** for any test seam needing connection mocking — per CLAUDE.md.
- **Async handler mocking:** Mock `DbConnection`/`DbCommand`/`DbDataReader` abstract bases, NOT `IDbConnection`/etc. interfaces — per CLAUDE.md. Only relevant when Phase 3/4 lands; Phase 2 has no async handler code.
- **MSTest 4.x assertions:** `Assert.AreEqual`, `Assert.ThrowsExactly<T>` — NEVER `Assert.ThrowsException<>`. Per CLAUDE.md.
- **LGPL-2.1 license header** on every new `.cs` file.
- **Build cleanliness:** `Transport.RelationalDatabase` + both transport main projects must build clean against net10.0 + net8.0 with `TreatWarningsAsErrors` (Release config) — XML docs required on every new public type.
- **No regressions:** Existing SQLite, LiteDb, Memory, Redis transport tests must still pass. The `ExternalTransaction` property add to `SendMessageCommand` is additive and defaults to null, so the existing self-managed path is unaffected. Architect must include a verification step that runs the existing handler unit-test suites unchanged.
- **PR-trigger Jenkins:** Per CLAUDE.md, full Jenkins validation requires a draft PR. Phase 2 verification ideally opens a draft PR to confirm the 14-stage matrix is green; if the user prefers to defer that to Phase 3+, the plan should note that as deferred.

## Exit Criteria for Phase 2

1. **New public surface** (10 items per Decision 1) exists and is XML-doc'd.
2. **Layering invariant** holds: `grep -rn "Microsoft.Data.SqlClient\|Npgsql\b" Source/DotNetWorkQueue.Transport.RelationalDatabase/` returns no matches.
3. **Bypass mechanism works**: a unit test on each retry decorator (SqlServer sync + async, PostgreSQL sync + async = 4 tests) confirms that a command with `SkipRetry == true` (or implementing `IRetrySkippable` with `SkipRetry == true`, depending on Decision 2 resolution) invokes the inner handler exactly once with no `IPolicies.Registry` access.
4. **Validator unit tests** (5 cases) green.
5. **Build clean** on net10.0 + net8.0 with `TreatWarningsAsErrors`.
6. **No regressions** in existing SqlServer + PostgreSQL + RelationalDatabase + Shared test suites.
7. **Capability cast smoke test**: a SimpleInjector resolution test confirms `container.GetInstance<IProducerQueue<MyEvent>>() is IRelationalProducerQueue<MyEvent>` is `true` for SqlServer (and same for PostgreSQL in Phase 4 — Phase 2 likely cannot wire DI without the transport-specific init classes, so this may be deferred to Phase 3/4. Architect: confirm or push back.).
8. **Spike PoC removal** as Phase 2's first task: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs` is deleted. Phase 1 memo persists; PoC is throwaway-by-design.

## Out of Scope (Phase 2)

- Transport-specific `IExternalDbNameExtractor` implementations (`SqlServerExternalDbNameExtractor`, `PostgreSqlExternalDbNameExtractor`) — Phase 3 + 4.
- Transport-specific handler forks (`HandleExternalTx`) on `SendMessageCommandHandler[Async]` for SqlServer + PostgreSQL — Phase 3 + 4.
- `SqlServerMessageQueueInit` / `PostgreSQLMessageQueueInit` DI registration of `IRelationalProducerQueue<T>` and extractor — Phase 3 + 4.
- The capability-cast smoke test (deferred to Phase 3/4 — see exit criterion 7).
- Integration tests against real databases — Phase 6.
- `docs/outbox-pattern.md` user-facing documentation — Phase 7.
- Memory/Redis/LiteDb/SQLite negative-path tests — Phase 5.

## Dependencies

- **Phase 1 spike** (complete). Marker mechanism choice ratified.
- The throwaway PoC at `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs` must be deleted by Phase 2's first task (Exit Criterion 8) — preserves the spike's invariant that the PoC is throwaway-by-design.
