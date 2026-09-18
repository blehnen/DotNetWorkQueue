using Microsoft.VisualStudio.TestTools.UnitTesting;

// MSTest does not parallelize within an assembly unless told to, so this stage ran one
// test at a time while the agent sat near-idle. The shared consumer scenarios spend
// their time in Thread.Sleep waiting on timers, not doing queue work, so overlapping
// them costs almost nothing: measured at four workers, the sum of per-test durations
// rose 1.2% (SQLite) and 0.5% (PostgreSQL) while wall clock fell ~3.6x.
//
// Eight rather than four since #281: the old cap existed because three stages shared one
// PostgreSQL server in CI and each held roughly 59 connections at this level. Every stage
// now starts its own containers, so that ceiling is gone.
//
// Measured over the full suite against containers, 408 tests each time:
//   4 workers   5m34s / 5m39s
//   8 workers   3m31s
//   16 workers  3m06s
// CPU time was ~1m40 at all three, which is the point -- these tests wait on timers rather
// than work, so more workers stop the waiting without costing anything. Eight takes most of
// the gain; sixteen adds a little more for twice the concurrency against the containers, on
// an agent already sharing a host with other stages, which is not a good trade.
//
// Measured on a developer machine against a remote daemon, not on an agent, so the knee may
// sit elsewhere in CI. Tests are isolated by construction -- each generates its own queue
// name and drops it in a finally block. See docs/lessons-learned.md.
[assembly: Parallelize(Workers = 8, Scope = ExecutionScope.MethodLevel)]
