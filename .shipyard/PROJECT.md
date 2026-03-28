# Project: Security & Stability Fixes

## Description

Address critical and high-severity concerns identified during codebase analysis. This project focuses on hardening the serialization layer against deserialization attacks, preventing SQL injection through queue name validation, fixing async disposal patterns, removing stale project artifacts, and documenting security considerations for the dynamic LINQ compilation feature.

The goal is to make the library "safe by default" without breaking existing users, while providing clear documentation for features with inherent security trade-offs.

## Goals

1. Protect consumers from Newtonsoft.Json deserialization attacks by shipping a deny-list `ISerializationBinder` as the default, with an optional allow-list binder for maximum security
2. Prevent SQL injection via queue names by adding per-transport validation in each transport's connection info classes
3. Fix the sync-over-async anti-pattern in `DashboardConsumerClient.Dispose()` by implementing `IAsyncDisposable`
4. Remove the stale `IntegrationTests.Metrics` project (net48-only, no test runner, artifact from App.Metrics migration)
5. Document Dynamic LINQ compilation security risks in a dedicated wiki security page

## Non-Goals

- Changing `TypeNameHandling.Auto` to `TypeNameHandling.None` (would break polymorphic message deserialization)
- Sandboxing or restricting Dynamic LINQ compilation at runtime (documented feature with known trade-offs)
- Addressing Medium/Low severity concerns (deferred to future project after reviewing results)
- Centralized package management (H-6) — deferred
- Dashboard API security hardening (H-3, H-4) — deferred
- Enabling nullable reference types — deferred

## Requirements

### Serialization Security (C-1)
- Create a `DenyListSerializationBinder` that blocks known Newtonsoft.Json gadget types (ObjectDataProvider, WindowsIdentity, FileInfo, Process, etc.)
- Register it as the default binder in `JsonSerializer` and `JsonSerializerInternal`
- Create an optional `AllowListSerializationBinder` users can register via DI for maximum lockdown
- Ensure existing users sending POCOs are not broken — only known dangerous types are blocked
- The deny-list must be extensible (users can add types to block)
- Update integration test helper (`Helpers.cs`) to not use `TypeNameHandling.All`

### Queue Name Validation (H-2)
- Add validation in each transport's connection info class (SqlServer, PostgreSQL, SQLite, LiteDB, Redis)
- Validation rules: alphanumeric characters, underscores, dots; transport-specific rules allowed (e.g., hyphens)
- Throw `ArgumentException` with a clear message if validation fails
- Validation occurs at construction time (fail fast)

### Async Dispose Fix (H-5)
- Implement `IAsyncDisposable` on `DashboardConsumerClient`
- `DisposeAsync()` properly awaits the HTTP DELETE call
- Keep synchronous `Dispose()` as fallback using fire-and-forget with documented rationale
- Remove silent exception swallowing — log or propagate appropriately

### Stale Project Removal (H-7)
- Remove `DotNetWorkQueue.IntegrationTests.Metrics` project directory
- Remove from `DotNetWorkQueue.sln`
- Verify no other projects reference it

### Security Documentation (C-2)
- Create a wiki-style security considerations page (markdown in docs or repo)
- Cover: Dynamic LINQ compilation risks, mitigation strategies, recommended deployment patterns
- Reference existing README documentation
- Include guidance on network-level protections for queue backends

## Non-Functional Requirements

- **Backward compatibility**: No breaking changes for existing users. Deny-list binder is additive protection.
- **Testing**: Unit tests required for all code changes (binder deny/allow-list, queue name validation per transport, async dispose)
- **Performance**: Binder lookup must be O(1) (HashSet-based). Queue name validation at construction only, not per-operation.
- **Multi-target**: All changes must work across net10.0, net8.0, net48, and netstandard2.0

## Success Criteria

1. Deserialization of known Newtonsoft.Json gadget types throws `JsonSerializationException` by default
2. Queue names containing SQL injection characters (`]; DROP TABLE`, `'OR 1=1`, etc.) are rejected at construction
3. `DashboardConsumerClient` implements `IAsyncDisposable` and properly awaits cleanup
4. `IntegrationTests.Metrics` project is removed from solution with no build errors
5. Security documentation covers Dynamic LINQ risks with clear guidance
6. All existing unit tests continue to pass
7. All new behavior has unit test coverage

## Constraints

- **Technical**: Must support all four target frameworks (net10.0, net8.0, net48, netstandard2.0)
- **Git**: Manual commit strategy (user controls all git operations)
- **CI**: TeamCity runs all tests; GitHub Actions runs unit + in-memory integration only
- **Dependencies**: No new NuGet package dependencies for the security fixes
