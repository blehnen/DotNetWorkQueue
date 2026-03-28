# Plan 1.1: DenyListSerializationBinder + Unit Tests

---
phase: serialization-security
plan: "1.1"
wave: 1
dependencies: []
must_haves:
  - DenyListSerializationBinder blocks known Newtonsoft.Json gadget types on BindToType
  - Pre-populated HashSet of at least 15 known gadget type names
  - AddDeniedType and AddDeniedTypes extensibility methods
  - BindToName delegates to DefaultSerializationBinder (no filtering on serialization)
  - Throws JsonSerializationException for denied types
  - Full unit test coverage
files_touched:
  - Source/DotNetWorkQueue/Serialization/DenyListSerializationBinder.cs (new)
  - Source/DotNetWorkQueue.Tests/Serialization/DenyListSerializationBinderTests.cs (new)
tdd: true
---

## Context

This plan creates the `DenyListSerializationBinder` class -- the core security mechanism that prevents deserialization of known Newtonsoft.Json gadget types. This binder will become the default for all JSON deserialization in the queue (wired in Plan 2.1). It is a standalone class with no dependencies on other queue types, making it independently testable.

The binder implements `Newtonsoft.Json.Serialization.ISerializationBinder`. On `BindToType`, it checks whether the requested type's full name (in `assemblyName, typeName` form) matches any entry in its deny list. If it matches, it throws `Newtonsoft.Json.JsonSerializationException`. If not, it delegates to `Newtonsoft.Json.Serialization.DefaultSerializationBinder` for standard type resolution. On `BindToName`, it always delegates to `DefaultSerializationBinder` (serialization/outbound is trusted).

## Dependencies

None. This is a Wave 1 plan with no prerequisites.

## Tasks

### Task 1: Create DenyListSerializationBinder class
**Files:** `Source/DotNetWorkQueue/Serialization/DenyListSerializationBinder.cs` (new)
**Action:** create
**Description:**

Create a new file at `Source/DotNetWorkQueue/Serialization/DenyListSerializationBinder.cs` with the following implementation:

1. **License header**: Include the standard 18-line LGPL-2.1 header (copy format exactly from `Source/DotNetWorkQueue/Serialization/JsonSerializer.cs` lines 1-18).

2. **Usings**: `System`, `System.Collections.Generic`, `Newtonsoft.Json`, `Newtonsoft.Json.Serialization`.

3. **Namespace**: `DotNetWorkQueue.Serialization`

4. **Class declaration**: `public class DenyListSerializationBinder : ISerializationBinder`

5. **Private fields**:
   - `private readonly HashSet<string> _deniedTypes` -- stores full type names to block
   - `private readonly DefaultSerializationBinder _defaultBinder = new DefaultSerializationBinder()` -- delegates to default for allowed types

6. **Constructor** (parameterless): Initializes `_deniedTypes` with the pre-populated deny list. Call a private static method `GetDefaultDeniedTypes()` that returns a `HashSet<string>` containing at least these known gadget type names:
   - `System.Windows.Data.ObjectDataProvider`
   - `System.Security.Principal.WindowsIdentity`
   - `System.IO.FileInfo`
   - `System.IO.DirectoryInfo`
   - `System.Diagnostics.Process`
   - `System.Configuration.Install.AssemblyInstaller`
   - `System.Activities.Presentation.WorkflowDesigner`
   - `System.Windows.ResourceDictionary`
   - `System.Windows.Forms.BindingSource`
   - `System.Web.Security.RolePrincipal`
   - `Microsoft.VisualStudio.Text.Formatting.TextFormattingRunProperties`
   - `System.IdentityModel.Tokens.SessionSecurityToken`
   - `System.Security.Claims.ClaimsIdentity`
   - `System.Security.Claims.ClaimsPrincipal`
   - `System.Data.DataSet`
   - `System.Data.DataTable`
   - `System.Xml.XmlDocument`
   - `System.Xml.XmlDataDocument`
   - `System.Management.Automation.PSObject`
   - `System.Runtime.Serialization.Formatters.Soap.SoapFormatter`

7. **Public methods for extensibility**:
   - `public void AddDeniedType(string typeName)` -- adds a single type name to `_deniedTypes`. Use `Guard.NotNullOrEmpty(() => typeName, typeName)` for validation (use the project's Guard class from `DotNetWorkQueue.Validation`).
   - `public void AddDeniedTypes(IEnumerable<string> typeNames)` -- iterates and calls `AddDeniedType` for each.

8. **`BindToType` implementation**:
   ```
   public Type BindToType(string assemblyName, string typeName)
   {
       if (_deniedTypes.Contains(typeName))
       {
           throw new JsonSerializationException(
               $"Deserialization of type '{typeName}' is blocked by the deny-list serialization binder. " +
               $"This type is a known deserialization gadget. If you believe this is a false positive, " +
               $"replace the binder via DI registration.");
       }
       return _defaultBinder.BindToType(assemblyName, typeName);
   }
   ```

9. **`BindToName` implementation**: Delegates directly to `_defaultBinder.BindToName(serializedType, out assemblyName, out typeName)`.

10. **XML doc comments** on the class and all public members (required for Release build with TreatWarningsAsErrors).

**Acceptance Criteria:**
- File compiles as part of `dotnet build "Source\DotNetWorkQueue\DotNetWorkQueue.csproj"`
- Class implements `Newtonsoft.Json.Serialization.ISerializationBinder`
- Deny list contains at least 20 known gadget type names
- `BindToType` throws `JsonSerializationException` for denied types
- `BindToType` delegates to `DefaultSerializationBinder` for allowed types
- `BindToName` always delegates to `DefaultSerializationBinder`
- `AddDeniedType` and `AddDeniedTypes` methods allow extending the deny list
- LGPL-2.1 license header is present
- All public members have XML doc comments

### Task 2: Create DenyListSerializationBinder unit tests
**Files:** `Source/DotNetWorkQueue.Tests/Serialization/DenyListSerializationBinderTests.cs` (new)
**Action:** create
**Description:**

Create a new test file at `Source/DotNetWorkQueue.Tests/Serialization/DenyListSerializationBinderTests.cs`. Follow the existing test conventions:
- No license header (matching the majority of test files in this project).
- Use MSTest (`[TestClass]`, `[TestMethod]`).
- Test method naming: `PascalCase_With_Underscores` describing the scenario.
- Use `Assert.ThrowsExactly<T>` for exception assertions (matching existing pattern in `JsonSerializerTests.cs`).

Implement these test methods:

1. **`BindToType_Denied_ObjectDataProvider_Throws_JsonSerializationException`**: Create a `DenyListSerializationBinder`, call `BindToType("", "System.Windows.Data.ObjectDataProvider")`, assert it throws `JsonSerializationException`.

2. **`BindToType_Denied_WindowsIdentity_Throws_JsonSerializationException`**: Same pattern for `System.Security.Principal.WindowsIdentity`.

3. **`BindToType_Denied_Process_Throws_JsonSerializationException`**: Same pattern for `System.Diagnostics.Process`.

4. **`BindToType_Denied_FileInfo_Throws_JsonSerializationException`**: Same pattern for `System.IO.FileInfo`.

5. **`BindToType_Denied_DataSet_Throws_JsonSerializationException`**: Same pattern for `System.Data.DataSet`.

6. **`BindToType_Allowed_Type_Returns_Type`**: Call `BindToType` with a safe, known type (e.g., `"System"` assembly, `"System.String"` type name). Assert it returns `typeof(string)`.

7. **`BindToType_Allowed_Custom_Poco_Returns_Type`**: Call `BindToType` with the test assembly and a simple test POCO class defined in the test file. Assert it returns the correct type.

8. **`BindToName_Delegates_To_Default`**: Call `BindToName(typeof(string), out assemblyName, out typeName)`. Assert `typeName` is not null (proving delegation worked).

9. **`AddDeniedType_Blocks_New_Type`**: Create binder, call `AddDeniedType("My.Custom.DangerousType")`, then call `BindToType("", "My.Custom.DangerousType")`. Assert throws `JsonSerializationException`.

10. **`AddDeniedTypes_Blocks_Multiple_New_Types`**: Create binder, call `AddDeniedTypes` with two custom type names, verify both throw.

11. **`AddDeniedType_Null_Throws_ArgumentException`**: Call `AddDeniedType(null)`, assert throws `ArgumentNullException`.

12. **`Denied_Type_Lookup_Is_Case_Sensitive`**: Call `BindToType` with `"system.diagnostics.process"` (lowercase). Assert it does NOT throw (HashSet is case-sensitive by default, and type names are case-sensitive in .NET).

**Acceptance Criteria:**
- All 12 test methods pass: `dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj" --filter "FullyQualifiedName~DenyListSerializationBinderTests"`
- Tests cover: blocking known gadgets, allowing safe types, extensibility via AddDeniedType/AddDeniedTypes, null input rejection, case sensitivity behavior, BindToName delegation

## Verification

```bash
dotnet build "Source\DotNetWorkQueue\DotNetWorkQueue.csproj" && dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj" --filter "FullyQualifiedName~DenyListSerializationBinderTests"
```

Success: Build succeeds with zero warnings. All 12 DenyListSerializationBinder tests pass.
