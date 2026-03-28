# Phase 1: Serialization Security -- Plan Verification and Critique

**Date:** 2026-03-26
**Type:** plan-review
**Phase:** Phase 1 -- Serialization Security (Deny-List and Allow-List Binders)

---

## Part 1: Plan Verification

### Success Criteria Coverage Matrix

| # | Success Criterion (from ROADMAP.md) | Covered By | Status |
|---|-------------------------------------|------------|--------|
| 1 | DenyListSerializationBinder blocks ObjectDataProvider, WindowsIdentity, FileInfo, Process, and 10+ other gadget types | PLAN-1.1 Task 1 (20 types listed) | COVERED |
| 2 | Attempting to deserialize a blocked type throws JsonSerializationException | PLAN-1.1 Task 1 (BindToType impl) | COVERED |
| 3 | Normal POCO message round-trip continues to work identically | PLAN-2.1 Task 3 (existing Test_Serialization must pass) | COVERED |
| 4 | AllowListSerializationBinder permits only explicitly registered types, rejects all others | PLAN-1.2 Task 1 | COVERED |
| 5 | Binder lookup is O(1) via HashSet | PLAN-1.1 Task 1 (HashSet<string>), PLAN-1.2 Task 1 (HashSet<string>) | COVERED |
| 6 | Deny-list binder is the default for both JsonSerializer and JsonSerializerInternal without user config | PLAN-2.1 Tasks 1+2 | COVERED |
| 7 | Users can extend the deny list by adding types after construction | PLAN-1.1 Task 1 (AddDeniedType, AddDeniedTypes) | COVERED |
| 8 | Users can replace the binder entirely via DI registration override | PLAN-2.1 Task 2 (registered in RegisterSharedDefaults, overridable via registerService callback) | COVERED |
| 9 | All existing unit tests pass | PLAN-2.1 Task 3 + Verification commands | COVERED |
| 10 | All in-memory integration tests pass | PLAN-2.1 Verification (final integration check) | COVERED |
| 11 | New binder tests pass with full coverage of deny/allow/extend/override scenarios | PLAN-1.1 Task 2 (12 tests), PLAN-1.2 Task 2 (10 tests), PLAN-2.1 Task 3 (2 new tests) | COVERED |

**Result:** All 11 success criteria are covered by at least one plan.

### Task Count Check

| Plan | Tasks | Limit (3) | Status |
|------|-------|-----------|--------|
| PLAN-1.1 | 2 | 3 | PASS |
| PLAN-1.2 | 2 | 3 | PASS |
| PLAN-2.1 | 3 | 3 | PASS |

### Wave Ordering and Dependency Check

| Plan | Wave | Dependencies | Valid? | Notes |
|------|------|--------------|--------|-------|
| PLAN-1.1 | 1 | none | PASS | Standalone; creates DenyListSerializationBinder |
| PLAN-1.2 | 1 | none | PASS | Standalone; creates AllowListSerializationBinder |
| PLAN-2.1 | 2 | 1.1, 1.2 | PASS | Correctly depends on both Wave 1 plans |

Wave 1 plans (1.1 and 1.2) are independent and can execute in parallel. Wave 2 plan (2.1) correctly waits for both.

### File Conflict Check (Wave 1 Parallel Plans)

| File | PLAN-1.1 | PLAN-1.2 | Conflict? |
|------|----------|----------|-----------|
| DenyListSerializationBinder.cs (new) | CREATE | -- | NO |
| DenyListSerializationBinderTests.cs (new) | CREATE | -- | NO |
| AllowListSerializationBinder.cs (new) | -- | CREATE | NO |
| AllowListSerializationBinderTests.cs (new) | -- | CREATE | NO |

**Result:** No file conflicts between parallel Wave 1 plans. Each plan touches entirely disjoint files.

### Design Decisions Reflected in Plans

| Decision (from CONTEXT-1.md) | Reflected In | Status |
|-------------------------------|-------------|--------|
| 1. Extensibility via method on binder instance (AddDeniedType, AddDeniedTypes) | PLAN-1.1 Task 1, items 7 | PASS |
| 2. Deserialization only (BindToType enforces deny-list; BindToName passes through) | PLAN-1.1 Task 1, items 8-9 | PASS |
| 3. Integration test helper -- leave as-is | No plan touches Helpers.cs | PASS |
| 4. Deny-list is default, allow-list is optional DI override | PLAN-2.1 Task 2 (registers DenyList as default) | PASS |
| 4. HashSet<string> for O(1) lookup | PLAN-1.1 Task 1, item 5 | PASS |
| 4. No new NuGet dependencies | No plan adds NuGet references | PASS |

**Result:** All design decisions from CONTEXT-1.md are properly reflected.

### ROADMAP vs Plans: Discrepancy Check

The ROADMAP mentions updating `SerializerThatWillCrashOnDeSerialization` in `Helpers.cs`, but CONTEXT-1.md (Decision #3) explicitly overrides this: "Leave as-is." The plans correctly follow the CONTEXT-1.md decision and do NOT touch `Helpers.cs`. This is correct because the test helper always throws on deserialization anyway (it never resolves types), so the binder provides no benefit there.

---

## Part 2: Plan Critique (Feasibility Stress Test)

### PLAN-1.1: DenyListSerializationBinder + Unit Tests

#### File Path Verification

| File | Action | Exists? | Expected | Status |
|------|--------|---------|----------|--------|
| `Source/DotNetWorkQueue/Serialization/DenyListSerializationBinder.cs` | CREATE | No | No (new file) | PASS |
| `Source/DotNetWorkQueue.Tests/Serialization/DenyListSerializationBinderTests.cs` | CREATE | No | No (new file) | PASS |
| `Source/DotNetWorkQueue/Serialization/` directory | -- | Yes | Yes | PASS |
| `Source/DotNetWorkQueue.Tests/Serialization/` directory | -- | Yes | Yes | PASS |

#### API Surface Verification

| API Reference | Actual Codebase | Status |
|---------------|-----------------|--------|
| `Newtonsoft.Json.Serialization.ISerializationBinder` | Confirmed present in Newtonsoft.Json 13.0.4 XML docs | PASS |
| `Newtonsoft.Json.Serialization.DefaultSerializationBinder` | Confirmed present in Newtonsoft.Json 13.0.4 XML docs | PASS |
| `Newtonsoft.Json.JsonSerializationException` | Used at `JsonSerializerInternalTests.cs:59` | PASS |
| `Guard.NotNullOrEmpty(() => typeName, typeName)` | Guard class at `netfx/System/Guard.cs:66` has `NotNullOrEmpty(Expression<Func<string>>, string)` | PASS |
| `DotNetWorkQueue.Validation` namespace | Guard.cs at line 37: `namespace DotNetWorkQueue.Validation` | PASS |
| License header format (18 lines) | `JsonSerializer.cs` lines 1-18 | PASS |

#### Verification Commands

| Command | Runnable? | Notes |
|---------|-----------|-------|
| `dotnet build "Source\DotNetWorkQueue\DotNetWorkQueue.csproj"` | Yes | Project path verified |
| `dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj" --filter "FullyQualifiedName~DenyListSerializationBinderTests"` | Yes | Test project path verified |

#### Complexity Flags

- Files touched: 2 (under limit)
- Directories: 2 (`Serialization/` in core and tests)
- No complexity concern

#### Issues Found

**ISSUE 1.1-A (LOW): Guard.NotNull signature mismatch for Type overload.** Plan 1.1 Task 1 item 7 specifies `Guard.NotNullOrEmpty(() => typeName, typeName)` for `AddDeniedType(string)`. This matches the actual Guard API signature `NotNullOrEmpty(Expression<Func<string>>, string)` at `Guard.cs:66`. PASS -- no issue.

**ISSUE 1.1-B (INFO): Test naming convention.** Plan specifies `Assert.ThrowsExactly<T>` which matches the pattern in existing tests at `JsonSerializerTests.cs:16` and `JsonSerializerInternalTests.cs:17`. PASS.

**PLAN-1.1 VERDICT: READY** -- No blocking issues.

---

### PLAN-1.2: AllowListSerializationBinder + Unit Tests

#### File Path Verification

| File | Action | Exists? | Expected | Status |
|------|--------|---------|----------|--------|
| `Source/DotNetWorkQueue/Serialization/AllowListSerializationBinder.cs` | CREATE | No | No (new file) | PASS |
| `Source/DotNetWorkQueue.Tests/Serialization/AllowListSerializationBinderTests.cs` | CREATE | No | No (new file) | PASS |

#### API Surface Verification

Same as PLAN-1.1 (`ISerializationBinder`, `DefaultSerializationBinder`, `Guard`). All verified above.

Additional API:
| API Reference | Actual Codebase | Status |
|---------------|-----------------|--------|
| `Guard.NotNull(() => type, type)` for `Type` parameter | Guard class at `Guard.cs:52` has `NotNull<T>(Expression<Func<T>>, T)` | PASS |
| `type.FullName` property | Standard .NET API | PASS |

#### Verification Commands

| Command | Runnable? | Notes |
|---------|-----------|-------|
| `dotnet build "Source\DotNetWorkQueue\DotNetWorkQueue.csproj"` | Yes | Verified |
| `dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj" --filter "FullyQualifiedName~AllowListSerializationBinderTests"` | Yes | Verified |

#### Complexity Flags

- Files touched: 2 (under limit)
- Directories: 2
- No complexity concern

#### Issues Found

None.

**PLAN-1.2 VERDICT: READY** -- No blocking issues.

---

### PLAN-2.1: Wiring Binders into Serializers, DI Registration, Integration Tests

#### File Path Verification

| File | Action | Exists? | Expected | Status |
|------|--------|---------|----------|--------|
| `Source/DotNetWorkQueue/Serialization/JsonSerializer.cs` | MODIFY | Yes | Yes | PASS |
| `Source/DotNetWorkQueue/Serialization/JsonSerializerInternal.cs` | MODIFY | Yes | Yes | PASS |
| `Source/DotNetWorkQueue/IoC/ComponentRegistration.cs` | MODIFY | Yes | Yes | PASS |
| `Source/DotNetWorkQueue.Tests/Serialization/JsonSerializerTests.cs` | MODIFY | Yes | Yes | PASS |
| `Source/DotNetWorkQueue.Tests/Serialization/JsonSerializerInternalTests.cs` | MODIFY | Yes | Yes | PASS |

#### API Surface Verification

| API Reference in Plan | Actual Code | Status |
|----------------------|-------------|--------|
| `JsonSerializer` has parameterless constructor | `JsonSerializer.cs:39`: `public JsonSerializer()` -- parameterless | PASS |
| `JsonSerializer` has `_serializerSettings` as field initializer | `JsonSerializer.cs:31-34`: field initializer `new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto }` | PASS |
| `JsonSerializerInternal` is `internal class` | `JsonSerializerInternal.cs:33`: `internal class JsonSerializerInternal : IInternalSerializer` | PASS |
| `JsonSerializerInternal` has no explicit constructor | Confirmed: no constructor defined, uses implicit parameterless | PASS |
| `JsonSerializerInternal` creates new `JsonSerializerSettings` in each method | `JsonSerializerInternal.cs:44-47` (ConvertToBytes) and `JsonSerializerInternal.cs:60-64` (ConvertBytesTo) | PASS |
| `ComponentRegistration.RegisterSharedDefaults` exists | `ComponentRegistration.cs:273`: `private static void RegisterSharedDefaults(IContainer container, QueueConnection connection)` | PASS |
| `ISerializer` registration at line 129 | `ComponentRegistration.cs:129`: `container.Register<ISerializer, JsonSerializer>(LifeStyles.Singleton);` | PASS |
| `IInternalSerializer` registration at line 135 | `ComponentRegistration.cs:135`: `container.Register<IInternalSerializer, JsonSerializerInternal>(LifeStyles.Singleton);` | PASS |
| `using DotNetWorkQueue.Serialization;` already in ComponentRegistration | `ComponentRegistration.cs:33` | PASS |
| `IContainer.Register<TService, TImplementation>` supports interface as TService | `IContainer.cs:60-62`: `where TService : class` (interfaces qualify) | PASS |
| `InternalsVisibleTo("DotNetWorkQueue.Tests")` | `InternalsVisibleForTests.cs:21` | PASS |
| Existing test `Create()` uses `fixture.Create<JsonSerializer>()` | `JsonSerializerTests.cs:65-67` | PASS |
| Existing test `Create()` uses `fixture.Create<JsonSerializerInternal>()` | `JsonSerializerInternalTests.cs:69-71` | PASS |
| No `new JsonSerializer()` or `new JsonSerializerInternal()` in production code | Grep confirms: zero matches for `new JsonSerializer\b` and `new JsonSerializerInternal` in Source/ | PASS |

#### Verification Commands

| Command | Runnable? | Notes |
|---------|-----------|-------|
| `dotnet build "Source\DotNetWorkQueue\DotNetWorkQueue.csproj"` | Yes | Verified |
| `dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj" --filter "FullyQualifiedName~Serializ"` | Yes | Currently runs 15 tests (all pass). Verified baseline. |
| `dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj"` | Yes | Full unit test suite |

#### Forward References Check

Plan 2.1 depends on Plans 1.1 and 1.2 (Wave 2 after Wave 1). No forward references within the same wave.

#### Hidden Dependencies Check

Plans 1.1 and 1.2 in Wave 1 have no hidden dependencies -- they create independent files in independent namespaces. Neither plan imports or references the other's output.

#### Complexity Flags

- Files touched: 5 (modify only, no new files)
- Directories: 3 (`Serialization/`, `IoC/`, `Tests/Serialization/`)
- Under the 10-file and 3-directory thresholds

#### Issues Found

**ISSUE 2.1-A (MEDIUM): Plan Task 2 has contradictory placement instructions.** The plan's Task 2 description goes through three different placement proposals for the `ISerializationBinder` registration in `ComponentRegistration.cs`:

1. First says: "add immediately before the existing `ISerializer` registration (which is on line 129)" -- this is in `RegisterDefaults`.
2. Then says: "In the `RegisterSharedDefaults` method, add the same registration" -- this is a different method.
3. Finally says: "the registration should go in `RegisterSharedDefaults` only (not duplicated in `RegisterDefaults`)" -- this is the correct final answer, in the `#region Singletons` section around line 280.

The plan ultimately arrives at the right answer, but the contradictory progression could confuse the builder. The final instruction (place in `RegisterSharedDefaults` after the `IConfiguration` registration around line 280) is correct because:
- `RegisterSharedDefaults` is called by all three entry points (`RegisterDefaults` line 103, `RegisterDefaultsForScheduler` line 62, `RegisterDefaultsForJobScheduler` line 87).
- The serializers are registered later in `RegisterDefaults` (lines 129, 135), so the binder will already be available when they're resolved.
- Single registration avoids duplication.

**Mitigation:** Builder should follow ONLY the final instruction: add `container.Register<ISerializationBinder, DenyListSerializationBinder>(LifeStyles.Singleton);` in `RegisterSharedDefaults`, in the `#region Singletons` section, after the `IConfiguration` registration (after line 280).

**ISSUE 2.1-B (MEDIUM): Missing `using Newtonsoft.Json.Serialization;` in ComponentRegistration.cs.** Plan Task 2 says to add `using Newtonsoft.Json.Serialization;` because `ISerializationBinder` is in that namespace. However, `ComponentRegistration.cs` already has `using DotNetWorkQueue.Serialization;` (line 33) which is a DIFFERENT namespace. The new `using Newtonsoft.Json.Serialization;` IS required and the plan correctly identifies this. The plan's instruction about where to add it ("after the `Microsoft.*` usings and before `Polly.*`") references `Polly.Registry` (line 41) which is correct placement.

**Mitigation:** No issue -- plan is accurate here.

**ISSUE 2.1-C (LOW): Test `Deserialize_Denied_Type_Throws_JsonSerializationException` in JsonSerializerTests.cs.** The plan crafts a malicious JSON: `{"Message":{"$type":"System.Diagnostics.Process, System"}}`. This uses the `SerializationWrapper<T>` structure (the wrapper class at `JsonSerializer.cs:81-90`). The `$type` is on the `Message` property inside the wrapper. When `JsonSerializer.ConvertBytesToMessage<object>` is called, Newtonsoft will attempt to resolve the `$type` on the inner `Message` value. The binder's `BindToType` will be called for `System.Diagnostics.Process`, which will throw `JsonSerializationException`. This approach is correct.

**ISSUE 2.1-D (LOW): `JsonSerializerInternal` constructor visibility.** Plan specifies `internal JsonSerializerInternal(ISerializationBinder serializationBinder)`. The class is `internal`, so an `internal` constructor is correct and accessible to both the DI container (via SimpleInjector with appropriate access) and the test project (via InternalsVisibleTo). SimpleInjector's container wrapper in this project can construct internal types -- confirmed by the existing registration of `JsonSerializerInternal` at `ComponentRegistration.cs:135`. PASS.

**ISSUE 2.1-E (INFO): Existing `Test_Serialization_With_Interface` test behavior after binder change.** The `JsonSerializerTests.cs` `Test_Serialization_With_Interface` test (lines 48-61) serializes `ITestData` (backed by private `TestData` class) and deserializes it. With `TypeNameHandling.Auto`, Newtonsoft embeds `$type` for the `TestData` class. The deny-list binder will see `DotNetWorkQueue.Tests.Serialization.JsonSerializerTests+TestData` which is NOT on the deny list, so the test will continue to pass. PASS.

**ISSUE 2.1-F (INFO): Existing `Test_Serialization_With_Interface_Exception` test in JsonSerializerInternalTests.** This test (line 49-64) currently passes because `JsonSerializerInternal` (without the wrapper class) embeds `$type` for the `TestData` class, but when deserializing as `ITestData`, the type resolution fails. With the new binder, `BindToType` will be called first -- the type name will NOT match the deny list, so it delegates to `DefaultSerializationBinder`, which will then fail with the same `JsonSerializationException` as before. PASS.

**PLAN-2.1 VERDICT: CAUTION** -- Plan is feasible but contains confusing contradictory placement instructions (Issue 2.1-A). Builder must follow only the final instruction.

---

## Coverage of Non-Functional Requirements from PROJECT.md

| Requirement | Covered By | Status |
|-------------|-----------|--------|
| Backward compatibility (deny-list is additive, not breaking) | PLAN-2.1 Task 3 verifies existing round-trip tests pass | COVERED |
| Testing required for all changes | 24 new tests across 3 plans | COVERED |
| O(1) binder lookup via HashSet | PLAN-1.1 and 1.2 both use HashSet<string> | COVERED |
| Multi-target (net10.0, net8.0, net48, netstandard2.0) | ISerializationBinder, DefaultSerializationBinder, HashSet, JsonSerializationException -- all available on all targets. No target-specific APIs used. | COVERED |
| No new NuGet dependencies | No plan adds PackageReference entries | COVERED |

---

## Gaps

1. **Integration test helper not updated (intentional).** ROADMAP.md key files list `Helpers.cs` for update, but CONTEXT-1.md Decision #3 explicitly says "Leave as-is." The plans correctly follow CONTEXT-1.md. This is a documentation gap in the ROADMAP, not a plan gap. The ROADMAP should be updated to remove `Helpers.cs` from the key files list, or note it as explicitly excluded.

2. **No integration test through the DI container.** The plans verify the binder works through the serializer classes directly (unit tests), but there is no test that creates a full DI container and verifies the binder is wired in end-to-end. The verification command `dotnet test "Source\DotNetWorkQueue.Transport.Memory.Integration.Tests\DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj"` would cover this indirectly (existing integration tests use the full DI pipeline), but no NEW test explicitly asserts the binder is present in the container. This is low risk because the existing integration tests will exercise the full serialization pipeline.

3. **No test for DI override with AllowListSerializationBinder.** Success criterion #8 says "Users can replace the binder entirely via DI registration override." The plans mention this is possible via `QueueContainer`'s `registerService` callback, but no test verifies it. This is a unit-test-level gap. However, the DI override mechanism is a general framework feature already tested elsewhere, so the risk is low.

---

## Recommendations

1. **For Issue 2.1-A:** Before building, the builder should read Plan 2.1 Task 2 completely and follow ONLY the final placement instruction (add in `RegisterSharedDefaults` `#region Singletons` section after line 280). Ignore the earlier contradictory suggestions.

2. **For Gap #2:** After all plans are complete, run the in-memory integration tests as the final verification step. If they pass, the DI wiring is confirmed end-to-end.

3. **For Gap #3:** Consider adding one additional test in PLAN-2.1 Task 3 that creates a DI container with an `AllowListSerializationBinder` override and verifies it takes effect. This is optional/nice-to-have.

---

## Verdict

**READY** -- All three plans are feasible against the actual codebase. All success criteria are covered. File paths, API surfaces, and test infrastructure are verified. The one caution (contradictory placement instructions in PLAN-2.1 Task 2) is a readability issue, not a blocking issue -- the plan self-corrects and the final instruction is accurate. No file conflicts exist between parallel Wave 1 plans. Wave ordering correctly respects dependencies.
