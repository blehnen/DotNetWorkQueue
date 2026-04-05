# Security Audit Report -- Phase 1 (Serialization Security)

## Executive Summary

**Verdict:** PASS
**Risk Level:** Medium

Phase 1 adds meaningful defense-in-depth against Newtonsoft.Json deserialization attacks by introducing a deny-list binder that blocks 19 known gadget types. Both core serializers (`JsonSerializer` and `JsonSerializerInternal`) now enforce the binder, and the DI container registers it as the default. The implementation is sound and well-tested. However, the deny list is missing several well-known ysoserial.net gadget families, the `JsonExpressionSerializer` does not use the binder at all, and the integration test helper uses `TypeNameHandling.All` without any binder protection. None of these are directly exploitable in the current architecture (the expression serializer deserializes to a strongly-typed `Expression<>`, and the test helper is test-only code), but they represent gaps that should be closed for completeness.

### What to Do

| Priority | Finding | Location | Effort | Action |
|----------|---------|----------|--------|--------|
| 1 | Deny list missing known gadget types | DenyListSerializationBinder.cs:112-136 | Trivial | Add ~10 missing ysoserial.net gadget families |
| 2 | JsonExpressionSerializer has no binder | JsonExpressionSerializer.cs:40 | Small | Inject ISerializationBinder and attach to settings |
| 3 | Case-sensitive matching can be bypassed | DenyListSerializationBinder.cs:114 | Small | Use OrdinalIgnoreCase or normalize to consistent casing |
| 4 | HashSet not thread-safe for concurrent add/read | DenyListSerializationBinder.cs:34 | Small | Use ConcurrentDictionary or document startup-only mutation |
| 5 | Integration test helper uses TypeNameHandling.All with no binder | IntegrationTests.Shared/Helpers.cs:112 | Trivial | Add DenyListSerializationBinder to test serializer settings |

### Themes
- The binder is correctly integrated into the two primary serialization paths but one secondary path (`JsonExpressionSerializer`) is unprotected.
- The deny list is a good starting point but incomplete relative to the known ysoserial.net gadget catalog.
- Thread-safety of the mutable deny list could cause subtle issues under concurrent access.

## Detailed Findings

### Important

**[I1] Deny list missing well-known ysoserial.net gadget types**
- **Location:** `Source/DotNetWorkQueue/Serialization/DenyListSerializationBinder.cs:112-136`
- **Description:** The deny list contains 19 types but is missing several well-documented Newtonsoft.Json deserialization gadgets from the ysoserial.net project and security research. Missing types include:
  - `System.Windows.Forms.AxHost+State` (AxHost gadget)
  - `System.CodeDom.Compiler.TempFileCollection` (file deletion gadget)
  - `System.Resources.ResourceSet` (resource manipulation)
  - `System.Runtime.Remoting.Channels.Tcp.TcpServerChannel` / `TcpClientChannel` (remote channel gadgets)
  - `System.Runtime.Remoting.Channels.Http.HttpChannel` (remote channel gadgets)
  - `System.Workflow.ComponentModel.Serialization.ActivitySurrogateSelector` (RCE gadget)
  - `Microsoft.Exchange.DxStore.Common.DxSerializationUtil+LazyStreamContent` (if Exchange assemblies present)
  - `System.Data.Services.Internal.ExpandedWrapper` (type confusion)
  - `System.Runtime.Serialization.Formatters.Binary.BinaryFormatter` (nested formatter)
  - `System.Xaml.XamlObjectReader` (XAML gadgets)
  - `System.Windows.Markup.XamlReader` (XAML gadgets)
- **Impact:** An attacker with write access to the message queue backend could craft a payload using a gadget type not on the deny list. Since `TypeNameHandling.Auto` is enabled, the type would be instantiated during deserialization. (CWE-502, OWASP A08:2021 -- Software and Data Integrity Failures)
- **Remediation:** Add the missing types to `GetDefaultDeniedTypes()`. Consider also adding a namespace-prefix check (e.g., deny all types starting with `System.Runtime.Remoting`, `System.Windows.Markup`, `System.Workflow`) to catch future gadgets in known-dangerous namespaces.
- **Evidence:**
  ```csharp
  private static HashSet<string> GetDefaultDeniedTypes()
  {
      return new HashSet<string>(StringComparer.Ordinal)
      {
          // 19 types listed -- missing ActivitySurrogateSelector, BinaryFormatter,
          // XamlReader, AxHost+State, TempFileCollection, etc.
      };
  }
  ```

**[I2] JsonExpressionSerializer creates settings without a serialization binder**
- **Location:** `Source/DotNetWorkQueue/Serialization/JsonExpressionSerializer.cs:40`
- **Description:** `JsonExpressionSerializer` creates a bare `new JsonSerializerSettings()` without attaching any `SerializationBinder`. While `TypeNameHandling` defaults to `None` in this case (which is safe), the `ExpressionJsonConverter` internally handles type resolution for expression trees and could potentially be influenced by crafted type references within serialized expressions. This is the only production serializer path that does not use the binder.
- **Impact:** Low direct risk because `TypeNameHandling` is not explicitly set (defaults to `None`), but the expression converter's internal type handling is a separate concern. Inconsistency in the serialization security posture. (CWE-502)
- **Remediation:** Inject `ISerializationBinder` into the constructor and attach it to the settings for defense-in-depth consistency:
  ```csharp
  public JsonExpressionSerializer(ISerializationBinder serializationBinder)
  {
      _serializerSettings = new JsonSerializerSettings
      {
          SerializationBinder = serializationBinder
      };
      _serializerSettings.Converters.Add(...);
  }
  ```

**[I3] Case-sensitive deny list matching allows trivial bypass via case manipulation**
- **Location:** `Source/DotNetWorkQueue/Serialization/DenyListSerializationBinder.cs:114`
- **Description:** The deny list uses `StringComparer.Ordinal` (case-sensitive). Newtonsoft.Json's `DefaultSerializationBinder` uses `Type.GetType()` for resolution, which on .NET is case-sensitive for type names. However, assembly-qualified name resolution can vary across platforms and configurations. The test at line 148-163 in the test file correctly identifies this as case-sensitive, but an attacker could potentially use type forwarding or assembly binding redirects to map an alternate casing to the same type.
- **Impact:** Low probability on standard .NET runtimes but represents a defense gap if the application runs in environments with case-insensitive type resolution. (CWE-178 -- Improper Handling of Case Sensitivity)
- **Remediation:** Use `StringComparer.OrdinalIgnoreCase` for the deny list HashSet, or add both the exact and lowercase versions of each denied type. The cost is negligible and eliminates the theoretical bypass vector.

### Advisory

- **[A1]** `HashSet<string> _deniedTypes` in `DenyListSerializationBinder` (line 34) is not thread-safe. If `AddDeniedType()` is called concurrently with `BindToType()`, the `HashSet.Contains()` call could throw or return incorrect results. The XML doc warns about this, which is good, but consider using `ConcurrentDictionary<string, byte>` or making the collection immutable after construction. (CWE-362)

- **[A2]** `AllowListSerializationBinder._allowedTypes` (line 37) has the same thread-safety concern as the deny list. Consider the same remediation.

- **[A3]** Integration test helper `SerializerThatWillCrashOnDeSerialization` in `IntegrationTests.Shared/Helpers.cs:110-113` uses `TypeNameHandling.All` with no binder. While this is test-only code that intentionally throws on deserialization, it sets a bad example. Attach a `DenyListSerializationBinder` for consistency. (CWE-502)

- **[A4]** `RedisTransportOptionsFactory` (line 61) uses `JsonConvert.DeserializeObject<RedisBaseTransportOptions>(json)` without explicit settings. This defaults to `TypeNameHandling.None` which is safe, but for defense-in-depth, consider passing settings with the binder explicitly.

- **[A5]** Error messages in both binders include the denied/unregistered type name in the exception message (e.g., `$"Deserialization of type '{typeName}' is not allowed"`). This is appropriate for debugging but ensure these exceptions are not surfaced to end users in production HTTP responses, as they reveal internal type information. (CWE-209 -- Generation of Error Message Containing Sensitive Information)

## Cross-Component Analysis

**Serialization path coverage:** The binder is correctly wired into both `JsonSerializer` (user message serialization) and `JsonSerializerInternal` (internal configuration serialization) via DI. The `ComponentRegistration.RegisterSharedDefaults()` method registers `DenyListSerializationBinder` as the default `ISerializationBinder`, which means all queue instances get protection automatically. This is the correct architectural approach.

**Dashboard API safety:** The Dashboard API's `DashboardService` uses `TypeNameHandling.None` in its own `JsonSerializerSettings` (lines 337, 351), which is inherently safe against type-based deserialization attacks. The `EditMessageBody` path at line 622 uses bare `JsonConvert.DeserializeObject(bodyJson, resolvedType)` without explicit settings, but since `resolvedType` is controlled by the server (resolved from message headers, not user input), and the default `TypeNameHandling` is `None`, this is safe.

**Consistency gap:** The only production code path that creates `JsonSerializerSettings` without a binder is `JsonExpressionSerializer`. All other production paths either use the binder or use `TypeNameHandling.None`. This is a minor inconsistency that should be closed.

**DI registration correctness:** The `ISerializationBinder` registration as `Singleton` in `RegisterSharedDefaults()` (ComponentRegistration.cs:283) is correct -- both serializers that depend on it are also singletons, so there is no lifecycle mismatch.

## Analysis Coverage

| Area | Checked | Notes |
|------|---------|-------|
| Code Security (OWASP) | Yes | Focused on CWE-502 (deserialization), all serialization paths reviewed |
| Secrets & Credentials | Yes | No secrets, API keys, or credentials found in any changed file |
| Dependencies | Yes | Newtonsoft.Json 13.0.4 -- no new dependencies added |
| Infrastructure as Code | N/A | No IaC changes in this phase |
| Docker/Container | N/A | No Docker changes in this phase |
| Configuration | Yes | DI registration reviewed for correctness |

## Dependency Status

| Package | Version | Known CVEs | Status |
|---------|---------|-----------|--------|
| Newtonsoft.Json | 13.0.4 | CVE-2024-21907 (DoS via deep nesting, fixed in 13.0.4) | OK |

No new dependencies were added in this phase.

## IaC Findings

N/A -- No infrastructure-as-code changes in this phase.
