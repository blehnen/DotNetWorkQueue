# DotNetWorkQueue.Benchmarks

Micro-benchmarks used to attribute cost inside the library. Not shipped, not part of
`DotNetWorkQueueNoTests.sln`, and not run by CI.

## Running

Benchmarks require a Release build — BenchmarkDotNet refuses to run otherwise, and a Debug
measurement of this library is meaningless.

```bash
# everything
dotnet run -c Release --project Source/DotNetWorkQueue.Benchmarks -- --filter '*'

# one suite, faster and still statistically meaningful
dotnet run -c Release --project Source/DotNetWorkQueue.Benchmarks -- --job short --filter '*SendPath*'

# smoke test only - runs each benchmark once, proves nothing about timing
dotnet run -c Release --project Source/DotNetWorkQueue.Benchmarks -- --job dry --filter '*'
```

## SendPathBenchmarks

Decomposes a single SQLite `Send`. Read it as a ladder — the difference between two adjacent
rungs is the cost of what the upper rung adds:

| benchmark | isolates |
|---|---|
| raw table, 1 statement | the floor: a hand-written narrow outbox table |
| raw table, DNWQ statement shape (held connection) | *minus the row above* = the write transaction, i.e. the "critical section" |
| raw table, DNWQ shape using `INSERT … RETURNING` | *minus the row above* = the separate `last_insert_rowid()` round trip |
| raw table, DNWQ shape with commands reused | *minus the held-connection row* = building commands and re-preparing statements |
| raw table, DNWQ shape + pooled connection per send | *minus the row above* = connection lifecycle |
| pooled connection open + close, no work | connection acquisition alone |
| DatabaseExists check | the existence check the send path runs per message |
| serialize body + headers | the real configured serializer, via the interceptor graph |
| core producer pipeline (Memory transport) | the pipeline with no serialization and no SQL |
| DotNetWorkQueue SQLite send (end to end) | the whole thing |

`MemoryDiagnoser` is on, so the allocation columns are as informative as the timings — allocation
is often where hidden work shows up that latency alone hides.

## Findings

Measured on net10, WSL2/ext4, ShortRun, `Synchronous=NORMAL`. Absolute values are local; the
relationships between rows are the durable result.

| finding | evidence |
|---|---|
| Parsing the connection string was the single largest allocation on the send path | `DatabaseExists` alone was 7,238 ns / 20.2 KB. A send parsed twice — once for the existence check, once when creating a connection — so caching the parse took the whole send from 101.0 to 81.5 us and from 63.2 KB to 22.8 KB |
| The separate `SELECT last_insert_rowid()` round trip is **not** worth removing | `INSERT … RETURNING` measured 43,770 ns against 43,642 ns for the round trip — inside the noise band. It saves 896 B and nothing else. Do not re-derive this |
| Statement preparation is real, and roughly 2.4 us per statement | Reusing the command objects, which is what lets System.Data.SQLite keep a prepared statement, took the three-statement shape from 43,642 to 36,445 ns and from 5,600 to 2,432 B |

## Why this exists

A scratch decomposition in August 2026 found that the write transaction was ~3% of the gap
between DotNetWorkQueue and a hand-written table, while connection lifecycle was ~58% and
serialization ~0.2%. That falsified the assumption the performance work had been scoped around —
that the fix was to shorten the critical section.

That scratch harness had a ±25% run-to-run band and derived its largest remaining component by
subtraction. This project exists to replace estimates with numbers precise enough to act on, and
to give any optimisation a before/after it cannot argue with.

## Gotchas

**`synchronous` is a per-connection pragma.** Unlike `journal_mode` it is not persisted in the
database file, so every connection string here sets it explicitly. Omitting it silently reverts a
connection to `FULL`, which buys an fsync the comparison rows do not pay — an earlier version of
this comparison reported a number that was entirely that mistake.

**Numbers are platform-specific.** These were developed on WSL2/ext4. File handle and fsync
behaviour differ on Windows/NTFS, so treat ratios between rows as the durable result and absolute
values as local.

**`SendPathBenchmarks` reflects into the send chain** to obtain the live serializer, so it
measures the configured instances rather than a hand-built approximation that could drift. If the
decorator layout or private field names change, it throws with an explanatory message rather than
silently measuring the wrong thing.

## ⚠️ Do not run these from a WSL drive mount

**Resolved trap, guarded at runtime.** The repository lives on `/mnt/f`, a Windows drive mounted
into WSL. .NET memory-maps assemblies, and page faults against drvfs/9p are slow enough to impose
a uniform floor on every operation in the process. The identical binaries measured:

| assemblies loaded from | pooled connection open+close |
|---|---|
| `/mnt/f` (Windows drive mount) | **19,798 us** |
| `/tmp` (native ext4) | **569 us** |

A 35x error, uniform across pooled and unpooled variants alike — which is exactly why it looked
like a real result rather than an artifact. It was found only by copying the binaries to a native
path; changing the *working* directory does nothing, because it is the assemblies' location that
matters, not the process's cwd.

`Program.Main` now prints a red banner and the correct command whenever it detects assemblies
under `/mnt/`. To get valid numbers on WSL:

```bash
dotnet build -c Release Source/DotNetWorkQueue.Benchmarks
cp -r Source/DotNetWorkQueue.Benchmarks/bin/Release/net10.0/. /tmp/bench/
cd /tmp/bench && dotnet ./DotNetWorkQueue.Benchmarks.dll --job short --filter '*' --inProcess
```

`--inProcess` is needed when running from a copied output directory, because BenchmarkDotNet's
default toolchain wants to generate and compile a project.

This does not affect Windows, where the repository is on a real NTFS volume with no translation
layer. It also did not affect the earlier scratch measurements, whose build output happened to
live under `/tmp`.

