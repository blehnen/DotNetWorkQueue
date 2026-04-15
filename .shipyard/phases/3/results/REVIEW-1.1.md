# Review: Plan 1.1

## Verdict: PASS

## Findings

### Critical
- None.

### Minor
- **`AssemblyInit.cs` is missing `[assembly: DoNotParallelize]` relative to the mirror project** — the Memory.Integration.Tests `AssemblyInit.cs` does NOT have `[assembly: DoNotParallelize]`; the attribute was added per spec in the new file (line 4, outside the namespace block). This is correct per the plan. No issue.
- **`NextPort` increments the counter before returning it** — `Interlocked.Increment(ref counter)` returns the new (incremented) value, meaning the first call returns `PortBase + 1`, not `PortBase`. The PLAN spec reproduces exactly this code, so the implementation matches the spec. However, Wave-2 plans initialize `_portCounter = TestHelpers.EndToEndPortBase` and call `NextPort` expecting the first port to be `50001`, `55001`, `60001` respectively. This is behaviorally consistent across all three Wave-2 plans and TestHelpers, so it is not a defect — just worth noting that port base constants are exclusive lower-bounds, not the first allocated port.

### Positive
- `Directory.Packages.props` line 16: `<PackageVersion Include="DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler" Version="0.4.0" />` — correct CPM pin, no Version attribute in the csproj PackageReference.
- csproj: `<TargetFrameworks>net10.0</TargetFrameworks>` single-target matches the corrected spec and the Memory mirror. All 8 bare PackageReferences present. All 3 ProjectReferences present (DotNetWorkQueue, Transport.Memory, IntegrationTests.Shared). No `TreatWarningsAsErrors` override.
- `DotNetWorkQueueNoTests.sln`: 0 matches for the new project name — correctly excluded.
- `DotNetWorkQueue.sln`: Project entry at line 20 with SDK-style type GUID `{9A19103F-16F7-4668-BE54-9A1E7A4F7556}`. Fresh GUID `{8327C430-4C67-4A09-BC25-47875DEC068B}` appears exactly 13 times (1 Project line + 12 configuration platform lines) — no collision with any other project.
- Full 12-line `GlobalSection(ProjectConfigurationPlatforms)` block present (Debug/Release x Any CPU/x64/x86, all mapped to Any CPU) — matches the Memory.Integration.Tests pattern.
- `AssemblyInit.cs`: `[assembly: DoNotParallelize]` outside the namespace block (line 4), namespace `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests`, calls `MsTestHelper.ClearSynchronizationContext()` — exact spec match. No inline LGPL header (Option A).
- `TestHelpers.cs`: `internal static class`, platform-aware `BeaconInterface` using `RuntimeInformation.IsOSPlatform(OSPlatform.Linux)` with correct `string.Empty` / `"loopback"` values, all three port-base constants at correct values (50000/55000/60000), `NextPort(ref int counter)` using `Interlocked.Increment`. No inline LGPL header. Block-scoped namespace with braces — matches house style.
- Wave-2 compatibility confirmed: PLAN-2.1 references `TestHelpers.BeaconInterface`, `TestHelpers.NextPort(ref _portCounter)`, `TestHelpers.EndToEndPortBase`; PLAN-2.2 references `TestHelpers.ConcurrencyPortBase`; PLAN-2.3 references `TestHelpers.NodeDiscoveryPortBase` and `TestHelpers.BeaconInterface` — all names, visibility, and signatures match exactly what is implemented.
- SUMMARY-1.1.md reports `dotnet build` on the csproj returned `Build succeeded. 0 Warning(s). 0 Error(s)` with net10.0 output confirmed.
