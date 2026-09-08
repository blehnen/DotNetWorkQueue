using Microsoft.VisualStudio.TestTools.UnitTesting;

// MSTest does not parallelize within an assembly unless told to, so this stage ran one
// test at a time while the agent sat near-idle. The shared consumer scenarios spend
// their time in Thread.Sleep waiting on timers, not doing queue work, so overlapping
// them costs almost nothing: measured at four workers, the sum of per-test durations
// rose 1.2% (SQLite) and 0.5% (PostgreSQL) while wall clock fell ~3.6x.
//
// Four rather than more: three stages share one PostgreSQL server in CI and each holds
// roughly 59 connections at this level. Tests are isolated by construction -- each
// generates its own queue name and drops it in a finally block. See docs/lessons-learned.md.
[assembly: Parallelize(Workers = 4, Scope = ExecutionScope.MethodLevel)]
