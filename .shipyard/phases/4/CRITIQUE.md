# Phase 4 Plan Critique (Feasibility Stress Test)

**Date:** 2026-05-14
**Reviewer:** Senior Verification Engineer
**Scope:** Phase 4 plans PLAN-1.1, PLAN-2.1, PLAN-2.2 — feasibility stress test (Step 6a of `/shipyard:plan`)
**Note:** Coverage-mode verification already PASSED (`.shipyard/phases/4/VERIFICATION.md`). This pass adds concrete grounding against the live tree.

## Overall Verdict: READY

All file paths exist, API surfaces match plan assumptions exactly, insertion-point line numbers are correct against the current codebase, verification commands are syntactically sound, and Wave 2 plans are file-disjoint. No blocking infeasibility found.

---

## Per-Plan Findings

### PLAN-1.1 (Wave 1 — Foundation)

#### File paths
| Path | Status | Evidence |
|------|--------|----------|
| `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs` | EXISTS | `ls -la` confirms file (13597 bytes) |
| `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/` | EXISTS | Directory listed; contains existing tests (`PostgreSqlJobQueueCreationTests.cs` et al.) |
| New `PostgreSqlExternalDbNameExtractor.cs` | TO CREATE | Plan creates at correct path under `Basic/` (matches existing layout) |
| New `PostgreSqlRelationalProducerQueue.cs` | TO CREATE | Plan creates at correct path under `Basic/` |
| `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerExternalDbNameExtractor.cs` | EXISTS (template) | Phase 3 reference confirmed |
| `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalProducerQueue.cs` | EXISTS (template) | Phase 3 reference confirmed |
| `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRelationalProducerQueue.cs` | EXISTS | Phase 2 interface at line 40 |
| `Source/DotNetWorkQueue.Transport.RelationalDatabase/IExternalDbNameExtractor.cs` | EXISTS | Phase 2 interface at line 34 |
| `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/ExternalTransactionValidator.cs` | EXISTS | Phase 2 sealed class at line 42 |
| `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalProducerQueue.cs` | EXISTS | Phase 2 base class at line 39 with 4 `protected virtual SendWithExternalTransaction*` hooks at lines 105/121/135/149 |

#### API surface
- `RelationalProducerQueue<T>` 6-param base ctor (`QueueProducerConfiguration, ISendMessages, IMessageFactory, ILogger, GenerateMessageHeaders, AddStandardMessageHeaders`) confirmed in source — `base(configuration, sendMessages, messageFactory, log, generateMessageHeaders, addStandardMessageHeaders)` call in plan body line 484-485 is valid.
- 4 virtual hooks (`SendWithExternalTransaction`, `SendWithExternalTransactionAsync`, `SendWithExternalTransactionBatch`, `SendWithExternalTransactionBatchAsync`) all confirmed present on the base type.
- `ContainerWrapper.RegisterConditional(Type, Type, LifeStyles)` overload at `Source/DotNetWorkQueue/IoC/ContainerWrapper.cs:179` — exact signature plan uses.
- `ICommandHandlerWithOutput<SendMessageCommand, long>` and `ICommandHandlerWithOutputAsync<SendMessageCommand, long>` confirmed registered in `PostgreSQLMessageQueueInit.cs:209,213` (decorator wrap path) — DI will supply these to the producer subclass.

#### Insertion-point line numbers
- `init.RegisterStandardImplementations(...)` at `PostgreSQLMessageQueueInit.cs:62` — MATCHES plan.
- `//**all` at `PostgreSQLMessageQueueInit.cs:64` — MATCHES plan.
- Line 63 is blank; insertion at line 63 between lines 62 and 64 is unambiguous.

#### Verification commands
All commands in the `## Verification` section parse cleanly. The `awk '/public PostgreSqlRelationalProducerQueue\(/,/\)$/' ... | tr -cd ',' | wc -c` comma-counting trick is the same trusted pattern used in Phase 3 PLAN-1.1 verification. Expected count of 10 (11 params → 10 commas) is correct.

#### Complexity flag
- Files touched: 3 (1 init modified, 2 new source files) + 2 new test files = 5 files in 2 directories. Well under thresholds (>10 files / >3 dirs).
- Test count: 9 (2 extractor + 6 producer + 1 capability-cast). Matches plan claim.

#### Notes / observations
- Phase 3 SqlServer producer also captures a `_generateMessageHeaders` private field in its ctor body (`SqlServerRelationalProducerQueue.cs:99`). PLAN-1.1's PG producer body omits this field. Functional behaviour is unchanged (the field is unused in override bodies — header generation happens in the base class), but the Phase 3 → Phase 4 structural mirror is slightly imperfect. Not a blocker; can be flagged at review time if cross-transport consistency is desired.
- The `QueueProducerConfiguration` test-fixture ctor in `BuildSut` (PLAN-1.1 lines 281-286) uses `Substitute.For<IConfiguration>()`. Per CLAUDE.md "IConfiguration namespace conflict" lesson, in `DotNetWorkQueue.*` test code the bare `IConfiguration` resolves to `DotNetWorkQueue.IConfiguration` (correct here) — no namespace clash because the test does not need MS config types. PASS.

---

### PLAN-2.1 (Wave 2 — Sync Handler Fork)

#### File paths
| Path | Status | Evidence |
|------|--------|----------|
| `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandler.cs` | EXISTS | 12034 bytes |
| `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessage.cs` | EXISTS | 7494 bytes (read-only dependency) |
| `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/CommandHandler/` | EXISTS | Already contains `SetJobLastKnownEventCommandHandlerTests.cs` — fork smoke tests will be the second file in this folder |

#### API surface
Verified against `SendMessageCommandHandler.cs` lines 39–95:

| Plan assumption | Actual source | Match |
|-----------------|---------------|-------|
| `_getTime` is a private field | `private readonly IGetTime _getTime;` (line 49) | YES |
| Method is `_getTime.GetCurrentUtcDate()` | Confirmed by existing self-managed-tx call site line 153: `_getTime.GetCurrentUtcDate()` | YES |
| `_jobExistsHandler` field name | Confirmed line 47 | YES |
| `_sendJobStatus` field name | Confirmed line 46 | YES |
| `DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>` is a valid type | Confirmed by existing self-managed-tx call site line 120 | YES |
| `SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>` is a valid type | Confirmed by field type line 46 + call site line 162 | YES |
| `CreateMetaDataRecord` signature: 8 params ending in `DateTime currentTime` | Confirmed lines 212-213: `(TimeSpan? delay, TimeSpan expiration, NpgsqlConnection connection, long id, IMessage message, IAdditionalMessageData data, NpgsqlTransaction trans, DateTime currentTime)` | YES |
| `CreateStatusRecord(NpgsqlConnection, long, IMessage, IAdditionalMessageData, NpgsqlTransaction)` | Confirmed lines 189-190 | YES |
| `_commandCache.GetCommand(CommandStringTypes.InsertMessageBody)` | Confirmed by existing call site line 126 | YES |
| `using NpgsqlTypes;` already imported (for `NpgsqlDbType.Bytea`) | Confirmed line 32 | YES |
| `using Npgsql;` already imported (for `NpgsqlConnection`/`NpgsqlTransaction` casts) | Confirmed line 31 | YES |

All API surface assumptions hold.

#### Insertion-point line numbers
- `Handle(SendMessageCommand commandSend)` method opens at line 99 — MATCHES plan (`line 99`).
- Lazy-init block `if (!_messageExpirationEnabled.HasValue) { ... }` runs lines 101-104 — MATCHES plan claim that "block ends at line 104".
- `var jobName = _jobSchedulerMetaData.GetJobName(...)` at line 106 — MATCHES plan ("before line 106").
- Line 105 is blank — insertion between 104 and 106 is unambiguous.
- `CreateStatusRecord` declaration at line 189 — MATCHES plan ("before line 189").
- `CreateMetaDataRecord` declaration at line 212 (plan body cited "lines 212-213" — accurate).
- Self-managed-tx body lines 115-178 — MATCHES plan reference ("preserved unchanged, lines 115-178").

#### Verification commands
All grep/awk gates parse cleanly. Single risk: the `awk '/private long HandleExternalTx/,/^        }$/'` range-extract relies on the closing brace being at exactly 8-space indentation. Phase 3 SqlServer used the same pattern successfully — see SUMMARY-2.1; safe.

#### Complexity flag
- Files touched: 1 modified + 1 new test = 2 files in 2 directories. Well under thresholds.

---

### PLAN-2.2 (Wave 2 — Async Handler Fork)

#### File paths
| Path | Status | Evidence |
|------|--------|----------|
| `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs` | EXISTS | 12389 bytes |
| New `SendMessageCommandHandlerAsyncForkSmokeTests.cs` | TO CREATE | Sibling of PLAN-2.1's smoke test — no folder conflict |

#### API surface
Verified against `SendMessageCommandHandlerAsync.cs`:

| Plan assumption | Actual source | Match |
|-----------------|---------------|-------|
| `_getTime` field present | Line 52 | YES |
| `HandleAsync` returns `async Task<long>` | Line 101 | YES |
| `_commandCache`/`_serializer`/`_headers`/`_jobSchedulerMetaData`/`_jobExistsHandler`/`_sendJobStatus`/`_options`/`_messageExpirationEnabled` all available | Lines 42-52 | YES |
| `CreateMetaDataRecordAsync` is `private async Task` with 8 params ending in `DateTime currentTime` | Lines 221-222 | YES |
| `CreateStatusRecordAsync` is `private async Task(NpgsqlConnection, long, IMessage, IAdditionalMessageData, NpgsqlTransaction)` | Lines 196-197 | YES |
| Existing async pattern uses `await command.ExecuteScalarAsync().ConfigureAwait(false)` | Line 145 | YES |
| `_sendJobStatus.Handle(...)` is called synchronously in existing async path | Line 167 | YES (PLAN-2.2 inline comment is accurate) |
| `_jobExistsHandler.Handle(...)` is called synchronously in existing async path | Line 122 | YES |

All API surface assumptions hold.

#### Insertion-point line numbers
- `HandleAsync` opens at line 101 — MATCHES plan (`line 101`).
- Lazy-init block lines 103-106 — MATCHES plan ("ends at line 106").
- `var jobName = ...` at line 108 — MATCHES plan ("before line 108").
- Line 107 is blank — insertion unambiguous.
- `CreateStatusRecordAsync` declaration at line 196 — MATCHES plan ("before line 196").
- `CreateMetaDataRecordAsync` at line 221.
- Self-managed-tx body lines 117-184 — MATCHES plan reference.

#### Verification commands
All grep/awk gates parse cleanly. The closing-brace regex (`^        }$`) is the same 8-space-indent pattern as PLAN-2.1.

#### Complexity flag
- Files touched: 1 modified + 1 new test = 2 files in 2 directories. Well under thresholds.

---

## Cross-Plan Concerns

### Forward references (Wave 2 ↔ Wave 2)
- PLAN-2.1 modifies ONLY `SendMessageCommandHandler.cs`.
- PLAN-2.2 modifies ONLY `SendMessageCommandHandlerAsync.cs`.
- Both READ `SendMessage.cs` (static SQL builders) — but neither modifies it.
- New test files: `SendMessageCommandHandlerForkSmokeTests.cs` vs `SendMessageCommandHandlerAsyncForkSmokeTests.cs` — disjoint filenames in the same folder, no conflict.
- **Verdict: truly file-disjoint. Parallel execution safe.**

### Dependency chain
- PLAN-1.1 (Wave 1) creates the `RegisterConditional` wiring that routes `RelationalSendMessageCommand` to the registered handlers. Both Wave 2 forks rely on Wave 1's DI to be reachable in any integration scenario, but the Wave 2 unit tests are pure source/reflection grep — they do NOT require runtime DI. **Wave 2 unit tests can compile + pass independent of Wave 1**, though Wave 1 is still the correct logical dependency.
- Plan front-matter `dependencies: [1.1]` on both Wave 2 plans is conservatively correct.

### Pre-existing risks now grounded
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/QueueCreatorTests.cs` has 6 `Assert.ThrowsExactly<Npgsql.NpgsqlException>` assertions at lines 38, 53, 68, 88, 103, 118. Plan-1.1's Rule A enforcement (RegisterConditional) is exactly the right mitigation — confirmed by Phase 3 SUMMARY-1.1 precedent.
- Layering invariant grep `grep -rn "using Npgsql\|using Microsoft\.Data\.SqlClient" Source/DotNetWorkQueue.Transport.RelationalDatabase/` returns 0 matches today (exit code 1) — Phase 4 must not regress this. Verification gate in PLAN-1.1 lines 769-770 enforces.
- ISSUE-032 (open) — pre-existing `NU1902` advisory on `Transport.SQLite` is an active "Release+CI=true" gate failure. Phase 4's Release build verification on `Transport.PostgreSQL` ONLY (not the full solution) sidesteps this; the per-project Release build is the right scope for plan-local validation.

### Hidden assumptions (low risk)
- Plan body of PLAN-2.2 says `_jobExistsHandler.Handle(...)` exists synchronously on the async handler at "line ~122 in the pre-Phase-4 baseline" — actual is line 122 exactly. Accurate.
- Plan body of PLAN-2.2 says "existing handler's await style (line 145 + 157 + 162 of the current file)" — actual `await` sites in `HandleAsync` are line 145 (ExecuteScalarAsync), 154-157 (CreateMetaDataRecordAsync), and 160-162 (CreateStatusRecordAsync). Approximately correct.

---

## Recommendation

**Verdict: READY.** Builders may execute Phase 4 plans as written. Key strengths:

1. Every line-number anchor in the plans (`62`, `64`, `99`, `104`, `106`, `189`, `212` in sync; `101`, `106`, `108`, `196`, `221` in async) is verified against the current `SendMessageCommandHandler[Async].cs` and `PostgreSQLMessageQueueInit.cs` — no drift since the researcher's inspection.
2. Every named field, method, and type in the plan bodies (`_getTime`, `_jobExistsHandler`, `_sendJobStatus`, `_commandCache`, `_serializer`, `_headers`, `CreateMetaDataRecord[Async]`, `CreateStatusRecord[Async]`, `DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>`, `SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>`) exists in the source with the signature the plan body assumes.
3. PG-specific deviations from SqlServer (8-param `CreateMetaDataRecord` + `_getTime.GetCurrentUtcDate()` + `NpgsqlDbType.Bytea` + pass-through extractor) are correctly identified in both RESEARCH.md and the plan tasks. The two most likely build-fail modes (missing `currentTime` arg → CS7036, or `SqlDbType.VarBinary` → CS1503) are explicitly called out in PLAN-2.1 line 188-189 and PLAN-2.2 lines 195-198.
4. Wave 2 plans are file-disjoint, no parallel-edit risk.
5. Rule A (RegisterConditional), Rule B (lifecycle comment phrasing), and Rule C (11-param ctor) all have grep-level enforcement gates wired into the `## Verification` sections.

### Optional polish items (non-blocking)

1. **`_generateMessageHeaders` field capture (consistency only):** Phase 3 SqlServer producer captures a `_generateMessageHeaders` field from the ctor that the Phase 4 PG producer body does not capture. The field is unused in override bodies in both transports (header generation happens in the base class). If cross-transport consistency is desired, the builder could mirror the Phase 3 capture; otherwise this is purely cosmetic and the plan as written is functionally complete. No action required to ship.
2. **`Substitute.For<DbConnection>` returns a mock connection whose `Database` property is `null` by default** — the smoke tests in PLAN-1.1 explicitly stub `conn.Database.Returns(...)` before each call site, so the `?? string.Empty` fallback in the extractor implementation is not exercised by tests. If full branch coverage is desired, a third extractor test `Extract_NullDatabase_ReturnsEmpty` could be added; otherwise the fallback is defensive code only.

Neither item changes the verdict. Plans are READY to execute.
