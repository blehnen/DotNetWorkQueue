# STACK.md -- Technology Stack Inventory

## Overview

DotNetWorkQueue is a C# producer/distributed consumer library built on .NET SDK-style projects with MSBuild. It multi-targets four frameworks (net10.0, net8.0, net48, netstandard2.0), uses SimpleInjector for dependency injection, and ships 12 NuGet packages from a single solution. The test suite runs on MSTest 3.x with NSubstitute, AutoFixture, and FluentAssertions, orchestrated by TeamCity locally and GitHub Actions in the cloud.

## Findings

### Language and Runtime

- **Language**: C# (no explicit `LangVersion` in most projects; `LangVersion=latest` set in `DotNetWorkQueue.Dashboard.Client`)
  - Evidence: `Source/DotNetWorkQueue.Dashboard.Client/DotNetWorkQueue.Dashboard.Client.csproj` (line 16)
  - All other projects rely on the SDK default C# version for their target framework.
- **Target Frameworks**: `net10.0;net8.0;net48;netstandard2.0` for all core and transport libraries
  - Evidence: `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (line 4)
  - Evidence: `Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj` (line 4)
  - Dashboard projects (Api, Client, Ui) target `net10.0;net8.0` only (no net48/netstandard2.0)
    - Evidence: `Source/DotNetWorkQueue.Dashboard.Api/DotNetWorkQueue.Dashboard.Api.csproj` (line 4)
- **Test project frameworks**: All unit and integration test projects target `net48` only
  - Evidence: `Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj` (line 4)
  - Exception: Dashboard test projects target `net10.0;net8.0`
    - Evidence: `Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj` (line 4)

### Conditional Compilation Symbols

- **`NETFULL`**: Defined for `net48` targets across all projects. Used for .NET Framework-specific code paths (thread abort support, dynamic LINQ).
  - Evidence: `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (lines 36-37)
- **`NETSTANDARD2_0`**: Defined for `netstandard2.0` targets.
  - Evidence: `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (lines 24-25)
- **`LIBLOG_PUBLIC` / `LIBLOG_PORTABLE`**: Defined on the core project for LibLog compatibility (netstandard2.0, net8.0, net10.0).
  - Evidence: `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (lines 25, 29, 33)
- **`CODE_ANALYSIS`**: Enabled in Debug builds across all projects.
  - Evidence: `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (line 25)

### Build System

- **SDK**: `Microsoft.NET.Sdk` for library projects; `Microsoft.NET.Sdk.Web` for the Dashboard UI
  - Evidence: `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (line 1)
  - Evidence: `Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj` (line 1)
- **Solution files**:
  - `Source/DotNetWorkQueue.sln` -- Full solution including all tests (37 projects)
  - `Source/DotNetWorkQueueNoTests.sln` -- Library projects only (no test projects)
- **No `Directory.Build.props`**: Each `.csproj` defines its own target frameworks, conditional compilation, and package metadata independently. There is no centralized build configuration.
- **Release builds**: Enable `TreatWarningsAsErrors` and XML documentation generation
  - Evidence: `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (lines 42-43, 49-50, 57-58)
- **NuGet packaging**: `GeneratePackageOnBuild=true` on all library projects
  - Evidence: `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (line 21)
  - Vendored DLLs are packed into TFM-specific lib folders via a custom MSBuild target `IncludeVendoredDllsInPack`
    - Evidence: `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (lines 128-142)
- **Package output**: `Deploy/` directory contains pre-built `.nupkg` files for all 12 packages at version 0.9.10
  - Evidence: `Deploy/DotNetWorkQueue.0.9.10.nupkg` (and 11 others)
- **Current version**: `0.9.10` across all projects
  - Evidence: `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (line 7)
- **License**: LGPL-2.1-or-later, with license headers in all source files
  - Evidence: `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (line 13)

### Core NuGet Dependencies

| Package | Version | Project(s) | Purpose |
|---------|---------|------------|---------|
| SimpleInjector | 5.5.0 | Core | DI container |
| Polly.Core | 8.6.5 | Core, PostgreSQL | Resilience/retry pipelines (V8 API) |
| Newtonsoft.Json | 13.0.4 | Core | Serialization |
| OpenTelemetry | 1.14.0 | Core | Distributed tracing |
| System.Diagnostics.DiagnosticSource | 10.0.1 | Core | Built-in metrics via `System.Diagnostics.Metrics` |
| Microsoft.IO.RecyclableMemoryStream | 3.0.1 | Core, Redis | Memory-efficient stream pooling |
| Microsoft.Extensions.Caching.Memory | 9.0.3 | Core | In-memory caching |
| GuerrillaNtp | 3.1.0 | Core | NTP time synchronization |
| Microsoft.CSharp | 4.7.0 | Core | Dynamic/runtime C# support |

- Evidence: `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (lines 69-78)

### Transport-Specific NuGet Dependencies

| Package | Version | Transport | Purpose |
|---------|---------|-----------|---------|
| Microsoft.Data.SqlClient | 6.1.3 | SqlServer | SQL Server ADO.NET provider |
| Npgsql | 8.0.8 | PostgreSQL | PostgreSQL ADO.NET provider |
| StackExchange.Redis | 2.10.1 | Redis | Redis client |
| MsgPack.Cli | 1.0.1 | Redis | MessagePack serialization |
| System.Data.SQLite.Core | 1.0.119 | SQLite | SQLite ADO.NET provider |
| LiteDB | 5.0.21 | LiteDB | Embedded NoSQL database |

- Evidence: `Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj` (line 64)
- Evidence: `Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj` (line 77)
- Evidence: `Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj` (lines 75-77)
- Evidence: `Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj` (line 76)
- Evidence: `Source/DotNetWorkQueue.Transport.LiteDB/DotNetWorkQueue.Transport.LiteDb.csproj` (line 76)

### Dashboard NuGet Dependencies

| Package | Version | Project | Purpose |
|---------|---------|---------|---------|
| Swashbuckle.AspNetCore | 7.2.0 | Dashboard.Api | Swagger/OpenAPI generation |
| MudBlazor | 9.1.0 | Dashboard.Ui | Blazor component library |
| Microsoft.Extensions.Http | 9.0.3 | Dashboard.Client | `IHttpClientFactory` support |
| Microsoft.AspNetCore.TestHost | 8.0.13 / 10.0.3 | Dashboard.Api.Integration.Tests | In-process ASP.NET test server |

- Evidence: `Source/DotNetWorkQueue.Dashboard.Api/DotNetWorkQueue.Dashboard.Api.csproj` (line 41)
- Evidence: `Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj` (line 20)
- Evidence: `Source/DotNetWorkQueue.Dashboard.Client/DotNetWorkQueue.Dashboard.Client.csproj` (line 41)
- Evidence: `Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj` (lines 39-40)

### Vendored Libraries (Lib/)

Three custom/forked libraries are checked in as pre-compiled DLLs, each with per-TFM binaries:

| Library | TFMs Available | Purpose | Source |
|---------|---------------|---------|--------|
| Schyntax | net10.0, net8.0, net48, netstandard2.0 | Cron-like schedule syntax for recurring jobs | https://github.com/blehnen/cs-schyntax |
| Aq.ExpressionJsonSerializer | net10.0, net8.0, net48, netstandard2.0 | LINQ expression tree JSON serialization | https://github.com/blehnen/expression-json-serializer |
| JpLabs.DynamicCode | net48 only (single DLL) | Dynamic lambda expression compilation | http://jp-labs.blogspot.com/2008/11/dynamic-lambda-expressions-using.html |

- Evidence: `Lib/Schyntax/README.md`, `Lib/Aq.ExpressionJsonSerializer/README.md`, `Lib/JpLabs.DynamicCode/README.md`
- Evidence: `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (lines 81-126) -- per-TFM HintPath references
- JpLabs.DynamicCode is only referenced unconditionally (all TFMs), but only ships as a net48 DLL in the NuGet package (line 138). [Inferred] It is likely only functional on net48.

### Test Framework and Tooling

| Package | Version | Purpose |
|---------|---------|---------|
| MSTest.TestFramework | 4.1.0 | Test framework (attributes, assertions) |
| MSTest.TestAdapter | 4.1.0 | Test discovery and execution |
| Microsoft.NET.Test.Sdk | 18.0.1 | Test host/runner infrastructure |
| NSubstitute | 5.3.0 | Mocking framework |
| AutoFixture | 4.18.1 | Test data generation |
| AutoFixture.AutoNSubstitute | 4.18.1 | AutoFixture + NSubstitute integration |
| FluentAssertions | 8.8.0 | Fluent assertion library |
| Tynamix.ObjectFiller | 1.5.9 | Object generation (integration tests) |
| CompareNETObjects | 4.84.0 | Deep object comparison (SqlServer, PostgreSQL, SQLite, LiteDb tests) |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.14.0 | OTLP exporter (integration test metrics) |

- Evidence: `Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj` (lines 17-24)
- Evidence: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj` (lines 17-25)
- Evidence: `Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj` (lines 23-29)
- **Residual xUnit reference**: `DotNetWorkQueue.Transport.Memory.Tests` still has a `xunit.runner.visualstudio` package reference, likely a leftover from the xUnit-to-MSTest migration.
  - Evidence: `Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj` (line 24)

### Test Project Structure

| Category | Count | Target Framework | Notes |
|----------|-------|-----------------|-------|
| Unit test projects | 10 | net48 (transport); net10.0+net8.0 (dashboard) | No external dependencies needed |
| Integration test projects (in-memory) | 3 | net48 | Memory transport, Memory Linq, Dashboard API (filtered) |
| Integration test projects (external services) | 8 | net48 | Require running DB/cache instances |
| Shared test infrastructure | 2 | net48 | IntegrationTests.Shared, IntegrationTests.Metrics |

### CI/CD Configuration

- **GitHub Actions**: `.github/workflows/ci.yml`
  - Runs on `windows-latest`
  - .NET SDK versions: `8.0.x` and `10.0.100`
  - Triggers: push to `master` or `*.*.x` branches, PRs to `master`
  - Scope: Build full solution, run all 10 unit test projects, 2 in-memory integration test projects, and Dashboard API integration tests (filtered to Memory/SQLite/LiteDb only)
  - Evidence: `.github/workflows/ci.yml` (lines 1-66)
- **TeamCity**: Local CI server that runs the full test suite including integration tests against real SQL Server, PostgreSQL, and Redis instances
  - [Inferred] Based on `CLAUDE.md` documentation and the presence of `TeamCity_DotNetWorkQueueGitCore_20260324_130127.zip` in the repo root
- **Code Coverage**: XML report at `Coverage/CoverageReport.xml`
  - Evidence: `Coverage/CoverageReport.xml`

### Dashboard UI Technology

- **Blazor Server** with MudBlazor 9.1.0 component library
  - Evidence: `Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj` (lines 1, 20)
- **Nullable reference types** and **implicit usings** enabled (only in Dashboard.Ui)
  - Evidence: `Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj` (lines 5-6)

## Summary Table

| Item | Detail | Confidence |
|------|--------|------------|
| Primary language | C# | Observed |
| Target frameworks (libraries) | net10.0, net8.0, net48, netstandard2.0 | Observed |
| Target frameworks (dashboard) | net10.0, net8.0 | Observed |
| Target frameworks (tests) | net48 (transport tests); net10.0, net8.0 (dashboard tests) | Observed |
| Current version | 0.9.10 | Observed |
| Build system | MSBuild / .NET SDK | Observed |
| DI container | SimpleInjector 5.5.0 | Observed |
| Serialization | Newtonsoft.Json 13.0.4 | Observed |
| Resilience | Polly.Core 8.6.5 (V8 API) | Observed |
| Tracing | OpenTelemetry 1.14.0 | Observed |
| Metrics | System.Diagnostics.Metrics (built-in) | Observed |
| Test framework | MSTest 4.1.0 | Observed |
| Mocking | NSubstitute 5.3.0 | Observed |
| CI (cloud) | GitHub Actions, windows-latest | Observed |
| CI (local) | TeamCity | Inferred |
| Package count | 12 NuGet packages | Observed |
| Vendored libraries | 3 (Schyntax, Aq.ExpressionJsonSerializer, JpLabs.DynamicCode) | Observed |
| Centralized build props | None (no Directory.Build.props) | Observed |
| Dashboard UI | Blazor Server + MudBlazor 9.1.0 | Observed |

## Open Questions

- What .NET SDK version is required to build the full solution locally? The CI uses `10.0.100` but there is no `global.json` currently tracked (it is listed as deleted in git status).
- Are the vendored Schyntax and Aq.ExpressionJsonSerializer libraries built from specific tagged versions of their forks, or from HEAD? There is no version information in the `Lib/` directory.
- The `xunit.runner.visualstudio` reference in `DotNetWorkQueue.Transport.Memory.Tests` -- is this intentional (some tests still use xUnit) or a migration leftover?
- JpLabs.DynamicCode ships as a single DLL (not per-TFM). Is it functional on non-net48 targets, or is it dead code on those TFMs?
