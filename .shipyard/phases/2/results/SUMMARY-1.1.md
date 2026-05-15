# Build Summary: Plan 1.1

## Status: complete

## Tasks Completed

- Task 1: Delete Phase 1 throwaway PoC — complete — `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs` removed (191 lines)
- Task 2: Add `ExternalTransaction` property to `SendMessageCommand` — complete — `Source/DotNetWorkQueue.Transport.Shared/Basic/Command/SendMessageCommand.cs` (+ `using System.Data.Common;` and `public DbTransaction ExternalTransaction { get; init; }` with XML doc)
- Task 3: Create `IRetrySkippable` marker interface — complete — `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRetrySkippable.cs` (NEW, 40 lines; LGPL header + interface with one `bool SkipRetry { get; }` member, both XML-doc'd)

## Commits

| SHA | Task | Subject |
|-----|------|---------|
| `49e587bf` | 1 | `shipyard(phase-2): delete throwaway Phase 1 polly bypass PoC` |
| `8a86d5c2` | 2 | `shipyard(phase-2): add ExternalTransaction property to SendMessageCommand` |
| `cb6827a8` | 3 | `shipyard(phase-2): add IRetrySkippable marker interface` |

## Files Modified

- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs` — DELETED
- `Source/DotNetWorkQueue.Transport.Shared/Basic/Command/SendMessageCommand.cs` — MODIFIED (constructor, MessageToSend, MessageData untouched; only the new property + using added)
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRetrySkippable.cs` — NEW

## Decisions Made

- None. All 3 edits followed plan wording verbatim.

## Issues Encountered

- Pre-existing NU1902 warnings on `OpenTelemetry.Api 1.15.2` (GHSA-g94r-2vxg-569j, moderate severity) surface on every transitive reference. NOT caused by Plan 1.1 and NOT a `TreatWarningsAsErrors` trigger (NU-prefixed audit warnings, not C# compiler warnings). Out of scope.
- Git emitted standard LF→CRLF warning on Task 3 file creation in WSL — cosmetic, `.gitattributes` normalizes the committed bytes to LF.

## Verification Results

| Gate | Result |
|------|--------|
| PoC deletion (`test ! -f`) | exit 0 |
| `DotNetWorkQueueNoTests.sln` Debug | 0 errors |
| `Transport.Shared` Release | 0 errors (TreatWarningsAsErrors + XML doc on) |
| `Transport.RelationalDatabase` Release | 0 errors (TreatWarningsAsErrors + XML doc on) |
| Layering grep `Microsoft.Data.SqlClient\|using Npgsql` on new file/dir | no matches |
| `SendMessageCommandTests.Create_Default` | 1/1 pass |
| SqlServer `RetryCommandHandlerOutputDecoratorTests` baseline | 3/3 pass |
| PostgreSQL `RetryCommandHandlerOutputDecoratorTests` baseline | 3/3 pass |

## Wave 2 Hand-off

- `RelationalSendMessageCommand` (Wave 2 PLAN-2.2) can subclass `SendMessageCommand` and set `ExternalTransaction` via the new `init` property.
- The subclass can implement `IRetrySkippable` directly (interface now exists in `Transport.RelationalDatabase`).
- SqlServer / PostgreSQL transports already reference `Transport.RelationalDatabase` — no `<ProjectReference>` additions needed for the Wave 3 decorator branches.
