-- ---------------------------------------------------------------------------
-- DotNetWorkQueue 0.11.0 -> 0.12.0, SQLite
--
-- Brings an existing queue database to the 0.12.0 shape without dropping and
-- re-creating it. RemoveQueue drops all seven tables, History and MetaDataErrors
-- among them, so re-creating a queue destroys message history and the error
-- queue.
--
-- Two steps, independent of each other. Skipping either leaves exactly the
-- 0.11.0 behaviour for that half.
--
--   1. ErrorTracking gains a unique index on (QueueID, ExceptionType), which is
--      what lets the retry count be written in one atomic statement (#299).
--   2. The database moves to WAL journal mode, which is what lets a reader and a
--      writer work at the same time.
--
-- SQLite needs nothing for #311: it stores timestamps as text that was always
-- correct UTC, and 0.12.0's DateTimeKind=Utc fixes how existing files are read
-- with no change to the file.
--
-- BEFORE RUNNING
--   * Replace YourQueueName below. sqlite3 has no variables, so the names are
--     written out - a find-and-replace of YourQueueName is the whole edit.
--   * Take a copy of the database file.
--   * Stop everything using the file. Step 2 needs the database to itself for an
--     instant and SQLite refuses it otherwise, and step 1 is a schema change on
--     an engine that serialises writers anyway.
--
-- This is plain SQL with no sqlite3 directives in it, so it can also be run
-- through any client; from the sqlite3 shell, stop on the first error with
--
--   sqlite3 -bail /path/to/queue.db < sqlite.sql
-- ---------------------------------------------------------------------------

-- ---------------------------------------------------------------------------
-- Step 1 - one error row per message per exception type
--
-- 0.11.0 counted errors with a read followed by a write. Two workers failing the
-- same message at the same moment could both read "no row" and both insert, and
-- the count then read low from either row: a message got more attempts than it
-- was configured for, and a poison message could loop instead of reaching the
-- error queue. 0.12.0 writes the count in a single statement that depends on
-- this index; without it the code keeps using the old path.
--
-- Duplicates are collapsed first, because the index cannot be created while they
-- exist. RetryCount is summed rather than discarded: each row counted attempts
-- that really happened, so summing preserves the total and a message keeps the
-- attempts it has already used.
--
-- The index name has the table name appended because SQLite's index names are
-- database-wide rather than scoped to a table, and one file can hold many
-- queues. That is the name 0.12.0 creates and the name it looks for.
-- ---------------------------------------------------------------------------
BEGIN IMMEDIATE;

CREATE TEMP TABLE dnwq_upgrade_totals AS
SELECT MIN(ErrorTrackingID) AS KeepId,
       QueueID,
       ExceptionType,
       SUM(RetryCount)      AS TotalRetries
FROM   YourQueueNameErrorTracking
GROUP BY QueueID, ExceptionType
HAVING COUNT(*) > 1;

DELETE FROM YourQueueNameErrorTracking
WHERE ErrorTrackingID IN (
    SELECT t.ErrorTrackingID
    FROM   YourQueueNameErrorTracking t
    JOIN   dnwq_upgrade_totals d
      ON   t.QueueID = d.QueueID
     AND   t.ExceptionType = d.ExceptionType
    WHERE  t.ErrorTrackingID <> d.KeepId);

UPDATE YourQueueNameErrorTracking
SET    RetryCount = (SELECT d.TotalRetries
                     FROM   dnwq_upgrade_totals d
                     WHERE  d.KeepId = YourQueueNameErrorTracking.ErrorTrackingID)
WHERE  ErrorTrackingID IN (SELECT KeepId FROM dnwq_upgrade_totals);

DROP TABLE dnwq_upgrade_totals;

-- The index is dropped and recreated rather than created with IF NOT EXISTS.
--
-- IF NOT EXISTS checks the name and nothing else, so an index that merely shares
-- the name without being a two column unique key would make the statement
-- succeed while the queue kept counting on the older racy path - a silent no-op
-- reported as a successful upgrade. The runtime detector matches on shape rather
-- than on the name, so the two would disagree with nothing to say why.
-- Recreating leaves the right shape whatever was there before, and stays safe to
-- run twice. ErrorTracking holds one row per failing message, so rebuilding the
-- index costs nothing.
--
-- But index names in SQLite are database-wide rather than scoped to a table, so
-- the name existing does not prove it belongs to this queue - and dropping
-- somebody else's index would take a uniqueness constraint with it. The insert
-- below fails its CHECK when the name is attached to any other table, which is
-- how a plain SQL script says "stop" on an engine with no IF. The error names
-- the guard, so what went wrong is legible:
--
--   CHECK constraint failed: dnwq_index_name_is_on_another_table
--
-- If that happens, the fix is to rename or drop whatever else holds the name,
-- and run this again.
DROP TABLE IF EXISTS dnwq_index_name_is_on_another_table;
CREATE TEMP TABLE dnwq_index_name_is_on_another_table (
    offenders INTEGER CONSTRAINT dnwq_index_name_is_on_another_table CHECK (offenders = 0));

INSERT INTO dnwq_index_name_is_on_another_table (offenders)
SELECT COUNT(*) FROM sqlite_master
WHERE type = 'index'
  AND name = 'IX_QueueIDExceptionTypeYourQueueNameErrorTracking'
  AND tbl_name <> 'YourQueueNameErrorTracking';

DROP TABLE dnwq_index_name_is_on_another_table;

DROP INDEX IF EXISTS IX_QueueIDExceptionTypeYourQueueNameErrorTracking;

CREATE UNIQUE INDEX IX_QueueIDExceptionTypeYourQueueNameErrorTracking
    ON YourQueueNameErrorTracking (QueueID, ExceptionType);

COMMIT;

-- ---------------------------------------------------------------------------
-- Step 2 - WAL journal mode
--
-- 0.12.0 puts new databases into WAL mode, where a reader and a writer can work
-- at the same time instead of taking turns on one lock. It is applied when the
-- queue tables are created, so a database that already had them never goes
-- through that path and stays in rollback-journal mode on upgrade.
--
-- The mode is a property of the file, not of a connection: setting it once here
-- is permanent and every later connection inherits it.
--
-- This prints the mode the database is actually in afterwards. If it prints
-- anything other than "wal", something else still had the file open - close it
-- and run this step again. Nothing is harmed by the failed attempt.
--
-- Skip this step if the application sets EnableWalMode = false on purpose.
-- ---------------------------------------------------------------------------
PRAGMA busy_timeout = 3000;
PRAGMA journal_mode = WAL;
