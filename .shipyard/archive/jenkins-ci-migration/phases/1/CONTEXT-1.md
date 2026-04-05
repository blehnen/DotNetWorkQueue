# Phase 1: Serialization Security — Design Decisions

## Decisions Captured

### 1. Extensibility Model
**Decision:** Method on binder instance
- `DenyListSerializationBinder` exposes `AddDeniedType(string)` and `AddDeniedTypes(IEnumerable<string>)`
- Users get the default instance from DI and add types
- Example: `container.GetInstance<ISerializationBinder>() as DenyListSerializationBinder`

### 2. Protection Direction
**Decision:** Deserialization only
- Block dangerous types only when reading/deserializing messages (inbound)
- Serialization (outbound/producer side) is trusted — your own code produces it
- Binder's `BindToType` enforces the deny-list; `BindToName` passes through unchanged

### 3. Integration Test Helper
**Decision:** Leave as-is
- `SerializerThatWillCrashOnDeSerialization` in `Helpers.cs` uses `TypeNameHandling.All` intentionally
- It's a test helper designed to create a broken serializer for failure testing
- Do NOT modify this class

### 4. Prior Decisions (from brainstorm)
- Deny-list binder is the default (non-breaking for existing users)
- Allow-list binder is optional, registered via DI override
- Deny-list uses HashSet<string> for O(1) lookup
- Both binders registered through existing IoC pipeline in ComponentRegistration
- No new NuGet dependencies
