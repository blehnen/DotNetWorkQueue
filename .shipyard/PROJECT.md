# Project: CONCERNS.md Tier B — Moderate Effort Fixes

## Description

Address the moderate-effort items from CONCERNS.md in a single PR. This covers Dashboard API hardening (exception disclosure fix, CORS configuration, health check endpoint), centralized package version management via Directory.Packages.props, TODO/HACK comment audit, and fixing the integration test serialization binder gap.

This is the second of two planned PRs. Tier A (quick wins) was completed and merged on 2026-03-30.

## Goals

1. Stop leaking exception details in Dashboard API responses (H-4)
2. Add configurable CORS policy to Dashboard API (H-3/M-9)
3. Add health check endpoint to Dashboard API (H-3)
4. Document internal-only deployment recommendation for Dashboard API (H-3)
5. Centralize all NuGet package versions via Directory.Packages.props (H-6)
6. Audit and resolve all TODO/HACK comments in production code (M-3)
7. Fix integration test Helpers.cs to use DenyListSerializationBinder (N-3)

## Non-Goals

- HTTPS enforcement or redirection (infrastructure concern)
- Rate limiting middleware (infrastructure concern)
- Dropping .NET Framework 4.8 support
- Replacing FluentAssertions with MSTest assertions (future work)
- Design-decision items from CONCERNS.md (M-6, M-7, M-10, M-11, L-2, L-4, L-7, L-8, N-2, N-5)

## Requirements

### Dashboard API Hardening (H-3, H-4, M-9)
- H-4: Change DashboardExceptionFilter to return generic error message in non-Development environments; log full exception server-side
- H-3 CORS: Add configurable CORS policy to DashboardOptions; default to allowing the configured dashboard UI origin; wire up in DashboardExtensions/DashboardApi
- H-3 Health: Add /health endpoint using ASP.NET Core built-in health check infrastructure; return 200 OK with basic status (queue connections reachable, service uptime)
- H-3 Docs: Update Dashboard API README to state internal-only recommendation; document that HTTPS and rate limiting should be handled at infrastructure layer
- M-9: Closed by CORS configuration in H-3

### Central Package Management (H-6)
- Create Directory.Packages.props at Source/ with all package versions consolidated
- Add ManagePackageVersionsCentrally to Directory.Build.props
- Remove Version attributes from all PackageReference elements across all .csproj files
- Key packages: SimpleInjector 5.5.0, Polly 8.6.5, Newtonsoft.Json 13.0.4, Microsoft.Data.SqlClient 6.1.3, OpenTelemetry 1.14.0, MSTest 3.x, NSubstitute, AutoFixture, FluentAssertions 6.12.2, plus all transport-specific packages

### TODO/HACK Audit (M-3)
- InterceptorFactory.cs line 52: Replace HACK comment with NOTE explaining SimpleInjector decorator pattern limitation
- ReceiveMessage.cs (PostgreSQL) line 175: Replace TODO with NOTE referencing CONCERNS.md L-4
- CreateDequeueStatement.cs (SqlServer) line 237: Same treatment as PostgreSQL TODO
- ReceiveMessage.cs (SqlServer) line 100: Replace TODO with NOTE explaining synchronous design decision
- LiteDbConnectionInformation.cs: Already fixed in tier A (L-5)

### Integration Test Binder Fix (N-3)
- Update Helpers.cs line 112 to use DenyListSerializationBinder with TypeNameHandling.All
- Ensures integration tests exercise the production serialization security boundary

## Non-Functional Requirements

- All existing unit tests must pass
- No breaking changes to public API surface (CORS and health check are additive)
- Central Package Management must resolve all packages correctly across all target frameworks
- Dashboard API exception filter change must not affect Development environment debugging

## Success Criteria

1. DashboardExceptionFilter returns generic error in non-Development; full exception logged
2. CORS policy configurable via DashboardOptions; Blazor UI can connect cross-origin
3. GET /health returns 200 OK with status info
4. Dashboard API README documents internal-only recommendation
5. Directory.Packages.props exists and all .csproj files have no Version= on PackageReference
6. dotnet build Source/DotNetWorkQueue.sln succeeds with central package management
7. No TODO or HACK comments remain in production code
8. Integration test Helpers.cs uses DenyListSerializationBinder
9. All unit tests pass

## Constraints

- Single PR for all tier B changes
- Dashboard API: no HTTPS redirect, no rate limiting (infrastructure concerns)
- Must work across all target frameworks (net10.0, net8.0, net48, netstandard2.0)
