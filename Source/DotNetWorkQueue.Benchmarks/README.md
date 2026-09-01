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
| DotNetWorkQueue SQLite batch send (100 messages) | the batch path, reported per batch — divide by 100 for the per-message cost |

`MemoryDiagnoser` is on, so the allocation columns are as informative as the timings — allocation
is often where hidden work shows up that latency alone hides.

## ReceivePathBenchmarks

Decomposes a single SQLite dequeue. Every rung runs against an **empty** queue: a dequeue consumes
the row it finds, so a populated queue would make each iteration depend on what the last one left
behind, which BenchmarkDotNet cannot control. An empty poll still runs the whole script and simply
finds nothing, so it measures everything except materialising a row and the follow-up status
commands — and it is a real workload in its own right, since an idle consumer does exactly this.

`SQLiteCommand.Prepare()` is a no-op in System.Data.SQLite — statements compile lazily on
execution — so preparation cannot be timed on its own. The fresh-versus-reused pair measures it
where it actually happens.

| benchmark | isolates |
|---|---|
| generate the dequeue SQL | the `StringBuilder` script — rebuilt per dequeue before the cache, now built once per routes+clause |
| dequeue, fresh command | the receive path before any of the caching. Like the rows below it, this is a reduced shape: no transaction and no `BuildDequeueCommand`, so read the ratios between rows rather than the absolute numbers |
| dequeue, command from the factory cache | the same acquired through `DbFactory` and its per-connection command cache — *against the row above* = what the command cache delivers |
| dequeue, script and command both cached | what the transport does now — *against the row above* = what caching the generated SQL is worth |
| dequeue, command reused, parameters rebuilt | *minus the row above* = generating the script and recompiling its statements |
| dequeue, command reused | the same with parameters kept too — the ceiling, for comparison |

## Findings

Measured on net10, WSL2/ext4, ShortRun, `Synchronous=NORMAL`. Absolute values are local; the
relationships between rows are the durable result.

| finding | evidence |
|---|---|
| Parsing the connection string was the single largest allocation on the send path | `DatabaseExists` alone was 7,238 ns / 20.2 KB. A send parsed twice — once for the existence check, once when creating a connection — so caching the parse took the whole send from 101.0 to 81.5 us and from 63.2 KB to 22.8 KB |
| The separate `SELECT last_insert_rowid()` round trip is **not** worth removing | `INSERT … RETURNING` measured 43,770 ns against 43,642 ns for the round trip — inside the noise band. It saves 896 B and nothing else. Do not re-derive this |
| Statement preparation is real, and roughly 2.4 us per statement | Reusing the command objects, which is what lets System.Data.SQLite keep a prepared statement, took the three-statement shape from 43,642 to 36,445 ns and from 5,600 to 2,432 B |
| Statement compilation dominates a dequeue, far more than a send | An empty-queue dequeue costs 27,389 ns and 22,144 B with a fresh command and 4,458 ns and 552 B with the command reused - 6.1x and 40x. Only 536 ns of the saving is building the SQL; the rest is recompiling the script |
| Keeping the compiled statement is enough; the parameters do not matter | Reusing the command but rebuilding its parameters each time measured 4,458 ns against 4,230 ns for keeping them - 99% of the win. Callers can keep using CreateParameter |
| Command reuse pays off *less* in a batch, not more | 11.4% against 13.2% for a single send. A chunk recompiles one statement per message under default options, not several: the body insert is one multi-row statement for the whole chunk and `EnableStatusTable` is off. Do not re-derive this |

### Batching, and what command reuse is worth

Measured by disabling the command cache at `DbFactory.CreateCommand` and re-running, so both
columns are the same code on the same machine:

| rung | commands not reused | commands reused | change |
|---|---|---|---|
| single send | 82.79 us / 22.82 KB | 71.88 us / 20.19 KB | −13.2% |
| batch of 100 | 2,013.35 us / 1,843 KB | 1,784.36 us / 1,738 KB | −11.4% |
| …per message in that batch | 20.13 us | 17.84 us | −11.4% |

**Batching itself is the larger effect: 17.84 us per message against 71.88 us sending one at a
time, roughly 4x.**

**Command reuse does *not* pay off more in a batch, contrary to what was claimed when it was
built.** It was assumed a chunk would recompile several statements per message. With the default
options it recompiles exactly one — `EnableStatusTable` is off, and the body insert is a single
multi-row statement for the whole chunk since the bulk-insert work in 0.9.41 — so only the meta
data insert is per-message. 100 messages x 1 statement x ~2.4 us predicts ~240 us saved; 229 us
was measured. A single send recompiles three statements, which is why it gains the larger share.

Turn `EnableStatusTable` on and a batch recompiles two per message, so the gap would narrow.


### Caching the dequeue script

| rung | time | allocated |
|---|---|---|
| fresh command, script rebuilt (the original) | 29,854 ns | 22,144 B |
| command cached, script rebuilt | 7,170 ns | 7,496 B |
| **script and command both cached (now)** | **5,800 ns** | **648 B** |

Generating the script was **91% of everything a dequeue allocated** — 6,848 B of 7,496 B — and
caching it removes essentially all of it. End to end against the original: 5.1x on time and 34x
on allocation.

The script is keyed on the caller's clause and the routes, the only inputs to it that vary.
Parameter *values* are bound rather than written into the SQL, so the factory forms of
`GetUserClause` and `GetUserParameters` are still called on every dequeue and keep working — a
clause that changes simply produces a different key.

## CorePathBenchmarks

Decomposes the cost of a send that belongs to the core library rather than to a transport, so a
change to any of it moves every transport at once. The rungs are paired: the first of a pair is
what the library did, the second is what replaced it, both on the same input in the same process.

| benchmark | isolates |
|---|---|
| body: SerializeObject then UTF8.GetBytes (current) | what `JsonSerializer` does today |
| body: cached serializer, still via a string | *minus the row above* = constructing a Newtonsoft `JsonSerializer` from the settings on every call |
| body: cached serializer, direct to UTF8 bytes | writing straight to a stream instead of a string |
| body: cached serializer + pooled writer buffers | the writer's internal char buffers rented rather than allocated |
| body: JSON payload size (reference, not a candidate) | the floor: `SerializeObject` alone, with no byte conversion at all |
| headers: … | the same pair for the header dictionary |
| header: portable type name, uncached / cached | `Assembly.GetName()` per send against a per-type cache |
| validation: 14x Guard.NotNull, expression tree / compiler-supplied name | the argument validation a single send runs |

### Findings

| finding | evidence |
|---|---|
| Argument validation was the largest removable cost in the core library | `Guard` took its parameter name from an `Expression<Func<T>>`, so the compiler emitted tree-building code at every call site and ran it on every call, including the ones that pass. 605 ns and 2,152 B for the 14 calls a send makes, against 2.7 ns and nothing for the same checks with the name supplied by the compiler — 43 ns and 154 B per call. A send makes 14 and a message consumed makes roughly 19 |
| The `MessageBodyType` header was rebuilt from the assembly identity on every send | `Assembly.GetName()` parses the identity and allocates an `AssemblyName` for a value that is fixed per type: 205 ns and 520 B per message, against 2.9 ns and nothing from a per-type cache |
| Writing JSON straight to UTF-8 bytes allocates **more**, not less | 7.92 KB against 4.3 KB for the string round trip, and 25% slower. `StreamWriter` brings a 1 KB char buffer, an encoder and a byte buffer, all of which cost more than the intermediate string they avoid. Do not re-derive this |
| Caching the Newtonsoft `JsonSerializer` is worth almost nothing here | 4.08 KB against 4.3 KB, about 5%. `JsonConvert.SerializeObject` does construct one per call, but that construction is not where the money is |
| The two shipped serializers, for the opt-in choice | Newtonsoft costs 682 ns / 4,464 B to serialize a message whose body is a 256-byte string, and 768 ns / 4,552 B to read it back; `SystemTextJsonSerializer` costs 270 ns / 752 B and 407 ns / 1,272 B. About 7 KB less garbage per message round trip, which is why the opt-in exists |
| Serialization cannot be meaningfully improved while the serializer is Newtonsoft | `SerializeObject` **alone**, producing only a string and converting nothing, costs 539 ns and 3.91 KB for a message whose body is a 256-byte string (the serialized form is larger, since it carries the wrapper and the type name). That is the floor; the whole current rung is 616 ns and 4.3 KB. Everything above the floor is the output byte array. Reducing this means replacing the serializer, not tuning the call - which is what `SystemTextJsonSerializer` does |

Measured on net10, WSL2/ext4, ShortRun. End to end, removing the expression trees and caching the
type name took a SQLite send from 20,674 B to 17,721 B, a 100-message batch from 1,795 us to
1,600 us, and the transport-independent producer pipeline from 4,005 ns / 4,512 B to
2,839 ns / 3,088 B.

## LiteDbPathBenchmarks

Decomposes a LiteDb send, the same ladder shape as `SendPathBenchmarks`. LiteDb is the other
embedded single-file transport, so the SQLite findings are the obvious hypotheses — this exists to
test them rather than assume them, and the first result was that the obvious hypothesis was wrong.

| benchmark | isolates |
|---|---|
| raw LiteDB, 1 insert (held database) | the floor |
| raw LiteDB, DNWQ shape (held database) | *minus the row above* = the second collection and the transaction |
| raw LiteDB, DNWQ shape + database per send | *minus the row above* = the connection lifecycle, which is what a shared connection does per operation |
| LiteDatabase open + close, no work | construction alone, with nothing to checkpoint on dispose |
| existence check (parse + stat) | the check the send path runs per message; nothing is cached |
| DotNetWorkQueue LiteDb send, direct | the whole thing, as a caller experiences it |
| DotNetWorkQueue LiteDb send, shared | the same on a shared connection — *against the row above* = what the mode costs |
| DotNetWorkQueue LiteDb batch send (100) | reported per batch; divide by 100 and compare with the single send |

## LiteDbConcurrencyBenchmarks

Measures what a single-threaded ladder cannot see. Every rung sends the same fixed number of
messages and is reported per batch, so rows compare directly: **if more threads do not make the
batch faster, something is serializing them.** The raw rungs are controls — LiteDB does its own
locking, so they show what the storage engine allows before the transport is added.

## LiteDb findings

Measured on net10, WSL2/ext4, ShortRun. Direct connection unless stated.

| finding | evidence |
|---|---|
| A process-wide lock on every send was the ceiling, not the missing bulk-send path | `SendMessageCommandHandler` took a `static readonly object` on **every** message, though its stated purpose is the scheduled-job check-then-act. Four producer threads ran **1.35x slower** than one, and two queues with **separate database files** performed no better than a single queue (40.31 ms against 38.68 ms for 200 sends) — they were waiting on each other for no reason |
| Taking that lock only for job sends is worth 1.8x across queues | Two queues on four threads went from 40.31 ms to 21.87 ms for 200 sends, and from *slower* than single-threaded (1.40x) to faster (0.67x). An ordinary send is covered by the transaction and needs no lock at all |
| The two send handlers did not share their lock | Found while narrowing the change, and unrelated to performance: `SendMessageCommandHandler` and its async twin each held a `static` lock, so a `Send` and a `SendAsync` of the same scheduled job excluded others of their own kind but never each other, and both could insert. Pre-existing on master; fixed alongside this work |
| Narrowing the locks per database was tried, measured at nothing, and reverted | It looks like the obvious next step, and it is a trap. The benefit above comes entirely from *skipping* the lock, so keying it per database moved no number — every send in these rungs carries no job name and takes no lock either way. It also cannot be done safely from a path: `Path.GetFullPath` does not resolve symbolic links, and nothing path-based resolves hard links, so two connection strings reaching one file could get different locks. On the de-queue path that means two consumers claiming the same record. Do not re-derive this without a real file-identity check |
| The remaining single-database ceiling is LiteDB's, not this library's | Raw LiteDB doing the same transaction-wrapped writes with no transport involved goes 12.13 ms on one thread to 20.86 ms on four — **1.72x slower**, worse than the transport's own 1.31x. A write transaction takes an exclusive engine lock. Do not chase this in the transport |
| Batching was **worse** than not batching, and a single transaction fixed it | 21,057 us for 100 messages was 211 us each, against 145 us sending them one at a time: LiteDb had no bulk path, so `SendMessages` fell back to `Parallel.ForEach` over single sends, which fans threads into LiteDB's exclusive write transaction and buys contention instead of parallelism. One connection and one transaction for the whole batch takes it to 2,450 us, or **24.5 us a message — 8.6x faster, and now 5.8x better than sending one at a time** |
| The ceiling for a LiteDb batch is about 9 us a message | Raw LiteDB writing the same shape — a body row and a meta row per message — inside one transaction on an open database costs 933 us for 100. `InsertBulk` is 502 us but returns a count rather than the generated ids, so it is only worth reaching for if the ids can be recovered another way. The transport's 24.5 us is that 9.3 us plus serialization and the core pipeline |
| A shared connection costs about 17x a direct one **per operation**, and connection reuse cannot fix it | 2,566 us against 151 us for the same single send. The obvious cause looks like `LiteDbConnectionManager.GetDatabase` building a new `LiteDatabase` per operation in shared mode — **that attribution was wrong**, and an earlier revision of this file said so. Measured directly: holding one `LiteDatabase` across 200 shared-mode writes costs 2,235 us an operation against 2,190 us for building one each time, a ratio of **1.0x**. The same measurement in direct mode is 100 us against 2,076 us, 20.8x, which is why reuse matters there and is already done |
| Shared mode's cost is inside LiteDB, and it is the price of what shared mode does | `SharedEngine` opens and closes the engine under a cross-process mutex on every operation. A two-process probe confirms why that has to happen: while one process holds a shared-mode database open, a second process can still write, and both see the result — where the same probe in direct mode silently loses the second process's write. Do not try to reuse the connection in shared mode; it buys nothing and removes the isolation that mode exists for |
| The only lever that works on shared mode is doing fewer operations, which the batch path does | A batch is one operation, so it pays the mutex-and-open cost once rather than once per message. A shared-mode batch of 100 is 6,862 us — **68.6 us a message against 2,566 us sending them one at a time, 37x**. It also takes shared mode from 17x a direct send to 2.8x a direct batch |
| Disposing a written-to database is the expensive half, not constructing one | Open and close with no work is 31.9 us, but the same construction wrapped around two inserts is 2,034 us. All three GC generations collect on those rungs, so the cost is the flush and the large buffers a LiteDatabase brings, not the constructor |
| The existence check is small here, unlike SQLite | 0.9 us and 1.42 KB per send against a 149 us send. It parses the connection string every time and is not cached, so it is worth fixing eventually, but it is under 1% and not the lever |

## LiteDbReceiveBenchmarks

Isolates the query a de-queue runs to find the next message, against the index sets that could
serve it. Depth is a parameter because that is the whole question: a scan grows with queue depth
and an ordered walk does not. The rungs measure the query alone, which mutates nothing and is
therefore repeatable — a real de-queue is not.

### Findings

| finding | evidence |
|---|---|
| The de-queue scanned the queue, and got slower the deeper it was | Filtering on `Status`, `HeartBeat`, `QueueProcessTime` and `ExpirationTime` then sorting by `QueuedDateTime`: 2.3 ms against a thousand waiting messages, 31 ms against ten thousand, allocating 22 MB to find one message. LiteDB uses **one index per query** and chose `Status`, where every waiting row holds the same value — so it selected the whole backlog and sorted it |
| Adding an index for the sort field does **nothing** | `Status` and `HeartBeat` are indexed already (both options are hard-coded true), and the planner keeps choosing one of them: 30.2 ms against 29.1 ms. The naive fix is not a fix. Do not re-derive this |
| Indexing `Status` makes this query *worse*, not better | With only the primary key indexed it is 22.9 ms against 29.1 ms. An equality seek on a field where every candidate row matches selects everything and still has to sort |
| The fix is to leave only the key in the `Where` | Walking the collection in primary-key order and testing eligibility in memory: **55 us at any depth**, 157 KB. The predicates still run — over a window of 64 rather than over the whole collection |
| Page by seeking, never by `Skip` | Both are ~55 us when the head of the queue is ready. When the whole head is deferred, seeking costs 12.5 ms and `Skip` costs **375 ms and 1.5 GB** — 12x *worse* than the scan it replaced, because `Skip` re-walks from the start on every page |
| Key on `Id`, not `QueuedDateTime` | Same speed, and two things the timestamp cannot offer: it is unique, so paging cannot step over messages that share a value — a batch stamps many messages the same millisecond — and it is the primary key, so it is always indexed and the change needs no new index and no migration |
| Dropping `Status` and `HeartBeat` also works, and costs more than it saves | It gives 25 us, slightly better than the walk, but the heartbeat monitor is the one query where `Status == Processing` really is selective: it goes from 83 us to 2.2 ms, 26x worse. Keeping every index and changing the query gives nearly all of the win and no regression |

### A measurement that was wrong, and how

An earlier version of this file reported the sort-field index as a 931x win. That was measured
against a collection with **no** indexes, which is not what ships — `MetaDataTable` builds four.
Re-baselining against the real schema turned the same change into 1.04x. Read the schema, not a
truncated grep of it.

A later version moved the predicates into memory and reported 52 us with no regression anywhere.
That was measured with a filter that compared a `DateTime` LiteDB had returned as `Local` against
`UtcNow`, which compares raw ticks without applying the offset — so it read a message deferred an
hour into the future as ready and matched on the first page. Fast because it was wrong. The values
the transport stores do come back as UTC, which `DateKindIsPreserved` now pins.

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

