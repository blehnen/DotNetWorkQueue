# Plan Critique: Phase 1 — Polly Decorator Bypass Spike

**Critic:** orchestrator (verifier agent path bypassed — follows the same direct-execution pattern as the researcher step, per `feedback_agent_lockups.md`)

**Plans reviewed:** `PLAN-1.1.md` (single plan, three tasks)

**Verdict:** **READY**

---

## Per-Plan Findings

### PLAN-1.1 — Memo + Throwaway PoC

#### 1. File paths exist

| Referenced path | Status |
|---|---|
| `.shipyard/notes/` | ✓ Directory created during phase setup |
| `.shipyard/notes/phase-1-polly-bypass-spike.md` | Will be created by Task 1 |
| `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/` | ✓ Exists; contains 4 sibling `Retry*Tests.cs` files following the same convention |
| `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs` | Will be created by Task 2 |
| `.shipyard/PROJECT.md` | ✓ Exists (committed `194ba0a3`); Task 3 modifies one line |

All paths resolved. No missing-file landmines.

#### 2. API surface matches

| API referenced | Verified |
|---|---|
| `SendMessageCommand` public class | ✓ `Source/DotNetWorkQueue.Transport.Shared/Basic/Command/SendMessageCommand.cs:26` — public, subclassable, two-arg constructor `(IMessage, IAdditionalMessageData)` |
| `ICommandHandlerWithOutput<TCommand, TOutput>` | ✓ Used by existing `RetryCommandHandlerOutputDecoratorTests.cs:38` for the same mocking pattern |
| `IPolicies` (internal) | ✓ `InternalsVisibleTo` from `Transport.SqlServer` to `Transport.SqlServer.Tests` confirmed in `Source/DotNetWorkQueue.Transport.SqlServer/InternalsVisibleForTests.cs:21` |
| `TransportPolicyDefinitions.RetryCommandHandler` (internal const) | ✓ Same internals-visibility path; existing test `RetryCommandHandlerOutputDecoratorTests.cs:60` already references it |
| MSTest 4.x `[TestClass]` / `[TestMethod]` / `Assert.AreEqual` | ✓ Matches existing `RetryCommandHandlerOutputDecoratorTests.cs` style |
| NSubstitute `Substitute.For<...>` + `Received(1)` | ✓ Standard pattern in sibling tests |
| `Polly.Registry.ResiliencePipelineRegistry<string>` | ✓ Used by existing decorator tests |

No API mismatches.

#### 3. Verification commands runnable

| Command | Runnable in current state? |
|---|---|
| `test -f .shipyard/notes/phase-1-polly-bypass-spike.md` | ✓ (POSIX) |
| `grep -c "^##" ...` | ✓ (POSIX) |
| `dotnet test Source/DotNetWorkQueue.Transport.SqlServer.Tests/... --filter "FullyQualifiedName~_SpikePollyBypassPoC"` | ✓ Matches the project's existing unit-test invocation convention from CLAUDE.md |
| `git diff --name-only master` | ✓ Repo is a git working tree on master |
| `grep -A1 "Polly decorator bypass cleanness" .shipyard/PROJECT.md` | ✓ String present in PROJECT.md after commit `194ba0a3` |

All four verifications are syntactically valid and executable.

#### 4. Forward references

The plan has no plans in the same wave (it is the only plan in this phase). No cross-plan dependencies in this wave.

#### 5. Hidden dependencies

The PoC test file's compilation depends on:
- `Microsoft.Data.SqlClient` (no — PoC doesn't reference it)
- `Polly` (yes — already a project dependency)
- `Transport.SqlServer` internals (yes — `InternalsVisibleTo` confirmed)
- `Transport.Shared.Basic.Command.SendMessageCommand` (yes — public; test project transitively references `Transport.Shared`)

The test project's existing `Decorator/` test files reference all these without issue. No hidden deps.

#### 6. Complexity flags

Files touched: 3 (one new memo, one new PoC test, one PROJECT.md edit). No directory cross-cutting. Below the >10-files or >3-directories thresholds. Low complexity.

---

## Cross-Cutting Observations

1. **Researcher / verifier agent bypass.** Both upstream agents stalled. The orchestrator-direct pattern from the `[2026-04-17]` and earlier memory entries is in effect; this is expected and not a finding against the plan itself.

2. **PoC throwaway encoding.** The PoC file's underscore-prefix naming (`_SpikePollyBypassPoC.cs`) is the only mechanism flagging it as throwaway. Phase 2's first task must explicitly reference this filename to delete it. The plan correctly notes this responsibility but doesn't write the Phase-2 deletion task — that's Phase 2's planning step's job, not Phase 1's. Acceptable but worth flagging so the next plan cycle doesn't forget.

3. **`_SpikePatchedRetryDecorator` is the architectural commitment, not just a test fixture.** The PoC's `_SpikePatchedRetryDecorator<TCommand, TOutput>` is a near-copy of the production decorator with the proposed marker-check branch added. Phase 2 will translate this exact branch into the production decorator. The test is therefore not just an experiment — it is a reference implementation. Worth noting in the memo that the PoC's decorator branch is the proposed production code.

4. **PROJECT.md edit (Task 3) is a one-line risk-table update.** Low-risk modification. Verifier-style sanity check: `grep -c "Polly decorator bypass" .shipyard/PROJECT.md` should equal 1 both before and after, since the line is replaced in place (not duplicated).

---

## Final Verdict: **READY**

All three tasks are feasible against the current codebase. File paths exist, API references resolve, verification commands are syntactically valid and runnable. No forward dependencies, no hidden coupling, no complexity flags triggered. Proceed to build.
