// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    /// <summary>
    /// Caches SQL command strings
    /// </summary>
    public class SqlServerCommandStringCache
    {
        private readonly Dictionary<SqlServerCommandStringTypes, string> _commandCache;
        private readonly ConcurrentDictionary<string, string> _commandCacheRunTime;
        private readonly TableNameHelper _tableNameHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerCommandStringCache" /> class.
        /// </summary>
        /// <param name="tableNameHelper">The table name helper.</param>
        public SqlServerCommandStringCache(TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _tableNameHelper = tableNameHelper;
            _commandCache = new Dictionary<SqlServerCommandStringTypes, string>();
            _commandCacheRunTime = new ConcurrentDictionary<string, string>();

            BuildCommands();
        }

        /// <summary>
        /// Gets the command for the indicated command type
        /// </summary>
        /// <param name="type">The command type.</param>
        /// <returns></returns>
        public string GetCommand(SqlServerCommandStringTypes type)
        {
            return _commandCache[type];
        }

        /// <summary>
        /// Adds a new cached command string
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public string Add(string key, string value)
        {
            _commandCacheRunTime.TryAdd(key, value);
            return value;
        }

        /// <summary>
        /// Gets the specified command string.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public string Get(string key)
        {
            string value;
            return _commandCacheRunTime.TryGetValue(key, out value) ? value : null;
        }

        /// <summary>
        /// Determines whether [contains] [the specified key].
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public bool Contains(string key)
        {
            return _commandCacheRunTime.ContainsKey(key);
        }

        /// <summary>
        /// Builds the commands.
        /// </summary>
        private void BuildCommands()
        {
            _commandCache.Add(SqlServerCommandStringTypes.DeleteFromErrorTracking,
                $"delete from {_tableNameHelper.ErrorTrackingName} where queueID = @queueID");

            _commandCache.Add(SqlServerCommandStringTypes.DeleteFromQueue,
                $"delete from {_tableNameHelper.QueueName} where queueID = @queueID");

            _commandCache.Add(SqlServerCommandStringTypes.DeleteFromMetaData,
                $"delete from {_tableNameHelper.MetaDataName} where queueID = @queueID");

            _commandCache.Add(SqlServerCommandStringTypes.DeleteFromStatus,
                $"delete from {_tableNameHelper.StatusName} where queueID = @queueID");

            _commandCache.Add(SqlServerCommandStringTypes.SaveConfiguration,
                $"insert into {_tableNameHelper.ConfigurationName} (Configuration) Values (@Configuration)");

            _commandCache.Add(SqlServerCommandStringTypes.UpdateStatusRecord,
                $"update {_tableNameHelper.StatusName} set status = @status where queueID = @queueID");

            _commandCache.Add(SqlServerCommandStringTypes.ResetHeartbeat,
                $"update {_tableNameHelper.MetaDataName} with (updlock, readpast, rowlock) set status = @Status, heartbeat = null where queueID = @QueueID and status = @SourceStatus and HeartBeat = @HeartBeat");

            _commandCache.Add(SqlServerCommandStringTypes.SendHeartBeat,
                $"declare @date as datetime set @date = GETUTCDATE() Update {_tableNameHelper.MetaDataName} set HeartBeat = @date where status = @status and queueID = @queueID select @date");

            _commandCache.Add(SqlServerCommandStringTypes.InsertMessageBody,
                $"Insert into {_tableNameHelper.QueueName} (Body, Headers) VALUES (@Body, @Headers) select SCOPE_IDENTITY() ");

            _commandCache.Add(SqlServerCommandStringTypes.UpdateErrorCount,
                $"update {_tableNameHelper.ErrorTrackingName} set retrycount = retrycount + 1 where queueid = @queueid and ExceptionType = @ExceptionType");

            _commandCache.Add(SqlServerCommandStringTypes.InsertErrorCount,
                $"Insert into {_tableNameHelper.ErrorTrackingName} (QueueID,ExceptionType, RetryCount) VALUES (@QueueID,@ExceptionType,1)");

            _commandCache.Add(SqlServerCommandStringTypes.GetHeartBeatExpiredMessageIds,
                $"select queueid, heartbeat from {_tableNameHelper.MetaDataName} with (updlock, readpast, rowlock) where status = @status and heartbeat is not null and (DATEDIFF(SECOND, heartbeat, GETUTCDATE()) > @time)");

            _commandCache.Add(SqlServerCommandStringTypes.GetColumnNamesFromTable,
                "select c.name from sys.columns c inner join sys.tables t on t.object_id = c.object_id and t.name = @TableName and t.type = 'U'");

            _commandCache.Add(SqlServerCommandStringTypes.GetErrorRecordExists,
                $"Select 1 from {_tableNameHelper.ErrorTrackingName} where queueid = @queueid and ExceptionType = @ExceptionType");

            _commandCache.Add(SqlServerCommandStringTypes.GetErrorRetryCount,
                $"Select RetryCount from {_tableNameHelper.ErrorTrackingName} where queueid = @queueid and ExceptionType = @ExceptionType");

            _commandCache.Add(SqlServerCommandStringTypes.GetConfiguration,
                $"select Configuration from {_tableNameHelper.ConfigurationName}");

            _commandCache.Add(SqlServerCommandStringTypes.GetTableExists,
                "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_CATALOG = @Database AND TABLE_NAME = @Table");

            _commandCache.Add(SqlServerCommandStringTypes.GetUtcDate,
                "select getutcdate()");

            _commandCache.Add(SqlServerCommandStringTypes.GetPendingExcludeDelayCount,
                $"Select count(queueid) from {_tableNameHelper.MetaDataName} with (NOLOCK) where status = {Convert.ToInt32(QueueStatuses.Waiting)} AND (QueueProcessTime < getutcdate())");

            _commandCache.Add(SqlServerCommandStringTypes.GetPendingCount,
                $"Select count(queueid) from {_tableNameHelper.StatusName} with (NOLOCK) where status = {Convert.ToInt32(QueueStatuses.Waiting)} ");

            _commandCache.Add(SqlServerCommandStringTypes.GetWorkingCount,
                $"Select count(queueid) from {_tableNameHelper.StatusName} with (NOLOCK) where status = {Convert.ToInt32(QueueStatuses.Processing)} ");

            _commandCache.Add(SqlServerCommandStringTypes.GetErrorCount,
                $"Select count(queueid) from {_tableNameHelper.StatusName} with (NOLOCK) where status = {Convert.ToInt32(QueueStatuses.Error)} ");

            _commandCache.Add(SqlServerCommandStringTypes.GetPendingDelayCount,
                $"Select count(queueid) from {_tableNameHelper.MetaDataName} with (NOLOCK) where status = {Convert.ToInt32(QueueStatuses.Waiting)} AND (QueueProcessTime > getutcdate()) ");

            _commandCache.Add(SqlServerCommandStringTypes.GetJobLastKnownEvent,
                $"Select JobEventTime from {_tableNameHelper.JobTableName} where JobName = @JobName");

            _commandCache.Add(SqlServerCommandStringTypes.SetJobLastKnownEvent,
                $"MERGE {_tableNameHelper.JobTableName} USING(VALUES(@JobName, @JobEventTime, @JobScheduledTime)) AS updateJob(JobName, JobEventTime, JobScheduledTime) ON {_tableNameHelper.JobTableName}.JobName = updateJob.JobName WHEN MATCHED THEN UPDATE SET JobEventTime = updateJob.JobEventTime, JobScheduledTime = updateJob.JobScheduledTime WHEN NOT MATCHED THEN INSERT(JobName, JobEventTime, JobScheduledTime) VALUES(JobName, JobEventTime, JobScheduledTime); ");

            _commandCache.Add(SqlServerCommandStringTypes.DoesJobExist,
                $"Select Status from {_tableNameHelper.StatusName} where JobName = @JobName");

            _commandCache.Add(SqlServerCommandStringTypes.GetJobId,
                $"Select QueueID from {_tableNameHelper.StatusName} where JobName = @JobName");

            _commandCache.Add(SqlServerCommandStringTypes.GetJobLastScheduleTime,
                $"Select JobScheduledTime from {_tableNameHelper.JobTableName} where JobName = @JobName");
        }
    }

    /// <summary>
    /// Command types
    /// </summary>
    public enum SqlServerCommandStringTypes
    {
        /// <summary>
        /// delete from meta data
        /// </summary>
        DeleteFromMetaData,
        /// <summary>
        /// delete from queue
        /// </summary>
        DeleteFromQueue,
        /// <summary>
        /// delete from error tracking
        /// </summary>
        DeleteFromErrorTracking,
        /// <summary>
        /// delete from status table
        /// </summary>
        DeleteFromStatus,
        /// <summary>
        /// save configuration
        /// </summary>
        SaveConfiguration,
        /// <summary>
        /// update status record
        /// </summary>
        UpdateStatusRecord,
        /// <summary>
        /// reset heartbeat
        /// </summary>
        ResetHeartbeat,
        /// <summary>
        /// send heart beat
        /// </summary>
        SendHeartBeat,
        /// <summary>
        /// insert message body
        /// </summary>
        InsertMessageBody,
        /// <summary>
        /// update error count
        /// </summary>
        UpdateErrorCount,
        /// <summary>
        /// insert error count
        /// </summary>
        InsertErrorCount,
        /// <summary>
        /// get heart beat expired message ids
        /// </summary>
        GetHeartBeatExpiredMessageIds,
        /// <summary>
        /// get column names from table
        /// </summary>
        GetColumnNamesFromTable,
        /// <summary>
        /// get error record exists
        /// </summary>
        GetErrorRecordExists,
        /// <summary>
        /// get error retry count
        /// </summary>
        GetErrorRetryCount,
        /// <summary>
        /// get configuration
        /// </summary>
        GetConfiguration,
        /// <summary>
        /// get table exists
        /// </summary>
        GetTableExists,
        /// <summary>
        /// get UTC date
        /// </summary>
        GetUtcDate,
        /// <summary>
        /// Gets the number of pending items from the queue
        /// </summary>
        GetPendingCount,
        /// <summary>
        /// Gets the number of pending items from the queue, not included items that are still delayed
        /// </summary>
        GetPendingExcludeDelayCount,
        /// <summary>
        /// Gets the number of working items from the queue
        /// </summary>
        GetWorkingCount,
        /// <summary>
        /// Gets the number of items that have stopped processing due to an error
        /// </summary>
        GetErrorCount,
        /// <summary>
        /// Gets the number of records that are pending, but are scheduled for a future time
        /// </summary>
        GetPendingDelayCount,
        /// <summary>
        /// Gets the last known event time for a job
        /// </summary>
        GetJobLastKnownEvent,
        /// <summary>
        /// Sets the last known event time for a job
        /// </summary>
        SetJobLastKnownEvent,
        /// <summary>
        /// Determines if a job (via job name) already is queued
        /// </summary>
        DoesJobExist,
        /// <summary>
        /// Gets job identifier (via job name)
        /// </summary>
        GetJobId,
        /// <summary>
        /// The get job last schedule time from the last time the job was queued
        /// </summary>
        GetJobLastScheduleTime
    }
}
