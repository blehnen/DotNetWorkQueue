# CRITIQUE: Phase 3 Plan Feasibility

**Initial verdict:** REVISE
**Post-revision verdict:** READY (2026-05-18 — architect applied factory-delegate pattern to PLAN-1.1, gated receive-path setter in PLAN-2.1, and added two option-driven smoke tests to PLAN-2.2)

---

## Per-plan findings

### PLAN-1.1 — Class + DI registration

**File paths exist:** PASS
- `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs` exists; outbox-block registration sits at lines 64-73 with the `//override so that we can use schema as needed` comment at line ~75 as the insertion anchor (verified). ✓
- `Source/DotNetWorkQueue.Transport.SqlServer/Basic/ConnectionHolder.cs` exists; `Transaction` property is `public SqlTransaction Transaction { get; set; }` (lines 89-101). ✓
- License header source `ConnectionHolder.cs:1-18` exists. ✓

**API surface matches:** PARTIAL
- `WorkerNotification` ctor signature confirmed (`IHeaders`, `IQueueCancelWork`, `TransportConfigurationReceive`, `ILogger`, `IMetrics`, `ActivitySource`). ✓
- `IRelationalWorkerNotification.Transaction` is typed `DbTransaction` (non-nullable, per Phase 2 `IRelationalWorkerNotification.cs:65` and the XML doc on lines 60-64: **"Never null when the containing interface is implemented"**). The plan's design relies on this property returning null when the underlying holder lacks a transaction — that violates the Phase 2 interface contract. ✗

**🚨 CRITICAL — registration model conflicts with PROJECT.md spec:**

`PROJECT.md` §Functional New Public API lines 35-38 explicitly requires:
> Per-transport `WorkerNotification` factories on SqlServer, PostgreSQL, and SQLite branch on `EnableHoldTransactionUntilMessageCommitted`:
> - Option `true` → construct a notification class that implements `IRelationalWorkerNotification`
> - Option `false` → construct the existing notification class that does NOT implement `IRelationalWorkerNotification`

`PROJECT.md` §Success Criteria #2 states:
> "With the option false, the cast fails on the same transport."

`ROADMAP.md` line 63-64 also explicitly requires option-driven branching.

**PLAN-1.1's chosen approach (unconditional `container.Register<IWorkerNotification, SqlServerRelationalWorkerNotification>(LifeStyles.Transient)` regardless of option) does NOT satisfy this.** With unconditional registration:
- `is IRelationalWorkerNotification` succeeds even when the user sets `EnableHoldTransactionUntilMessageCommitted = false`.
- `Transaction` returns null — but the Phase 2 interface contract says it's non-null when the interface is implemented.
- User-handler code following the documented `if (notification is IRelationalWorkerNotification r) { ... r.Transaction.Connection ... }` pattern would throw `NullReferenceException` at production runtime when the user misconfigures the option — exactly the failure mode the capability-cast pattern is designed to prevent.
- CONTEXT-3 §2 explicitly required "A second case with the option `false` confirms the cast returns `null` cleanly" — i.e., the cast must FAIL when option false. PLAN-1.1 cannot satisfy that test.

**Remediation:** Switch to option-driven registration. Three viable patterns in SimpleInjector:

1. **Factory-delegate registration (simplest, recommended):**
   ```csharp
   container.Register<IWorkerNotification>(() =>
   {
       var optionsFactory = container.GetInstance<ITransportOptionsFactory>();
       var options = (SqlServerMessageQueueTransportOptions)optionsFactory.Create();
       return options.EnableHoldTransactionUntilMessageCommitted
           ? container.GetInstance<SqlServerRelationalWorkerNotification>()
           : container.GetInstance<WorkerNotification>();
   }, LifeStyles.Transient);
   ```
   Needs `Register<SqlServerRelationalWorkerNotification>(LifeStyles.Transient)` and `Register<WorkerNotification>(LifeStyles.Transient)` registered first so the lambda can resolve them. The factory delegate inspects options at resolution-time (per-message), so option flips between scope creation work correctly. This is exactly the pattern used at `SQLServerMessageQueueInit.cs:110-114` for `IBaseTransportOptions`.

2. **RegisterConditional with PredicateContext** — possible but messier; the predicate fires once per resolution but needs late-binding to options.

3. **Two separate `WorkerNotification` registrations under different keyed resolutions** — over-engineered for one decision.

**Other PLAN-1.1 issues (smaller):**
- `Transaction` property declaration: PLAN-1.1 implies `=> ConnectionHolder?.Transaction;`. Since `IRelationalWorkerNotification.Transaction` is non-nullable `DbTransaction`, this produces a CS8603 "possible null reference return" warning under nullable reference types enabled… but the SqlServer project has NRT disabled per researcher §1, so the warning is suppressed. Confirm by checking csproj. If NRT is later enabled on this csproj, the `?.` becomes a build break. Minor — flag as a follow-up.
- Sealed-type cast risk: the plan must guarantee no `(SqlTransaction)` cast in the new file. Confirmed by grep-guard in the verification commands.

---

### PLAN-2.1 — Receive-path wiring

**File paths exist:** PASS
- `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueReceive.cs` exists; `GetConnectionAndSetOnContext(IMessageContext context)` at line 162 creates the `ConnectionHolder` and stores it via `context.Set(_sqlHeaders.Connection, connection)`. Insertion point for the new setter call is right after `return connection;` at line ~178, OR more precisely: just before the return, by mutating `context.WorkerNotification` (which exists per `IMessageContext.cs:119`). ✓
- `IMessageContext.WorkerNotification` is at line 119 of `Source/DotNetWorkQueue/IMessageContext.cs`. ✓ Property is get-only on the interface, but its IMPL may be settable — verify in the implementing class. Architect should re-check.

**Conditional behavior gap:**
- PLAN-2.1 says "the assignment happens regardless of `EnableHoldTransactionUntilMessageCommitted`" and notes that the new `Transaction` returns null when option=false. This is consistent with PLAN-1.1's flawed unconditional-registration design. If PLAN-1.1 is revised to option-driven registration (Pattern 1 above), PLAN-2.1's wiring should ALSO branch — or alternatively, PLAN-2.1's setter can run unconditionally and the cast check protects against null usage (only the option-true path produces a `SqlServerRelationalWorkerNotification` to cast to).
- **Recommended revision:** keep PLAN-2.1's logic but include a `notification is SqlServerRelationalWorkerNotification relational` cast inside the receive path so the setter only fires when the relational impl is the one in scope. This way the receive path works correctly regardless of whether registration is conditional or not.

**Verify commands:** runnable as written. ✓

---

### PLAN-2.2 — Unit tests + SimpleInjector smoke

**Test file location:** PASS
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/` exists with sibling test files (`SqlServerExternalDbNameExtractorTests.cs`, `SqlServerRelationalProducerQueueTests.cs`, etc.) following the same naming pattern. ✓

**MSTest 3.x:** Verified — sibling tests use `Assert.ThrowsExactly<T>` (per researcher §5). The plan task descriptions need to confirm `Assert.ThrowsExactly`, not `Assert.ThrowsException`. ✓

**SimpleInjector smoke test:** PLAN-2.2 (per researcher §5) uses `QueueContainer<SqlServerMessageQueueInit>` + `Assert.ThrowsExactly<SqlException>` to trigger container verification. Works without a live DB. ✓

**🚨 Smoke test requirement vs PLAN-1.1's design:**
PLAN-2.2 reportedly includes a test that "confirms the cast returns `null` cleanly" when option=false (per CONTEXT-3 §2). If PLAN-1.1 ships unconditional registration, this test will FAIL because the cast will succeed (returning a `SqlServerRelationalWorkerNotification` instance, not null).

If PLAN-1.1 is revised to factory-delegate registration, this test passes naturally.

---

## Cross-cutting

**Forward references:** PASS — PLAN-2.1 and PLAN-2.2 depend on PLAN-1.1; declared correctly.

**Hidden dependencies:** Receive-path edit in PLAN-2.1 may affect existing receive-path tests. The plan does not enumerate which existing tests might be touched. Architect should add an acceptance criterion: "Pre-existing `SqlServerMessageQueueReceive` tests in `Source/DotNetWorkQueue.Transport.SqlServer.Tests/` still pass after the wiring change."

**Complexity flags:** Each plan touches ≤2 files. ≤3 tasks per plan. ✓

---

## Most load-bearing assumption check

**Researcher claimed:** "DI registration override goes inside `SQLServerMessageQueueInit.RegisterImplementations()` after the outbox block (~line 73). No `RegisterConditional` needed."

**Reality:** Confirmed for the placement. INCORRECT on "no conditional needed" — PROJECT.md spec REQUIRES option-driven registration. The researcher missed that PROJECT.md §Functional New Public API explicitly mandates the branch. The architect followed researcher's recommendation literally, producing a plan that ships a working interface but violates the capability-cast contract.

**Resolution:** Architect revises PLAN-1.1 to use factory-delegate registration (Pattern 1 above). PLAN-2.1 and PLAN-2.2 may need minor adjustments to keep their tests/wiring aligned with the conditional model.

---

## Resolution (post-revision)

The architect applied the factory-delegate pattern (Pattern 1) verbatim:

- **PLAN-1.1** now pre-registers both concrete classes as Transient and registers `IWorkerNotification` via a factory delegate that reads `SqlServerMessageQueueTransportOptions.EnableHoldTransactionUntilMessageCommitted` at each resolution. Acceptance criteria include a grep guard forbidding the unconditional `Register<IWorkerNotification, SqlServerRelationalWorkerNotification>` form.
- **PLAN-2.1**'s receive-path setter is gated on `if (context.WorkerNotification is SqlServerRelationalWorkerNotification relational)` so option=false (no relational impl in scope) skips the setter cleanly. A new acceptance criterion confirms existing receive-path tests still pass.
- **PLAN-2.2** replaces the single smoke test with two option-driven smokes (`Resolves_Relational_When_HoldTransaction_Enabled` and `Resolves_NonRelational_When_HoldTransaction_Disabled`). Test seam left to builder discretion among three documented options (`QueueContainer` register-lambda, direct SimpleInjector container build, or init options seam).

Wave/plan layout preserved. All constraints honored. Plans READY for build.

---

## Original verdict rationale (kept for history)

**REVISE.** Two blocking issues, both stemming from the same root cause — PLAN-1.1's unconditional `Register` instead of option-driven factory:

1. Violates PROJECT.md §Functional New Public API lines 35-38 (explicit option-driven branch required).
2. Violates PROJECT.md §Success Criteria #2 ("With the option false, the cast fails on the same transport").

A third concern is design-correctness: when option=false, the unconditional model would let `is IRelationalWorkerNotification` succeed but `Transaction` return null, defeating the capability-cast pattern's whole purpose and producing exactly the user-visible NullReferenceException the design aims to prevent.

The remediation is mechanical — switch one `container.Register<IWorkerNotification, SqlServerRelationalWorkerNotification>` line to a factory-delegate that inspects options (Pattern 1 above). Researcher §6 even documented this pattern as available in the codebase (`SQLServerMessageQueueInit.cs:110-114`). One revision cycle should resolve it.

PLAN-2.1 and PLAN-2.2 are otherwise sound; minor adjustments to align with the conditional-registration model are needed.
