# Phase 1 Simplification Review

**Phase:** 1 — Polly Decorator Bypass Spike
**Date:** 2026-05-13
**Files analyzed:** 2 new (memo + PoC test), 1 single-line edit (PROJECT.md), shipyard state
**Findings:** 0 high, 1 medium, 2 low

## Overall Findings: LOW

## Throwaway-Code Context
- The PoC file `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs` is explicitly deleted by Phase 2 Task 1 per `.shipyard/phases/1/plans/PLAN-1.1.md`. The leading-underscore + `_Spike` prefix on every nested type makes the throwaway intent visible at a glance. Simplification recommendations below are calibrated against this — code-to-be-deleted that costs more to clean up than to leave alone is left alone.

## Findings

### High Priority (worth fixing now)
- None.

### Medium Priority (worth noting)

#### M1. `BuildCommand` helper builds a `_SpikeSendCommand` but is typed to return `SendMessageCommand`
- **Type:** Refactor (cosmetic; throwaway code)
- **Locations:** `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs:141-146`
- **Description:** The helper signature is `private static SendMessageCommand BuildCommand(bool skipRetry)` but it returns a `_SpikeSendCommand`. Both call sites (lines 155, 177) immediately pass the result into `sut.Handle(...)` which is typed against `SendMessageCommand`, so the upcast is real. However, the test asserts `_SpikeSendCommand` semantics (the `IRetrySkippable` marker is on the subclass, not the base). The widened return type slightly obscures what's being tested.
- **Suggestion:** Either inline the two-line construction at each call site (the test would gain four lines but lose one helper), or change the return type to `_SpikeSendCommand` to make the marker-bearing subclass explicit. Both are fine for throwaway code.
- **Impact:** Minor clarity; no production effect.

### Low Priority / Cosmetic

#### L1. `_ = policies.DidNotReceiveWithAnyArgs().Registry;` and `_ = policies.Received().Registry;` are unusual NSubstitute idioms
- **Locations:** `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs:164, 188`
- **Description:** The `_ =` discard on an NSubstitute property assertion is intentional — the property getter has to be touched to register the call, and the compiler would warn on a bare expression statement. The inline comments at lines 161-164 and 183-188 explain this clearly. This is fine, but it's a pattern most readers will pause on. Not a defect.
- **Suggestion:** No action. The comments already cover it.

#### L2. Memo Phase 2 file list reads "Six files modified" but the bulleted list contains six items including a "new file" — minor numbering nit
- **Locations:** `.shipyard/notes/phase-1-polly-bypass-spike.md:100-107`
- **Description:** Says "Six files modified to ship the production change" then item #1 is a **new file**. Strictly the count is "1 new + 5 modified". The current phrasing is harmless but slightly imprecise.
- **Suggestion:** Change "Six files modified" to "Six files touched (1 new + 5 modified)" — or leave alone, it's a minor stylistic call.

### Positive

- **PoC file is appropriately minimal.** Two test methods (positive and negative case), one local marker interface, one local command subclass, one recording handler, one patched decorator subclass. Each scaffolding type is used exactly once for its declared purpose. No dead helpers, no unused factory methods, no abstraction added "just in case."
- **`_SpikePatchedRetryDecorator` is a faithful mirror of production.** Side-by-side comparison with `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecorator.cs:28-67` shows the spike adds exactly the proposed early-return branch (lines 120-123) and otherwise reproduces the production structure verbatim including the `ObjectDisposedException` swallow. No copied lines beyond what's needed to demonstrate the branch slots into place.
- **All usings in the PoC are referenced.** `System` (ObjectDisposedException), `DotNetWorkQueue` (IPolicies), `DotNetWorkQueue.Messages` (IMessage, IAdditionalMessageData), `DotNetWorkQueue.Transport.Shared` (ICommandHandlerWithOutput), `DotNetWorkQueue.Transport.Shared.Basic.Command` (SendMessageCommand), `DotNetWorkQueue.Transport.SqlServer.Basic` (TransportPolicyDefinitions), `DotNetWorkQueue.Validation` (Guard), MSTest, NSubstitute, Polly, Polly.Registry — all touched.
- **Memo style matches repo convention.** File-path + line-number anchors throughout; no inflated language ("comprehensive", "robust", "extensive" — none found); decorator inventory uses a compact table; the proposed code snippet is shown once, not repeated.
- **PoC file naming convention is documented and intentional.** Leading underscore + `_Spike` prefix on every nested type ensures throwaway status is visible in IDE tree views and grep results.

## Specific Checks

- **Duplication within PoC file:** None. Each helper type appears once and is used once.
- **Dead/unused code in PoC:** None. Every nested type is constructed and exercised by at least one test method.
- **Unused usings in PoC:** None. Verified each `using` line against symbol usage.
- **Memo repetition:** Minor — the "PoC reference" section (lines 117-119) restates content covered in the headline (line 3) and Risk #1 Disposition (line 115). All three mentions land at the file boundaries (top, near-bottom, very-bottom), which is acceptable as a structured memo — top is the executive answer, bottom is the reference pointer. Not worth changing.
- **Memo inflated language:** None found. No "comprehensive", "robust", "extensive", "seamless", "elegantly", "leverage", "utilize". Tone matches `docs/jenkins-setup.md`.
- **`_SpikePatchedRetryDecorator` faithfulness to production decorator:** **Faithful + minimal.** Compared against `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecorator.cs`:
  - Fields, constructor, Guard calls: identical pattern (lines 104-114 of PoC ≈ lines 30-46 of production).
  - `Handle` body: identical except for the new lines 120-123 (the spike branch under test).
  - No additional members, no extra abstractions, no copied XML doc comments beyond what the spike scenario needs.
- **Memo length:** 119 lines — well under the 200-line target stated in PLAN-1.1 Task 1.
- **Negative-test discriminator (line 188):** The negative test confirms the **non-bypass** path was taken by asserting `policies.Received().Registry` (the property getter was hit). The inline comment at lines 183-188 explains the NSubstitute limitation (concrete `ResiliencePipelineRegistry.TryGetPipeline` cannot be intercepted) and why the indirect assertion is sound. This is the right call for the proof — it discriminates bypass from non-bypass without faking the registry's internal call path.

## Recommendation

**Accept as-is — appropriate for spike scope.**

The PoC is faithful to production, minimally scaffolded, all symbols are used, and the throwaway intent is signposted at file, namespace, and type level. The memo is direct, anchored in concrete file paths and line numbers, and within the stated length target. The two medium/low items above are stylistic and not worth touching code that will be deleted in Phase 2 Task 1.

No simplification action recommended before Phase 1 closeout. Phase 2 builders can use this PoC as a working reference for the production change without first having to clean it up.
