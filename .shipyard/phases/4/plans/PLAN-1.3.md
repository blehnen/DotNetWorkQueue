---
phase: 4-ci-docs-version
plan: "1.3"
wave: 1
dependencies: []
must_haves:
  - Remove net48/netstandard2.0 references from README.md
  - Remove dynamic LINQ content while preserving compiled LINQ documentation
  - Remove JpLabs.DynamicCode from third-party libraries
  - Remove AppDomain sandbox security note
  - Update CLAUDE.md to reflect net10.0/net8.0 only
files_touched:
  - README.md
  - CLAUDE.md
tdd: false
---

# Plan 1.3 -- Documentation Updates (README + CLAUDE.md)

## Context

README.md and CLAUDE.md still reference net48, netstandard2.0, dynamic LINQ, and JpLabs.DynamicCode. The README's LINQ section (lines 81-130) interleaves dynamic LINQ content with compiled LINQ documentation -- it cannot be deleted wholesale. Careful surgical editing is needed.

## Tasks

<task id="1" files="README.md" tdd="false">
  <action>Edit README.md with the following changes. Line numbers reference the current file.

**Line 8** -- Update targets:
- FROM: `Targets .NET 4.8, .NET 8.0, .NET 10.0, and .NET Standard 2.0.`
- TO: `Targets .NET 8.0 and .NET 10.0.`

**Line 12** -- Remove dynamic mention from high-level features:
- FROM: `- Queue / process LINQ statements (compiled or dynamic, expressed as a string)`
- TO: `- Queue / process compiled LINQ expressions`

**Lines 61-67** -- Delete the entire "Differences Between Versions" section:
```
## Differences Between Versions

.NET Standard 2.0, 8.0, and 10.0 are missing the following features compared to the full framework version:

- No support for dynamic LINQ statements

---
```
Delete all 7 lines (61-67 inclusive, including the `---` separator).

**Lines 85-86** -- Edit the producer note to remove dynamic LINQ references:
- FROM: `> **Note:** It is possible for a producer to queue work that a consumer cannot process. In order for a consumer to execute a LINQ statement, all types must be resolvable. For dynamic statements, it is also possible to queue work that doesn't compile due to syntax errors — this won't be discovered until the consumer dequeues the work.`
- TO: `> **Note:** It is possible for a producer to queue work that a consumer cannot process. In order for a consumer to execute a LINQ expression, all referenced types must be resolvable.`

**Lines 87-95** -- Delete the "Producer" subsection header and the dynamic LINQ casting note + code block. These 9 lines:
```
### Producer

> **Note:** When passing `message` or `workerNotification` as arguments to dynamic LINQ, you must cast them, as the internal compiler treats them as `object`. This is not necessary when using standard LINQ expressions.

\```csharp
// Cast types when using dynamic LINQ:
(IReceivedMessage<MessageExpression>) message
(IWorkerNotification) workerNotification
\```
```
Delete all of them. The sample link on the next line (SQLiteProducerLinq) stays -- it is a compiled LINQ example.

**Lines 96-107** -- Delete the "Dynamic Arguments" section. These lines:
```
Dynamic arguments like `Guid` and `int` cannot be passed directly — they must be embedded as string literals and parsed using built-in .NET methods:

\```csharp
var id = Guid.NewGuid();
var runTime = 200;
$"(message, workerNotification) => StandardTesting.Run(new Guid(\"{id}\"), int.Parse(\"{runTime}\"))"
\```

This produces a LINQ expression that can be compiled and executed by the consumer, provided it can resolve all referenced types.
```
Delete all of them.

**Lines 109-114** -- Edit the Consumer subsection. Remove the assembly resolver note (only relevant to dynamic LINQ with runtime compilation). Keep the consumer sample link. Change from:
```
### Consumer

The consumer is generic and can process any LINQ expression, but it must be able to resolve all types the expression references. You may need to wire up an assembly resolver if your DLLs cannot be located automatically.

- [AppDomain.AssemblyResolve (MSDN)](https://msdn.microsoft.com/en-us/library/system.appdomain.assemblyresolve(v=vs.110).aspx)
- [SQLiteConsumerLinq/Program.cs](...)
```
TO:
```
### Consumer

The consumer is generic and can process any LINQ expression, but it must be able to resolve all types the expression references.

- [SQLiteConsumerLinq/Program.cs](...)
```

**Lines 116-129** -- Delete the entire "Security Considerations" subsection. This section is about dynamic LINQ sandboxing and AppDomain, which no longer applies:
```
### Security Considerations

No sandboxing or checking for risky commands is performed. For example, the following statement will cause the consumer host to exit:

\```csharp
"(message, workerNotification) => Environment.Exit(0)"
\```

If configuration files define dynamic LINQ statements, or if you cannot fully trust the producer, consider running the consumer in an application domain sandbox. Without that, the only protection against destructive commands is O/S user permissions:

\```csharp
"(message, workerNotification) => System.IO.Directory.Delete(@\"C:\\Windows\\\", true)"
\```
```
Delete all 14 lines.

**Line 187** -- Remove JpLabs.DynamicCode from custom libraries:
- FROM: `Custom libraries in \`/Lib\`: [Schyntax](...), [Aq.ExpressionJsonSerializer](...), [JpLabs.DynamicCode](...)`
- TO: `Custom libraries in \`/Lib\`: [Schyntax](https://github.com/blehnen/cs-schyntax), [Aq.ExpressionJsonSerializer](https://github.com/blehnen/expression-json-serializer)`
</action>
  <verify>cd /mnt/f/git/dotnetworkqueue && grep -c "dynamic LINQ\|JpLabs\|DynamicCode\|net48\|netstandard2.0\|AppDomain.AssemblyResolve\|application domain sandbox" README.md</verify>
  <done>`grep -c "dynamic LINQ\|JpLabs\|DynamicCode\|net48\|netstandard2.0\|AppDomain.AssemblyResolve\|application domain sandbox" README.md` returns 0. The LINQ Expressions section still documents compiled LINQ with the producer sample link and consumer sample link. The targets line says "net8.0 and net10.0" only. The third-party libraries section lists only Schyntax and Aq.ExpressionJsonSerializer.</done>
</task>

<task id="2" files="CLAUDE.md" tdd="false">
  <action>Edit CLAUDE.md with the following changes:

**Line 5 (Project Overview)**: Change targets from:
- `Targets .NET 10.0, .NET 8.0, .NET Framework 4.8, and .NET Standard 2.0.`
- TO: `Targets .NET 10.0 and .NET 8.0.`

**Line 43 (test commands)**: Remove the `AppMetrics.Tests` line:
```
dotnet test "Source\DotNetWorkQueue.AppMetrics.Tests\DotNetWorkQueue.AppMetrics.Tests.csproj"
```
This project does not exist (confirmed in Phase 2 research).

**Lines 56-57 (net48 test commands)**: The GitHub Actions comment block should be updated. Change:
```
# GitHub Actions (.github/workflows/ci.yml) runs net48 unit tests only for .NET Framework compatibility validation.
```
TO:
```
# GitHub Actions (.github/workflows/ci.yml) runs net10.0 unit tests on ubuntu-latest for CI validation.
```

**Conventions section**: Find `NETFULL` reference in the multi-targeting note. Change:
- FROM: `Projects use conditional compilation: \`NETFULL\` for .NET 4.8-specific code (dynamic LINQ, SoapFormatter), \`NETSTANDARD2_0\` for .NET Standard paths.`
- TO: `Projects target net10.0 and net8.0. Legacy conditional compilation symbols (NETFULL, NETSTANDARD2_0) have been removed.`

**Key Dependencies section**: If there is a JpLabs.DynamicCode reference, remove it. Check for: `JpLabs.DynamicCode (dynamic lambdas)` in the custom libraries line and remove it, keeping Schyntax and Aq.ExpressionJsonSerializer.

Commit README.md and CLAUDE.md together with message "shipyard(phase-4): update README and CLAUDE.md for net48/dynamic LINQ removal (issue #101)".</action>
  <verify>cd /mnt/f/git/dotnetworkqueue && grep -c "net48\|netstandard2.0\|NETFULL.*4.8\|AppMetrics.Tests\|DynamicCode\|dynamic LINQ" CLAUDE.md</verify>
  <done>`grep -c` returns 0 for all removed terms. CLAUDE.md says "net10.0 and net8.0" only. No AppMetrics.Tests reference. Conventions note says legacy symbols removed. README and CLAUDE.md committed together.</done>
</task>

## Builder Notes

- README editing is the most delicate task. The LINQ section (lines 81-130) mixes dynamic and compiled content. The goal is to remove all dynamic-specific content while keeping the compiled LINQ documentation flow intact.
- After all the deletions in README, verify the document reads coherently: the LINQ Expressions section should flow from intro note -> producer sample link -> consumer description -> consumer sample link -> Job Scheduler section.
- The "Differences Between Versions" section (lines 61-67) should be deleted entirely since there are no longer multiple framework targets with different feature sets.
- For CLAUDE.md, also check the "Lessons Learned" section for any net48-specific entries. The entry about `#if NETFULL` guards is still useful as historical context -- do NOT delete lessons learned entries.
