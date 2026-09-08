using Microsoft.VisualStudio.TestTools.UnitTesting;

// Two workers here, not the four the other transports use.
//
// StackExchange.Redis 3.x starves the thread pool on the synchronous Lua path -- the
// subject of #161. Every sync call blocks a worker thread waiting on a completion that
// itself needs a worker thread, so past ThreadPool.MinThreads, where new threads arrive
// at one or two a second, the two deadlock until syncTimeout fires. A CI run at four
// workers produced exactly that: "Timeout performing SCRIPT (15000ms)" with qu:0, qs:0
// -- nothing queued and nothing on the wire -- against WORKER (Busy=215, Min=200).
//
// The timings agree. Redis Linq was the worst scaler of the thirteen stages at 1.88x,
// against 3.0-3.3x elsewhere, so it was already at its concurrency ceiling before this.
// Halving the concurrent sync calls keeps the stage near 9 minutes, still inside the
// critical path set by SQLite Linq, so it costs no wall clock.
//
// The real fix is an async receive path (#256); until then this is a ceiling, not a tuning knob.
[assembly: Parallelize(Workers = 2, Scope = ExecutionScope.MethodLevel)]
