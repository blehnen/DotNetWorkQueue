# Roadmap: Security & Stability Fixes

## Milestone: Harden DotNetWorkQueue for Safe-by-Default Operation

This roadmap addresses 2 Critical and 3 High severity concerns from the codebase analysis. The phases are ordered by risk (highest first) and dependency (foundations first). Each phase is independently shippable -- it can be merged, released, and verified without waiting for later phases.

---

### Phase 1: Serialization Security (Deny-List and Allow-List Binders)

**Complexity:** Medium
**Dependencies:** None
**Items:** C-1

**Description:**
The `JsonSerializer` and `JsonSerializerInternal` classes use `TypeNameHandling.Auto` with no `SerializationBinder`, allowing known Newtonsoft.Json gadget chains (ObjectDataProvider, WindowsIdentity, Process, FileInfo, etc.) to execute arbitrary code during deserialization. This is the highest-risk item because any actor who can write a crafted message to any queue backend achieves Remote Code Execution on the consumer.

This phase creates two new classes in `DotNetWorkQueue/Serialization/`:

1. **`DenyListSerializationBinder`** -- A `DefaultSerializationBinder` subclass that maintains a `HashSet<string>` of blocked type names. On `BindToType`, if the requested type matches the deny list, it throws `JsonSerializationException`. The deny list ships pre-populated with well-known Newtonsoft.Json gadget types and is extensible (users can add additional types). This becomes the default binder wired into both `JsonSerializer` and `JsonSerializerInternal`.

2. **`AllowListSerializationBinder`** -- An optional binder that only permits explicitly registered types. Users who want maximum lockdown can register this via DI to override the default deny-list binder.

Both binders are registered through the existing IoC pipeline. The deny-list binder is registered as a singleton default in `ComponentRegistration.RegisterDefaults`. Users can override it via the standard `registerService` callback on `QueueContainer`.

The integration test helper `SerializerThatWillCrashOnDeSerialization` in `Helpers.cs` also uses `TypeNameHandling.All` and must be updated to use the deny-list binder for consistency.

**Key Files:**
- `Source/DotNetWorkQueue/Serialization/JsonSerializer.cs` -- Wire in binder to `_serializerSettings`
- `Source/DotNetWorkQueue/Serialization/JsonSerializerInternal.cs` -- Wire in binder to both settings instances
- `Source/DotNetWorkQueue/Serialization/DenyListSerializationBinder.cs` -- New file
- `Source/DotNetWorkQueue/Serialization/AllowListSerializationBinder.cs` -- New file
- `Source/DotNetWorkQueue/IoC/ComponentRegistration.cs` -- Register default binder (around line 129-135 where serializers are registered)
- `Source/DotNetWorkQueue.IntegrationTests.Shared/Helpers.cs` -- Update `SerializerThatWillCrashOnDeSerialization` to use deny-list binder
- `Source/DotNetWorkQueue.Tests/Serialization/DenyListSerializationBinderTests.cs` -- New test file
- `Source/DotNetWorkQueue.Tests/Serialization/AllowListSerializationBinderTests.cs` -- New test file
- `Source/DotNetWorkQueue.Tests/Serialization/JsonSerializerTests.cs` -- Add binder integration tests (may need to create or extend)

**Success Criteria:**
- [ ] `DenyListSerializationBinder` blocks deserialization of `System.Windows.Data.ObjectDataProvider`, `System.Security.Principal.WindowsIdentity`, `System.IO.FileInfo`, `System.Diagnostics.Process`, and at least 10 other known gadget types
- [ ] Attempting to deserialize a blocked type throws `JsonSerializationException`
- [ ] Normal POCO message round-trip (serialize then deserialize) continues to work identically to current behavior
- [ ] `AllowListSerializationBinder` permits only explicitly registered types and rejects all others
- [ ] Binder lookup is O(1) via `HashSet` (not a linear scan)
- [ ] The deny-list binder is the default for both `JsonSerializer` and `JsonSerializerInternal` without any user configuration
- [ ] Users can extend the deny list by adding types after construction
- [ ] Users can replace the binder entirely via DI registration override
- [ ] All existing unit tests pass: `dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj"`
- [ ] All in-memory integration tests pass: `dotnet test "Source\DotNetWorkQueue.Transport.Memory.Integration.Tests\DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj"`
- [ ] New binder tests pass with full coverage of deny/allow/extend/override scenarios

---

### Phase 2: Queue Name Validation

**Complexity:** Medium
**Dependencies:** None (parallel with Phase 1)
**Items:** H-2

**Description:**
Queue names are concatenated directly into SQL table names and Redis key names without any validation. A malicious or careless queue name containing SQL metacharacters (e.g., `]; DROP TABLE --`) would result in SQL injection at the DDL/DML level. The risk is mitigated by the fact that queue names are set by application developers, but there is no enforcement.

This phase adds fail-fast validation in the `BaseConnectionInformation` constructor (the base class all transports inherit from) with a shared validation method, plus transport-specific overrides where needed. Validation occurs at construction time so invalid queue names never reach the SQL layer.

The validation rule for the base class: queue names must match `^[A-Za-z0-9_][A-Za-z0-9_.]*$` (start with alphanumeric or underscore, then alphanumeric, underscore, or dot). Individual transports can tighten or loosen this rule. Redis allows hyphens (keys support more characters). The Memory transport is the most permissive but still benefits from the base validation to maintain consistency.

Each transport's existing connection info test file already exists and needs new test cases for validation rejection.

**Key Files:**
- `Source/DotNetWorkQueue/Configuration/BaseConnectionInformation.cs` -- Add `ValidateQueueName()` method called from constructor
- `Source/DotNetWorkQueue.Transport.SqlServer/SQLConnectionInformation.cs` -- Override or call additional SQL Server-specific validation (bracket chars, semicolons)
- `Source/DotNetWorkQueue.Transport.PostgreSQL/SQLConnectionInformation.cs` -- Override or call additional PostgreSQL-specific validation (double-quote chars)
- `Source/DotNetWorkQueue.Transport.SQLite/SqliteConnectionInformation.cs` -- Inherits base validation
- `Source/DotNetWorkQueue.Transport.LiteDB/LiteDbConnectionInformation.cs` -- Inherits base validation
- `Source/DotNetWorkQueue.Transport.Redis/RedisConnectionInfo.cs` -- Override to allow hyphens in Redis key names
- `Source/DotNetWorkQueue/Transport/Memory/ConnectionInformation.cs` -- Inherits base validation
- `Source/DotNetWorkQueue.Tests/Configuration/BaseConnectionInformationTests.cs` -- Add validation tests
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/SqlConnectionInformationTests.cs` -- Add validation tests
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/SqlConnectionInformationTests.cs` -- Add validation tests
- `Source/DotNetWorkQueue.Transport.Redis.Tests/RedisConnectionInfoTests.cs` -- Add validation tests
- `Source/DotNetWorkQueue.Transport.SQLite.Tests/SQLiteConnectionInformationTests.cs` -- Add validation tests
- `Source/DotNetWorkQueue.Transport.LiteDb.Tests/LiteDbConnectionInformationTests.cs` -- Add validation tests

**Success Criteria:**
- [ ] Queue names containing SQL injection patterns (`]; DROP TABLE`, `' OR 1=1`, `"; DELETE FROM`) are rejected with `ArgumentException` at construction time
- [ ] Queue names containing only alphanumeric characters, underscores, and dots are accepted
- [ ] Redis transport additionally accepts hyphens in queue names
- [ ] Empty and null queue names are rejected (some transports may already handle this)
- [ ] Validation error messages clearly state which characters are permitted
- [ ] All existing unit tests pass (existing test queue names must comply with the new rules -- verify this before implementing)
- [ ] Unit tests cover: valid names, SQL injection attempts, special characters, empty/null, boundary cases (single char, max-length considerations)
- [ ] Per-transport test commands all pass:
  - `dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj"`
  - `dotnet test "Source\DotNetWorkQueue.Transport.SqlServer.Tests\DotNetWorkQueue.Transport.SqlServer.Tests.csproj"`
  - `dotnet test "Source\DotNetWorkQueue.Transport.PostgreSQL.Tests\DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj"`
  - `dotnet test "Source\DotNetWorkQueue.Transport.Redis.Tests\DotNetWorkQueue.Transport.Redis.Tests.csproj"`
  - `dotnet test "Source\DotNetWorkQueue.Transport.SQLite.Tests\DotNetWorkQueue.Transport.SQLite.Tests.csproj"`
  - `dotnet test "Source\DotNetWorkQueue.Transport.LiteDb.Tests\DotNetWorkQueue.Transport.LiteDb.Tests.csproj"`

---

### Phase 3: Async Dispose Fix for DashboardConsumerClient

**Complexity:** Small
**Dependencies:** None (parallel with Phases 1 and 2)
**Items:** H-5

**Description:**
`DashboardConsumerClient.Dispose()` calls `.ConfigureAwait(false).GetAwaiter().GetResult()` on an async HTTP DELETE call, which is a sync-over-async anti-pattern that can deadlock in SynchronizationContext environments (ASP.NET, WPF, WinForms). The `catch` block also silently swallows all exceptions.

This phase implements `IAsyncDisposable` on `DashboardConsumerClient`, providing a proper `DisposeAsync()` that awaits the HTTP DELETE call. The synchronous `Dispose()` is kept as a fallback but changed to fire-and-forget the HTTP call (rather than blocking), with a code comment documenting the rationale. The Dashboard.Client project targets only net10.0 and net8.0, so `IAsyncDisposable` is available without conditional compilation.

**Key Files:**
- `Source/DotNetWorkQueue.Dashboard.Client/DashboardConsumerClient.cs` -- Implement `IAsyncDisposable`, add `DisposeAsync()`, refactor `Dispose()` to fire-and-forget
- `Source/DotNetWorkQueue.Dashboard.Client.Tests/DashboardConsumerClientTests.cs` -- Add tests for `DisposeAsync` behavior and verify `Dispose` no longer blocks

**Success Criteria:**
- [ ] `DashboardConsumerClient` implements both `IDisposable` and `IAsyncDisposable`
- [ ] `DisposeAsync()` properly awaits the HTTP DELETE call to unregister the consumer
- [ ] `DisposeAsync()` stops the heartbeat timer before attempting unregistration
- [ ] Synchronous `Dispose()` does not call `.GetAwaiter().GetResult()` (no sync-over-async)
- [ ] `Dispose()` uses fire-and-forget with a comment documenting why
- [ ] Both `Dispose()` and `DisposeAsync()` are idempotent (safe to call multiple times)
- [ ] Existing tests pass: `dotnet test "Source\DotNetWorkQueue.Dashboard.Client.Tests\DotNetWorkQueue.Dashboard.Client.Tests.csproj"`
- [ ] New tests cover: `DisposeAsync` awaits cleanup, double-dispose is safe, `Dispose` does not deadlock

---

### Phase 4: Stale Project Cleanup (IntegrationTests.Metrics)

**Complexity:** Small
**Dependencies:** None (parallel with all other phases, but safest to do after Phase 2 since it touches the integration test infrastructure)
**Items:** H-7

**Description:**
The `DotNetWorkQueue.IntegrationTests.Metrics` project targets only net48 and provides stub `IMetrics` implementations (Counter, Meter, Timer, etc.) for integration tests. It has no test runner and is a remnant from the App.Metrics migration. However, it is **not dead** -- it is actively referenced by `DotNetWorkQueue.IntegrationTests.Shared` (ProjectReference in its .csproj) and used in `ProducerMethodMultipleDynamicShared.cs` (under `#if NETFULL`).

Removal requires:
1. Verifying that the types from `IntegrationTests.Metrics` used in `IntegrationTests.Shared` under `#if NETFULL` can be replaced with the existing `Metrics.NoOp` types from the core library (which already provides `CounterNoOp`, `MeterNoOp`, `TimerNoOp`, etc.), or inlined.
2. Removing the ProjectReference from `IntegrationTests.Shared.csproj`.
3. Removing the `using DotNetWorkQueue.IntegrationTests.Metrics` and replacing it with the alternative.
4. Removing the `InternalsVisibleTo("DotNetWorkQueue.IntegrationTests.Metrics")` from the core project.
5. Removing the project entry from `DotNetWorkQueue.sln`.
6. Deleting the `DotNetWorkQueue.IntegrationTests.Metrics/` directory.

**Key Files:**
- `Source/DotNetWorkQueue.IntegrationTests.Metrics/` -- Entire directory to be removed
- `Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj` -- Remove ProjectReference (line 37)
- `Source/DotNetWorkQueue.IntegrationTests.Shared/ProducerMethod/ProducerMethodMultipleDynamicShared.cs` -- Replace `using DotNetWorkQueue.IntegrationTests.Metrics` with alternative (line 9, under `#if NETFULL`)
- `Source/DotNetWorkQueue/InternalsVisibleForTests.cs` -- Remove `InternalsVisibleTo("DotNetWorkQueue.IntegrationTests.Metrics")` (line 25)
- `Source/DotNetWorkQueue.sln` -- Remove project entry for `{B7974956-3764-4B0C-B6F2-0B8F8A25BEFE}`

**Success Criteria:**
- [ ] `DotNetWorkQueue.IntegrationTests.Metrics` directory no longer exists
- [ ] Project is removed from `DotNetWorkQueue.sln`
- [ ] No `.csproj` file references `IntegrationTests.Metrics`
- [ ] No `.cs` file contains `using DotNetWorkQueue.IntegrationTests.Metrics`
- [ ] `InternalsVisibleTo("DotNetWorkQueue.IntegrationTests.Metrics")` is removed
- [ ] Full solution builds without errors: `dotnet build "Source\DotNetWorkQueue.sln" -c Debug`
- [ ] Core unit tests pass: `dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj"`
- [ ] In-memory integration tests pass: `dotnet test "Source\DotNetWorkQueue.Transport.Memory.Integration.Tests\DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj"`

---

### Phase 5: Security Documentation (Dynamic LINQ)

**Complexity:** Small
**Dependencies:** Phase 1 (references the binder work as a mitigation for serialization risks)
**Items:** C-2

**Description:**
The Dynamic LINQ compilation feature (`DynamicCodeCompiler`, `LinqCompiler`) compiles arbitrary code strings into executable delegates with no sandboxing. This is a documented feature with inherent trade-offs -- disabling it would break a core use case. The correct mitigation is clear documentation.

This phase creates a security considerations document in the repository that covers:
- The Dynamic LINQ compilation threat model (arbitrary code execution if attacker can enqueue LINQ expression messages)
- The serialization binder protections added in Phase 1
- Recommended deployment patterns (trusted networks, network-level ACLs on queue backends)
- Guidance on disabling Dynamic LINQ compilation if not needed
- Queue backend access control best practices

**Key Files:**
- `Source/DotNetWorkQueue/SECURITY.md` -- New file (placed alongside the core library for visibility, referenced from root README)
- `Source/DotNetWorkQueue/LinqCompile/DynamicCodeCompiler.cs` -- Reference only (for documentation accuracy)
- `Source/DotNetWorkQueue/LinqCompile/LinqCompiler.cs` -- Reference only
- `Lib/JpLabs.DynamicCode/` -- Reference only (net48-only vendored binary, important context for documentation)

**Success Criteria:**
- [ ] `SECURITY.md` exists and covers: Dynamic LINQ risks, serialization binder protections, deployment recommendations, queue backend access control
- [ ] Document references the specific classes and features involved (`DynamicCodeCompiler`, `LinqCompiler`, `DenyListSerializationBinder`)
- [ ] Document explains that Dynamic LINQ is net48-only (via `JpLabs.DynamicCode`)
- [ ] Document provides actionable guidance (not just "be careful")
- [ ] No code changes in this phase (documentation only)

---

## Phase Summary

| Phase | Title | Complexity | Dependencies | Items | Risk |
|-------|-------|------------|--------------|-------|------|
| 1 | Serialization Security (Binders) | Medium | None | C-1 | Critical -- RCE via deserialization |
| 2 | Queue Name Validation | Medium | None | H-2 | High -- SQL injection via table names |
| 3 | Async Dispose Fix | Small | None | H-5 | High -- deadlock in UI/ASP.NET contexts |
| 4 | Stale Project Cleanup | Small | None | H-7 | Low -- build/maintenance hygiene |
| 5 | Security Documentation | Small | Phase 1 | C-2 | Critical severity but documentation-only mitigation |

## Parallelism

Phases 1, 2, 3, and 4 can all execute in parallel -- they touch completely different areas of the codebase with no file overlap. Phase 5 depends on Phase 1 because it documents the binder protections introduced there.

```
Wave 1:  Phase 1 (Serialization) | Phase 2 (Queue Names) | Phase 3 (Async Dispose) | Phase 4 (Cleanup)
Wave 2:  Phase 5 (Documentation)
```

## Risk Assessment

- **Phase 1** carries the highest implementation risk because it modifies the serialization pipeline used by every message in every transport. The binder must not break existing polymorphic deserialization (`TypeNameHandling.Auto` relies on `$type` metadata for derived types). Thorough integration testing with the Memory transport is essential.
- **Phase 2** carries moderate risk because it adds validation to construction paths used by every transport. If the regex is too strict, it could break existing users whose queue names contain characters not anticipated. Scanning existing integration tests for queue name patterns before finalizing the regex is critical.
- **Phases 3, 4, 5** are low implementation risk. Phase 3 is a focused change to one class. Phase 4 requires careful dependency tracing (already done above) but is mechanically simple. Phase 5 is documentation only.
