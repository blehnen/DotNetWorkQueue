---
phase: 3-linq-integration-tests
plan: 2
wave: 1
dependencies: []
must_haves:
  - Remove all #if NETFULL blocks from Redis, LiteDB, and Memory Linq integration test .cs files
  - Remove net48 target and conditional blocks from Redis, LiteDB, and Memory Linq integration test csproj files
  - All three projects build successfully targeting net10.0 only
files_touched:
  - Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/**/*.cs (15 files)
  - Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.csproj
  - Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/**/*.cs (17 files)
  - Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.csproj
  - Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/**/*.cs (12 files)
  - Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj
tdd: false
risk: low
---

# Phase 3 - PLAN 1.2: Redis, LiteDB, Memory Linq Integration Tests

Remove net48/NETFULL from the three non-SQL Linq integration test projects. Same pattern as PLAN-1.1: delete `#if NETFULL` ... `#endif` blocks from .cs files, strip net48 targets and conditional blocks from csproj files.

**Builder note:** Use Perl regex for bulk .cs edits. Write output to /tmp then cp back (Perl `-i` fails on WSL cross-mount). Pattern: `perl -0777 -pe 's/\n*#if NETFULL\b.*?#endif[^\n]*//gs'`

---

<task id="1" files="Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/**/*.cs, Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.csproj" tdd="false">
  <action>
  Remove net48/NETFULL from the Redis Linq integration test project:

  1. **15 .cs files** -- delete all `#if NETFULL` ... `#endif` blocks. Files are in ConsumerMethod/ (8), ConsumerMethodAsync/ (4), ProducerMethod/ (2 -- SimpleMethodProducer.cs, SimpleMethodProducerAsync.cs), JobScheduler/ (1 -- JobSchedulerTests.cs). Note: Redis has no batch producer methods and no JobSchedulerMultipleTests.

  2. **csproj** -- make these changes:
     - Change `<TargetFrameworks>net10.0;net48</TargetFrameworks>` to `<TargetFramework>net10.0</TargetFramework>`
     - Remove the two `<PropertyGroup Condition="...net48...">` blocks (Debug and Release, lines 6-14)
     - Note: this csproj has NO net48-conditional ItemGroup (no Microsoft.CSharp/System.Net.Http references to remove)
  </action>
  <verify>
  cd /mnt/f/git/dotnetworkqueue && \
  grep -r "NETFULL\|net48" "Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/" --include="*.cs" --include="*.csproj" | grep -v "/obj/" && echo "FAIL: residual references found" || echo "PASS: no residual references" && \
  dotnet build "Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.csproj" -c Debug
  </verify>
  <done>Zero matches for NETFULL or net48 in non-obj files. Project builds successfully on net10.0 with no errors.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/**/*.cs, Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.csproj" tdd="false">
  <action>
  Remove net48/NETFULL from the LiteDB Linq integration test project:

  1. **17 .cs files** -- delete all `#if NETFULL` ... `#endif` blocks. Files are in ConsumerMethod/ (8), ConsumerMethodAsync/ (4), ProducerMethod/ (5). No JobScheduler files contain NETFULL in this project.

  2. **csproj** (note filename casing: `DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.csproj` -- lowercase "b" in LiteDb) -- make these changes:
     - Change `<TargetFrameworks>net10.0;net48</TargetFrameworks>` to `<TargetFramework>net10.0</TargetFramework>`
     - Remove the two `<PropertyGroup Condition="...net48...">` blocks (Debug and Release, lines 6-12)
     - Remove the `<ItemGroup Condition=" '$(TargetFramework)' == 'net48' ">` block (lines 14-16, contains Microsoft.CSharp reference)
  </action>
  <verify>
  cd /mnt/f/git/dotnetworkqueue && \
  grep -r "NETFULL\|net48" "Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/" --include="*.cs" --include="*.csproj" | grep -v "/obj/" && echo "FAIL: residual references found" || echo "PASS: no residual references" && \
  dotnet build "Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.csproj" -c Debug
  </verify>
  <done>Zero matches for NETFULL or net48 in non-obj files. Project builds successfully on net10.0 with no errors.</done>
</task>

<task id="3" files="Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/**/*.cs, Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj" tdd="false">
  <action>
  Remove net48/NETFULL from the Memory Linq integration test project:

  1. **12 .cs files** -- delete all `#if NETFULL` ... `#endif` blocks. Files are in ConsumerMethod/ (2 -- ConsumerMethodMultipleDynamic.cs, SimpleMethodConsumer.cs), ConsumerMethodAsync/ (1 -- SimpleMethodConsumerAsync.cs), ProducerMethod/ (7 -- MultiMethodProducer.cs, SimpleMethodProducer.cs, SimpleMethodProducerAsync.cs, SimpleMethodProducerAsyncBatch.cs, SimpleMethodProducerBatch.cs, SimpleMethodProducerInterceptor.cs, SimpleMethodProducerDynamicListSend.cs), JobScheduler/ (2 -- JobSchedulerTests.cs, JobSchedulerInterceptorTests.cs).

  2. **csproj** -- make these changes:
     - Change `<TargetFrameworks>net10.0;net48</TargetFrameworks>` to `<TargetFramework>net10.0</TargetFramework>`
     - Remove the two `<PropertyGroup Condition="...net48...">` blocks (Debug and Release, lines 6-14)
     - Remove the `<ItemGroup Condition=" '$(TargetFramework)' == 'net48' ">` block (lines 32-35, contains Microsoft.CSharp and System.Net.Http references)
  </action>
  <verify>
  cd /mnt/f/git/dotnetworkqueue && \
  grep -r "NETFULL\|net48" "Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/" --include="*.cs" --include="*.csproj" | grep -v "/obj/" && echo "FAIL: residual references found" || echo "PASS: no residual references" && \
  dotnet build "Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj" -c Debug
  </verify>
  <done>Zero matches for NETFULL or net48 in non-obj files. Project builds successfully on net10.0 with no errors.</done>
</task>
