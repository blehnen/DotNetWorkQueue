# Review: Plan 3.1

## Verdict: PASS

## Stage 1: Spec Compliance — PASS

### Task 1: Sync decorator bypass branch — PASS
- File: `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecorator.cs`.
- Evidence:
  - Line 20: `using DotNetWorkQueue.Transport.RelationalDatabase;` inserted alphabetically between `System` (line 19) and `DotNetWorkQueue.Transport.Shared` (line 21). Note plan wording said "between `Shared` and `SqlServer.Basic`" but the absolute alphabetical position is in fact *before* `Shared` (`R` < `S`); the actual placement in the file is the strictly correct alphabetical slot. Plan wording was off-by-one; code is correct.
  - Lines 54–55: 2-line branch `if (command is IRetrySkippable skippable && skippable.SkipRetry) return _decorated.Handle(command);` placed immediately after `Guard.NotNull(() => command, command);` (line 52) and a blank line, before the existing `ResiliencePipeline pipeline = null;` (line 57).
  - Existing no-pipeline / `ObjectDisposedException` / pipeline-execute branches (lines 57–70) are unchanged verbatim.
  - Class remains `internal`; signature unchanged.

### Task 2: Async decorator bypass branch — PASS
- File: `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs`.
- Evidence:
  - Line 20: same `using DotNetWorkQueue.Transport.RelationalDatabase;` directive, inserted at the alphabetical slot.
  - Lines 55–56: branch `if (command is IRetrySkippable skippable && skippable.SkipRetry) return await _decorated.HandleAsync(command).ConfigureAwait(false);` placed after `Guard.NotNull` (line 53) and a blank line. Branch uses `.ConfigureAwait(false)` per plan, matching the existing async style (lines 70–71).
  - Existing pipeline lookup / `ObjectDisposedException` / pipeline-execute branches (lines 58–71) are unchanged verbatim.
  - Method signature `public async Task<TOutput> HandleAsync(TCommand command)` unchanged.

### Task 3: Bypass-branch unit tests — PASS
- File: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/RetryCommandHandlerOutputDecoratorBypassTests.cs` (NEW, 86 lines).
- Evidence:
  - LGPL header lines 1–18 verbatim.
  - Namespace `DotNetWorkQueue.Transport.SqlServer.Tests.Decorator` (line 29) matches plan.
  - Single `[TestClass]` (line 38) with exactly 2 `[TestMethod]` methods (lines 49, 69).
  - `BuildCommandWithTx()` helper (lines 41–47) constructs `RelationalSendMessageCommand` with `Substitute.For<DbTransaction>()` — verified against `RelationalSendMessageCommand.cs:54` (`SkipRetry => ExternalTransaction != null`), so the substitute non-null transaction reliably triggers `SkipRetry == true`.
  - Sync test (line 50): asserts `Assert.AreEqual(42L, result)`, `decorated.Received(1).Handle(command)`, and `_ = policies.DidNotReceiveWithAnyArgs().Registry;` — property-getter assertion pattern per Phase 1 SUMMARY-1.1, exactly as specified.
  - Async test (line 70): mirrors sync — `await decorated.Received(1).HandleAsync(command)` and same `_ = policies.DidNotReceiveWithAnyArgs().Registry;` assertion.
  - MSTest 4.x APIs only (`Assert.AreEqual`); no MSTest 2.x `Assert.ThrowsException<>` usage.
  - `InternalsVisibleTo("DotNetWorkQueue.Transport.SqlServer.Tests")` confirmed at `Source/DotNetWorkQueue.Transport.SqlServer/InternalsVisibleForTests.cs:21`, so the test's direct construction of the `internal` decorator generic class compiles cleanly.

### Cross-cutting Stage 1 checks — PASS
- Builder summary reports `141 passed, 0 failed, 0 skipped` on full SqlServer.Tests suite — no regression on the existing 3 tests in `RetryCommandHandlerOutputDecoratorTests.cs` or `RetryCommandHandlerOutputDecoratorAsyncTests.cs`.
- All 3 commits (`7c6e348a`, `64eb91b1`, `86a16287`) atomic, one-per-task, with `shipyard(phase-2):` prefix.
- No file overlap with PLAN-3.2 (PostgreSQL companion) — different transport directory.
- Layering invariant on `Transport.RelationalDatabase/` held (builder grep returned no matches).

## Stage 2: Code Quality

### Critical
- None.

### Important
- None.

### Suggestions
- `RetryCommandHandlerOutputDecoratorAsync.cs:25` — pre-existing `using System.Threading.Tasks;` is out of alphabetical order (sits below `Polly`). Not introduced by this plan and not in scope; flagging only for cleanup the next time this file is opened. Remediation (optional, future): re-sort the `using` block in a janitor pass.
- `RetryCommandHandlerOutputDecoratorBypassTests.cs:50–67` and `:69–84` — the property-getter assertion `_ = policies.DidNotReceiveWithAnyArgs().Registry;` proves the registry was not consulted, but it does not assert *which* return path the decorator took (the bypass branch versus a hypothetical future "no pipeline + no registry" path). Today the bypass is the only path that reaches `_decorated.Handle(command)` without first reading `_policies.Registry`, so the test is unambiguous against the current implementation. If a future refactor adds another no-registry-read path, the test could pass for the wrong reason. Remediation (optional): add a comment in each test body stating "the no-registry-read shape of this assertion is unique to the bypass branch as of Plan 3.1; if a second no-registry path is added later, strengthen the assertion."
- The plan wording in Task 1 said the new `using` goes "between `DotNetWorkQueue.Transport.Shared` and `DotNetWorkQueue.Transport.SqlServer.Basic`" — but `RelationalDatabase` alphabetically precedes `Shared`, so the correct slot (and what the builder produced) is one line earlier. Cosmetic plan-wording bug, not a code defect. Remediation (optional): if a future plan author copies this prose, fix the slot description. Builder correctly followed alphabetical convention over literal wording.

### Positive
- 3 atomic commits, one per task — clean bisect surface, matches Wave 1 / Wave 2 cadence on this milestone.
- Test uses the **production** `RelationalSendMessageCommand` (Wave 2 / PLAN-2.2) as the input shape, not a hand-rolled `IRetrySkippable` test double. This integrates the Wave 1 marker → Wave 2 derived command → Wave 3 bypass chain end-to-end at the unit-test layer; if any one of those three pieces regresses, this test breaks.
- Property-getter assertion `_ = policies.DidNotReceiveWithAnyArgs().Registry;` is the correct workaround for the un-mockable sealed `ResiliencePipelineRegistry<string>` (CLAUDE.md lesson + Phase 1 SUMMARY-1.1), and is applied symmetrically to sync and async tests.
- Bypass branch placement immediately after `Guard.NotNull` (and before pipeline lookup) means the null-input contract still holds for skippable commands — a null `IRetrySkippable` would throw `ArgumentNullException` first, which is the existing decorator contract.
- Branch is a single `is`-pattern + boolean-short-circuit — no allocation, no virtual dispatch overhead beyond the pattern match. Hot path cost on non-skippable commands is one type-test (the C# compiler emits `isinst` + null-check), which is the cheapest possible discriminator.
- Async branch uses `.ConfigureAwait(false)` and `await ... return` (rather than `return await` without ConfigureAwait, or just returning the unawaited `Task`). Matches the existing file style at lines 70–71 exactly.
- Existing 3 tests per decorator (sync + async = 6) continue to use `FakeCommand` which does *not* implement `IRetrySkippable`, so the bypass branch is correctly inert for those tests — no test pollution.
- Builder's `Decisions Made` section is "None" — every line of code traces back to plan wording, no improvisation. Easy review surface.

## Summary
Verdict: APPROVE. PLAN-3.1's three atomic commits cleanly land the SqlServer half of Wave 3: the sync and async retry decorators now short-circuit on `IRetrySkippable.SkipRetry == true` with a 2-line branch each, and the new co-located unit test pair (`RetryCommandHandlerOutputDecoratorBypassTests`) covers both paths using the production `RelationalSendMessageCommand` input shape and the correct property-getter assertion against `IPolicies.Registry`. Existing 141 SqlServer unit tests pass without regression. The layering invariant on `Transport.RelationalDatabase` holds. The PLAN-3.2 PostgreSQL companion is disjoint by directory and unaffected.
Critical: 0 | Important: 0 | Suggestions: 3
