-- ---------------------------------------------------------------------------
-- DotNetWorkQueue 0.11.0 -> 0.12.0, SQL Server
--
-- Brings an existing queue to the 0.12.0 schema without dropping and re-creating
-- it. RemoveQueue drops all seven tables, History and MetaDataErrors among them,
-- so re-creating a queue destroys message history and the error queue.
--
-- One step. Skipping it leaves exactly the 0.11.0 behaviour - an existing queue
-- runs on 0.12.0 without this script, it just does not gain the fix.
--
-- ErrorTracking gains a unique index on (QueueID, ExceptionType), which is what
-- lets the retry count be written in one atomic statement (#299).
--
-- SQL Server needs nothing for #311: its timestamp columns are `datetime` and
-- were always stored as written. That change was PostgreSQL-only.
--
-- BEFORE RUNNING
--   * Set @QueueName below.
--   * Take a backup.
--
-- Run it as a principal that can create an index on the queue tables.
-- ---------------------------------------------------------------------------

SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @QueueName sysname = N'YourQueueName';   -- <<< the queue, exactly as the application names it

DECLARE @Tracking  sysname = @QueueName + N'ErrorTracking';
DECLARE @Sql       nvarchar(max);
DECLARE @Collapsed int;

IF OBJECT_ID(QUOTENAME(@Tracking), N'U') IS NULL
BEGIN
    RAISERROR (N'No table %s, so %s is not a queue in this database', 16, 1, @Tracking, @QueueName);
    RETURN;
END;

-- The index name carries no table name in it. SQL Server scopes index names to
-- their table, and appending the table pushed the identifier past the 128
-- character limit for the longest queue names the validator allows.
IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = N'IX_QueueIDExceptionType'
             AND object_id = OBJECT_ID(QUOTENAME(@Tracking)))
BEGIN
    PRINT N'IX_QueueIDExceptionType already exists on ' + @Tracking + N', nothing to do';
    RETURN;
END;

-- ---------------------------------------------------------------------------
-- Collapse duplicates first - the index cannot be created while they exist.
--
-- 0.11.0 counted errors with a read followed by a write. Two workers failing the
-- same message at the same moment could both read "no row" and both insert, and
-- the count then read low from either row: a message got more attempts than it
-- was configured for, and a poison message could loop instead of reaching the
-- error queue.
--
-- RetryCount is summed rather than discarded. Each row counted attempts that
-- really happened, so summing preserves the total and a message keeps the
-- attempts it has already used.
--
-- Both statements run in one transaction so a failure cannot leave the rows
-- collapsed but the totals unwritten.
-- ---------------------------------------------------------------------------
BEGIN TRANSACTION;
BEGIN TRY

    SET @Sql = N'
        SELECT MIN(ErrorTrackingID) AS KeepId,
               QueueID,
               ExceptionType,
               SUM(RetryCount)      AS TotalRetries
        INTO   #totals
        FROM   ' + QUOTENAME(@Tracking) + N' WITH (UPDLOCK, HOLDLOCK)
        GROUP BY QueueID, ExceptionType
        HAVING COUNT(*) > 1;

        SET @CollapsedOut = @@ROWCOUNT;

        DELETE t
        FROM   ' + QUOTENAME(@Tracking) + N' t
        JOIN   #totals d
          ON   t.QueueID = d.QueueID
         AND   t.ExceptionType = d.ExceptionType
        WHERE  t.ErrorTrackingID <> d.KeepId;

        UPDATE t
        SET    t.RetryCount = d.TotalRetries
        FROM   ' + QUOTENAME(@Tracking) + N' t
        JOIN   #totals d
          ON   t.ErrorTrackingID = d.KeepId;

        DROP TABLE #totals;';

    EXEC sp_executesql @Sql, N'@CollapsedOut int OUTPUT', @CollapsedOut = @Collapsed OUTPUT;

    IF @Collapsed > 0
        PRINT N'Collapsed duplicates for ' + CAST(@Collapsed AS nvarchar(20))
            + N' message/exception pairs, retry counts summed';

    SET @Sql = N'CREATE UNIQUE INDEX IX_QueueIDExceptionType ON '
             + QUOTENAME(@Tracking) + N' (QueueID, ExceptionType)';
    EXEC sp_executesql @Sql;

    COMMIT TRANSACTION;
    PRINT N'Created IX_QueueIDExceptionType on ' + @Tracking;

END TRY
BEGIN CATCH
    -- XACT_ABORT alone does not cover RAISERROR, and a doomed transaction has to
    -- be rolled back explicitly or the connection carries it out of this batch.
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    DECLARE @Message nvarchar(2048) = ERROR_MESSAGE();
    DECLARE @Severity int = ERROR_SEVERITY();
    DECLARE @State int = ERROR_STATE();

    -- A live consumer can insert a duplicate between the collapse and the index
    -- creation. Nothing is left half-done - re-run, or stop consumers for the
    -- moment this takes.
    RAISERROR (@Message, @Severity, @State);
END CATCH;
