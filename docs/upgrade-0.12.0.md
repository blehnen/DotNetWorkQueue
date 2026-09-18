# Upgrading an existing queue to 0.12.0

0.12.0 changes the schema in two places. **Neither change is required.** An existing queue
runs on 0.12.0 untouched — it just keeps the old behaviour in those two places until you
run the script for your transport.

The scripts are in [`docs/upgrade/0.12.0`](upgrade/0.12.0). They exist so you do not have
to drain and re-create a queue to get the fixes, because re-creating one is not a step:
`RemoveQueue` drops all seven tables, `History` and `MetaDataErrors` among them, so it
destroys message history and the error queue on top of quiescing producers, waiting out
in-flight messages, and coordinating a fleet.

## What each transport needs

| transport | error-count index (#299) | UTC timestamps (#311) | WAL journal mode |
|---|---|---|---|
| SQL Server | yes | not affected | n/a |
| PostgreSQL | yes | yes | n/a |
| SQLite | yes | not affected | yes |
| LiteDb, Redis | n/a | n/a | n/a |

## The changes

### One error row per message per exception type

0.11.0 counted errors with a read followed by a write. Two workers failing the same message
at the same moment could both read "no row" and both insert. The count then read low from
either row, so **a message got more attempts than it was configured for, and a poison
message could loop instead of reaching the error queue.**

0.12.0 writes the count in one atomic statement, which needs a unique index on
`(QueueID, ExceptionType)`. Without the index the library detects its absence and keeps
using the older path, so nothing breaks — the race simply remains.

The script collapses duplicate rows before creating the index, because the index cannot be
created while they exist. The surviving row keeps the **largest** `RetryCount` of its group,
not their sum. The column holds an absolute total of failures so far rather than an
increment, and the queue's own write already reconciles two values for one pair by taking the
greater. That write also matches on `(QueueID, ExceptionType)` alone, so it sets every
duplicate row for the pair to the same value — summing them would roughly double the count and
send the message to the error queue with attempts still owed to it.

> Earlier copies of these scripts summed instead, on the reasoning that each row had counted
> real attempts. That holds only for rows inserted and never updated since. If you have
> already run one, the inflated counts cannot be recovered — the individual rows are gone —
> and the effect is that affected messages retire earlier than configured (GitHub #374).

### Timestamps that read back as UTC — PostgreSQL only

Npgsql maps a `DateTime` to `timestamp with time zone`. Writing a UTC value into a naive
`timestamp` column therefore converted it to the session time zone on the way in, and the
stored value was local time under a column named `...Utc`.

`ALTER ... TYPE timestamptz` corrects the stored values in place rather than discarding
them: PostgreSQL reinterprets each naive value in the session time zone, which is the zone
it was written in.

**The time zone you set in the script is the entire correctness of this step.** It must be
the zone the application server wrote from — not yours, and not the server default. Getting
it wrong silently shifts every historical timestamp by the difference, and re-running does
not fix it, because the second run reinterprets values that are already correct. If the
application wrote from more than one zone, the script cannot correct the values: convert
per-zone by row, or accept the history as it is and skip the step.

SQL Server stores `datetime` and SQLite stores text, and both were always written as
intended. Neither needs anything.

### WAL journal mode — SQLite only

0.12.0 puts new databases into WAL mode, where a reader and a writer can work at the same
time instead of taking turns on one lock. It is applied when the queue tables are created,
so a database that already has them never goes through that path and stays in
rollback-journal mode. The mode is a property of the file, so setting it once is permanent.

Skip this step if you set `EnableWalMode = false` deliberately.

## Costs, stated plainly

- **PostgreSQL step 2 takes an `ACCESS EXCLUSIVE` lock and rewrites the table.** Brief on
  `MetaData`, which holds only in-flight messages. `History` is as large as your retention
  on it — measure there before choosing a window. Producers and consumers block for the
  duration.
- **The de-dup and the index creation race a live consumer** that inserts a duplicate
  between them, and the creation then fails. Nothing is left half-done: re-run, or stop
  consumers for the moment it takes. On PostgreSQL, `CREATE UNIQUE INDEX CONCURRENTLY`
  avoids the long lock but cannot run inside a transaction, so it does not fit the script
  as written.
- **SQLite needs the database to itself** for the journal mode change and will refuse
  otherwise. Stop everything using the file first. The script prints the mode it ended up
  in; anything other than `wal` means something still had the file open, and re-running is
  safe.
- Every script is safe to run twice. PostgreSQL and SQL Server check for the index by its
  *shape* rather than its name, the way the library does at runtime — a name check would
  report "nothing to do" against an index that merely shares the name, leaving the queue on
  the racy path with nothing to say why. SQLite recreates the index for the same reason.
  PostgreSQL's conversion checks the column type first, which is what stops a second run
  shifting timestamps again.
- PostgreSQL resolves each table through `to_regclass` rather than matching an assembled
  name, so a queue name containing a dot — which the transport accepts, and which names a
  schema — is handled the same way the transport handles it.

## Running them

Set the queue name at the top of the script — a find-and-replace of `YourQueueName`. On
PostgreSQL also set the time zone; on SQL Server also set `@Schema` if the queue was
configured with `SetSchema`, since the transport qualifies its tables with it and an
unqualified name would resolve through whatever default schema you happen to log in with.
Run one script per queue.

```bash
psql -v ON_ERROR_STOP=1 -f docs/upgrade/0.12.0/postgresql.sql
sqlcmd -i docs/upgrade/0.12.0/sqlserver.sql
sqlite3 -bail /path/to/queue.db < docs/upgrade/0.12.0/sqlite.sql
```

The scripts are plain SQL with no client-specific directives, so any client will run them.

## How they are tested

`UpgradeScript0120Tests` in the SQL Server, PostgreSQL and SQLite integration test projects
builds a queue on the 0.11.0 schema, runs **the file in `docs/upgrade/0.12.0`** — not a copy
of it — and then asserts the outcome: that the library's own index detection now reports the
index, that a collapsed row keeps the largest count of its group, that a row which was never
duplicated is untouched, and on PostgreSQL that history written before the upgrade reads
back as the instant it was written at.

Reading the shipped file is the point. A copy would drift from it the moment either was
edited alone, and the tests would keep passing.
