# Phase 1 Plan Critique

## PLAN-1.1: ObjectPool Dead Code Deletion

### File Existence
- `Source/DotNetWorkQueue/Cache/ObjectPool.cs` -- EXISTS
- `Source/DotNetWorkQueue/IObjectPool.cs` -- EXISTS
- `Source/DotNetWorkQueue/IPooledObject.cs` -- EXISTS

### API Surface: MATCH
- No API dependencies to check -- pure deletion

### Verify Command: RUNNABLE
- `dotnet build "Source/DotNetWorkQueue/DotNetWorkQueue.csproj" -c Debug --no-restore` -- valid syntax

### Forward References: NONE
- No shared files with PLAN-1.2

### Complexity: LOW
- 3 files, 1 directory

### Verdict: READY

---

## PLAN-1.2: In-Memory Trace Exporter

### File Existence
- `Source/DotNetWorkQueue.IntegrationTests.Shared/SharedSetup.cs` -- EXISTS
- `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/Producer/SimpleProducer.cs` -- EXISTS

### API Surface: CAUTION
- `ActivitySourceWrapper` class -- EXISTS at SharedSetup.cs:184
- `CreateTrace(string name)` method -- EXISTS at SharedSetup.cs:155
- `ActivitySourceWrapper.Source` property -- EXISTS at SharedSetup.cs:191

### Critical Finding: Task 2 Test Pattern Mismatch
The proposed test in Task 2 manually creates a `QueueCreationContainer`, `SharedSetup.CreateTrace()`, and `SharedSetup.CreateCreator()` -- but the actual Memory `SimpleProducer` test delegates to `Implementation.SimpleProducer` → `ProducerShared.RunTest()` which already calls `SharedSetup.CreateTrace("producer")` internally (line 41 of ProducerShared.cs).

**Impact:** The proposed test will work but:
1. It duplicates the setup pattern already in ProducerShared instead of following existing conventions
2. The `trace` variable inside `ProducerShared.RunTest()` is not accessible to the caller, so the proposed test correctly creates its own trace -- but this means it's a parallel code path, not reusing the existing flow

**Mitigation:** The proposed approach is still valid -- it creates its own trace and producer to assert on `CollectedActivities`. It just doesn't follow the existing delegation pattern. This is acceptable because the point is to PROVE the ActivityListener works, not to exercise the same path as existing tests. The existing tests will automatically get trace coverage from Task 1's ActivityListener change.

### Forward References: NONE
- PLAN-1.1 and PLAN-1.2 touch completely different files

### Hidden Dependencies: NONE
- Plans are truly independent

### Complexity: LOW-MEDIUM
- 2 files touched, 1 directory

---

## Overall Verdict: **CAUTION**

Proceed with awareness:
1. **Task 2 test pattern doesn't match existing conventions** -- the proposed test manually orchestrates producer setup instead of delegating to the shared implementation. This is acceptable for a trace verification test but the builder should be aware of the existing delegation pattern.
2. **After Task 1, ALL existing integration tests will collect activities** -- this is a positive side effect that means trace coverage gains happen automatically across all transports, not just from the new test.
