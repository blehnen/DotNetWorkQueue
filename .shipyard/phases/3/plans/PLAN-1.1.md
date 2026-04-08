---
phase: 3-linq-integration-tests
plan: 1
wave: 1
dependencies: []
must_haves:
  - Remove all #if NETFULL blocks from SqlServer, PostgreSQL, and SQLite Linq integration test .cs files
  - Remove net48 target and conditional blocks from SqlServer, PostgreSQL, and SQLite Linq integration test csproj files
  - All three projects build successfully targeting net10.0 only
files_touched:
  - Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/**/*.cs (18 files)
  - Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.csproj
  - Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/**/*.cs (18 files)
  - Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.csproj
  - Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/**/*.cs (17 files)
  - Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.csproj
tdd: false
risk: low
---

# Phase 3 - PLAN 1.1: SqlServer, PostgreSQL, SQLite Linq Integration Tests

Remove net48/NETFULL from the three SQL-family Linq integration test projects. All three follow the same pattern: delete `#if NETFULL` ... `#endif` blocks from .cs files (these contain `LinqMethodTypes.Dynamic` test method overloads), and strip net48 targets plus conditional PropertyGroup/ItemGroup blocks from the csproj.

**Builder note:** Use Perl regex for bulk .cs edits. Write output to /tmp then cp back (Perl `-i` fails on WSL cross-mount). Pattern: `perl -0777 -pe 's/\n*#if NETFULL\b.*?#endif[^\n]*//gs'`

---

<task id="1" files="Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/**/*.cs, Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.csproj" tdd="false">
  <action>
  Remove net48/NETFULL from the SqlServer Linq integration test project:

  1. **18 .cs files** -- delete all `#if NETFULL` ... `#endif` blocks (including any leading blank lines before `#if`). Files are in ConsumerMethod/ (8), ConsumerMethodAsync/ (4), ProducerMethod/ (5), JobScheduler/ (1 -- JobSchedulerTests.cs).

  2. **csproj** -- make these changes:
     - Change `<TargetFrameworks>net10.0;net48</TargetFrameworks>` to `<TargetFramework>net10.0</TargetFramework>`
     - Remove the two `<PropertyGroup Condition="...net48...">` blocks (Debug and Release, lines 6-14)
     - Remove the `<ItemGroup Condition=" '$(TargetFramework)' == 'net48' ">` block (lines 36-39, contains Microsoft.CSharp and System.Net.Http references)
  </action>
  <verify>
  cd /mnt/f/git/dotnetworkqueue && \
  grep -r "NETFULL\|net48" "Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/" --include="*.cs" --include="*.csproj" | grep -v "/obj/" && echo "FAIL: residual references found" || echo "PASS: no residual references" && \
  dotnet build "Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.csproj" -c Debug
  </verify>
  <done>Zero matches for NETFULL or net48 in non-obj files. Project builds successfully on net10.0 with no errors.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/**/*.cs, Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.csproj" tdd="false">
  <action>
  Remove net48/NETFULL from the PostgreSQL Linq integration test project:

  1. **18 .cs files** -- delete all `#if NETFULL` ... `#endif` blocks. Files are in ConsumerMethod/ (8), ConsumerMethodAsync/ (4), ProducerMethod/ (5), JobScheduler/ (1 -- JobSchedulerTests.cs).

  2. **csproj** -- make these changes:
     - Change `<TargetFrameworks>net10.0;net48</TargetFrameworks>` to `<TargetFramework>net10.0</TargetFramework>`
     - Remove the two `<PropertyGroup Condition="...net48...">` blocks (Debug and Release, lines 6-14)
     - Remove the `<ItemGroup Condition=" '$(TargetFramework)' == 'net48' ">` block (lines 35-38, contains Microsoft.CSharp reference)
  </action>
  <verify>
  cd /mnt/f/git/dotnetworkqueue && \
  grep -r "NETFULL\|net48" "Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/" --include="*.cs" --include="*.csproj" | grep -v "/obj/" && echo "FAIL: residual references found" || echo "PASS: no residual references" && \
  dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.csproj" -c Debug
  </verify>
  <done>Zero matches for NETFULL or net48 in non-obj files. Project builds successfully on net10.0 with no errors.</done>
</task>

<task id="3" files="Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/**/*.cs, Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.csproj" tdd="false">
  <action>
  Remove net48/NETFULL from the SQLite Linq integration test project:

  1. **17 .cs files** -- delete all `#if NETFULL` ... `#endif` blocks. Files are in ConsumerMethod/ (8), ConsumerMethodAsync/ (4), ProducerMethod/ (5). No JobScheduler files contain NETFULL in this project.

  2. **csproj** -- make these changes:
     - Change `<TargetFrameworks>net10.0;net48</TargetFrameworks>` to `<TargetFramework>net10.0</TargetFramework>`
     - Remove the two `<PropertyGroup Condition="...net48...">` blocks (Debug and Release, lines 6-12)
     - Remove the `<ItemGroup Condition=" '$(TargetFramework)' == 'net48' ">` block (lines 14-16, contains Microsoft.CSharp reference)
  </action>
  <verify>
  cd /mnt/f/git/dotnetworkqueue && \
  grep -r "NETFULL\|net48" "Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/" --include="*.cs" --include="*.csproj" | grep -v "/obj/" && echo "FAIL: residual references found" || echo "PASS: no residual references" && \
  dotnet build "Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.csproj" -c Debug
  </verify>
  <done>Zero matches for NETFULL or net48 in non-obj files. Project builds successfully on net10.0 with no errors.</done>
</task>
