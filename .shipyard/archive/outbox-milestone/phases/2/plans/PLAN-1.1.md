---
phase: foundation-layer
plan: 1.1
wave: 1
dependencies: []
must_haves:
  - Delete throwaway Phase 1 PoC (Exit Criterion 8)
  - Add optional ExternalTransaction property to SendMessageCommand in Transport.Shared
  - Create IRetrySkippable marker interface in Transport.RelationalDatabase
files_touched:
  - Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs
  - Source/DotNetWorkQueue.Transport.Shared/Basic/Command/SendMessageCommand.cs
  - Source/DotNetWorkQueue.Transport.RelationalDatabase/IRetrySkippable.cs
tdd: false
risk: low
---

# Plan 1.1: Cleanup + Base Types (Wave 1)

## Context

Wave 1 establishes the lowest-layer additions that every downstream Wave 2/3 plan depends on. Three tightly-coupled, sequential edits in this single plan:

1. Delete the Phase 1 throwaway PoC (per CONTEXT-2 Exit Criterion 8). Phase 1 SUMMARY-1.1 confirmed the PoC has no production dependencies — file removal is mechanical.
2. Add the optional `DbTransaction ExternalTransaction { get; init; }` property to the existing `SendMessageCommand` in `Transport.Shared`. Additive only; defaults to null so the existing self-managed path is unaffected. Per RESEARCH.md §Section 1, the class is public, non-sealed, and has exactly 4 `new SendMessageCommand(...)` construction sites (all in `Transport.Shared/Basic/SendMessages.cs`) — none of which need to change.
3. Create the `IRetrySkippable` marker interface in `Transport.RelationalDatabase`. Per CONTEXT-2 Decision 2 (with RESEARCH.md confirming layering is clean for Option B: derived class), this marker lives in `Transport.RelationalDatabase`, not `Transport.Shared`. Layering check: SqlServer and PostgreSQL projects already reference `Transport.RelationalDatabase` (verified — see RESEARCH.md §Section 10 + .csproj files at lines 44 and 55 respectively), so the Wave 3 decorator branches have no new `<ProjectReference>` to add.

**Architect deviation from suggested structure: NONE.** The suggested Wave 1 PLAN-1.1 shape is preserved exactly.

## Dependencies

None. This is Wave 1.

## Tasks

### Task 1: Delete Phase 1 throwaway PoC

**Files:**
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs` (DELETE)

**Action:** delete

**Description:**
Delete `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs` outright. This file was the Phase 1 spike PoC for the IRetrySkippable bypass design. Phase 1 memo at `.shipyard/notes/phase-1-polly-bypass-spike.md` already documents the mechanism — the spike test is throwaway-by-design (file's own header block at lines 19–27 explicitly says "Phase 2's first task DELETES this file").

The PoC only references `Transport.SqlServer.Tests` types plus its own private classes (`_SpikeIRetrySkippable`, `_SpikeSendCommand`, `_SpikeRecordingHandler`, `_SpikePatchedRetryDecorator<,>`); no other file in the repo depends on it. After deletion, `Transport.SqlServer.Tests` builds clean and the 3 baseline tests in `RetryCommandHandlerOutputDecoratorTests.cs` continue to pass.

Use `git rm` so the deletion is staged in a single commit.

**Acceptance Criteria:**
- `test ! -f Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs` returns success (exit 0).
- `dotnet build "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --nologo` completes with no errors.
- `dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --filter "FullyQualifiedName~RetryCommandHandlerOutputDecoratorTests"` passes (3 baseline tests, no regressions).

### Task 2: Add ExternalTransaction property to SendMessageCommand

**Files:**
- `Source/DotNetWorkQueue.Transport.Shared/Basic/Command/SendMessageCommand.cs` (MODIFY)

**Action:** modify

**Description:**
Add a single new optional property to the existing `SendMessageCommand` class. The class today (RESEARCH.md §Section 1) has two get-only properties (`MessageToSend`, `MessageData`) set in the constructor. The new property is set via object initializer (`init`) so existing constructor call sites in `Transport.Shared/Basic/SendMessages.cs` (4 call sites) compile unchanged.

Add:

```csharp
using System.Data.Common;
```

at the top (the file currently has only `using DotNetWorkQueue.Validation;`).

Inside the class, after the existing `MessageData` property, add:

```csharp
/// <summary>
/// Optional caller-supplied transaction for the outbox pattern. When set, the relational
/// transport's send-message handler skips its internal connection/transaction management
/// and uses this transaction's connection and transaction reference instead. When null
/// (the default), the transport manages its own connection and transaction lifecycle
/// exactly as before.
/// </summary>
/// <remarks>
/// Wired to the bypass mechanism via <c>RelationalSendMessageCommand</c> (a derived class
/// in <c>Transport.RelationalDatabase</c>) which exposes <c>SkipRetry</c> through
/// <c>IRetrySkippable</c>. The base <c>SendMessageCommand</c> itself does NOT implement
/// <c>IRetrySkippable</c> to keep <c>Transport.Shared</c> free of references to
/// <c>Transport.RelationalDatabase</c>.
/// </remarks>
public DbTransaction ExternalTransaction { get; init; }
```

Do NOT change the existing constructor signature or the existing `MessageToSend` / `MessageData` properties. Do NOT implement `IRetrySkippable` on this class — that's deliberately handled by the derived `RelationalSendMessageCommand` in Wave 2 (per CONTEXT-2 Decision 2 Option B).

Preserve the LGPL license header verbatim.

**Acceptance Criteria:**
- File contains exactly one new property `public DbTransaction ExternalTransaction { get; init; }` with XML doc.
- File contains exactly one new `using System.Data.Common;` directive.
- No other lines in the file change (existing constructor, `MessageToSend`, `MessageData` untouched; license header preserved).
- `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Debug --nologo` succeeds with zero errors (this verifies all 4 `new SendMessageCommand(...)` call sites still compile because the new property is `init`-only, not a constructor parameter).
- `dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" -c Debug --filter "FullyQualifiedName~SendMessageCommandTests"` passes (the existing `Create_Default` round-trip test must still pass — confirms additive change).
- `dotnet build "Source/DotNetWorkQueue.Transport.Shared/DotNetWorkQueue.Transport.Shared.csproj" -c Release --nologo` succeeds (Release config enables `TreatWarningsAsErrors` + XML doc generation — missing-doc warnings are build breaks).

### Task 3: Create IRetrySkippable marker interface

**Files:**
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRetrySkippable.cs` (NEW)

**Action:** create

**Description:**
Create a new file `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRetrySkippable.cs` containing the marker interface. The interface lives at the root namespace `DotNetWorkQueue.Transport.RelationalDatabase` (matches existing file layout — see RESEARCH.md §Section 2; the project root holds top-level interfaces, with `Basic/` for implementations).

File content (exact):

```csharp
// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    /// <summary>
    /// Marker interface for command objects that opt out of the relational retry decorator
    /// on a per-call basis. The retry decorator inspects this property at <c>Handle()</c>
    /// time and invokes the inner handler directly when <see cref="SkipRetry"/> is
    /// <c>true</c>.
    /// </summary>
    /// <remarks>
    /// Introduced by the outbox-pattern feature so caller-supplied-transaction sends bypass
    /// the Polly retry pipeline (the caller owns retry semantics on this path). Implemented
    /// by <c>RelationalSendMessageCommand</c>.
    /// </remarks>
    public interface IRetrySkippable
    {
        /// <summary>
        /// Gets a value indicating whether the retry decorator should skip its Polly
        /// pipeline and invoke the inner handler directly for this command instance.
        /// </summary>
        bool SkipRetry { get; }
    }
}
```

The interface has exactly one bool property (`SkipRetry`). Do not add any other members. Do not place this file in a subfolder — it belongs at the project root next to the existing top-level interfaces.

**Acceptance Criteria:**
- File `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRetrySkippable.cs` exists.
- File starts with the LGPL-2.1 license header (lines 1–18 match the standard repo header verbatim).
- File defines exactly one public interface `IRetrySkippable` in namespace `DotNetWorkQueue.Transport.RelationalDatabase` with exactly one member: `bool SkipRetry { get; }`.
- `dotnet build "Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj" -c Release --nologo` succeeds (Release config = `TreatWarningsAsErrors` + XML doc — must have XML doc on the interface AND the `SkipRetry` property or the build breaks).
- Layering grep returns empty: `grep -rn "Microsoft\.Data\.SqlClient\|using Npgsql" Source/DotNetWorkQueue.Transport.RelationalDatabase/IRetrySkippable.cs` → no output.

## Verification

Run all three task-level verification commands plus the phase-wide layering invariant from CONTEXT-2 Hard Rules. Each must produce the listed output.

```bash
# Task 1
test ! -f Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs && echo "PoC deleted"

# Task 2 — additive change does not break consumers
dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Debug --nologo
# expected: Build succeeded. 0 Error(s)

dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" -c Debug --filter "FullyQualifiedName~SendMessageCommandTests" --nologo
# expected: 1 test pass, 0 fail (Create_Default)

# Task 3 — Release config exercises TreatWarningsAsErrors + XML doc generation
dotnet build "Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj" -c Release --nologo
# expected: Build succeeded. 0 Error(s), 0 Warning(s)

# Cross-cutting layering invariant (CONTEXT-2 Hard Rules)
grep -rn "Microsoft\.Data\.SqlClient\|using Npgsql" Source/DotNetWorkQueue.Transport.RelationalDatabase/ --include="*.cs" --include="*.csproj"
# expected: no matches (exit code 1)

# No regressions on existing SqlServer + PostgreSQL retry decorator tests
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --filter "FullyQualifiedName~RetryCommandHandlerOutputDecoratorTests" --nologo
# expected: 3 tests pass (Handle_WhenRegistryDisposed_FallsThroughToDecorated, Handle_WhenPipelineRegistered_ExecutesThroughPipeline, Handle_WhenNoPipelineRegistered_CallsDecoratedDirectly)

dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Debug --filter "FullyQualifiedName~RetryCommandHandlerOutputDecoratorTests" --nologo
# expected: 3 tests pass (mirror of SqlServer baseline)
```
