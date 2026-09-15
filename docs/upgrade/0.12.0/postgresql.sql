-- ---------------------------------------------------------------------------
-- DotNetWorkQueue 0.11.0 -> 0.12.0, PostgreSQL
--
-- Brings an existing queue to the 0.12.0 schema without dropping and re-creating
-- it. RemoveQueue drops all seven tables, History and MetaDataErrors among them,
-- so re-creating a queue destroys message history and the error queue.
--
-- Two independent steps. Either can be skipped, and skipping one leaves exactly
-- the 0.11.0 behaviour for that half - an existing queue runs on 0.12.0 without
-- this script, it just does not gain the fixes.
--
--   1. ErrorTracking gains a unique index on (QueueID, ExceptionType), which is
--      what lets the retry count be written in one atomic statement (#299).
--   2. The timestamp columns become timestamptz, so values written as UTC read
--      back as UTC (#311).
--
-- BEFORE RUNNING
--   * Set queue_name below.
--   * Set the time zone in step 2 to the one the application wrote from. This
--     is not optional and the server default is not a safe guess - read the
--     note above that step.
--   * Take a backup. Step 2 rewrites tables and cannot be undone by re-running.
--
-- Run it as the owner of the queue tables. This is plain SQL with no psql
-- directives in it, so it can also be run through any client; from psql, stop on
-- the first error with
--
--   psql -v ON_ERROR_STOP=1 -f postgresql.sql
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
-- Duplicate rows are collapsed first, because the index cannot be created while
-- they exist. Their RetryCount values are summed rather than discarded: each row
-- counted attempts that really happened, so summing preserves the total and a
-- message keeps the attempts it has already used.
--
-- CONCURRENCY. A live consumer can insert a duplicate between the collapse and
-- the index creation, and the creation then fails. Nothing is left half-done -
-- re-run this step, or stop consumers for the moment it takes.
-- ---------------------------------------------------------------------------
DO $$
DECLARE
    queue_name  text := 'YourQueueName';   -- <<< the queue, exactly as the application names it
    tracking    text := lower(queue_name || 'ErrorTracking');
    index_name  text := lower('IX_QueueIDExceptionType' || queue_name || 'ErrorTracking');
    collapsed   bigint;
BEGIN
    IF to_regclass(quote_ident(tracking)) IS NULL THEN
        RAISE EXCEPTION 'No table %, so % is not a queue in this database', tracking, queue_name;
    END IF;

    --Already done, or created by 0.12.0 in the first place.
    --
    --Matched on the shape of the index rather than on its name, and that matters here: PostgreSQL
    --truncates an identifier at 63 bytes, so for anything but a short queue name the index that
    --actually exists is NOT called what the line above builds - the trailing ErrorTracking is cut
    --off. A name check therefore reports "absent" against an index that is present, and the CREATE
    --below then fails with "already exists" on the truncated name.
    --
    --The library detects it the same way, for the same reason (GitHub #299).
    IF EXISTS (
        SELECT 1 FROM pg_index ix
        WHERE ix.indrelid = to_regclass(quote_ident(tracking))
          AND ix.indisunique AND ix.indpred IS NULL
          AND ix.indnkeyatts = 2
          AND EXISTS (SELECT 1 FROM pg_attribute a
                      WHERE a.attrelid = ix.indrelid AND a.attnum IN (ix.indkey[0], ix.indkey[1])
                        AND lower(a.attname) = 'queueid')
          AND EXISTS (SELECT 1 FROM pg_attribute a
                      WHERE a.attrelid = ix.indrelid AND a.attnum IN (ix.indkey[0], ix.indkey[1])
                        AND lower(a.attname) = 'exceptiontype')) THEN
        RAISE NOTICE 'Step 1: a unique index on (QueueID, ExceptionType) is already on %, nothing to do', tracking;
        RETURN;
    END IF;

    --totals have to be taken before anything is deleted
    EXECUTE format(
        'CREATE TEMP TABLE dnwq_upgrade_totals ON COMMIT DROP AS
             SELECT min(ErrorTrackingID) AS keep_id,
                    QueueID,
                    ExceptionType,
                    sum(RetryCount)      AS total_retries
             FROM %I
             GROUP BY QueueID, ExceptionType
             HAVING count(*) > 1', tracking);

    GET DIAGNOSTICS collapsed = ROW_COUNT;

    IF collapsed > 0 THEN
        EXECUTE format(
            'DELETE FROM %I t
             USING dnwq_upgrade_totals d
             WHERE t.QueueID = d.QueueID
               AND t.ExceptionType = d.ExceptionType
               AND t.ErrorTrackingID <> d.keep_id', tracking);

        EXECUTE format(
            'UPDATE %I t
             SET RetryCount = d.total_retries
             FROM dnwq_upgrade_totals d
             WHERE t.ErrorTrackingID = d.keep_id', tracking);

        RAISE NOTICE 'Step 1: collapsed duplicates for % message/exception pairs, retry counts summed', collapsed;
    END IF;

    EXECUTE format('CREATE UNIQUE INDEX %I ON %I (QueueID, ExceptionType)', index_name, tracking);
    RAISE NOTICE 'Step 1: created %', index_name;
END $$;

-- ---------------------------------------------------------------------------
-- Step 2 - timestamp columns become timestamptz
--
-- Npgsql maps a DateTime to "timestamp with time zone". Writing a UTC value into
-- a naive `timestamp` column therefore converted it to the session time zone on
-- the way in, and the stored value was local time under a column named ...Utc.
--
-- ALTER ... TYPE timestamptz corrects the stored values in place: PostgreSQL
-- reinterprets each naive value in the session time zone, which is the zone it
-- was written in, so existing history becomes correct rather than being thrown
-- away.
--
--   before (naive):                   2001-02-02 22:05:06
--   ALTER COLUMN ... TYPE timestamptz
--   after, read as UTC:               2001-02-03 04:05:06+00
--
-- THE TIME ZONE BELOW IS THE WHOLE CORRECTNESS OF THIS STEP. It must be the zone
-- the application server wrote from, not the zone of whoever runs this script
-- and not the server default. Getting it wrong silently shifts every historical
-- timestamp by the difference, and re-running does not fix it - the second run
-- reinterprets values that are already correct.
--
-- If the application wrote from more than one zone, this script cannot correct
-- the values; convert per-zone by row, or accept the history as it is and skip
-- this step.
--
-- COST. ALTER ... TYPE takes an ACCESS EXCLUSIVE lock and rewrites the table.
-- Brief on MetaData, which holds only in-flight messages; History is as large as
-- the retention on it, so measure there before choosing a window. Producers and
-- consumers block for the duration.
-- ---------------------------------------------------------------------------
BEGIN;

SET LOCAL TIME ZONE 'UTC';   -- <<< the zone the APPLICATION wrote from

DO $$
DECLARE
    queue_name text := 'YourQueueName';   -- <<< the same queue as step 1
    target     record;
    converted  int := 0;
BEGIN
    FOR target IN
        SELECT * FROM (VALUES
            (lower(queue_name || 'MetaData'),       'queueddatetime'),
            (lower(queue_name || 'MetaDataErrors'), 'queueddatetime'),
            (lower(queue_name || 'MetaDataErrors'), 'lastexceptiondate'),
            (lower(queue_name || 'History'),        'enqueuedutc'),
            (lower(queue_name || 'History'),        'startedutc'),
            (lower(queue_name || 'History'),        'completedutc')
        ) AS t(table_name, column_name)
    LOOP
        --the column is absent when the option that creates it is off, and already
        --timestamptz when this has been run before or the table was made by 0.12.0
        CONTINUE WHEN NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_name = target.table_name
              AND column_name = target.column_name
              AND data_type = 'timestamp without time zone');

        EXECUTE format('ALTER TABLE %I ALTER COLUMN %I TYPE timestamptz',
                       target.table_name, target.column_name);
        converted := converted + 1;
        RAISE NOTICE 'Step 2: %.% is now timestamptz', target.table_name, target.column_name;
    END LOOP;

    IF converted = 0 THEN
        RAISE NOTICE 'Step 2: nothing to convert - already timestamptz, or those options are off';
    END IF;
END $$;

COMMIT;
