---
phase: multi-source-integration-tests
plan: "1.2"
wave: 1
dependencies: []
must_haves:
  - Multi-source integration tests using 2+ DashboardTestServer instances
  - Test grouped listing returns connections from all sources
  - Test source isolation (connections don't leak between sources)
  - Test partial failure (one source offline, other unaffected)
  - All existing 38+ Dashboard API integration tests pass unchanged
files_touched:
  - Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/Tests/MultiSourceTests.cs
tdd: true
risk: low
---

# Plan 1.2: Multi-Source Integration Tests

## Context

Phase 3 requires integration tests verifying that multiple independent DashboardTestServer instances can serve as separate API sources. These tests operate at the API client level — they create 2 `DashboardTestServer` instances (each with Memory transport), call endpoints via `HttpClient`, and verify source isolation, connection attribution, and partial failure behavior. They do NOT depend on the Blazor UI changes in PLAN-1.1 — they test the API layer that the UI consumes.

The existing test infrastructure (`DashboardTestServer.CreateAsync`, `TransportFixture`, `QueueNameGenerator`, `ConnectionStrings.Memory`, `FakeMessage`) provides everything needed. Each test creates its own server instances with unique queue names to avoid state leakage.

## Dependencies

Depends on Phase 2 completion (DashboardTestServer and API infrastructure). Does NOT depend on PLAN-1.1 (no shared files).

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/Tests/MultiSourceTests.cs" tdd="true">
  <action>
  Create `Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/Tests/MultiSourceTests.cs` with LGPL-2.1 license header.

  Namespace: `DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests`
  Using: `System`, `System.Collections.Generic`, `System.Net`, `System.Net.Http.Json`, `System.Threading.Tasks`, `DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers`, `DotNetWorkQueue.Dashboard.Api.Models`, `DotNetWorkQueue.Transport.Memory`, `DotNetWorkQueue.Transport.Memory.Basic`, `FluentAssertions`, `Microsoft.VisualStudio.TestTools.UnitTesting`

  Class `MultiSourceTests` with `[TestClass]` attribute.

  **Setup** (`[TestInitialize]`):
  - Create 2 unique queue names via `QueueNameGenerator.Create()`
  - Create 2 `TransportFixture<MemoryDashboardInit, MessageQueueCreation>` instances, each with `ConnectionStrings.Memory` and their respective queue name
  - Send 3 messages via fixture1 and 2 messages via fixture2 (use `SendMessages<FakeMessage>(count)`)
  - Create 2 `DashboardTestServer` instances, each configured with `options.EnableSwagger = false` and `options.AddConnection<MemoryDashboardInit>` for their respective fixture's connection string, scope, and queue name

  **Teardown** (`[TestCleanup]`):
  - Dispose both servers and both fixtures (null-safe)

  **Fields**:
  - `DashboardTestServer _server1, _server2`
  - `TransportFixture<MemoryDashboardInit, MessageQueueCreation> _fixture1, _fixture2`
  - `string _queueName1, _queueName2`

  **Test methods**:

  1. `[TestMethod] async Task Source1_ReturnsOwnConnections()`:
     - GET `api/v1/dashboard/connections` from `_server1.Client`
     - Assert: returns exactly 1 connection with QueueCount == 1

  2. `[TestMethod] async Task Source2_ReturnsOwnConnections()`:
     - GET `api/v1/dashboard/connections` from `_server2.Client`
     - Assert: returns exactly 1 connection with QueueCount == 1

  3. `[TestMethod] async Task Sources_HaveIndependentConnections()`:
     - GET connections from both servers
     - Assert: connection IDs from server1 and server2 are different (no overlap)
     - Assert: each server returns exactly 1 connection

  4. `[TestMethod] async Task Source1_Messages_MatchSendCount()`:
     - GET connections from server1, get the queueId, GET messages
     - Assert: 3 messages (the count sent to fixture1)

  5. `[TestMethod] async Task Source2_Messages_MatchSendCount()`:
     - GET connections from server2, get the queueId, GET messages
     - Assert: 2 messages (the count sent to fixture2)

  6. `[TestMethod] async Task WriteOperation_RoutesToCorrectSource()`:
     - GET connections from server1, discover queueId and a messageId
     - DELETE that message via server1's client
     - Assert: server1 message count is now 2
     - Assert: server2 message count is still 2 (unaffected)

  7. `[TestMethod] async Task Health_BothServersRespond()`:
     - GET `api/v1/dashboard/health` from both servers
     - Assert: both return `HttpStatusCode.OK`
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" --filter "FullyQualifiedName~MultiSourceTests" --verbosity normal 2>&1 | tail -15</verify>
  <done>All 7 MultiSourceTests pass. Each test verifies source isolation, independent message counts, write routing, and health endpoints across 2 independent DashboardTestServer instances.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/Tests/MultiSourcePartialFailureTests.cs" tdd="true">
  <action>
  Create `Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/Tests/MultiSourcePartialFailureTests.cs` with LGPL-2.1 license header.

  Namespace: `DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests`
  Same usings as MultiSourceTests.

  Class `MultiSourcePartialFailureTests` with `[TestClass]` attribute.

  **Setup** (`[TestInitialize]`):
  - Create 2 `TransportFixture` instances with unique queue names, send 3 messages to each
  - Create 2 `DashboardTestServer` instances

  **Teardown** (`[TestCleanup]`):
  - Dispose both servers and fixtures (null-safe, catch exceptions during cleanup)

  **Fields**: same pattern as MultiSourceTests

  **Test methods**:

  1. `[TestMethod] async Task AfterDispose_DisposedServerReturnsError()`:
     - Verify server2 is healthy: GET connections returns OK
     - Dispose server2 via `await _server2.DisposeAsync()`
     - Attempt GET `api/v1/dashboard/connections` from server2's client
     - Assert: throws `HttpRequestException` or `ObjectDisposedException` (wrap in try/catch, assert exception was thrown)
     - Set `_server2 = null` to prevent double-dispose in cleanup

  2. `[TestMethod] async Task AfterDispose_OtherServerStillWorks()`:
     - Dispose server2 via `await _server2.DisposeAsync()`, set `_server2 = null`
     - GET `api/v1/dashboard/connections` from server1's client
     - Assert: returns 1 connection, server1 is unaffected

  3. `[TestMethod] async Task AfterDispose_OtherServerMessagesIntact()`:
     - Dispose server2 via `await _server2.DisposeAsync()`, set `_server2 = null`
     - GET connections from server1, discover queueId, GET messages
     - Assert: 3 messages still present on server1

  4. `[TestMethod] async Task AfterDispose_OtherServerHealthStillOk()`:
     - Dispose server2 via `await _server2.DisposeAsync()`, set `_server2 = null`
     - GET `api/v1/dashboard/health` from server1
     - Assert: returns `HttpStatusCode.OK`
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" --filter "FullyQualifiedName~MultiSourcePartialFailureTests" --verbosity normal 2>&1 | tail -15</verify>
  <done>All 4 MultiSourcePartialFailureTests pass. Tests verify that disposing one server does not affect the other server's ability to serve connections, messages, and health checks.</done>
</task>

<task id="3" files="" tdd="false">
  <action>
  Run the full existing Dashboard API integration test suite (Memory-only filter to avoid external service dependencies) to verify no regressions from the new test files.

  Run: `dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" --filter "FullyQualifiedName~Memory|FullyQualifiedName~MultiSource|FullyQualifiedName~Health|FullyQualifiedName~Empty|FullyQualifiedName~Edge|FullyQualifiedName~Assembly|FullyQualifiedName~Consumers"`

  This covers all tests that run without external services:
  - MemoryEndpointTests (20 tests)
  - MemoryStatefulTests
  - MemoryHistoryTests
  - MultiQueueTests
  - EmptyQueueTests
  - EdgeCaseTests
  - HealthEndpointTests
  - ConsumersEndpointTests
  - AssemblyPathTests
  - NEW: MultiSourceTests (7 tests)
  - NEW: MultiSourcePartialFailureTests (4 tests)

  All tests must pass.
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" --filter "FullyQualifiedName~Memory|FullyQualifiedName~MultiSource|FullyQualifiedName~Health|FullyQualifiedName~Empty|FullyQualifiedName~Edge|FullyQualifiedName~Assembly|FullyQualifiedName~Consumers" --verbosity quiet 2>&1 | tail -5</verify>
  <done>All existing Memory-compatible integration tests pass alongside the 11 new multi-source tests. Zero regressions.</done>
</task>

## Verification

```bash
# Run just the new multi-source tests
dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" --filter "FullyQualifiedName~MultiSource" --verbosity normal

# Run all Memory-compatible integration tests (no external services needed)
dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" --filter "FullyQualifiedName~Memory|FullyQualifiedName~MultiSource|FullyQualifiedName~Health|FullyQualifiedName~Empty|FullyQualifiedName~Edge|FullyQualifiedName~Assembly|FullyQualifiedName~Consumers"

# Full solution build
dotnet build "Source/DotNetWorkQueue.sln" -c Debug
```
