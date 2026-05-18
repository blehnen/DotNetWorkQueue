# Research: Phase 2 Foundation Layer — `IRelationalWorkerNotification` Interface

Scope: `Transport.RelationalDatabase` only. Phase 2 ships the interface definition; no implementation,
no transport-specific code. Per CONTEXT-2.md §Decisions decision 1 and ROADMAP line 37.

---

## §1 Existing `IWorkerNotification` Surface

**File:** `Source/DotNetWorkQueue/IWorkerNotification.cs`
**Namespace:** `DotNetWorkQueue`
**Visibility:** `public interface`
**XML-doc style:** Triple-slash `<summary>` on every member, `<value>` on properties, `<remarks>` where
semantics need clarification. See lines 27–91 for the full pattern.

Members (lines 30–91):

| Member | Type | Setter? | Notes |
|--------|------|---------|-------|
| `WorkerStopping` | `ICancelWork` | `get; set;` | Shutdown signal |
| `HeaderNames` | `IHeaders` | `get;` | Read-only |
| `HeartBeat` | `IWorkerHeartBeatNotification` | `get; set;` | |
| `TransportSupportsRollback` | `bool` | `get;` | Informational flag |
| `Log` | `ILogger` | `get;` | `Microsoft.Extensions.Logging.ILogger` |
| `Metrics` | `IMetrics` | `get;` | |
| `Tracer` | `System.Diagnostics.ActivitySource` | `get;` | |
| `MessageCancellation` | `IMessageCancellation` | `get; set;` | Never null per remarks |

Using directives present at top of file: `System.Diagnostics`, `DotNetWorkQueue.Logging`,
`Microsoft.Extensions.Logging`, `OpenTelemetry.Trace`.

No ADO.NET (`System.Data`) usings are present — this is a core-assembly type that must stay
provider-agnostic.

---

## §2 `Transport.RelationalDatabase` csproj Shape

**File:** `Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj`

Key properties relevant to Phase 2:

- **TargetFrameworks:** `net10.0;net8.0` (lines 4)
- **Release|net10.0 PropertyGroup (lines 22–27):**
  - `TreatWarningsAsErrors` = `true`
  - `WarningsAsErrors` = (blank — all)
  - `GenerateDocumentationFile` = `true`
- **Release|net8.0 PropertyGroup (lines 29–34):** identical settings to net10.0
- **No ADO.NET provider references** — only `ProjectReference` to `Transport.Shared` and `DotNetWorkQueue`
- **No `WarningsNotAsErrors` suppression** — clean doc builds are required

Implication for Phase 2: every `public` member of the new interface requires a `<summary>` XML comment,
or `CS1591` will fail the Release build under `TreatWarningsAsErrors`.

---

## §3 Extractor Placement Convention (informational — for Phase 5)

Existing extractors confirming per-transport placement:

| Extractor | Location | Namespace |
|-----------|----------|-----------|
| `SqlServerExternalDbNameExtractor` | `Source/DotNetWorkQueue.Transport.SqlServer/Basic/` | `DotNetWorkQueue.Transport.SqlServer.Basic` |
| `PostgreSqlExternalDbNameExtractor` | `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/` | `DotNetWorkQueue.Transport.PostgreSQL.Basic` |

Interface (`IExternalDbNameExtractor`) lives in `Transport.RelationalDatabase` (shared).
Implementations live in each transport's `/Basic/` subdirectory.

The SQLite extractor (Phase 5) must follow the same pattern:
`Source/DotNetWorkQueue.Transport.SQLite/Basic/SqliteExternalDbNameExtractor.cs` in namespace
`DotNetWorkQueue.Transport.SQLite.Basic`.

---

## §4 Test Project for `Transport.RelationalDatabase`

**File:** `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj`

- **TargetFrameworks:** `net10.0` only (test projects are single-target)
- **Test framework:** `MSTest.TestFramework` + `MSTest.TestAdapter`
- **Mocking:** `NSubstitute`
- **Fixtures:** `AutoFixture` + `AutoFixture.AutoNSubstitute`
- **Assertions:** `FluentAssertions` (pinned 6.12.2 per MEMORY.md)
- **Coverage:** `coverlet.collector`

Established test pattern (`TestHelpers/AdoNetMockFixture.cs` lines 43–65):

- NSubstitute mocks for `IDbConnectionFactory`, `IDbConnection`, `IDbCommand`, `IDataReader`
- Sync handlers use `IDbConnection`/`IDbCommand` (interfaces, not sealed types)
- Async handlers mock `DbConnection`/`DbCommand`/`DbDataReader` abstract base classes

For Phase 2 (interface-only, no implementation), the test is a contract test: verify the interface
declares `DbTransaction Transaction { get; }` with the correct type and visibility. A simple reflection
or direct compile test suffices. No ADO.NET mock scaffolding required.

---

## §5 ConnectionHolder + DbTransaction Wiring (informational)

**`IConnectionHolder<TConnection, TTransaction, TCommand>`**
File: `Source/DotNetWorkQueue.Transport.RelationalDatabase/IConnectionHolder.cs` lines 33–57

- Generic constraints: `TConnection : IDbConnection`, `TTransaction : IDbTransaction`, `TCommand : IDbCommand`
- Exposes `TTransaction Transaction { get; set; }` (line 51)

**`ITransactionWrapper`**
File: `Source/DotNetWorkQueue.Transport.RelationalDatabase/ITransactionWrapper.cs` lines 27–42

- `IDbConnection Connection { get; set; }`
- `IDbTransaction BeginTransaction()` — returns `IDbTransaction`

**`TransactionWrapper`**
File: `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/TransactionWrapper.cs` lines 24–44

- Concrete implementation; calls `Connection.BeginTransaction()` returning `IDbTransaction`

Confirmation: transaction is exposed internally as `IDbTransaction` throughout the relational database
layer. The new `IRelationalWorkerNotification.Transaction` member must use `System.Data.Common.DbTransaction`
(the abstract base class) per ROADMAP §interface-shape, NOT `IDbTransaction`. See §7 for the risk detail.

---

## §6 License Header + Interface-File Template

**License header source:** `Source/DotNetWorkQueue/DotNetWorkQueue.licenseheader`
(linked into `Transport.RelationalDatabase` via `<None ... Link="DotNetWorkQueue.licenseheader" />`)

Required header block for every new file (17-line LGPL-2.1 block, copyright 2015-2026 Brian Lehnen).
Reference any existing interface file in `Transport.RelationalDatabase` for the exact text — all are
identical. Example: `IConnectionHolder.cs` lines 1–18.

Interface file structure template based on existing conventions:

```
[17-line license header]

using System.Data;                     // for IDbTransaction (if needed)
using System.Data.Common;              // for DbTransaction
using DotNetWorkQueue;                 // for IWorkerNotification

namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    /// <summary>
    /// [one-line description]
    /// </summary>
    /// <remarks>
    /// [capability-cast pattern explanation]
    /// </remarks>
    public interface IRelationalWorkerNotification : IWorkerNotification
    {
        /// <summary>
        /// [member description]
        /// </summary>
        /// <value>
        /// [value description]
        /// </value>
        DbTransaction Transaction { get; }
    }
}
```

---

## §7 Risks and Pitfalls for Phase 2 Architect

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Using `IDbTransaction` instead of `DbTransaction` for the property type | Medium | High | ROADMAP and CONTEXT-2 specify `DbTransaction` (abstract base class from `System.Data.Common`). `IDbTransaction` is the interface. Consumer code pattern (capability-cast + `await using`) requires the abstract base for `async` dispose. Use `DbTransaction` explicitly and add `using System.Data.Common;` |
| Missing XML doc on the new member causing `CS1591` build failure | High | High | `TreatWarningsAsErrors` is enabled for both Release targets. Every `public` member needs `<summary>`. |
| `DotNetWorkQueue.IConfiguration` namespace-walk-up shadowing pattern | Low | Medium | No `IConfiguration` usage in this interface, but if any future `using` adds configuration types, use `global::Microsoft.Extensions.Configuration.IConfiguration`. Same walk-up rule applies to any type whose short name collides with a `DotNetWorkQueue.*` type. |
| Introducing an ADO.NET provider reference into `Transport.RelationalDatabase` | Low | High | The csproj has no `Microsoft.Data.SqlClient`, `Npgsql`, or `Microsoft.Data.Sqlite` references — keep it that way. `DbTransaction` is in `System.Data.Common` (BCL, no NuGet required). |
| Naming drift: `Tx` abbreviation appearing in identifiers or commit messages | Medium | Low | CLAUDE.md and CONTEXT-2.md both prohibit `Tx`. Full word: `Transaction`. Applies to all symbols, XML docs, commit messages, and PR descriptions. |
| `Release|AnyCPU` PropertyGroup absence | Low | Medium | The csproj has `Release|net10.0` and `Release|net8.0` blocks but no `Release|AnyCPU` block. This matches the existing pattern and is intentional. Do not add a third block — it would duplicate settings. |

---

## Phase 2 Architect Handoff Summary

- **Single deliverable:** One new file, `IRelationalWorkerNotification.cs`, in `Source/DotNetWorkQueue.Transport.RelationalDatabase/`. Interface declares `public interface IRelationalWorkerNotification : IWorkerNotification` with one read-only member `DbTransaction Transaction { get; }` typed as `System.Data.Common.DbTransaction` (not `IDbTransaction`).
- **Build gates:** Release build (`TreatWarningsAsErrors` + `GenerateDocumentationFile` = true on both net10.0 and net8.0) requires complete XML docs; missing `<summary>` on the property = `CS1591` = build failure. Validate with `dotnet build -c Release -p:CI=true`.
- **Test requirement:** One contract-level unit test in `Transport.RelationalDatabase.Tests` confirming the interface inherits `IWorkerNotification` and exposes `Transaction` with type `DbTransaction`. No ADO.NET mock scaffolding needed for an interface-only phase.
