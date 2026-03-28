# Plan 2.1: Wire Binders into Serializers, DI Registration, and Integration Verification

---
phase: serialization-security
plan: "2.1"
wave: 2
dependencies: ["1.1", "1.2"]
must_haves:
  - DenyListSerializationBinder is the default for both JsonSerializer and JsonSerializerInternal
  - Binder is injected via constructor (DI-friendly)
  - ISerializationBinder registered as singleton in ComponentRegistration.RegisterDefaults
  - Users can override the binder via DI registration
  - Existing serialization tests continue to pass (POCO round-trip, interface round-trip)
  - New integration tests verify deny-list blocks gadgets through the full serialization path
files_touched:
  - Source/DotNetWorkQueue/Serialization/JsonSerializer.cs (modify)
  - Source/DotNetWorkQueue/Serialization/JsonSerializerInternal.cs (modify)
  - Source/DotNetWorkQueue/IoC/ComponentRegistration.cs (modify)
  - Source/DotNetWorkQueue.Tests/Serialization/JsonSerializerTests.cs (modify)
  - Source/DotNetWorkQueue.Tests/Serialization/JsonSerializerInternalTests.cs (modify)
tdd: false
---

## Context

This plan wires the binder classes created in Plans 1.1 and 1.2 into the production serialization pipeline. It modifies the two JSON serializer classes to accept an `ISerializationBinder` via their constructors and apply it to their `JsonSerializerSettings`. It registers `DenyListSerializationBinder` as the default `ISerializationBinder` in the DI container. Finally, it updates existing serializer tests and adds new tests that verify the deny-list is enforced through the full serialize/deserialize path.

The key design constraint is backward compatibility: existing users who send POCOs (including polymorphic types via `TypeNameHandling.Auto`) must continue to work. The deny-list only blocks known-dangerous types, not legitimate application types.

Note: `ISerializationBinder` here refers to `Newtonsoft.Json.Serialization.ISerializationBinder`, not a DotNetWorkQueue-defined interface. This is acceptable because Newtonsoft.Json is already a direct dependency of the core project.

## Dependencies

- **Plan 1.1** (DenyListSerializationBinder must exist)
- **Plan 1.2** (AllowListSerializationBinder must exist -- referenced in DI documentation comments, and both must compile together)

## Tasks

### Task 1: Modify JsonSerializer and JsonSerializerInternal to accept ISerializationBinder
**Files:** `Source/DotNetWorkQueue/Serialization/JsonSerializer.cs` (modify), `Source/DotNetWorkQueue/Serialization/JsonSerializerInternal.cs` (modify)
**Action:** modify
**Description:**

**JsonSerializer.cs changes:**

1. Add `using Newtonsoft.Json.Serialization;` to the usings block (after the existing `using Newtonsoft.Json;` on line 22).

2. Change the constructor from parameterless to accepting one parameter:
   ```csharp
   /// <summary>
   /// Initializes a new instance of the <see cref="JsonSerializer"/> class.
   /// </summary>
   /// <param name="serializationBinder">The serialization binder used to control type resolution during deserialization.</param>
   public JsonSerializer(ISerializationBinder serializationBinder)
   {
       Guard.NotNull(() => serializationBinder, serializationBinder);
       _serializerSettings = new JsonSerializerSettings
       {
           TypeNameHandling = TypeNameHandling.Auto,
           SerializationBinder = serializationBinder
       };
       DisplayName = "JSON";
   }
   ```

3. Change `_serializerSettings` from a field initializer to being set in the constructor (since it now depends on the constructor parameter). Make the field `private readonly JsonSerializerSettings _serializerSettings;` (declaration without initializer).

**JsonSerializerInternal.cs changes:**

1. Add `using Newtonsoft.Json.Serialization;` to the usings block.

2. Add a constructor that accepts `ISerializationBinder`:
   ```csharp
   private readonly ISerializationBinder _serializationBinder;

   /// <summary>
   /// Initializes a new instance of the <see cref="JsonSerializerInternal"/> class.
   /// </summary>
   /// <param name="serializationBinder">The serialization binder used to control type resolution during deserialization.</param>
   internal JsonSerializerInternal(ISerializationBinder serializationBinder)
   {
       Guard.NotNull(() => serializationBinder, serializationBinder);
       _serializationBinder = serializationBinder;
   }
   ```

3. Modify the `ConvertToBytes<T>` method to include the binder in its settings:
   ```csharp
   var serializerSettings = new JsonSerializerSettings
   {
       TypeNameHandling = TypeNameHandling.Auto,
       SerializationBinder = _serializationBinder
   };
   ```

4. Modify the `ConvertBytesTo<T>` method to include the binder in its settings:
   ```csharp
   var serializerSettings = new JsonSerializerSettings
   {
       TypeNameHandling = TypeNameHandling.Auto,
       ContractResolver = new PrivateSetterContractResolver(),
       SerializationBinder = _serializationBinder
   };
   ```

**Acceptance Criteria:**
- Both files compile: `dotnet build "Source\DotNetWorkQueue\DotNetWorkQueue.csproj"`
- `JsonSerializer` requires an `ISerializationBinder` in its constructor
- `JsonSerializerInternal` requires an `ISerializationBinder` in its constructor
- Both classes apply the binder to all `JsonSerializerSettings` instances
- Guard.NotNull validation on the binder parameter

### Task 2: Register DenyListSerializationBinder in ComponentRegistration
**Files:** `Source/DotNetWorkQueue/IoC/ComponentRegistration.cs` (modify)
**Action:** modify
**Description:**

1. In the `RegisterDefaults` method, add a registration for `ISerializationBinder` immediately before the existing `ISerializer` registration (which is on line 129). The new line should be:
   ```csharp
   container.Register<ISerializationBinder, DenyListSerializationBinder>(LifeStyles.Singleton);
   ```
   This goes right before `container.Register<ISerializer, JsonSerializer>(LifeStyles.Singleton);` on line 129.

2. Add `using Newtonsoft.Json.Serialization;` to the usings block at the top of the file (after the existing `using Newtonsoft.Json;` -- but note there is no existing `using Newtonsoft.Json;` in this file, so add it in alphabetical order among the third-party usings, after the `Microsoft.*` usings and before `Polly.*`).

3. In the `RegisterSharedDefaults` method, add the same `ISerializationBinder` registration for the shared context. Since `RegisterSharedDefaults` is called by `RegisterDefaultsForScheduler` and `RegisterDefaultsForJobScheduler` (which also use serialization indirectly), add:
   ```csharp
   container.Register<ISerializationBinder, DenyListSerializationBinder>(LifeStyles.Singleton);
   ```
   Place this in `RegisterSharedDefaults` near the other shared infrastructure registrations (around line 305, after the `InterceptorFactory` registration and before the `IPolicies` registration).

   **Important consideration**: `RegisterDefaults` calls `RegisterSharedDefaults` first (line 103), then registers `ISerializer` and `IInternalSerializer` (lines 129, 135). Since `RegisterSharedDefaults` runs first, the `ISerializationBinder` will already be registered when `JsonSerializer` and `JsonSerializerInternal` are resolved. This means the registration should go in `RegisterSharedDefaults` only (not duplicated in `RegisterDefaults`), since all paths call `RegisterSharedDefaults`.

   So the final approach: Add the `ISerializationBinder` registration in `RegisterSharedDefaults` only, in the `#region Singletons` section, after the `IConfiguration` registration (around line 280):
   ```csharp
   container.Register<ISerializationBinder, DenyListSerializationBinder>(LifeStyles.Singleton);
   ```

**Acceptance Criteria:**
- Full solution builds: `dotnet build "Source\DotNetWorkQueue\DotNetWorkQueue.csproj"`
- `ISerializationBinder` is registered as `DenyListSerializationBinder` singleton in `RegisterSharedDefaults`
- The registration is placed so it is available before `ISerializer` and `IInternalSerializer` are resolved
- Users can override with `AllowListSerializationBinder` (or any `ISerializationBinder`) via the `registerService` callback because overrides are enabled after transport registration in `CreateContainer.Create()`

### Task 3: Update existing serializer tests and add binder integration tests
**Files:** `Source/DotNetWorkQueue.Tests/Serialization/JsonSerializerTests.cs` (modify), `Source/DotNetWorkQueue.Tests/Serialization/JsonSerializerInternalTests.cs` (modify)
**Action:** modify
**Description:**

**JsonSerializerTests.cs changes:**

The existing `Create()` method uses `fixture.Create<JsonSerializer>()` which will auto-generate an `ISerializationBinder` via AutoNSubstitute (an NSubstitute mock). This mock's `BindToType` returns null by default, which causes `DefaultSerializationBinder` behavior in Newtonsoft.Json (it falls back to its own resolution). However, this means the existing round-trip tests may behave differently than production.

To fix this, update the `Create()` helper to provide a real `DenyListSerializationBinder`:

1. Change the `Create()` method to:
   ```csharp
   private ISerializer Create()
   {
       return new JsonSerializer(new DenyListSerializationBinder());
   }
   ```

2. Add `using DotNetWorkQueue.Serialization;` if not already present (it should already be there on line 4).

3. Add `using Newtonsoft.Json;` to the usings.

4. Add a new test that verifies the deny-list is enforced through the full serializer:
   ```csharp
   [TestMethod]
   public void Deserialize_Denied_Type_Throws_JsonSerializationException()
   {
       var serializer = Create();
       // Craft a JSON payload that contains a $type reference to a denied type
       var maliciousJson = "{\"Message\":{\"$type\":\"System.Diagnostics.Process, System\"}}";
       var bytes = System.Text.Encoding.UTF8.GetBytes(maliciousJson);

       Assert.ThrowsExactly<JsonSerializationException>(
           delegate
           {
               serializer.ConvertBytesToMessage<object>(bytes, null);
           });
   }
   ```

5. Verify the existing `Test_Serialization` and `Test_Serialization_With_Interface` tests still pass with the real binder (they should, because `TestData` is not on the deny list).

**JsonSerializerInternalTests.cs changes:**

1. Change the `Create()` method to:
   ```csharp
   private IInternalSerializer Create()
   {
       return new JsonSerializerInternal(new DenyListSerializationBinder());
   }
   ```

2. Add `using DotNetWorkQueue.Serialization;` if not already present.

3. Add `using Newtonsoft.Json;` if not already present (it should already be there on line 5).

4. Add a new test:
   ```csharp
   [TestMethod]
   public void Deserialize_Denied_Type_Throws_JsonSerializationException()
   {
       var test = Create();
       var maliciousJson = "{\"$type\":\"System.Diagnostics.Process, System\"}";
       var bytes = System.Text.Encoding.UTF8.GetBytes(maliciousJson);

       Assert.ThrowsExactly<JsonSerializationException>(
           delegate
           {
               test.ConvertBytesTo<object>(bytes);
           });
   }
   ```

5. The existing `Test_Serialization` test must still pass (round-trip with `TestData`).

6. The existing `Test_Serialization_With_Interface_Exception` test currently expects a `JsonSerializationException` when deserializing via interface. This test should still pass -- the `DenyListSerializationBinder` will delegate to `DefaultSerializationBinder` which will attempt to resolve the type via `$type`. Since `JsonSerializerInternal` does not use a `SerializationWrapper<T>` (unlike `JsonSerializer`), the polymorphic type embedded in the JSON is the private test class which will fail to resolve, still throwing `JsonSerializationException`. Verify this test still passes; if the error message changes, that is acceptable as long as the exception type is still `JsonSerializationException`.

**Acceptance Criteria:**
- All existing serializer tests pass (no regressions)
- New `Deserialize_Denied_Type_Throws_JsonSerializationException` test passes in both test files
- Tests use the real `DenyListSerializationBinder` (not a mock)
- `dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj" --filter "FullyQualifiedName~JsonSerializer"` -- all tests pass

## Verification

```bash
dotnet build "Source\DotNetWorkQueue\DotNetWorkQueue.csproj" && dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj" --filter "FullyQualifiedName~Serializ"
```

This filter catches `JsonSerializerTests`, `JsonSerializerInternalTests`, `DenyListSerializationBinderTests`, and `AllowListSerializationBinderTests`.

Success: Build succeeds. All serialization-related tests pass (existing + new), confirming:
1. Deny-list binder blocks known gadget types through the full serialization pipeline
2. Normal POCO round-trip (serialize then deserialize) continues to work
3. Polymorphic type round-trip (interface-typed messages) continues to work
4. The binder is wired into both `JsonSerializer` and `JsonSerializerInternal`

**Final integration check** (run after all plans are complete):
```bash
dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj"
```

This runs the full unit test suite to confirm no regressions anywhere in the codebase.
