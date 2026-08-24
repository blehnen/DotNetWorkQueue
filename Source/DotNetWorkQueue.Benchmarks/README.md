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
| raw table, DNWQ shape + pooled connection per send | *minus the row above* = connection lifecycle |
| pooled connection open + close, no work | connection acquisition alone |
| DatabaseExists check | the existence check the send path runs per message |
| serialize body + headers | the real configured serializer, via the interceptor graph |
| core producer pipeline (Memory transport) | the pipeline with no serialization and no SQL |
| DotNetWorkQueue SQLite send (end to end) | the whole thing |

`MemoryDiagnoser` is on, so the allocation columns are as informative as the timings — allocation
is often where hidden work shows up that latency alone hides.

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

## ⚠️ Unresolved: connection open/close reads ~30x high in this project

`PooledConnection_OpenClose` reports **~20 ms** per pooled open+close. A standalone project doing
the identical loop, on the same machine with the same `System.Data.SQLite` 1.0.119, reports
**~0.6 ms**. Until that is explained, **do not trust any figure from this harness that involves
connection acquisition** — which is most of them.

Ruled out so far:
- **BenchmarkDotNet.** `--selftest` runs the same loop as a plain `for` in this project's own
  process, outside BDN, and reproduces ~19.4 ms.
- **Working directory.** Same binary run from `/mnt/f` (a Windows drive mount) and from `/tmp`
  (Linux ext4): 19.66 ms and 19.72 ms.
- **The database path.** The database is on `/tmp` in both cases.
- **`MemoryDiagnoser` / inter-iteration GC** defeating the connection pool: removing the
  attribute changes nothing.
- **Shared `[GlobalSetup]`** building unrelated fixtures in the measured process: setup is now
  scoped per benchmark, which changed nothing (but was worth fixing anyway).
- **Native interop and runtime config.** Both projects ship the same `runtimes/*/native` layout
  and byte-identical `runtimeconfig.json`.

The remaining difference between the two projects is that this one references
`DotNetWorkQueue` and `DotNetWorkQueue.Transport.SQLite`. The next diagnostic is to add those
references to the standalone project and see whether it slows down — if it does, something in
the library's load affects `System.Data.SQLite` behaviour, which would be worth knowing
independently of benchmarking.

Note that the allocation column is the corroborating signal: ~103 KB per open/close is a full
connection construction, not a pool reuse.
