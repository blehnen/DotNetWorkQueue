# Review: Plan 1.2

## Verdict: PASS

## Commits Reviewed
- `485811d4`: shipyard(phase-5): PLAN-1.1 — CORS registration + DashboardAuthorizationConvention tests
  (PLAN-1.2 file was staged alongside PLAN-1.1 and bundled into this commit — confirmed by SUMMARY-1.2.md)

## Stage 1: Spec Compliance

### Task 1: Swagger services register when EnableSwagger = true
- Status: PASS
- Evidence: `Source/DotNetWorkQueue.Dashboard.Api.Tests/Extensions/DashboardExtensionsSwaggerTests.cs` lines 33–50 implement `AddDotNetWorkQueueDashboard_Registers_SwaggerServices_When_Enabled` exactly as specified. Resolves `IOptions<SwaggerGenOptions>` from the built provider, asserts `SwaggerDocs.ContainKey("v1")` and title `"DotNetWorkQueue Dashboard"`. Matches the production code at `DashboardExtensions.cs` lines 114–119.

### Task 2: ApiKey security definition registers when ApiKey is set
- Status: PASS
- Evidence: Lines 52–74 implement `AddDotNetWorkQueueDashboard_Registers_ApiKeySecurityScheme_When_ApiKey_Set`. Sets `ApiKey = "test-secret"`, resolves `SwaggerGenOptions`, and asserts `SecuritySchemes.ContainKey("ApiKey")` with `SecuritySchemeType.ApiKey`, `ParameterLocation.Header`, and `Name = "X-Api-Key"`. All four assertions match the production security definition at `DashboardExtensions.cs` lines 123–129.

### Task 3: Negative test — no ApiKey security scheme when ApiKey is empty
- Status: PASS
- Evidence: Lines 76–94 implement `AddDotNetWorkQueueDashboard_Does_Not_Register_ApiKeySecurityScheme_When_ApiKey_Empty`. Leaves `ApiKey` at default and asserts `SecuritySchemes.Should().NotContainKey("ApiKey")`. Correctly exercises the `!string.IsNullOrEmpty(options.ApiKey)` branch guard at `DashboardExtensions.cs` line 121.

## Stage 2: Code Quality

### Critical
- None

### Minor
- None

### Positive
- All three test methods exactly match the plan's code samples. No deviations, no unnecessary additions.
- `IConfiguration` namespace shadowing issue does not apply here: no `IConfiguration` is referenced in this file, so no `global::` qualifier is needed and none is incorrectly absent.
- LGPL-2.1 license header is present and correct (lines 1–18).
- MSTest 3.x `[TestClass]` / `[TestMethod]` attributes used correctly; no deprecated `Assert.ThrowsException` calls.
- FluentAssertions 6.12.2 is used throughout (`.Should().ContainKey(...)`, `.Should().NotContainKey(...)`), consistent with project pin.
- File is in the correct namespace (`DotNetWorkQueue.Dashboard.Api.Tests.Extensions`) and directory, matching PLAN-1.1's pattern as specified.
- `SecuritySchemes` assertion validates all four required properties (key presence, type, location, name) — nothing was omitted.
- No conflict with PLAN-1.1, PLAN-1.3, or PLAN-2.1: separate file, separate class, no shared state.
