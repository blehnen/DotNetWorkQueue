---
phase: quick-wins
plan: 1
wave: 1
dependencies: []
must_haves:
  - Delete ObjectPool dead code (3 files, zero references)
  - Clean build after deletion
files_touched:
  - Source/DotNetWorkQueue/Cache/ObjectPool.cs
  - Source/DotNetWorkQueue/IObjectPool.cs
  - Source/DotNetWorkQueue/IPooledObject.cs
tdd: false
risk: low
---

# Plan 1.1 -- ObjectPool Dead Code Deletion

## Context

`ObjectPool.cs`, `IObjectPool.cs`, and `IPooledObject.cs` have zero references outside their own files. No DI registration, no transport usage, no test coverage. They are dead code that inflates the codebase and drags down coverage metrics.

The csproj (`Source/DotNetWorkQueue/DotNetWorkQueue.csproj`) uses SDK-style wildcard globbing -- there are no explicit `<Compile Include>` entries for these files. Deleting the files is sufficient; no csproj edit needed.

## Tasks

<task id="1" files="Source/DotNetWorkQueue/Cache/ObjectPool.cs, Source/DotNetWorkQueue/IObjectPool.cs, Source/DotNetWorkQueue/IPooledObject.cs" tdd="false">
  <action>Delete the following three files:
- `Source/DotNetWorkQueue/Cache/ObjectPool.cs`
- `Source/DotNetWorkQueue/IObjectPool.cs`
- `Source/DotNetWorkQueue/IPooledObject.cs`

These files contain the `ObjectPool<T>` class, `IObjectPool<T>` interface, and `IPooledObject<T>` interface. All three are unreferenced dead code. No csproj changes are needed since SDK-style projects use wildcard compilation.</action>
  <verify>cd /mnt/f/git/dotnetworkqueue && dotnet build "Source/DotNetWorkQueue/DotNetWorkQueue.csproj" -c Debug --no-restore 2>&1 | tail -5</verify>
  <done>All three files are deleted. `dotnet build` of DotNetWorkQueue.csproj succeeds with 0 errors and 0 warnings. No remaining references to ObjectPool, IObjectPool, or IPooledObject exist in the codebase (verify with `grep -r "ObjectPool\|IPooledObject" Source/ --include="*.cs"` returning empty).</done>
</task>
