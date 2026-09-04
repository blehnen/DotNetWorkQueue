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

## MemoryPathBenchmarks

Takes apart the `core producer pipeline (Memory transport)` rung above. The Memory transport
stores the POCO in a dictionary — no serialization, no SQL, no I/O — so what is left is the core
library, and a change to any of it moves every transport at once.

| benchmark | isolates |
|---|---|
| raw store: dictionary + queue add | the floor: the two collections the transport writes to |
| `DataStorage.SendMessage` | the transport's storage layer |
| `DataStorage.SendMessage` + trace decorator | *minus the row above* = the trace decorator |
| `ISendMessages`, undecorated | the transport's `ISendMessages` |
| `ISendMessages`, decorated | *minus the row above* = the policy, history and metrics decorators |
| `producer.Send` (end to end) | *minus the row above* = header generation, the message factory and the standard headers — which the component rungs then split up |
| `producer.Send(List)` (end to end) | the batch path, per message |
| batch shape: `Parallel.ForEach` into a `ConcurrentBag` / ordered loop into an array | the batch shape on its own, against the same store |
| component: message data, collections eager / lazy | what `AdditionalMessageData` costs to construct, before and after, measured in one run |
| component: `GenerateMessageHeaders.HeaderSetup` | the correlation id, and the reads of `data.Headers` |
| component: `IMessageFactory.Create` | the message and its header dictionaries |
| component: `AddStandardMessageHeaders.AddHeaders` | the three standard headers stamped on every message |

Every rung runs against a queue created fresh for the iteration, and an iteration is a single
invocation of 50,000 operations. That matters: the Memory store is a process-wide static that
nothing drains here, so letting BenchmarkDotNet choose the invocation count would have later
iterations writing into a dictionary holding millions of entries, and would report that growth as
send cost.

### Findings

| finding | evidence |
|---|---|
| Building `AdditionalMessageData` was the largest single cost in a send, and it is not transport work | One is constructed for every message — `ProducerQueue.Send` makes one when the caller supplies no data — and its constructor eagerly created four collections that most messages never put anything in. **1,832 B against 72 B** for the same object with the collections created on first use, the two measured in the same run, and roughly an order of magnitude less time (573 ns against 11–32 ns, which moves between runs). Most of it was the `ConcurrentDictionary`, which sizes its lock array from the processor count |
| `IAdditionalMessageData.Headers` allocated a wrapper on every read, and the send path read it twice | The property built a fresh `ReadOnlyDictionary` per call. It now hands back one cached view that reads the dictionary rather than wrapping it — which is what keeps the old behaviour of showing a header set after the view was taken, now that the dictionary itself may not exist yet. `HeaderSetup` reads it once instead of twice: that rung goes from 112 B to 72 B, and what is left is the correlation id. Note the rung reuses one data object, so the view's own 24 B is paid per message in the end-to-end rung rather than here |
| The metrics decorator allocated two objects per message for a measurement nobody was collecting | `ITimer.NewContext()` created a context object holding a `Stopwatch` — both classes — and `Histogram.Record` then discarded the value when no collector was subscribed. Returning a shared do-nothing scope when the instrument has no listener, and keeping the start as a raw timestamp when it does, took the decorator stack from ~206 B per send to ~41 B |
| Half of what a message object cost was a dictionary nothing wrote to | `Message` created a second dictionary for internal headers in its constructor. Created on first use instead, `IMessageFactory.Create` went from 248 B to 168 B |
| The Memory transport's parallel batch bought nothing, and cost the caller's ordering | `Send(List<>)` fanned out with `Parallel.ForEach` into a `ConcurrentBag`, so results came back in whatever order the threads finished — while 0.9.41 declared results to be in caller input order, and every other transport returns them that way. The store behind it is an in-memory concurrent dictionary, so nothing was waiting on anything the parallelism could overlap: the two shapes measured the same to within the noise |
| The store-touching rungs are too noisy to read timings from; the allocation column is the reliable one | Those rungs write 50,000 entries into a `ConcurrentDictionary` and a `BlockingCollection` that grow as they go, and the resulting GC and resize behaviour moves the means around by 30–40% between iterations. Adjacent rungs come out non-monotonic. Read them for allocation, and read the component rungs — whose standard deviation is 2% — for time |

End to end: the `core producer pipeline (Memory transport)` rung went from 2,990 ns / 3.02 KB to
2,190 ns / 1.15 KB, and a SQLite send — measured back to back on the same machine from two build
outputs — from 17.64 KB to 15.62 KB, with a 100-message batch from 1,562 KB to 1,364 KB.

## MemoryReceiveBenchmarks

The consume-side counterpart to `MemoryPathBenchmarks`: what it costs the core library to hand a
caller one message, with no transport work in the number.

| benchmark | isolates |
|---|---|
| raw store: take + lookup | the floor: taking an id off the collection and looking the item up |
| `DataStorage.GetNextMessage` | the transport's storage layer |
| `IReceiveMessages`, undecorated | *minus the row above* = the message context and the receive's own work |
| `IReceiveMessages`, decorated | *minus the row above* = the four decorators: policy, trace, history and metrics |
| component: `IMessageContextFactory.Create` / `IWorkerNotificationFactory.Create` | the two objects built per message |
| component: `WorkerNotification` constructed directly | the same object without the container — *against the factory row* = what the resolve costs as opposed to the object |
| component: container resolve / cached producer for `IMessageContext` | whether the resolve cost is the type lookup or the graph |
| component: linked token source, per message / built once | the cancellation plumbing a de-queue does |
| component: `IMessageFactory.Create` / `IReceivedMessageFactory.Create` | rebuilding the message out of the store |
| component: event wiring, method groups / cached delegates | the commit, rollback and cleanup subscriptions |

Every rung consumes exactly the messages seeded for the iteration and no more. That is not
tidiness: a receive against an empty queue blocks for five seconds, so over-consuming would not
report a slow benchmark, it would report a hung one.

The consume loop's remaining half — the user's handler, the heartbeat worker and the commit — is
not measured here. Reaching `ProcessMessage` means registering a handler through `Start`, which
puts worker threads on the same queue the benchmark is draining.

### Findings

| finding | evidence |
|---|---|
| A receive spent about a third of its time inside the DI container | The path built two transients per message. `WorkerNotification` cost **437 ns to resolve and 20 ns to construct** — the container, not the object. The factories now build the default implementation directly and fall back to resolving when the registration has been replaced, which SQL Server and PostgreSQL both do. `IWorkerNotificationFactory.Create` 437 ns → 17.6 ns, `IMessageContextFactory.Create` 823 ns → 75 ns |
| The cost is not the type lookup, so caching a producer does not help | Resolving `IMessageContext` through a `SimpleInjector.InstanceProducer` looked up once measured 666 ns against 690 ns for `Container.GetInstance`. Do not re-derive this — the answer was to stop resolving, not to resolve faster |
| Subscribing the commit/rollback/cleanup handlers allocated twice over | Each `+=` and `-=` built a delegate for the method group (six per message, 384 B), and because the events were seeded with `delegate { }` every subscribe also had to combine two delegates and every unsubscribe to build another (313 B). Caching the delegates in the transports and dropping the seed in `MessageContext` took the wiring from 697 B to nothing |
| The linked cancellation source was rebuilt per de-queue for a result that never changed | 80 B and 58 ns per message to combine two tokens fixed for the life of the storage object; built once, 0 B and 3.7 ns |
| `Tokens.Any(t => t.IsCancellationRequested)` boxes an enumerator, twice per message, in every transport | `ICancelWork.Tokens` is a `List<T>` and LINQ reaches it as `IEnumerable<T>`. Replaced with `AnyCancellationRequested()`, a plain indexed loop |
| The four receive decorators are cheap, unlike the send side | 2,058 B undecorated against 2,112 B decorated. Their time is not separable from the noise of these rungs. The send path's decorators cost ~206 B before they were fixed; the expectation did not carry over |
| What is left is rebuilding the message | Of the storage layer's 1,637 B, `IMessageFactory.Create` is 416 B — it copies the header dictionary — and `IReceivedMessageFactory.Create` is 616 B. Those are the next candidates, and neither is a quick win |

End to end, a de-queue through the full decorated chain went from **2,964 ns / 2,833 B to
2,365 ns / 2,112 B**.

## LiteDbReceiveConcurrencyBenchmarks

Whether concurrent consumers scale, which the send-only concurrency suite could not say.
`ReceiveMessageQueryHandler` holds a process-wide `static` lock around every de-queue; the send
path had a lock of exactly that shape and removing it was the largest single finding of the LiteDb
pass, so the obvious hypothesis was that the same win was sitting here.

**It is not.** The lock is the correctness mechanism, not removable overhead.

| benchmark | isolates |
|---|---|
| 200 de-queues, 1 thread, one queue | the serial baseline |
| 200 de-queues, 4 / 8 threads, one queue | whether more consumers help |
| 200 de-queues, 4 threads, two separate queues | whether unrelated queues interfere |
| 200 raw LiteDB claims, 1 / 4 threads | the floor: a correct claim with no transport in the way |

Each iteration builds its queues from scratch. A de-queue marks a message processed rather than
deleting it, and the walk from #241 steps over ineligible rows in key order — so rows left by a
previous iteration would make each later iteration slower and quietly turn this into a
queue-depth measurement.

Not a `[MemoryDiagnoser]` suite, deliberately: with one invocation per iteration the diagnoser
counts the per-iteration fixtures — two queues, six containers, sixteen receive chains, four
hundred seeded messages — as de-queue cost, and read 98 MB per two hundred de-queues that way.
Allocation on this path belongs to `LiteDbReceiveBenchmarks`.

### Findings

| finding | evidence |
|---|---|
| **A correct claim cannot be parallel in LiteDB direct mode, so the de-queue lock is not overhead** | `BeginTrans` does not block in direct mode, so unsynchronized claim transactions interleave and take the same row. The control, run without a lock, made 200 claims that left **only 63 of 200 rows claimed** — and ran *faster* (7.4 ms against 21.7 ms) precisely because most of the work was wrong. Any measurement showing the raw engine "scaling" here is measuring double-delivery. This is why `ReceiveMessageQueryHandler` holds its lock, and why removing it is not on the table |
| Consumers do not scale on a LiteDb queue — they cost | 68.0 ms on one thread, **83.2 ms on four (1.23x slower)**, 85.0 ms on eight. The ceiling does not move between four and eight |
| The floor behaves the same way, so this is not something the transport adds carelessly | A correct raw claim goes 11.9 ms on one thread to 21.7 ms on four — 1.83x slower — with no transport in the way at all |
| Two unrelated queues are not made worse by sharing the process-wide lock, but they are not made better either | Four threads across two separate database files take 70.9 ms, which merely matches a *single* thread on one queue (68.0 ms) and beats four threads on one queue (83.2 ms). The lock still couples them; what is recovered is per-file contention, not parallelism |
| Per-database lock keying remains the only available lever, and it is a trap | It would let unrelated queues proceed independently. It was tried on the send path in #238 and removed: `Path.GetFullPath` does not resolve symlinks, so two spellings of one file take different locks and the messages get delivered twice. Any retry needs a real file identity, and the failure mode is silent |

Every rung asserts what it measured. A de-queue that finds no message throws, and each rung
verifies it claimed exactly `TotalMessages` **distinct** messages — so these rows are also a
positive statement that the de-queue stays exclusive under four and eight consumers and across two
queues, not just a timing. The duplicate check keys on queue *and* id, because a message id is an
auto-increment int scoped to its own database and the two-queue rung has an id 1 in each.

Measured on net10, WSL2/ext4, 15 warm-up iterations. The warm-up matters: with one invocation per
iteration the first benchmark in the process absorbs the JIT and file-cache cost and ran 223 ms on
its first iteration against 63 ms by its twelfth. Five warm-ups were not enough and made the
baseline look bimodal.

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
| A LiteDb send has no unattributed overhead — the earlier "50 KB unexplained" was a measurement artifact | The comparison charged the transport for work its floor was not doing. `RawInsert_DnwqShape` writes the **256-byte payload** into a meta collection with **no indexes**; the transport writes the **serialized message** — 4,464 bytes for the same payload — into a collection carrying **four**. Correcting both: 46.9 KB for the old floor, 59.8 KB once the document is the real size, 114.2 KB once the indexes match. The transport's end-to-end send is **101–115 KB across runs**, at or slightly below that floor. There is nothing left to find; a send costs what LiteDB charges for the writes it does |
| Half of a send is index maintenance, and 41.5 KB of it is the two optional indexes | Adding the structural pair (the key and the unique `QueueId`) to the corrected floor costs 13.0 KB; adding `Status` and `HeartBeat` on top costs a further **41.5 KB and 36 us**, about 36% of the send. Both are hard-coded `true` in `LiteDbMessageQueueTransportOptions`, so a caller cannot trade them away |
| Which leaves a real trade-off, now quantified on both sides | #241 measured what dropping `Status` and `HeartBeat` does to reads: the heartbeat monitor goes 83 us to 2.2 ms, 26x worse, which is why they were kept. What was not measured then is the write side — they cost 41.5 KB and 36 us on **every send**, while the heartbeat monitor runs on a timer. Whether that is the right bargain depends on the workload, and today it is not adjustable |
| The BSON mapper is not per-message work, and neither is the connection in direct mode | Two of this issue's hypotheses, answered by reading the code rather than measuring. `new LiteDatabase(connectionString)` uses `BsonMapper.Global`, which caches its entity mapper per type; and `LiteDbConnectionManager.GetDatabase` holds one `LiteDatabase` for the life of the manager in direct mode, handing out a non-owning wrapper per call. Only shared mode constructs one per operation, which is the mode measured above |
| Nothing leaks a database file on this line | The integration helper deletes only the main file, not LiteDB's `-log.db`, and it swallows a failed delete after one 3-second retry — so a handle leak could never fail a test, which is the same shape as #229. Measured anyway: a full 100-test LiteDb run left **zero** new files behind. The 35 databases sitting in the test output are dated 2021 to 2026-03, all of them older than this work. The swallowed delete is worth hardening with #229, not here |

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
| The walk has to be bounded, or it trades one scan for another | When nothing is eligible, an unbounded walk reads the whole collection to answer "no" - 12 ms against ten thousand rows, which is **1.7x worse than the query it replaced**, because materialising rows into .NET costs more than letting the engine reject them. Reading at most sixteen pages and resuming next poll costs 1.2 ms instead - **better than the original at every depth measured**, 605 us against 926 us at a thousand rows and 1.2 ms against 7.1 ms at ten thousand. Resuming does not get more expensive the deeper the position, which is the difference between seeking an index and `Skip`; the rung carries its position between invocations so that this is measured rather than assumed. Nothing starves: a fruitless poll advances the position, the end of the collection resets it, and a message that becomes eligible behind the position is found on the next pass |
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

## SqlServerPathBenchmarks and SqlServerReceiveBenchmarks

Decompose a SQL Server send and de-queue. Both need a server: set
`DNWQ_SQLSERVER_CONNECTION` to a connection string for a database the harness may create and drop
tables in. It is read from the environment rather than the integration tests' `connectionstring.txt`
because the harness runs from a copied output directory, and because a benchmark should not be the
thing that reads a credential file.

The shape of this transport is different enough from the embedded ones that most of the SQLite
playbook does not apply, and the ladder is built to show why rather than to assume it.

### Findings

| finding | evidence |
|---|---|
| **The round trip is not the unit to optimise here — the write is** | A bare `SELECT 1` costs 496 us against 8,491 us for a single insert. The write dominates a round trip by seventeen to one. Any ladder here is unreadable until that floor is on the table, which is what the `SELECT 1` rungs are for |
| ~~The library adds 2.3% of a send's time~~ - it now adds *less than nothing* against the shape it replaced | 9,654 us end to end against 9,429 us for a hand-written write of the same shape. Time optimisation on this path is close to pointless; the database is 97.7% of it. Allocation is the target — 30,522 B against a 16,594 B floor | **Superseded by the row above**: with the round trips collapsed, an end-to-end send (7.38 ms) is faster than a hand-written write of the four-round-trip shape (8.67 ms). Allocation remains the target - 24,648 B against a ~16 KB raw floor |
| **Collapsing the send's four round trips into one is done, and the library send is now faster than the raw four-round-trip shape** | An ordinary send made four: `BeginTransaction`, the body insert that returns the identity, the meta insert, `Commit`. As a single batch with the transaction and the identity kept server-side, the end-to-end send measures **7.38 ms against 8.67 ms for the raw four-round-trip rung and 7.63 ms for the raw one-round-trip rung, in the same run** - it beats the hand-written four-trip write it used to lose to, and sits at the one-trip floor. Allocation, which is deterministic and so comparable across runs, goes 29,298 B to 24,648 B. **Quote the within-run comparison, not a cross-run before/after**: absolute times on this ladder move by ~10% between runs, which is larger than the effect being measured. Two shapes keep the old path because each interleaves work between the statements - a scheduled job and a caller-supplied transaction. A status-table queue does **not**: its insert joins the same batch, since one `@CorrelationID` parameter serves every occurrence of the name in the text |
| The connection pool is free, so the largest SQLite win does not exist here | Pooled open plus close is 1.8 us. `Microsoft.Data.SqlClient` pools by default, unlike `System.Data.SQLite` |
| The meta insert's SQL was rebuilt per send, and it is worth less than it first looked | Cached per table-and-option shape: a send goes 30,522 B to 29,298 B. **1,224 B, about 4%** — not the 16% the rung suggested. That rung measures `BuildMetaCommand` as a whole and only 1,224 B of it is the text; the rest is `SqlParameter` construction, which no cache removes. Do not re-read that rung as SQL-generation cost |
| The de-queue statement was already cached, so the SQLite finding does not transfer | A cache hit is 11 ns and allocates nothing. On SQLite, generating the de-queue script was 91% of everything a de-queue allocated; here that work was already done |
| **But routes and a user clause bypassed the cache, on the one loop that never stops** | `GetDeQueueCommand` returned the cached text only with no routes and no user clause, so a consumer using either rebuilt the whole statement — a table variable, a CTE, forty-odd appends — on **every poll**: 439 ns and **5,368 B**, which is 45% of everything an empty de-queue allocates. Keyed on the route count, it is now 41 ns and **80 B**, a 98.5% cut. Route counts are small integers, so the cache holds a handful of entries. A user clause is **not** cached - `GetUserClause` invokes a caller-supplied factory on every de-queue, so keying on its text would add a permanent entry per poll. Note the `statement: routed consumer` rung changed meaning with the fix - it rebuilt before, and is a cache hit now - and is kept as the regression guard: if the route cache stops working it goes back to kilobytes a poll and says so |
| Only the route *count* reaches the statement, which is what makes it safe to key on | Routes become `@Route1..@RouteN` placeholders; their values are bound as parameters. The user clause is inlined and its factory runs per de-queue, so it is deliberately not cached at all - see the finding below |
| The delay and the expiration are written into the SQL as literals | `DATEADD(ms,12345,GetUTCDate())`. That is why the send-side cache covers only the invariant shape, and it also means SQL Server compiles a fresh plan per distinct delay value. Parameterising the two would fix both at once and has not been done |

### Two mistakes this ladder made first

Both are recorded because the numbers looked plausible either way.

**It reported the round trips as not worth collapsing, from a floor it had not measured.** The first
version had no `SELECT 1` rung, so there was nothing to say whether 9 ms was a round trip or a
write. It is a write, by a factor of seventeen — but that could only be stated once the bare round
trip was on the table.

**It reported the held-connection rung as the slowest**, at 14.1 ms against 8.4 ms for the same work
on a pooled connection, which is backwards. The rungs insert and never delete, BenchmarkDotNet runs
them in declaration order, and the later ones were paying for the data-file growth the earlier ones
caused. Truncating per iteration and fixing the invocation count removes it, and the ladder is
monotonic.

**A forced failure that fails at compile time proves nothing.** The atomicity test for the batch above first forced its failure by dropping the meta table. That makes the whole batch fail to
*compile*, so nothing executes - there is no body row to roll back, and the test passed with the
transaction removed entirely. Forcing it with a `CHECK` constraint instead makes the meta insert
fail at *run* time, after the body insert has already run, and that version fails when the
transaction is removed. Any test that asserts a rollback has to be checked against the un-fixed
code, not just the fixed code.

That check is what caught the real defect. The batch first relied on `SET XACT_ABORT ON` alone, and
the constraint test passed. `XACT_ABORT` has **no effect on errors raised by `RAISERROR`**, so a
trigger on the meta table raising one left the transaction alive, execution reached the unconditional
`COMMIT`, and a body row was committed while the caller was told the send failed - an orphan the
client-side transaction being replaced could not produce. `TRY/CATCH` with an explicit `ROLLBACK` is
what actually delivers the guarantee. One forced-failure mode is not enough to establish atomicity.


## PostgreSqlPathBenchmarks

Decomposes a single PostgreSQL `Send`, the way `SqlServerPathBenchmarks` does for SQL Server.
Needs a server: set `DNWQ_POSTGRES_CONNECTION` to a connection string for a database the harness
may create and drop tables in.

Written before any transport change, to answer one question first: is the round trip worth
collapsing on *this* transport? The SQL Server answer does not transfer on its own.

### Findings

| finding | evidence |
|---|---|
| The write dominates the round trip here too, but less than on SQL Server | A bare `SELECT 1` is 392 us against 4,741 us for a single insert - about 12 to 1, where SQL Server is 17 to 1 |
| **Collapsing the send's four round trips into one is worth ~10%, and is now done** | The raw four-round-trip shape is 5,376 us against 4,846 us for the same work as one statement - **530 us**. The collapsed form lands within 105 us of a *bare single insert*, so the meta write costs almost nothing once folded in; as separate round trips it costs 635 us |
| The end-to-end send moved by what the raw rungs predicted | 5,642 us to 5,111 us, a 531 us drop against a predicted 530 us |
| **Quote the ratio to a bare insert, not the absolute time** | Between the before and after runs the machine drifted ~7% *slower* (the raw four-trip rung went 5,376 -> 5,747 us), which is larger than the effect. Normalised within each run, the send went from **1.19x a bare single insert to 1.03x** - and from 4.9% slower than the raw four-round-trip write to 11% faster than it. That framing is run-independent; the absolute pair is not |
| PostgreSQL's version is a single statement, not a batch | Data-modifying CTEs with `RETURNING`. A single statement is atomic in PostgreSQL, so there is no `BEGIN`/`COMMIT` and no error handling to get wrong - unlike the SQL Server batch, which needed `TRY/CATCH` with an explicit rollback because `XACT_ABORT` does not cover `RAISERROR` |
| The transport had no send-side SQL cache at all | SQL Server gained one in #231; PostgreSQL rebuilt the meta insert's text on every send. The composed statement is now cached per table and option shape |
| ⚠️ **A delayed-processing queue cannot use that cache, and re-plans on every send** | `AddBuiltInColumnValues` inlines `currentDateTime.Ticks` whenever `EnableDelayedProcessing` is on - **even for a message carrying no delay** - so every send produces a different statement. SQL Server writes an invariant `GetUTCDate()` in that position. Parameterising the delay and expiration values fixes this and SQL Server's plan-per-delay problem at once, and has not been done |

### A note on the second run

The `raw: DNWQ shape, 1 round trip` rung was unusable in the after-run: 6,978 us with a standard
deviation of 1,731 us, against 4,846 us and a clean interval in the before-run. Nothing about that
rung changed - it is raw SQL the transport does not touch. It is recorded here so the number is not
read as a regression; the rungs with tight intervals are the ones to quote.

## PostgreSqlReceiveBenchmarks

The same question SQL Server's receive suite answered, asked again here rather than assumed.
`ReceiveMessage.GetDeQueueCommand` carried the identical deferral note, and the identical defect:
the cached statement was returned only with no routes and no user clause, so a routed consumer
reassembled it on every poll.

Needs a server: set `DNWQ_POSTGRES_CONNECTION`.

### Findings

| finding | evidence |
|---|---|
| The defect transferred, but **the number did not** | 2,648 B a poll against SQL Server's 5,368 B - about half, because PostgreSQL uses an updating CTE with `FOR UPDATE SKIP LOCKED` rather than a table variable, so its statement is smaller. Real either way, and worth measuring rather than carrying the other transport's figure across |
| Keyed on the route count and the clause, a routed poll allocates 80 B | 240 ns and 2,648 B rebuilding, **37 ns and 80 B** as a cache hit - a 97% cut. The unrouted path is unchanged at 10 ns and nothing |
| The same reasoning makes it safe here | Routes become `@Route1..@RouteN` placeholders, so only their count reaches the text and the cache holds one entry per route count. The user clause is not cached |
| A user clause must never be part of the cache key, which the first version of this got wrong | `SetUserParametersAndClause` takes a `Func<string>` that `GetUserClause` invokes on **every de-queue**, so the clause may differ each time. Keying on its text adds a permanent entry per poll to a cache that lives as long as the consumer - unbounded growth, found in review. Note the regression test has to assert on the cache rather than on the returned SQL: a per-clause key produces a different key each call, so a changing clause still returns *correct* text. It just leaks |

The routed rung changed meaning with the fix - it rebuilt before, and is a cache hit now - and is
kept as the regression guard, exactly as SQL Server's is.

## PostgreSqlAutoPrepareBenchmarks

Whether Npgsql's automatic statement preparation is worth turning on — #232 names it as the
PostgreSQL-specific lever, because it is off by default, has no equivalent in the other providers,
and would be a connection-string change rather than code.

Needs a server via `DNWQ_POSTGRES_CONNECTION`. The rungs are paired: identical work against two
queues differing only in their connection string.

### Findings

| finding | evidence |
|---|---|
| It pays on batches, and only on batches | A batch of 100 is **13-17% faster** across two runs (37.4 ms to 32.5 ms, then 69.3 ms to 57.6 ms - the absolute numbers moved a lot between runs because the server was busier, so read the within-run ratios). A single send showed 6% in the first run and **nothing** in the second (7.174 ms against 7.365 ms), which makes the single-send figure noise rather than a small win. Allocation is unchanged either way |
| The baseline has to state its setting, not inherit one | The first version passed the ambient connection string to the `off` rung unchanged. If a caller's string already enabled auto-prepare, that rung was not off and the suite would have compared a thing against itself - which looks exactly like "no effect" and is indistinguishable from a real result. Both rungs now say what they mean explicitly, and it was that change which showed the single-send gain was not there |
| It is documented, not enabled | Turning it on by default would hand every PostgreSQL consumer the DDL exposure below in exchange for 6–13%. It is described in the transport's README as a tuning option instead |
| **A dry run of this suite reported it as a five-fold win, and that was nonsense** | 55.8 ms against 10.5 ms, with one invocation per rung and the `off` fixture running first, so it paid the connection and warm-up cost for both. The same order-dependent trap the SQL Server ladder had to correct. Recorded because the number looked spectacular and meant nothing |
| The DDL risk is real, and only partly evidenced | Prepared statements live on the physical connection, the pool hands it back, and dropping a table invalidates any statement referencing it — and this library creates and drops queues routinely. `AutoPrepareSurvivesDdl` drives create → send past the threshold → drop → recreate under the same name → send, and passes. But Npgsql exposes no public counter for auto-prepared statements, so that is evidence the scenario works rather than proof the invalidation path was exercised. Do not read it as a safety guarantee |

## RelationalDecoratorBenchmarks

What the retry decorator costs per command, separated from the database call it wraps. Both #231
and #232 ask this, noting that a win here would be shared by all three relational transports rather
than transport-local.

It cannot be answered from the send ladders — the decorator wraps a call taking milliseconds, so its
own cost vanishes into the round trip. Here the inner handler does nothing, so what is left between
the two rungs is the decorator: a registry lookup, Polly's `Execute`, and the closure that
`pipeline.Execute(_ => _decorated.Handle(command))` allocates by capturing both the command and the
handler. SQLite is used because it needs no server and carries the same decorator.

### Findings

| finding | evidence |
|---|---|
| **The decorator stack is not a lever on a relational transport** | 148.6 ns and 120 B per command, against a SQL Server send of 9,654 us and 30 KB — **0.0015% of the time and 0.4% of the allocation**. Removing the closure would recover a fraction of 120 B. There is nothing here worth changing, and this rung exists to stop the question being asked a fourth time |
| It would matter if a command were cheap, which on these transports it never is | The floor rung is a handler that returns a constant, and the decorator is 148 ns on top of it. That ratio is what makes the stack look expensive in isolation and irrelevant in place. Measure the thing it wraps before optimising the wrapper |
| The decorator is triplicated, and the copies differ | SQL Server, PostgreSQL and SQLite each hold their own `RetryCommandHandlerOutputDecorator`. SQL Server and PostgreSQL short-circuit on `IRetrySkippable`; SQLite does not. **This is not a bug**: `SkipRetry` is `ExternalTransaction != null`, and SQLite never builds the shared `RelationalSendMessageCommand` that implements it, so the branch is unreachable there. Worth knowing before someone "fixes" the divergence |

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

