# Plan 1.2: AllowListSerializationBinder + Unit Tests

---
phase: serialization-security
plan: "1.2"
wave: 1
dependencies: []
must_haves:
  - AllowListSerializationBinder permits only explicitly registered types
  - Rejects all unregistered types with JsonSerializationException
  - AddAllowedType and AddAllowedTypes extensibility methods
  - BindToName delegates to DefaultSerializationBinder
  - Full unit test coverage
files_touched:
  - Source/DotNetWorkQueue/Serialization/AllowListSerializationBinder.cs (new)
  - Source/DotNetWorkQueue.Tests/Serialization/AllowListSerializationBinderTests.cs (new)
tdd: true
---

## Context

This plan creates the `AllowListSerializationBinder` class -- an optional, maximum-security binder that only permits deserialization of types explicitly registered by the user. Unlike the deny-list binder (Plan 1.1), which is permissive-by-default and blocks known-bad types, this binder is restrictive-by-default and allows only known-good types.

Users who want this level of lockdown register it via the DI override mechanism on `QueueContainer`:
```csharp
var container = new QueueContainer<MyTransportInit>(
    serviceRegister => serviceRegister.Register<ISerializationBinder, AllowListSerializationBinder>(LifeStyles.Singleton));
```

This plan has no dependency on Plan 1.1 -- the two binders are independent classes that implement the same interface. They can be built in parallel.

## Dependencies

None. This is a Wave 1 plan. It is independent of Plan 1.1 (they share no files).

## Tasks

### Task 1: Create AllowListSerializationBinder class
**Files:** `Source/DotNetWorkQueue/Serialization/AllowListSerializationBinder.cs` (new)
**Action:** create
**Description:**

Create a new file at `Source/DotNetWorkQueue/Serialization/AllowListSerializationBinder.cs` with the following implementation:

1. **License header**: Include the standard 18-line LGPL-2.1 header (copy format exactly from `Source/DotNetWorkQueue/Serialization/JsonSerializer.cs` lines 1-18).

2. **Usings**: `System`, `System.Collections.Generic`, `Newtonsoft.Json`, `Newtonsoft.Json.Serialization`, `DotNetWorkQueue.Validation`.

3. **Namespace**: `DotNetWorkQueue.Serialization`

4. **Class declaration**: `public class AllowListSerializationBinder : ISerializationBinder`

5. **Private fields**:
   - `private readonly HashSet<string> _allowedTypes = new HashSet<string>()` -- stores full type names that are permitted
   - `private readonly DefaultSerializationBinder _defaultBinder = new DefaultSerializationBinder()` -- delegates to default for type resolution after allow-check

6. **Constructor** (parameterless): No pre-populated list. The allow list starts empty -- users must explicitly add every type they want to permit. This is the intentional maximum-lockdown design.

7. **Public methods for extensibility**:
   - `public void AddAllowedType(string typeName)` -- adds a single type name to `_allowedTypes`. Use `Guard.NotNullOrEmpty(() => typeName, typeName)` for validation.
   - `public void AddAllowedTypes(IEnumerable<string> typeNames)` -- iterates and calls `AddAllowedType` for each.
   - `public void AddAllowedType(Type type)` -- convenience overload that calls `AddAllowedType(type.FullName)`. Use `Guard.NotNull(() => type, type)` for validation.

8. **`BindToType` implementation**:
   ```
   public Type BindToType(string assemblyName, string typeName)
   {
       if (!_allowedTypes.Contains(typeName))
       {
           throw new JsonSerializationException(
               $"Deserialization of type '{typeName}' is blocked by the allow-list serialization binder. " +
               $"Only explicitly registered types are permitted. " +
               $"Register the type via AddAllowedType() before deserializing.");
       }
       return _defaultBinder.BindToType(assemblyName, typeName);
   }
   ```

9. **`BindToName` implementation**: Delegates directly to `_defaultBinder.BindToName(serializedType, out assemblyName, out typeName)`.

10. **XML doc comments** on the class and all public members (required for Release build with TreatWarningsAsErrors). The class-level doc should explicitly state that this binder is opt-in and must be registered via DI to replace the default deny-list binder.

**Acceptance Criteria:**
- File compiles as part of `dotnet build "Source\DotNetWorkQueue\DotNetWorkQueue.csproj"`
- Class implements `Newtonsoft.Json.Serialization.ISerializationBinder`
- Empty allow list by default (blocks everything until types are registered)
- `BindToType` throws `JsonSerializationException` for unregistered types
- `BindToType` resolves registered types via `DefaultSerializationBinder`
- `BindToName` always delegates to `DefaultSerializationBinder`
- Three overloads for adding types: `string`, `IEnumerable<string>`, `Type`
- LGPL-2.1 license header is present
- All public members have XML doc comments

### Task 2: Create AllowListSerializationBinder unit tests
**Files:** `Source/DotNetWorkQueue.Tests/Serialization/AllowListSerializationBinderTests.cs` (new)
**Action:** create
**Description:**

Create a new test file at `Source/DotNetWorkQueue.Tests/Serialization/AllowListSerializationBinderTests.cs`. Follow the existing test conventions (no license header, MSTest, PascalCase_With_Underscores naming).

Implement these test methods:

1. **`BindToType_Unregistered_Type_Throws_JsonSerializationException`**: Create a new `AllowListSerializationBinder` (empty allow list). Call `BindToType("System", "System.String")`. Assert throws `JsonSerializationException`.

2. **`BindToType_Registered_Type_Returns_Type`**: Create binder, call `AddAllowedType("System.String")`, then `BindToType("mscorlib", "System.String")`. Assert returns `typeof(string)`.

3. **`AddAllowedType_String_Enables_Deserialization`**: Create binder, add type name string, verify `BindToType` succeeds for that type.

4. **`AddAllowedType_Type_Overload_Enables_Deserialization`**: Create binder, call `AddAllowedType(typeof(string))`, verify `BindToType("mscorlib", "System.String")` succeeds.

5. **`AddAllowedTypes_Enables_Multiple_Types`**: Create binder, call `AddAllowedTypes(new[] { "System.String", "System.Int32" })`, verify both resolve.

6. **`BindToType_Registered_Type_Still_Blocks_Others`**: Create binder, add `System.String`. Verify `BindToType("System", "System.Int32")` still throws.

7. **`BindToName_Delegates_To_Default`**: Call `BindToName(typeof(string), out assemblyName, out typeName)`. Assert `typeName` is not null.

8. **`AddAllowedType_Null_String_Throws_ArgumentException`**: Call `AddAllowedType((string)null)`, assert throws `ArgumentNullException`.

9. **`AddAllowedType_Null_Type_Throws_ArgumentNullException`**: Call `AddAllowedType((Type)null)`, assert throws `ArgumentNullException`.

10. **`Empty_Allow_List_Blocks_Everything`**: Create a fresh binder with no additions. Try several common types (`System.Object`, `System.String`, `System.Int32`). All should throw.

**Acceptance Criteria:**
- All 10 test methods pass: `dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj" --filter "FullyQualifiedName~AllowListSerializationBinderTests"`
- Tests cover: empty-list blocking, registration enabling, multiple registrations, Type overload, null rejection, BindToName delegation, selective blocking

## Verification

```bash
dotnet build "Source\DotNetWorkQueue\DotNetWorkQueue.csproj" && dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj" --filter "FullyQualifiedName~AllowListSerializationBinderTests"
```

Success: Build succeeds with zero warnings. All 10 AllowListSerializationBinder tests pass.
