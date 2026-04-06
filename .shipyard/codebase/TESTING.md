# Testing

## Overview
DotNetWorkQueue employs a two-tier testing strategy: unit tests that use AutoFixture with NSubstitute for isolated component testing, and integration tests that exercise full producer/consumer flows against actual or in-memory transports. The project has approximately 2,500 test methods across 720 test classes spanning 25 test projects. Tests use MSTest 3.x as the framework (migrated from xUnit), with FluentAssertions for expressive assertions in newer tests and MSTest `Assert` in the majority of existing tests.

## Findings

### Test Frameworks and Libraries

- **MSTest 4.1.0**: The primary test framework, used via `[TestClass]` and `[TestMethod]` attributes.
  - Evidence: `Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj` (lines 21-22: `MSTest.TestFramework` 4.1.0, `MSTest.TestAdapter` 4.1.0)
  - Evidence: `Source/DotNetWorkQueue.Tests/Queue/ConsumerQueueTests.cs` (line 12: `using Microsoft.VisualStudio.TestTools.UnitTesting;`)

- **Microsoft.NET.Test.Sdk 18.0.1**: Test host/runner.
  - Evidence: `Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj` (line 20)

- **AutoFixture 4.18.1 + AutoFixture.AutoNSubstitute 4.18.1**: Used for automatic test data generation and auto-mocking of constructor dependencies.
  - Evidence: `Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj` (lines 17-18)
  - Evidence: `Source/DotNetWorkQueue.Tests/Queue/ConsumerQueueTests.cs` (lines 4-5: `using AutoFixture; using AutoFixture.AutoNSubstitute;`)

- **NSubstitute 5.3.0**: Mocking library, used both through AutoFixture integration and directly.
  - Evidence: `Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj` (line 23)
  - Evidence: `Source/DotNetWorkQueue.Tests/Queue/ConsumerQueueTests.cs` (line 8: `using NSubstitute;`)

- **FluentAssertions 8.8.0**: Expressive assertion library, used in newer tests alongside MSTest Assert.
  - Evidence: `Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj` (line 19)
  - Evidence: `Source/DotNetWorkQueue.Tests/Queue/AddStandardMessageHeadersTests.cs` (line 24: `using FluentAssertions;`)
  - Used in at least 7 files in the core test project and extensively in integration test shared infrastructure.

- **CompareNETObjects 4.84.0**: Used in some transport-specific tests for deep object comparison.
  - Evidence: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj` (line 19)

- **Tynamix.ObjectFiller 1.5.9**: Used in transport and integration tests for generating complex test objects.
  - Evidence: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj` (line 25)
  - Evidence: `Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj` (line 29)

### Test Project Organization

- **25 test-related projects** organized into three categories:
  1. **Unit test projects** (suffix `.Tests`) -- test individual classes in isolation
  2. **Integration test projects** (suffix `.IntegrationTests` or `.Integration.Tests`) -- test full queue flows
  3. **Shared infrastructure projects** -- reusable test code

- **Unit test projects** (11 projects):
  - `DotNetWorkQueue.Tests` -- core library tests (840 test methods across 167 files)
  - `DotNetWorkQueue.Transport.SqlServer.Tests`
  - `DotNetWorkQueue.Transport.PostgreSQL.Tests`
  - `DotNetWorkQueue.Transport.Redis.Tests`
  - `DotNetWorkQueue.Transport.SQLite.Tests`
  - `DotNetWorkQueue.Transport.LiteDb.Tests`
  - `DotNetWorkQueue.Transport.RelationalDatabase.Tests`
  - `DotNetWorkQueue.Transport.Memory.Tests`
  - `DotNetWorkQueue.Dashboard.Api.Tests`
  - `DotNetWorkQueue.Dashboard.Client.Tests`
  - Evidence: CI workflow at `.github/workflows/ci.yml` (lines 28-56)

- **Integration test projects** (14 projects, 7 transport-specific + 7 LINQ variants + Dashboard):
  - `DotNetWorkQueue.Transport.Memory.Integration.Tests`
  - `DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests`
  - `DotNetWorkQueue.Transport.SqlServer.IntegrationTests`
  - `DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests`
  - `DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests`
  - `DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests`
  - `DotNetWorkQueue.Transport.Redis.IntegrationTests`
  - `DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests`
  - `DotNetWorkQueue.Transport.SQLite.Integration.Tests`
  - `DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests`
  - `DotNetWorkQueue.Transport.LiteDB.IntegrationTests`
  - `DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests`
  - `DotNetWorkQueue.Dashboard.Api.Integration.Tests`
  - `DotNetWorkQueue.IntegrationTests.Metrics`

- **Shared test infrastructure** (2 projects):
  - `DotNetWorkQueue.IntegrationTests.Shared` -- reusable integration test implementations
  - `DotNetWorkQueue.IntegrationTests.Metrics` -- metrics collection for integration tests
  - Evidence: `Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj`

### Test File Organization and Naming

- **Mirror structure**: Unit test directories mirror the source project namespace structure.
  - `Source/DotNetWorkQueue.Tests/Queue/` mirrors `Source/DotNetWorkQueue/Queue/`
  - `Source/DotNetWorkQueue.Tests/Configuration/` mirrors `Source/DotNetWorkQueue/Configuration/`
  - `Source/DotNetWorkQueue.Tests/Exceptions/` mirrors `Source/DotNetWorkQueue/Exceptions/`
  - `Source/DotNetWorkQueue.Tests/Factory/` mirrors `Source/DotNetWorkQueue/Factory/`

- **Integration test directories organized by scenario**: Integration tests are organized by operation type rather than mirroring source structure.
  - `Producer/` -- send-side tests
  - `Consumer/` -- receive-side tests
  - `ConsumerAsync/` -- async consumer tests
  - `JobScheduler/` -- scheduled job tests
  - `Dashboard/` -- dashboard API tests
  - `History/` -- message history tests
  - `Cancellation/` -- cancellation flow tests
  - Evidence: `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/` directory listing

### Unit Test Patterns

- **AutoFixture + AutoNSubstitute for SUT creation**: The standard pattern creates a `Fixture` with `AutoNSubstituteCustomization`, optionally injects specific values, then creates the system under test.
  - Evidence: `Source/DotNetWorkQueue.Tests/Queue/ConsumerQueueTests.cs` (lines 109-121):
    ```csharp
    private ConsumerQueue CreateQueue()
    {
        var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        var cancelWork = fixture.Create<IQueueCancelWork>();
        fixture.Inject(cancelWork);
        cancelWork.CancellationTokenSource.Returns(new CancellationTokenSource());
        cancelWork.StopTokenSource.Returns(new CancellationTokenSource());
        var stopWorker = fixture.Create<StopWorker>();
        fixture.Inject(stopWorker);
        return fixture.Create<ConsumerQueue>();
    }
    ```
  - This pattern appears in nearly all unit test files as a private `CreateXxx()` factory method.

- **Fixture creation per test method**: Tests typically create a new `Fixture` in each test method or in a private helper. There is no shared fixture initialization.
  - Evidence: `Source/DotNetWorkQueue.Tests/Configuration/HeartBeatConfigurationTests.cs` (multiple methods each calling `GetConfiguration()` which creates a new fixture)

- **Direct construction for simple types**: When the system under test has no dependencies or simple ones, tests construct directly without AutoFixture.
  - Evidence: `Source/DotNetWorkQueue.Tests/Exceptions/DotNetWorkQueueExceptionTests.cs` (direct `new DotNetWorkQueueException(...)`)
  - Evidence: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerMessageQueueTransportOptionsTests.cs` (direct `new SqlServerMessageQueueTransportOptions()`)

- **Standard dispose test suite**: Almost every disposable class has a standard set of dispose-related tests:
  1. `IsDisposed_False_By_Default`
  2. `Disposed_Instance_Sets_IsDisposed`
  3. `Call_Dispose_Multiple_Times_Ok`
  4. `Disposed_Instance_Get_Configuration_Exception`
  5. `Disposed_Instance_Start_Exception`
  - Evidence: `Source/DotNetWorkQueue.Tests/Queue/ConsumerQueueTests.cs` (lines 20-76)
  - Evidence: `Source/DotNetWorkQueue.Tests/Queue/ProducerQueueTests.cs` (lines 18-43)

- **Configuration read-only test suite**: Configuration classes have tests for:
  1. `Test_DefaultNotReadOnly`
  2. `Set_Readonly`
  3. `Set_{Property}_WhenReadOnly_Fails` (one per settable property)
  - Evidence: `Source/DotNetWorkQueue.Tests/Configuration/HeartBeatConfigurationTests.cs` (lines 49-110)

- **Exception constructor tests**: Every custom exception has tests covering all constructor overloads.
  - Evidence: `Source/DotNetWorkQueue.Tests/Exceptions/DotNetWorkQueueExceptionTests.cs` (tests `Create_Empty`, `Create`, `Create_Format`, `Create_Inner`)

### Assertion Patterns

- **MSTest `Assert` (majority)**: Most existing tests use `Assert.AreEqual`, `Assert.IsTrue`, `Assert.IsFalse`, `Assert.IsNotNull`, `Assert.ThrowsExactly<T>`.
  - Evidence: `Source/DotNetWorkQueue.Tests/Queue/ConsumerQueueTests.cs` (lines 25, 34, 54, 71, 88, 101)
  - Evidence: `Source/DotNetWorkQueue.Tests/Configuration/HeartBeatConfigurationTests.cs` (lines 26, 36, 53, 59)

- **FluentAssertions `.Should()` (newer tests)**: Newer tests use FluentAssertions for more readable assertions.
  - Evidence: `Source/DotNetWorkQueue.Tests/Queue/AddStandardMessageHeadersTests.cs` (lines 40-42):
    ```csharp
    message.Headers.Should().ContainKey("Queue-MessageBodyType");
    stamped.Should().Be($"{typeof(SimpleTestBody).FullName}, ...");
    ```
  - Evidence: `Source/DotNetWorkQueue.Tests/Queue/MessageCancellationTrackerTests.cs` (lines 34, 46, 58, 67, 76-77, 88, 104-106)

- **Mixed assertion styles**: Both assertion styles coexist in the same project. There is no strict rule requiring one or the other.
  - [Inferred] Newer tests prefer FluentAssertions; older tests use MSTest Assert. No migration effort to unify is visible.

### Parameterized Tests

- **`[DataRow]` attribute**: Integration tests and some unit tests use MSTest parameterized tests with `[DataRow]`.
  - Evidence: `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/Producer/SimpleProducer.cs` (lines 13-14):
    ```csharp
    [DataRow(1000, true),
     DataRow(1000, false)]
    ```
  - Evidence: `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/Consumer/SimpleConsumer.cs` (lines 16-18):
    ```csharp
    [DataRow(1000, 0, 240, 5),
    DataRow(50, 5, 200, 10),
    DataRow(10, 15, 180, 7)]
    ```
  - DataRow is used primarily in integration tests; unit tests rarely use parameterization.

### Mocking Strategy

- **NSubstitute via AutoFixture**: The primary mocking strategy auto-generates substitutes for all interface dependencies via `AutoNSubstituteCustomization`.
  - Evidence: `Source/DotNetWorkQueue.Tests/Queue/ConsumerQueueTests.cs` (line 111: `new Fixture().Customize(new AutoNSubstituteCustomization())`)

- **Explicit `.Returns()` for specific behaviors**: When a test needs specific mock behavior, `NSubstitute.Returns` is used.
  - Evidence: `Source/DotNetWorkQueue.Tests/Queue/ConsumerQueueTests.cs` (lines 115-116):
    ```csharp
    cancelWork.CancellationTokenSource.Returns(new CancellationTokenSource());
    cancelWork.StopTokenSource.Returns(new CancellationTokenSource());
    ```
  - Evidence: `Source/DotNetWorkQueue.Tests/Configuration/HeartBeatConfigurationTests.cs` (line 66: `configuration.ThreadPoolConfiguration.Received(1).SetReadOnly()`)

- **`fixture.Inject()` for specific instances**: When a specific mock instance needs to be shared across the object graph, `Inject` is used.
  - Evidence: `Source/DotNetWorkQueue.Tests/Queue/ConsumerQueueTests.cs` (lines 114, 119)

- **Hand-rolled fakes for test data types**: `FakeMessage` and `FakeAMessageData` classes are manually implemented in test helpers files.
  - Evidence: `Source/DotNetWorkQueue.Tests/Helpers.cs` (lines 44-106: `FakeAMessageData` implements `IAdditionalMessageData`)
  - Evidence: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Helpers.cs` (lines 10-65: duplicate `FakeAMessageData`)
  - [Inferred] The `FakeAMessageData` class is duplicated across multiple test projects rather than being shared, because each test project has its own `Helpers.cs`.

### Integration Test Patterns

- **Shared implementation classes**: Integration test logic is centralized in `DotNetWorkQueue.IntegrationTests.Shared` and parameterized by transport type.
  - Evidence: `Source/DotNetWorkQueue.IntegrationTests.Shared/Producer/Implementation/SimpleProducer.cs` -- generic method `Run<TTransportInit, TMessage, TTransportCreate>(...)`
  - Transport-specific tests delegate to the shared implementation:
  - Evidence: `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/Producer/SimpleProducer.cs` (lines 22-26):
    ```csharp
    var producer = new DotNetWorkQueue.IntegrationTests.Shared.Producer.Implementation.SimpleProducer();
    producer.Run<MemoryMessageQueueInit, FakeMessage, MessageQueueCreation>(...)
    ```

- **Queue lifecycle pattern**: Integration tests follow create-test-cleanup:
  1. Create queue via `QueueCreationContainer<TTransportInit>`
  2. Produce and/or consume messages
  3. Verify results (record counts, error states)
  4. Clean up via `RemoveQueue()` + `Dispose()` in finally block
  - Evidence: `Source/DotNetWorkQueue.IntegrationTests.Shared/Producer/Implementation/SimpleProducer.cs` (lines 24-48)

- **Connection info abstraction**: Each transport provides a connection info class.
  - Memory transport: `IntegrationConnectionInfo` returns `"none"` -- Evidence: `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/IntegrationConnectionInfo.cs`
  - SQL Server: reads from `connectionstring.txt` file -- Evidence: `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/ConnectionString.cs` (line 20)

- **Queue name generation**: Integration tests generate unique queue names using MD5 hashing of GUIDs.
  - Evidence: `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/GenerateQueueName.cs` (lines 19-24)

- **Text file logging in integration tests**: Integration tests log to per-queue text files and verify error/no-error conditions.
  - Evidence: `Source/DotNetWorkQueue.IntegrationTests.Shared/LoggerShared.cs` (lines 12-65: `TextFileLogProvider`, `CheckForErrors`, `ShouldHaveErrors`)

### MSTest Migration Infrastructure

- **`AssemblyInitialize` for SynchronizationContext clearing**: Integration test assemblies clear the `SynchronizationContext` at assembly init to prevent deadlocks from MSTest's context (a compatibility measure from the xUnit migration).
  - Evidence: `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/AssemblyInit.cs` (lines 7-14)
  - Evidence: `Source/DotNetWorkQueue.IntegrationTests.Shared/MsTestHelper.cs` (lines 13-16):
    ```csharp
    public static void ClearSynchronizationContext()
    {
        SynchronizationContext.SetSynchronizationContext(null);
    }
    ```

### Test Target Framework

- **Unit tests target `net48` only**: Unit test projects target .NET Framework 4.8 exclusively.
  - Evidence: `Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj` (line 4: `<TargetFrameworks>net48;</TargetFrameworks>`)
  - Evidence: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj` (line 3: `<TargetFrameworks>net48</TargetFrameworks>`)
  - Evidence: `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj` (line 3: `<TargetFrameworks>net48</TargetFrameworks>`)

- **Dashboard API tests target `net10.0` and `net8.0`**: The newer Dashboard API test project targets modern frameworks.
  - Evidence: `Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj` (line 3: `<TargetFrameworks>net10.0;net8.0</TargetFrameworks>`)

### CI Test Execution

- **GitHub Actions** (`.github/workflows/ci.yml`): Runs on `windows-latest` with .NET 8.0 and 10.0 SDKs. Executes:
  1. All unit test projects (11 projects -- lines 28-56)
  2. Memory integration tests (2 projects -- lines 58-62)
  3. Dashboard API integration tests filtered to Memory/SQLite/LiteDb only (line 64-65)
  - Evidence: `.github/workflows/ci.yml` (full file, 66 lines)
  - No external services (SQL Server, PostgreSQL, Redis) are required in CI.

- **TeamCity** (local CI): Runs the full test suite including all transport integration tests that require running database/service instances.
  - Evidence: Referenced in `CLAUDE.md` and repository memory but no TeamCity configuration files are checked into the repository.
  - [Inferred] TeamCity configuration is managed externally.

### What Is Tested

- **Core library (`DotNetWorkQueue.Tests`)**: 840 test methods covering:
  - Queue classes (consumer, producer, heartbeat, workers)
  - Configuration classes (read-only behavior, property get/set)
  - Factory classes
  - Exception constructors
  - Message handling and serialization
  - Metrics (NoOp and Net implementations)
  - Task scheduling
  - Tracing extensions
  - IoC container behavior
  - Notification classes
  - History decorators
  - Message cancellation
  - Evidence: Grep results showing 167 test files in `Source/DotNetWorkQueue.Tests/`

- **Transport-specific tests**: Each transport has a dedicated test project testing:
  - Transport options (enable/disable features, read-only behavior)
  - Schema/table generation
  - Queue creation
  - Connection information
  - Command/query classes
  - Policy creation (retry policies)
  - Evidence: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/` with tests in `Basic/`, `Schema/` subdirectories

- **Integration tests**: Full end-to-end flows:
  - Simple producer (single/batch messages)
  - Simple consumer (various worker counts, runtimes, timeouts)
  - Async consumer
  - Error handling (poison messages, error tables, rollback)
  - Heartbeat monitoring
  - Job scheduling
  - Route-based consumption
  - Multi-producer/multi-consumer scenarios
  - Dashboard API queries
  - Message history
  - Message cancellation
  - Evidence: `Source/DotNetWorkQueue.IntegrationTests.Shared/` directory listing showing Implementation subdirectories for each scenario

### What Is Not Tested (Gaps)

- **No visible code coverage configuration**: No `.coverlet`, `coverlet.runsettings`, or other coverage tool configuration files were found in the repository.
  - [Inferred] Coverage is tracked externally (the MEMORY.md mentions a goal of 84.5% to 90%+, and `codcov*.txt` files exist in the root, suggesting coverage reports are generated but the tooling configuration is not checked in).

- **No load/performance tests**: There are no visible performance benchmarks or load tests.
  - [Inferred] Integration tests with configurable message counts (1000, 50, 10) serve as basic throughput verification but are not formal performance tests.

- **Limited negative/edge-case testing in transports**: Transport unit tests primarily cover happy-path configuration and schema generation. Error-path testing is mainly handled by integration tests.
  - Evidence: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerMessageQueueTransportOptionsTests.cs` -- all tests are get/set/readonly verification, no error scenario tests.

### Test Data Generation

- **AutoFixture for random values**: Primitive values and simple objects are generated via `fixture.Create<T>()`.
  - Evidence: `Source/DotNetWorkQueue.Tests/Configuration/HeartBeatConfigurationTests.cs` (line 22: `var value = fixture.Create<int>();`, line 32: `fixture.Create<string>()`)

- **`Helpers.RandomStrings()` for string collections**: A custom helper generates random strings with specific length constraints.
  - Evidence: `Source/DotNetWorkQueue.Tests/Helpers.cs` (lines 22-41)

- **Transport-specific data generation**: Integration tests use `Helpers.GenerateData` methods (one per transport) to create appropriate `AdditionalMessageData`.
  - Evidence: `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/SharedClasses.cs` (lines 31-34: `Helpers.GenerateData` returns null for memory transport)

## Summary Table

| Item | Detail | Confidence |
|---|---|---|
| Test framework | MSTest 4.1.0 (migrated from xUnit) | Observed |
| Mocking library | NSubstitute 5.3.0 via AutoFixture.AutoNSubstitute 4.18.1 | Observed |
| Assertion libraries | MSTest Assert (majority) + FluentAssertions 8.8.0 (newer tests) | Observed |
| Total test methods | ~2,500 across 720 test classes | Observed |
| Core unit tests | 840 methods in 167 files | Observed |
| Unit test projects | 11 projects | Observed |
| Integration test projects | 14 projects (7 transports + 7 LINQ + Dashboard) | Observed |
| Test target framework | Unit tests: net48; Dashboard tests: net10.0/net8.0 | Observed |
| CI (GitHub Actions) | Unit tests + Memory integration + Dashboard (Memory/SQLite/LiteDb) | Observed |
| CI (TeamCity) | Full suite including all transport integrations | Inferred |
| Code coverage tool | Not configured in repository | Inferred |
| Coverage target | 84.5% baseline, 90%+ goal | Inferred (from MEMORY.md) |
| Parameterized tests | `[DataRow]` in integration tests | Observed |
| SynchronizationContext fix | `MsTestHelper.ClearSynchronizationContext()` in `[AssemblyInitialize]` | Observed |
| Connection strings | `connectionstring.txt` files per transport integration project | Observed |
| Shared test infra | `IntegrationTests.Shared` with generic `Run<TTransportInit,...>()` | Observed |

## Open Questions

- Why do unit test projects target only `net48` when the library supports `net8.0`, `net10.0`, and `netstandard2.0`? Multi-targeting tests would catch framework-specific issues.
- Is there a plan to unify assertion styles (MSTest Assert vs FluentAssertions) or will both coexist indefinitely?
- The `FakeAMessageData` class is duplicated in `DotNetWorkQueue.Tests/Helpers.cs` and `DotNetWorkQueue.Transport.SqlServer.Tests/Helpers.cs` (and likely other transport test projects). Could this be centralized?
- Code coverage tooling configuration is not checked into the repository. Where is it defined -- TeamCity? A local script?
- Some integration test project naming is inconsistent: some use `.IntegrationTests` (e.g., `Transport.SqlServer.IntegrationTests`) and others use `.Integration.Tests` (e.g., `Transport.Memory.Integration.Tests`). Is there a preferred convention?
