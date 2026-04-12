# Phase 1 Plan Critique and Verification
**Phase:** Multi-Source Configuration and Client Infrastructure
**Date:** 2026-04-09
**Type:** plan-review

## Part 1: Coverage Verification

### Requirement-to-Plan Coverage Matrix

| # | Roadmap Success Criterion | Covered By | Status |
|---|--------------------------|------------|--------|
| 1 | `DashboardApiSourceConfig` model exists with Name, BaseUrl, ApiKey properties | PLAN-1.1 Task 2 | COVERED -- creates `DashboardApiSourceConfig` class with Name, BaseUrl, ApiKey properties and computed Slug. |
| 2 | `ISourceRegistry` provides `GetAll()`, `GetBySlug()`, `GetByName()` methods | PLAN-1.1 Task 3 | COVERED -- creates `ISourceRegistry` interface with all 3 methods and `SourceRegistry` implementation. |
| 3 | `IMultiSourceDashboardApiClient.GetClientForSource(slug)` returns correct per-source `IDashboardApiClient` | PLAN-2.1 Task 1 | COVERED -- creates interface and implementation with `GetClientForSource(slug)` returning cached `IDashboardApiClient`. |
| 4 | Old flat config (`DashboardApi:BaseUrl` without `Sources`) produces `InvalidOperationException` at startup with migration instructions | PLAN-2.1 Task 2 | COVERED -- `DashboardConfigParser.ValidateNoLegacyConfig()` throws `InvalidOperationException` with migration example JSON. |
| 5 | In-process API registers as a source named "Local" (or configured name) | PLAN-2.1 Task 2 | COVERED -- Program.cs refactor adds "Local" source when `selfContained` is true. `LocalSourceHostedService` resolves actual listen address. |
| 6 | `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` succeeds with 0 errors | PLAN-2.1 Verification section | PARTIAL -- PLAN-2.1 verification runs `dotnet build` on the Ui project only, not the full solution. The `done` criteria for Task 2 explicitly acknowledges pages "may have @inject warnings." See Gap G1. |
| 7 | All existing Dashboard API integration tests pass unchanged (API layer untouched) | PLAN-2.1 Verification section | COVERED -- verification commands include `dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj" -c Debug`. |
| 8 | Unit tests pass for: valid config, old-format detection, slug generation, duplicate rejection, source lookup | PLAN-1.1 Tasks 2-3, PLAN-2.1 Tasks 1-3 | COVERED -- test files cover all enumerated scenarios: `DashboardApiSourceConfigTests` (slug generation), `SourceRegistryTests` (duplicate rejection, lookup), `ConfigValidationTests` (old-format detection), `MultiSourceDashboardApiClientTests` (source lookup via client), `LocalSourceHostedServiceTests`. |

### Structural Checks

| Check | Status | Evidence |
|-------|--------|----------|
| No plan exceeds 3 tasks | PASS | PLAN-1.1 has 3 tasks; PLAN-2.1 has 3 tasks. |
| Wave ordering respects dependencies | PASS | PLAN-2.1 (Wave 2) declares dependency on PLAN-1.1 (Wave 1). PLAN-1.1 has no dependencies. |
| No file conflicts between parallel plans | N/A | Only 1 plan per wave. |
| Acceptance criteria are testable | PASS | All `done` criteria reference specific test names and pass/fail outcomes. |

---

## Part 2: Feasibility Stress Test

### PLAN-1.1: Test Project Scaffold, Config Model, and Source Registry

#### File Path Verification

| Reference | Type | Exists | Evidence |
|-----------|------|--------|----------|
| `Source/DotNetWorkQueue.Dashboard.Ui/Services/` | Directory (parent for new files) | YES | Glob found 3 existing files: `DashboardAuthConfig.cs`, `DashboardApiClient.cs`, `IDashboardApiClient.cs` |
| `Source/DotNetWorkQueue.Dashboard.Ui.Tests/` | New project directory | NO (expected) | Glob returned no files -- directory does not exist yet. Will be created by Task 1. |
| `Source/DotNetWorkQueue.sln` | Modify | YES | Referenced in `dotnet sln add` command. Confirmed exists via build commands in CLAUDE.md. |
| `Source/DotNetWorkQueueNoTests.sln` | No change | YES | Listed as no-change in frontmatter. Correct -- test project excluded from this solution. |
| `.github/workflows/ci.yml` | Modify | YES | Glob confirmed at `.github/workflows/ci.yml`. |
| `Dashboard.Api.Tests.csproj` template | Reference | YES | Glob found `Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj`. |
| `DashboardAuthConfig.cs` (license header pattern) | Reference | YES | Read confirmed at `Source/DotNetWorkQueue.Dashboard.Ui/Services/DashboardAuthConfig.cs` -- contains LGPL-2.1 header. |

#### API Surface Verification

| Claim in Plan | Actual Code | Match |
|---------------|-------------|-------|
| `DashboardApiClient` takes `HttpClient` in constructor | `public DashboardApiClient(HttpClient http)` at line 33 of `DashboardApiClient.cs` | YES |
| `DashboardAuthConfig.cs` exists as pattern reference | Confirmed at `Services/DashboardAuthConfig.cs`, public class with properties and XML doc comments | YES |
| Central package management (no Version attributes in csproj) | `Source/Directory.Packages.props` exists with `ManagePackageVersionsCentrally=true`. All test packages listed. | YES |
| Template uses `Microsoft.NET.Sdk` (not Web) | Dashboard.Api.Tests.csproj uses `Microsoft.NET.Sdk` at line 1 | YES |
| TargetFrameworks `net10.0;net8.0` | Dashboard.Api.Tests.csproj line 4: `<TargetFrameworks>net10.0;net8.0</TargetFrameworks>` | YES |

#### Verification Command Checks

| Command | Runnable | Notes |
|---------|----------|-------|
| `dotnet build "Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj" -c Debug` | YES (after Task 1 creates the project) | Valid path pattern. |
| `dotnet test ... --filter "FullyQualifiedName~DashboardApiSourceConfigTests"` | YES | Valid MSTest filter syntax. |
| `dotnet test ... --filter "FullyQualifiedName~SourceRegistryTests"` | YES | Valid MSTest filter syntax. |
| `dotnet sln "Source/DotNetWorkQueue.sln" list \| grep Dashboard.Ui.Tests` | YES | Correct syntax. |
| `grep -A1 "Dashboard.Ui" .github/workflows/ci.yml` | YES | Will match the new CI step. |

#### CI YAML Insertion Point

PLAN-1.1 Task 1 says to add the step "after the Unit Tests - Dashboard.Client step." Inspecting `ci.yml` line 58-59: `- name: Unit Tests - Dashboard.Client` is the last test step. The insertion point is correct and the yml file ends with `- name: Unit Tests - Memory`, so the new step should go after Dashboard.Client (line 59) and before Memory (line 61). **CAUTION:** The plan says "after Dashboard.Client" but Memory is actually the last step (line 62). The builder should insert it between Dashboard.Client and Memory, or at the end. This is a minor ambiguity, not a blocker.

#### Findings: PLAN-1.1

- **F1 (INFO):** CI step insertion point is slightly ambiguous -- "after Dashboard.Client" could mean before or after Memory. Non-blocking; builder can resolve at execution time.
- No other issues found. All file paths, API surfaces, and verify commands are correct.

---

### PLAN-2.1: Multi-Source Client, In-Process Registration, and DI Refactor

#### File Path Verification

| Reference | Type | Exists | Evidence |
|-----------|------|--------|----------|
| `Source/DotNetWorkQueue.Dashboard.Ui/Services/IMultiSourceDashboardApiClient.cs` | New | Parent directory exists | OK |
| `Source/DotNetWorkQueue.Dashboard.Ui/Services/MultiSourceDashboardApiClient.cs` | New | Parent directory exists | OK |
| `Source/DotNetWorkQueue.Dashboard.Ui/Services/LocalSourceHostedService.cs` | New | Parent directory exists | OK |
| `Source/DotNetWorkQueue.Dashboard.Ui/Services/DashboardConfigParser.cs` | New | Parent directory exists | **MISSING FROM FRONTMATTER** -- see F2 |
| `Source/DotNetWorkQueue.Dashboard.Ui/Program.cs` | Modify | YES | Read confirmed 142-line file. |
| `Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/MultiSourceDashboardApiClientTests.cs` | New | Parent created by PLAN-1.1 | OK (dependency satisfied) |
| `Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/ConfigValidationTests.cs` | New | Parent created by PLAN-1.1 | OK |
| `Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/LocalSourceHostedServiceTests.cs` | New | Parent created by PLAN-1.1 | **MISSING FROM FRONTMATTER** -- see F3 |

#### API Surface Verification

| Claim in Plan | Actual Code | Match |
|---------------|-------------|-------|
| Program.cs lines 44-52 contain `AddHttpClient<IDashboardApiClient, DashboardApiClient>` | Lines 44-52 contain the comment, `apiBaseUrl`/`apiKey` vars, and `AddHttpClient` call | YES -- exact match |
| `DashboardApiClient(HttpClient http)` constructor called in `CreateClient` method | Constructor at line 33 is `public DashboardApiClient(HttpClient http)` | YES |
| `IDashboardApiClient` interface exists and is unchanged | Confirmed 26-method interface at `Services/IDashboardApiClient.cs` | YES |
| `selfContained` variable exists in Program.cs | Line 38: `var selfContained = dashboardSection.GetSection("Connections").GetChildren().Any();` | YES |
| `using DotNetWorkQueue.Dashboard.Ui.Services;` already at line 24 | Line 24: `using DotNetWorkQueue.Dashboard.Ui.Services;` | YES |
| `IHttpClientFactory` available via framework | Dashboard.Ui uses `Microsoft.NET.Sdk.Web` which includes `Microsoft.Extensions.Http` transitively | YES |
| `Microsoft.Extensions.Configuration.ConfigurationBuilder` available for tests | Test project references Dashboard.Ui (Sdk.Web project), which transitively provides all Configuration types | YES |

#### Program.cs Line Number Accuracy

PLAN-2.1 Task 2 says "Replace lines 44-52." Actual content of lines 44-52:

```
44: // --- API client (always registered; in self-contained mode, routes to the in-process API via localhost) ---
45: var apiBaseUrl = builder.Configuration["DashboardApi:BaseUrl"] ?? "http://localhost:5000";
46: var apiKey = builder.Configuration["DashboardApi:ApiKey"];
47: builder.Services.AddHttpClient<IDashboardApiClient, DashboardApiClient>(client =>
48: {
49:     client.BaseAddress = new Uri(apiBaseUrl);
50:     if (!string.IsNullOrEmpty(apiKey))
51:         client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
52: });
```

**EXACT MATCH.** The line references are correct.

#### Verification Command Checks

| Command | Runnable | Notes |
|---------|----------|-------|
| `dotnet test ... --filter "FullyQualifiedName~MultiSourceDashboardApiClientTests"` | YES | Valid filter, valid project path (created by PLAN-1.1). |
| `dotnet test ... --filter "FullyQualifiedName~ConfigValidationTests"` | YES | Valid. |
| `dotnet test ... --filter "FullyQualifiedName~LocalSourceHostedServiceTests"` | YES | Valid. |
| `dotnet build "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj" -c Debug` | YES | Valid path. |
| `dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj" -c Debug` | YES | Valid path, confirmed project exists. |

#### Findings: PLAN-2.1

- **F2 (MEDIUM): `DashboardConfigParser.cs` missing from `files_touched` frontmatter.** Task 2 creates `Source/DotNetWorkQueue.Dashboard.Ui/Services/DashboardConfigParser.cs` (a new file with `ParseSources` and `ValidateNoLegacyConfig` methods), but this file is NOT listed in the plan's YAML `files_touched` section. The task body correctly describes creating it. This is a metadata omission, not a functional issue, but it means dependency tracking and file conflict detection cannot account for this file. The builder will likely handle it correctly since it is described in the task action. **Severity: Low-medium.**

- **F3 (LOW): `LocalSourceHostedServiceTests.cs` missing from `files_touched` frontmatter.** Task 3 creates this test file, and it appears in the `<task id="3" files="...">` attribute, but NOT in the YAML frontmatter `files_touched`. Same metadata-only issue as F2. **Severity: Low.**

- **F4 (MEDIUM): `appsettings.json` not updated by either plan.** The ROADMAP Phase 1 "Files Touched" section lists `Source/DotNetWorkQueue.Dashboard.Ui/appsettings.json (new Sources[] format example)`. The current `appsettings.json` has the old flat `DashboardApi:BaseUrl/ApiKey` format. Neither plan updates it. After PLAN-2.1, the old format in `appsettings.json` will trigger the `InvalidOperationException` at startup. This means the app will crash on launch with default config. **Severity: Medium -- the builder must either update `appsettings.json` to the new `Sources[]` format or ensure the validation logic handles "neither present" gracefully (PLAN-2.1 Task 2 test `ValidateNoLegacyConfig_Does_Not_Throw_When_Neither_Present` suggests "neither present" is OK, but the current `appsettings.json` HAS `DashboardApi:BaseUrl` set to `"http://localhost:5000"`, which IS the old format).**

- **F5 (HIGH): Solution build criterion (success criterion #6) may fail.** PLAN-2.1 removes the `IDashboardApiClient` DI registration. Three Blazor pages inject it via `@inject IDashboardApiClient Api` (confirmed in `Home.razor:2`, `ConnectionDetail.razor:2`, `QueueDetail.razor:2`). Blazor's compile-time DI checking does NOT catch unregistered services -- it is a runtime error, not a build error. However, the success criterion says `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` succeeds with 0 errors. Since `@inject` is resolved at runtime, not compile time, the build WILL succeed. The plan's `done` note ("pages may have @inject warnings -- expected, fixed in Phase 2") is slightly misleading because there will be no compile warnings either -- the failure is purely at runtime. **This is acceptable for Phase 1 because Phase 2 rewires the pages, and Phase 1's success criterion only requires build success, not runtime success.** PASS with note.

- **F6 (LOW): `new DashboardApiClient(httpClient)` constructor call in `MultiSourceDashboardApiClient.CreateClient`.** The plan has `MultiSourceDashboardApiClient` directly instantiating `new DashboardApiClient(httpClient)`. This is a direct coupling to the concrete class rather than going through DI. This is fine because `DashboardApiClient` is a simple wrapper with no dependencies beyond `HttpClient`, and the plan explicitly describes this pattern. Just noting it for awareness.

- **F7 (INFO): PLAN-2.1 `must_haves` says "Old single-source IDashboardApiClient DI registration removed" but this is an intentional breakage.** CONTEXT-1.md confirms this design decision. This is consistent and not an issue.

#### Hidden Dependencies

| Dependency | Status |
|------------|--------|
| PLAN-2.1 Task 1 depends on PLAN-1.1 (needs `DashboardApiSourceConfig`, `ISourceRegistry`, `SourceRegistry`) | SATISFIED -- PLAN-2.1 is Wave 2, PLAN-1.1 is Wave 1. |
| PLAN-2.1 Task 2 creates `DashboardConfigParser.cs` and tests it in the same task | OK -- same task, sequential execution. |
| PLAN-2.1 Task 3 tests `LocalSourceHostedService` created in Task 2 | OK -- tasks within a plan are sequential. |
| PLAN-2.1 Task 2 modifies Program.cs, Task 1 does not | OK -- no intra-plan file conflict. |

#### Complexity Assessment

| Plan | Files Created/Modified | Directories Touched | Risk |
|------|----------------------|---------------------|------|
| PLAN-1.1 | 9 files (5 new, 2 modify, 2 no-change) | 4 directories | MEDIUM -- under 10 threshold |
| PLAN-2.1 | 8 files (6 new, 1 modify, 1 unlisted new) | 3 directories | HIGH -- marked high risk, DI composition root modification, but under 10-file threshold |

---

## Gaps

### G1: appsettings.json not migrated to new Sources[] format (MEDIUM)

The ROADMAP lists `appsettings.json` as a file to be updated with the new `Sources[]` format example. Neither plan touches it. The current `appsettings.json` contains `"DashboardApi": { "BaseUrl": "http://localhost:5000", "ApiKey": "" }` which is the old flat format. After PLAN-2.1's validation logic is in place, the app will throw `InvalidOperationException` on startup with default config.

**Impact:** Developers cloning the repo and running the UI project will get a crash. The self-contained mode (when `Dashboard:Connections` exists) will work because the code adds a "Local" source and the validation checks for "BaseUrl present WITHOUT Sources section." But in non-self-contained mode with default appsettings.json, it will crash.

**Recommendation:** Add a task to PLAN-2.1 (or a subtask within Task 2) to update `appsettings.json` from the old flat format to the new `Sources[]` format. Alternatively, the `ValidateNoLegacyConfig` logic could treat `BaseUrl = ""` or the default value as non-legacy (since the value is a default placeholder, not user configuration). The current test `ValidateNoLegacyConfig_Does_Not_Throw_When_Neither_Present` only covers the case where `DashboardApi:BaseUrl` is absent from config entirely, not where it exists with a default/empty value.

### G2: DashboardConfigParser.cs and LocalSourceHostedServiceTests.cs missing from PLAN-2.1 frontmatter (LOW)

Two files created by PLAN-2.1 are described in the task bodies but absent from the YAML `files_touched` metadata:
- `Source/DotNetWorkQueue.Dashboard.Ui/Services/DashboardConfigParser.cs` (new, created in Task 2)
- `Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/LocalSourceHostedServiceTests.cs` (new, created in Task 3)

**Impact:** File conflict detection between plans cannot account for these files. Since there is only one plan per wave, the practical impact is zero. However, the metadata should be accurate for auditability.

**Recommendation:** Add both files to PLAN-2.1's `files_touched` frontmatter.

---

## Regression Check

No prior VERIFICATION.md exists for Phase 1 (this is the first plan review). Checked `.shipyard/ISSUES.md` -- no open issues are related to Dashboard UI multi-source work. All open issues (ISSUE-016 through ISSUE-020) concern Redis, LiteDb, and RelationalDatabase transports, which are unrelated to this phase.

---

## Recommendations

1. **(MUST)** Update `appsettings.json` to the new `Sources[]` format as part of PLAN-2.1 Task 2, or adjust the legacy detection logic to not trigger on default/placeholder values. This is a gap that will cause startup crashes with default config.

2. **(SHOULD)** Add `DashboardConfigParser.cs` and `LocalSourceHostedServiceTests.cs` to PLAN-2.1's `files_touched` frontmatter for accurate tracking.

3. **(SHOULD)** Clarify the CI step insertion point in PLAN-1.1 Task 1 -- specify "after the Dashboard.Client step and before the Memory step" or "as the last step."

4. **(INFO)** The builder should be aware that after Phase 1, `@inject IDashboardApiClient Api` in three Blazor pages will be unresolvable at runtime. The build will succeed, but the app will fail at runtime on any page load. This is by design (Phase 2 fixes it), but integration test coverage should not attempt to exercise the UI after this phase.

---

## Verdict

**CAUTION** -- All 8 success criteria are covered by the plans. File paths, API surfaces, line numbers, and verification commands are accurate against the real codebase. The plans are well-structured with correct dependency ordering. Two issues require awareness:

1. **Gap G1 (appsettings.json)** is a real defect that will cause startup failures with default config. This is fixable within PLAN-2.1 Task 2 scope by adding 3-4 lines of appsettings.json editing. It does not require plan restructuring.

2. **Frontmatter metadata omissions (G2)** are low-impact but should be corrected before execution for clean tracking.

The plans are executable as-is if the builder addresses the appsettings.json gap during execution. No plan restructuring or redesign is needed. Proceed to build with the noted mitigations.
