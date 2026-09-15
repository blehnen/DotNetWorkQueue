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
DECLARE @Schema    sysname = N'dbo';            -- <<< the queue's schema, if SetSchema was used

-- Qualified with the schema, because the transport qualifies its own table names
-- the same way: SqlServerTableNameHelper builds every name as schema + "." +
-- queue name, and the schema comes from the "SqlSchema" connection setting that
-- SetSchema writes, defaulting to dbo. An unqualified name here would resolve
-- through whatever default schema the operator happens to log in with, which is
-- either the wrong table or no table at all.
DECLARE @Tracking  nvarchar(300) = QUOTENAME(@Schema) + N'.' + QUOTENAME(@QueueName + N'ErrorTracking');
DECLARE @ObjectId  int = OBJECT_ID(@Tracking, N'U');
DECLARE @Sql       nvarchar(max);
DECLARE @Collapsed int;

IF @ObjectId IS NULL
BEGIN
    RAISERROR (N'No table %s, so %s is not a queue in schema %s', 16, 1, @Tracking, @QueueName, @Schema);
    RETURN;
END;

-- Matched on the shape of the index rather than on its name, the way the library
-- detects it at runtime. A name check would report "nothing to do" against an
-- index that merely shares the name without being the two column unique key the
-- atomic count needs - and the queue would then keep counting on the older racy
-- path with nothing to say why.
--
-- The name carries no table in it: SQL Server scopes index names to their table,
-- and appending the table pushed the identifier past the 128 character limit for
-- the longest queue names the validator allows.
IF EXISTS (SELECT 1
           FROM sys.indexes i
           WHERE i.object_id = @ObjectId
             AND i.is_unique = 1
             AND i.has_filter = 0
             AND i.index_id > 0
             AND (SELECT COUNT(*) FROM sys.index_columns ic
                  WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
                    AND ic.is_included_column = 0) = 2
             AND EXISTS (SELECT 1 FROM sys.index_columns ic
                         JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                         WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
                           AND ic.is_included_column = 0 AND c.name = N'QueueID')
             AND EXISTS (SELECT 1 FROM sys.index_columns ic
                         JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                         WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
                           AND ic.is_included_column = 0 AND c.name = N'ExceptionType'))
BEGIN
    PRINT N'A unique index on (QueueID, ExceptionType) is already on ' + @Tracking + N', nothing to do';
    RETURN;
END;

-- The name is free but the shape is not: something else is already called this.
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_QueueIDExceptionType' AND object_id = @ObjectId)
BEGIN
    RAISERROR (N'%s already has an index named IX_QueueIDExceptionType that is not a unique key on (QueueID, ExceptionType). Drop or rename it, then run this again', 16, 1, @Tracking);
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
        FROM   ' + @Tracking + N' WITH (UPDLOCK, HOLDLOCK)
        GROUP BY QueueID, ExceptionType
        HAVING COUNT(*) > 1;

        SET @CollapsedOut = @@ROWCOUNT;

        DELETE t
        FROM   ' + @Tracking + N' t
        JOIN   #totals d
          ON   t.QueueID = d.QueueID
         AND   t.ExceptionType = d.ExceptionType
        WHERE  t.ErrorTrackingID <> d.KeepId;

        UPDATE t
        SET    t.RetryCount = d.TotalRetries
        FROM   ' + @Tracking + N' t
        JOIN   #totals d
          ON   t.ErrorTrackingID = d.KeepId;

        DROP TABLE #totals;';

    EXEC sp_executesql @Sql, N'@CollapsedOut int OUTPUT', @CollapsedOut = @Collapsed OUTPUT;

    IF @Collapsed > 0
        PRINT N'Collapsed duplicates for ' + CAST(@Collapsed AS nvarchar(20))
            + N' message/exception pairs, retry counts summed';

    SET @Sql = N'CREATE UNIQUE INDEX IX_QueueIDExceptionType ON '
             + @Tracking + N' (QueueID, ExceptionType)';
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
    DECLARE @State int = ERROR_STATE();

    -- Severity is clamped rather than passed through: RAISERROR refuses anything
    -- above 18 without sysadmin and WITH LOG, so re-raising a higher one fails and
    -- the original error is lost with it.
    DECLARE @Severity int = CASE WHEN ERROR_SEVERITY() > 18 THEN 18 ELSE ERROR_SEVERITY() END;

    -- Passed as data, not as the format string. RAISERROR reads its first argument
    -- as a format, so a per cent sign anywhere in the original message - a table
    -- name, a value in a constraint error - would corrupt the diagnostic.
    --
    -- A live consumer can insert a duplicate between the collapse and the index
    -- creation, and that is what usually lands here. Nothing is left half-done -
    -- re-run, or stop consumers for the moment this takes.
    RAISERROR (N'%s', @Severity, @State, @Message);
END CATCH;
