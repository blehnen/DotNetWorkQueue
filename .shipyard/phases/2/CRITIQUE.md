# CRITIQUE: Phase 2 Plan Feasibility

**Verdict:** READY

---

## Per-task findings

### Task 1: IRelationalWorkerNotification Interface

**Target file path:** `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRelationalWorkerNotification.cs`

**Infrastructure verification:**
- Parent directory exists with 20+ existing interface files (confirmed: `IDbPaginationSyntax.cs`, `IExternalDbNameExtractor.cs`, `IRelationalProducerQueue.cs`).
- License header source (`IConnectionHolder.cs` lines 1–18) confirmed as 18-line LGPL-2.1 block, copyright 2015–2026, identical across all existing Transport.RelationalDatabase interfaces.
- Base interface `IWorkerNotification` exists at `Source/DotNetWorkQueue/IWorkerNotification.cs:30–91` with established XML-doc pattern (`<summary>`, `<value>`, `<remarks>`).
- `System.Data.Common.DbTransaction` is BCL (no NuGet dependency); type choice aligns with RESEARCH §7 risk mitigation (abstract base for `async` dispose).

**Plan compliance:**
- Exact file content provided (lines 44–111 in plan).
- Header byte-identical to `IConnectionHolder.cs` with no edits to year/copyright.
- Single `using System.Data.Common;` directive specified (no redundant imports).
- XML documentation comprehensive: `<summary>` + `<remarks>` on interface; `<summary>` + `<value>` + `<remarks>` on property member.
- No `Tx` abbreviation — `Transaction` spelled fully throughout (lines 109, 77–78, 102, 106, 109).
- Read-only property: `DbTransaction Transaction { get; }` with no setter.
- Trailing newline convention noted.

**Build gates feasible:**
- Transport.RelationalDatabase csproj targets `net10.0;net8.0` with Release PropertyGroups (lines 22–34 of csproj) enforcing `TreatWarningsAsErrors=true` and `GenerateDocumentationFile=true` on both targets.
- Plan specifies XML docs on both interface and member (`<summary>`, `<value>`, `<remarks>`) — sufficient to satisfy `CS1591` (missing-XML-doc) strictness.
- No ADO.NET provider references in csproj (only `ProjectReference` to Transport.Shared and DotNetWorkQueue) — confirmed, no new provider imports needed.

---

### Task 2: Contract Unit Test

**Target file path:** `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/IRelationalWorkerNotificationContractTests.cs`

**Infrastructure verification:**
- Test project exists at `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/` with csproj targeting `net10.0` only (standard for test projects).
- MSTest version pinned to 4.2.1 (`Microsoft.VisualStudio.TestTools.UnitTesting`) in central `Directory.Packages.props`.
- Test file placement (root of test project) matches existing pattern; no subdirectory required.

**Plan compliance:**
- Five `[TestMethod]` test methods specified (lines 179–232):
  1. `Interface_Is_Public()` — validates interface public visibility.
  2. `Interface_Inherits_IWorkerNotification()` — checks inheritance chain via `IsAssignableFrom()`.
  3. `Transaction_Property_Exists_With_Expected_Type()` — reflection-based property existence and `DbTransaction` type assertion.
  4. `Transaction_Property_Is_Read_Only()` — validates getter presence and no public setter via `GetSetMethod(nonPublic: false)`.
  5. `Interface_Declares_Exactly_One_New_Property()` — tripwire against accidental drift (uses `BindingFlags.DeclaredOnly` to exclude inherited members).
- Header byte-identical to Task 1 source (LGPL-2.1, copyright 2015–2026).
- Imports specified: `System.Data.Common`, `System.Reflection`, `Microsoft.VisualStudio.TestTools.UnitTesting` (MSTest 4.2.1 compatible).
- No FluentAssertions, NSubstitute, or AutoFixture — pure reflection + MSTest assertions as required for contract tests.
- No `Tx` abbreviation in test file.

**Verification command feasible:**
- Plan specifies `dotnet test ... --filter "FullyQualifiedName~IRelationalWorkerNotificationContractTests"` — filter pattern valid for MSTest discovery.
- Post-execution: existing test suite must remain green; no regressions expected (new file, no modifications to existing tests).

---

### Task 3: Verification Gates

**Four verification commands specified (plan lines 275–294):**

| Gate | Command | Status |
|------|---------|--------|
| 1 | `dotnet build "Source/DotNetWorkQueue.Transport.RelationalDatabase/..." -c Release -p:CI=true` | **Runnable.** Both net10.0 and net8.0 Release targets will be invoked. Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`. |
| 2 | `dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/..."` | **Runnable.** Discovers all test classes including new `IRelationalWorkerNotificationContractTests`. Expected: 5 new tests + baseline count = total. |
| 3 | `grep -nE "Microsoft\.Data\.SqlClient\|Npgsql\|Microsoft\.Data\.Sqlite" "Source/.../DotNetWorkQueue.Transport.RelationalDatabase.csproj" ; test $? -eq 1` | **Portable.** Bash `test $? -eq 1` idiomatic (grep exit 1 = no matches). Confirmed no provider references in csproj. Expected: zero matches. |
| 4 | `grep -nE "\b(Tx\|TX)\b" "Source/.../IRelationalWorkerNotification.cs" "Source/.../IRelationalWorkerNotificationContractTests.cs" ; test $? -eq 1` | **Portable.** Word-boundary `\b` and case-sensitive matching correct. Expected: zero matches (only `Transaction`, `transaction` will appear). |

All four gates are concrete, runnable, and produce measurable output (pass/fail based on exit codes or test counts).

---

## Cross-cutting observations

**Forward references:**
- Phase 2 is explicitly interface-only, additive. No behavior changes, no implementation. Phase 5 will build extractors and wrappers on this foundation.
- No circular dependencies between Transport.RelationalDatabase, Transport.Shared, and core DotNetWorkQueue.
- Namespace `DotNetWorkQueue.Transport.RelationalDatabase` already established with 20+ public interfaces; no namespace walk-up shadowing risk (no `IConfiguration` usage in this interface).

**Hidden dependencies:**
- None identified. License header source (`IConnectionHolder.cs`) is stable. Base interface `IWorkerNotification` is stable and read-only. MSTest 4.2.1 is already pinned. System.Data.Common is BCL.

**Complexity:**
- Low. Single interface (8 lines of signature), single test class (5 methods, ~60 lines). No ADO.NET provider selection, no transport-specific logic, no multi-targeting concerns for the test project.

**Regressions:**
- No existing files modified. Addition only. Existing test suite is not touched. All gates are positive (new tests must pass) or null-match (no forbidden tokens).

---

## Verdict rationale

PLAN-1.1 is **ready to execute without revision**. The plan demonstrates:

1. **Precise specification.** File content, header source, test structure, and verification gates are all explicit with no ambiguity.
2. **Anchored to codebase.** License headers, base interface, target directories, and build configuration are all confirmed against the live repo state.
3. **Compliance-conscious.** MSTest version, XML-doc requirements, `Tx` abbreviation prohibition, and "no provider references" constraints are all addressed.
4. **Measurable gates.** All four verification commands produce concrete pass/fail evidence (build exit codes, test counts, grep match counts).
5. **Low risk.** Interface-only, additive change on a mature codebase with established patterns. No behavior change, no transport-specific logic. Contract test is a straightforward reflection + assertion pattern already used elsewhere in the codebase.

Recommendation: **Approve for execution immediately.** No blocking gaps or ambiguities identified.
