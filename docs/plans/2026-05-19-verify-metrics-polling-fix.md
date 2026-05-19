# VerifyMetrics Snapshot-Race Fix Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use shipyard:shipyard-executing-plans to implement this plan task-by-task.

**Goal:** Eliminate snapshot-race flakes in integration tests by extending the existing `VerifyProcessedCount` polling pattern to the other `VerifyMetrics` helpers (`VerifyPoisonMessageCount`, `VerifyExpiredMessageCount`, `VerifyRollBackCount`, `VerifyProducedCount`, `VerifyProducedAsyncCount`), and bumping the default polling timeout to survive chaos + hold-transaction scenarios.

**Architecture:** Extract a private `PollUntil` helper inside `VerifyMetrics` that polls `IMetrics.GetCollectedMetrics()` on a 100ms interval until a metric value reaches the expected count or the timeout elapses; then re-issue the existing `MetricsSnapshot`-based assertion as the final check (preserves error messages on timeout). Add polling overloads taking `IMetrics + timeoutMs` for each affected `Verify*` method and route all call sites in `DotNetWorkQueue.IntegrationTests.Shared/` to the polling overload.

**Tech Stack:** .NET 10 / .NET 8, MSTest 4.x, NSubstitute, AutoFixture, integration tests in `DotNetWorkQueue.Transport.{SQLite,SqlServer,PostgreSQL,Redis,LiteDb,Memory}.{,Linq.}Integration.Tests`.

**Background / why this plan exists:**
- Jenkins integration matrix on master flaked on 2026-05-19 with:
  - `SqlServer.IntegrationTests.ConsumerAsync.MultiConsumerAsync.Run(100,0,180,10,5,0,True,True)` — `processed=99/100`, chaos=True, hit polling timeout (existing 5s polling overload).
  - `SQLite.Integration.Tests.Consumer.ConsumerPoisonMessage.Run(10,60,1,False,False)` — `poison=9/10`, chaos=False, single snapshot.
  - `SQLite.Linq.Integration.Tests.ConsumerMethodAsync.ConsumerMethodAsyncPoisonMessage.Run(10,60,5,1,0,True,False)` — `poison=9/10`, chaos=False, single snapshot.
- CLAUDE.md lesson: "Integration test metrics assertions can race: the handler callback signals completion before `CommitMessage.Commit()` increments the counter. Poll the live `IMetrics` object instead of taking a single snapshot." That fix was applied to `VerifyProcessedCount` only; the same race exists structurally for poison/expired/rollback/produced counters.
- Master had been green for weeks. The race is probabilistic; recent commits (`79476c2c` send-command refactor, `74ae1b97`/`21c8b87e` test reorgs) likely shifted timing distributions enough to surface the latent issue.

**Out of scope:**
- Jenkins runner-count changes (user is reverting those — wrong lever).
- Phase 2 inbox-pattern work (this is a master-branch fix, separate from `.shipyard/phases/2/`).
- Restructuring `VerifyMetrics` static class into a non-static or DI-injected service.

---

## Pre-flight Checks

Before starting Task 1, run:

```bash
# Confirm baseline build is green
dotnet build "Source/DotNetWorkQueue.sln" -c Debug

# Confirm IntegrationTests.Shared builds clean
dotnet build "Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj" -c Debug
```

Expected: both succeed with no warnings as errors.

---

### Task 1: Extract `PollUntil` helper + bump `VerifyProcessedCount` default timeout

**Files:**
- Modify: `Source/DotNetWorkQueue.IntegrationTests.Shared/VerifyMetrics.cs:139-167`

**Step 1: Add private polling helper**

Add this private static helper immediately above `VerifyProcessedCount(string, IMetrics, long, int)` (i.e., before line 135):

```csharp
/// <summary>
/// Polls live metrics on a 100ms interval until <paramref name="getValue"/> reaches
/// <paramref name="expected"/> or <paramref name="timeoutMs"/> elapses, then re-issues
/// <paramref name="finalAssert"/> against the latest snapshot for a clean error message.
/// Fixes a class of race where the handler callback signals completion before a
/// metric counter/meter is incremented.
/// </summary>
private static void PollUntil(
    IMetrics metrics,
    Func<MetricsSnapshot, long?> getValue,
    long expected,
    int timeoutMs,
    Action<MetricsSnapshot> finalAssert)
{
    if (expected == 0)
    {
        finalAssert(metrics.GetCollectedMetrics());
        return;
    }

    var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
    while (DateTime.UtcNow < deadline)
    {
        var data = metrics.GetCollectedMetrics();
        var value = getValue(data);
        if (value.HasValue && value.Value >= expected)
        {
            finalAssert(data);
            return;
        }
        Thread.Sleep(100);
    }

    finalAssert(metrics.GetCollectedMetrics());
}
```

**Step 2: Refactor existing `VerifyProcessedCount(string, IMetrics, long, int)` to use `PollUntil`**

Replace the body of lines 139-167 with:

```csharp
/// <summary>
/// Polls the live metrics until CommitCounter reaches the expected value or times out.
/// Fixes a race where the handler callback signals completion before the commit metric is incremented.
/// Default timeout is generous enough to survive chaos + hold-transaction scenarios under CI load.
/// </summary>
public static void VerifyProcessedCount(string queueName, IMetrics metrics, long messageCount, int timeoutMs = 15000)
{
    const string name = "CommitMessage.CommitCounter";
    PollUntil(
        metrics,
        data =>
        {
            foreach (var counter in data.Counters.Where(
                c => c.Key.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                return counter.Value;
            }
            return null;
        },
        messageCount,
        timeoutMs,
        data => VerifyProcessedCount(queueName, data, messageCount));
}
```

Note the default timeout went from `5000` → `15000`. The 5s default was too tight for chaos paths.

**Step 3: Build and verify nothing broke**

```bash
dotnet build "Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj" -c Debug
```

Expected: build succeeds with no warnings.

**Step 4: Smoke-test a happy-path integration test that uses `VerifyProcessedCount`**

```bash
dotnet test "Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj" --filter "FullyQualifiedName~SimpleConsumer.Run" -c Debug
```

Expected: green. (Memory transport is fast and doesn't need external services.)

**Step 5: Commit**

```bash
git add Source/DotNetWorkQueue.IntegrationTests.Shared/VerifyMetrics.cs
git commit -m "test(verify-metrics): extract PollUntil helper, bump processed-count timeout to 15s

The 5s polling default in VerifyProcessedCount was too tight for chaos +
hold-transaction scenarios under CI load — caused a 99/100 flake in
SqlServer MultiConsumerAsync on 2026-05-19. Bump to 15s and extract the
polling loop so subsequent Verify* overloads can share it."
```

---

### Task 2: Add `VerifyPoisonMessageCount` polling overload + update call sites

**Files:**
- Modify: `Source/DotNetWorkQueue.IntegrationTests.Shared/VerifyMetrics.cs` (add overload after line 26)
- Modify: `Source/DotNetWorkQueue.IntegrationTests.Shared/Consumer/ConsumerPoisonMessageShared.cs:70`
- Modify: `Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerAsync/ConsumerAsyncPoisonMessageShared.cs:86`
- Modify: `Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethod/ConsumerMethodPoisonMessageShared.cs:61`
- Modify: `Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethodAsync/ConsumerMethodAsyncPoisonMessageShared.cs:78`

**Step 1: Add polling overload in `VerifyMetrics.cs`**

Insert immediately after the existing `VerifyPoisonMessageCount(string, MetricsSnapshot, long)` (after line 26):

```csharp
/// <summary>
/// Polls live metrics until <c>PoisonHandleMeter</c> reaches the expected value or times out.
/// Fixes a race where the handler callback signals completion before the poison meter is incremented.
/// </summary>
public static void VerifyPoisonMessageCount(string queueName, IMetrics metrics, long messageCount, int timeoutMs = 15000)
{
    const string name = "PoisonHandleMeter";
    PollUntil(
        metrics,
        data =>
        {
            foreach (var meter in data.Meters.Where(
                m => m.Key.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                return meter.Value;
            }
            return null;
        },
        messageCount,
        timeoutMs,
        data => VerifyPoisonMessageCount(queueName, data, messageCount));
}
```

**Step 2: Update all 4 call sites**

For each of the four files, change the existing call:

```csharp
VerifyMetrics.VerifyPoisonMessageCount(queueConnection.Queue, metrics.GetCurrentMetrics(), messageCount);
```

to:

```csharp
VerifyMetrics.VerifyPoisonMessageCount(queueConnection.Queue, metrics, messageCount);
```

(Drop the `.GetCurrentMetrics()` call — the polling overload takes the `IMetrics` instance directly and polls `GetCollectedMetrics()` internally.)

Exact locations:
- `Consumer/ConsumerPoisonMessageShared.cs:70`
- `ConsumerAsync/ConsumerAsyncPoisonMessageShared.cs:86`
- `ConsumerMethod/ConsumerMethodPoisonMessageShared.cs:61`
- `ConsumerMethodAsync/ConsumerMethodAsyncPoisonMessageShared.cs:78`

> ⚠ Each call site spans multiple lines (the `messageCount` argument is on the next line). Read 2-3 lines before/after to keep formatting consistent.

**Step 3: Build**

```bash
dotnet build "Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj" -c Debug
```

Expected: green.

**Step 4: Verify the previously-flaking SQLite poison-message tests**

```bash
# POCO variant (was failing on 2026-05-19)
dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Integration.Tests.csproj" --filter "FullyQualifiedName~ConsumerPoisonMessage" -c Debug

# Linq variant (was also failing)
dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.csproj" --filter "FullyQualifiedName~ConsumerMethodAsyncPoisonMessage" -c Debug
```

Expected: both pass. Run 3-5 times locally to gain confidence the race is gone.

**Step 5: Commit**

```bash
git add Source/DotNetWorkQueue.IntegrationTests.Shared/VerifyMetrics.cs \
        Source/DotNetWorkQueue.IntegrationTests.Shared/Consumer/ConsumerPoisonMessageShared.cs \
        Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerAsync/ConsumerAsyncPoisonMessageShared.cs \
        Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethod/ConsumerMethodPoisonMessageShared.cs \
        Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethodAsync/ConsumerMethodAsyncPoisonMessageShared.cs
git commit -m "test(verify-metrics): poll PoisonHandleMeter to fix snapshot race

SQLite + SQLite-Linq poison-message integration tests flaked with poison=9/10
on master 2026-05-19, chaos=False — classic snapshot race where the handler
callback signals completion before PoisonHandleMeter is incremented. Mirror
the VerifyProcessedCount polling pattern and route the four poison-message
shared test runners to the new overload."
```

---

### Task 3: Add `VerifyExpiredMessageCount` polling overload + update call sites

**Files:**
- Modify: `Source/DotNetWorkQueue.IntegrationTests.Shared/VerifyMetrics.cs` (add overload after line 48)
- Modify: `Source/DotNetWorkQueue.IntegrationTests.Shared/Consumer/ConsumerExpiredMessageShared.cs:71`
- Modify: `Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethod/ConsumerMethodExpiredMessageShared.cs:61`

**Step 1: Add polling overload**

Insert immediately after the existing `VerifyExpiredMessageCount(string, MetricsSnapshot, long)`:

```csharp
/// <summary>
/// Polls live metrics until the combined expired-message counters reach the expected value or times out.
/// Mirrors the GetExpiredMessageCount logic (sums ClearMessages.ResetCounter + HandleAsync.Expired).
/// </summary>
public static void VerifyExpiredMessageCount(string queueName, IMetrics metrics, long messageCount, int timeoutMs = 15000)
{
    PollUntil(
        metrics,
        data => GetExpiredMessageCount(data),
        messageCount,
        timeoutMs,
        data => VerifyExpiredMessageCount(queueName, data, messageCount));
}
```

Note: `GetExpiredMessageCount` already returns `long`. To make it work with `Func<MetricsSnapshot, long?>`, either cast at the call site (`data => (long?)GetExpiredMessageCount(data)`) or change `PollUntil`'s parameter to `Func<MetricsSnapshot, long>`. Prefer the cast — it's the smaller change.

Revised overload body:

```csharp
public static void VerifyExpiredMessageCount(string queueName, IMetrics metrics, long messageCount, int timeoutMs = 15000)
{
    PollUntil(
        metrics,
        data => (long?)GetExpiredMessageCount(data),
        messageCount,
        timeoutMs,
        data => VerifyExpiredMessageCount(queueName, data, messageCount));
}
```

**Step 2: Update the 2 call sites**

Same shape as Task 2:
- `Consumer/ConsumerExpiredMessageShared.cs:71` — change `metrics.GetCurrentMetrics()` → `metrics`
- `ConsumerMethod/ConsumerMethodExpiredMessageShared.cs:61` — same

**Step 3: Build + verify**

```bash
dotnet build "Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj" -c Debug

# Smoke-test expired-message tests on a fast transport
dotnet test "Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj" --filter "FullyQualifiedName~ConsumerExpiredMessage" -c Debug
```

Expected: green.

**Step 4: Commit**

```bash
git add Source/DotNetWorkQueue.IntegrationTests.Shared/VerifyMetrics.cs \
        Source/DotNetWorkQueue.IntegrationTests.Shared/Consumer/ConsumerExpiredMessageShared.cs \
        Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethod/ConsumerMethodExpiredMessageShared.cs
git commit -m "test(verify-metrics): poll expired-message counters to fix snapshot race"
```

---

### Task 4: Add `VerifyRollBackCount` polling overload + update call sites

**Files:**
- Modify: `Source/DotNetWorkQueue.IntegrationTests.Shared/VerifyMetrics.cs` (add overload after line 82)
- Modify: 8 call sites (see list below)

**Note:** `VerifyRollBackCount` is the most complex — it asserts TWO counters (`RollbackMessage.RollbackCounter` and conditionally `MessageFailedProcessingRetryMeter`). The polling overload should poll until the primary rollback counter reaches `messageCount * rollbackCount`, then do the full assertion (which covers both counters) as the final pass.

**Step 1: Add polling overload**

Insert after the existing `VerifyRollBackCount`:

```csharp
/// <summary>
/// Polls live metrics until <c>RollbackMessage.RollbackCounter</c> reaches
/// <paramref name="messageCount"/> * <paramref name="rollbackCount"/> or times out,
/// then asserts the full rollback + retry-meter invariants in one pass.
/// </summary>
public static void VerifyRollBackCount(string queueName, IMetrics metrics, long messageCount, int rollbackCount, int failedCount, int timeoutMs = 15000)
{
    const string name = "RollbackMessage.RollbackCounter";
    var expected = messageCount * rollbackCount;
    PollUntil(
        metrics,
        data =>
        {
            foreach (var counter in data.Counters.Where(
                c => c.Key.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                return counter.Value;
            }
            return null;
        },
        expected,
        timeoutMs,
        data => VerifyRollBackCount(queueName, data, messageCount, rollbackCount, failedCount));
}
```

**Step 2: Update the 8 call sites**

For each file, change `metrics.GetCurrentMetrics()` → `metrics` in the call. Locations:

- `Consumer/ConsumerErrorShared.cs:66`
- `Consumer/ConsumerRollBackShared.cs:70`
- `ConsumerAsync/ConsumerAsyncErrorShared.cs:87`
- `ConsumerAsync/ConsumerAsyncRollBackShared.cs:87`
- `ConsumerMethod/ConsumerMethodErrorShared.cs:95`
- `ConsumerMethod/ConsumerMethodRollBackShared.cs:69`
- `ConsumerMethodAsync/ConsumerMethodAsyncErrorShared.cs:127`
- `ConsumerMethodAsync/ConsumerMethodAsyncRollBackShared.cs:85`

> ⚠ The call signature has more args (`messageCount, rollbackCount, failedCount`) than poison-message — verify each call site keeps its argument ordering intact. Read the existing call before editing.

**Step 3: Build + smoke-test**

```bash
dotnet build "Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj" -c Debug

dotnet test "Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj" --filter "FullyQualifiedName~RollBack|FullyQualifiedName~ConsumerError" -c Debug
```

Expected: green.

**Step 4: Commit**

```bash
git add Source/DotNetWorkQueue.IntegrationTests.Shared/VerifyMetrics.cs \
        Source/DotNetWorkQueue.IntegrationTests.Shared/Consumer/ConsumerErrorShared.cs \
        Source/DotNetWorkQueue.IntegrationTests.Shared/Consumer/ConsumerRollBackShared.cs \
        Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerAsync/ConsumerAsyncErrorShared.cs \
        Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerAsync/ConsumerAsyncRollBackShared.cs \
        Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethod/ConsumerMethodErrorShared.cs \
        Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethod/ConsumerMethodRollBackShared.cs \
        Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethodAsync/ConsumerMethodAsyncErrorShared.cs \
        Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethodAsync/ConsumerMethodAsyncRollBackShared.cs
git commit -m "test(verify-metrics): poll rollback counter to fix snapshot race"
```

---

### Task 5: Add `VerifyProducedCount` + `VerifyProducedAsyncCount` polling overloads + update call sites

**Files:**
- Modify: `Source/DotNetWorkQueue.IntegrationTests.Shared/VerifyMetrics.cs` (add 2 overloads after lines 99 + 116)
- Modify: `Source/DotNetWorkQueue.IntegrationTests.Shared/Producer/ProducerShared.cs:68`
- Modify: `Source/DotNetWorkQueue.IntegrationTests.Shared/Producer/ProducerAsyncShared.cs:51`
- Modify: `Source/DotNetWorkQueue.IntegrationTests.Shared/ProducerMethod/ProducerMethodShared.cs:69`
- Modify: `Source/DotNetWorkQueue.IntegrationTests.Shared/ProducerMethod/ProducerMethodAsyncShared.cs:54`

**Note:** Producer races are less common than consumer races (the producer typically awaits all sends before the test asserts), but applying the same pattern for consistency removes a class of future flakes and matches the pattern reviewers will expect.

**Step 1: Add polling overloads in `VerifyMetrics.cs`**

After the existing `VerifyProducedAsyncCount`:

```csharp
public static void VerifyProducedAsyncCount(string queueName, IMetrics metrics, long messageCount, int timeoutMs = 15000)
{
    const string name = "SendMessagesMeter";
    PollUntil(
        metrics,
        data =>
        {
            foreach (var meter in data.Meters.Where(
                m => m.Key.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                return meter.Value;
            }
            return null;
        },
        messageCount,
        timeoutMs,
        data => VerifyProducedAsyncCount(queueName, data, messageCount));
}
```

After the existing `VerifyProducedCount` (note: same meter, same logic — the two sync/async methods are functionally identical right now; keeping the split preserves call-site semantics):

```csharp
public static void VerifyProducedCount(string queueName, IMetrics metrics, long messageCount, int timeoutMs = 15000)
{
    const string name = "SendMessagesMeter";
    PollUntil(
        metrics,
        data =>
        {
            foreach (var meter in data.Meters.Where(
                m => m.Key.EndsWith(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                return meter.Value;
            }
            return null;
        },
        messageCount,
        timeoutMs,
        data => VerifyProducedCount(queueName, data, messageCount));
}
```

**Step 2: Update the 4 call sites**

Same shape as before — drop `.GetCurrentMetrics()`:
- `Producer/ProducerShared.cs:68`
- `Producer/ProducerAsyncShared.cs:51`
- `ProducerMethod/ProducerMethodShared.cs:69`
- `ProducerMethod/ProducerMethodAsyncShared.cs:54`

**Step 3: Build + smoke-test**

```bash
dotnet build "Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj" -c Debug

dotnet test "Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj" --filter "FullyQualifiedName~Producer" -c Debug
```

Expected: green.

**Step 4: Commit**

```bash
git add Source/DotNetWorkQueue.IntegrationTests.Shared/VerifyMetrics.cs \
        Source/DotNetWorkQueue.IntegrationTests.Shared/Producer/ProducerShared.cs \
        Source/DotNetWorkQueue.IntegrationTests.Shared/Producer/ProducerAsyncShared.cs \
        Source/DotNetWorkQueue.IntegrationTests.Shared/ProducerMethod/ProducerMethodShared.cs \
        Source/DotNetWorkQueue.IntegrationTests.Shared/ProducerMethod/ProducerMethodAsyncShared.cs
git commit -m "test(verify-metrics): poll send-messages meter for producer-side consistency"
```

---

### Task 6: Update CLAUDE.md lesson + open PR

**Files:**
- Modify: `CLAUDE.md` (the "Lessons Learned" section, around the existing snapshot-race lesson)

**Step 1a: Replace the existing race lesson with the broadened version**

Find the existing bullet:

> Integration test metrics assertions can race: the handler callback signals completion before `CommitMessage.Commit()` increments the counter. Poll the live `IMetrics` object instead of taking a single snapshot.

Replace with:

> Integration test metrics assertions can race in **any** of the `VerifyMetrics.Verify*` methods — the handler callback signals completion before the underlying counter/meter is incremented. As of 2026-05-19 the polling pattern is applied uniformly to `VerifyProcessedCount`, `VerifyPoisonMessageCount`, `VerifyExpiredMessageCount`, `VerifyRollBackCount`, `VerifyProducedCount`, and `VerifyProducedAsyncCount` via a shared `PollUntil` helper. Default polling timeout is 15s (bumped from 5s after a `processed=99/100` chaos+hold-transaction flake on SqlServer). When adding a NEW `Verify*` helper, route it through `PollUntil` and accept `IMetrics` (not just `MetricsSnapshot`) — taking a one-shot snapshot is the wrong pattern.

**Step 1b: Update the two stale "(draft) PR" mentions to reflect the no-draft policy**

The CI Conventions section and the Jenkins Multibranch lesson both currently recommend draft PRs for Jenkins triggering. That predates the CodeRabbit constraint — CodeRabbit's free plan cannot review drafts, and the project's current preference is regular PRs so CodeRabbit reviews alongside Jenkins. Update both:

- **CI Conventions bullet** (around CLAUDE.md line 122): change "MUST open a (draft) PR" to "MUST open a regular (non-draft) PR" and add a clarifying sentence explaining the CodeRabbit constraint.
- **Jenkins Multibranch lesson** (in Lessons Learned): change `gh pr create --draft --base master --head <branch>` to `gh pr create --base master --head <branch>`, and add a note that draft PRs are explicitly NOT used.

Both edits are part of this PR (see CLAUDE.md diff). They keep the document's CI policy internally consistent with the polling-fix's no-draft commit-and-push workflow.

**Step 2: Commit the CLAUDE.md update**

Commit BEFORE pushing — otherwise the pushed branch / opened PR is missing one of its own required updates.

```bash
git add CLAUDE.md
git commit -m "docs(claude-md): broaden snapshot-race lesson to cover all Verify* helpers"
```

**Step 3: Push branch + open PR**

Open a regular (non-draft) PR. The repo's current policy is to skip draft PRs because CodeRabbit's free plan cannot review drafts — opening as a regular PR is what triggers CodeRabbit review alongside Jenkins. (Earlier CLAUDE.md guidance recommending draft PRs for Jenkins triggering predates the CodeRabbit constraint and should not be followed.)

```bash
git push -u origin <branch-name>
gh pr create --title "test(verify-metrics): poll all counters to eliminate snapshot-race flakes" --body "$(cat <<'EOF'
## Summary

- Master flaked on 2026-05-19 with three off-by-one integration test failures (`SqlServer.MultiConsumerAsync` processed=99/100 with chaos, `SQLite.ConsumerPoisonMessage` poison=9/10, `SQLite.Linq.ConsumerMethodAsyncPoisonMessage` poison=9/10).
- Root cause: `VerifyPoisonMessageCount`/`VerifyExpiredMessageCount`/`VerifyRollBackCount`/`VerifyProducedCount` all take a single `MetricsSnapshot`, racing the handler-completion callback. The polling fix from a prior incident was only applied to `VerifyProcessedCount`.
- Extracted a shared `PollUntil` helper inside `VerifyMetrics`, added polling overloads (`IMetrics + timeoutMs`) for all affected methods, and routed all 18 call sites in `DotNetWorkQueue.IntegrationTests.Shared/` to the polling overload.
- Bumped `VerifyProcessedCount` default polling timeout 5s → 15s to survive chaos + hold-transaction scenarios under CI load.

## Test plan

- [ ] `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` passes locally
- [ ] Repeated local runs of `SQLite.ConsumerPoisonMessage` and `SQLite.Linq.ConsumerMethodAsyncPoisonMessage` (5x each) — all green
- [ ] Memory + LiteDb fast integration suites green locally
- [ ] Jenkins 14-stage matrix green on this branch (11 runners; the temporary 9-runner change has been reverted)
- [ ] Two consecutive green Jenkins runs before merge (to gain confidence the flake-rate has dropped)

EOF
)"
```

---

## Final Verification Checklist

Before merging:

1. **Local build green:** `dotnet build "Source/DotNetWorkQueue.sln" -c Debug`
2. **Local repro of previously-failing tests:** run each of the three flaking tests 5x locally; all pass.
3. **Jenkins: two consecutive green runs** on the PR. (Per CLAUDE.md: Jenkins is PR-triggered; the first run validates the fix, the second confirms it isn't itself flaky.)
4. **Jenkins runner count is back to 11** — confirm `Jenkinsfile` matches pre-2026-05-19 state.
5. **CLAUDE.md lesson updated** so the next maintainer doesn't repeat the partial fix.

## Notes for the Implementer

- **Do not change test runtime semantics.** The polling pattern is a strict superset of the snapshot pattern — if the metric is already at the expected value on the first poll, the overload behaves identically to the snapshot version.
- **Do not parallelize Tasks 2-5.** They all modify `VerifyMetrics.cs`; sequential execution avoids merge pain.
- **If a smoke-test fails unexpectedly,** STOP and use `shipyard:shipyard-debugging` — do not bulk-edit forward through a failure.
- **The `GetCurrentMetrics()` → `metrics` switch is the ONLY call-site change.** Argument count and ordering are preserved.
