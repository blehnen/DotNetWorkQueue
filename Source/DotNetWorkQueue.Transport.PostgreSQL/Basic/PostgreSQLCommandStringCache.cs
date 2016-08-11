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

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <summary>
    /// Caches SQL command strings
    /// </summary>
    public class PostgreSqlCommandStringCache
    {
        private readonly Dictionary<PostgreSqlCommandStringTypes, string> _commandCache;
        private readonly ConcurrentDictionary<string, string> _commandCacheRunTime;
        private readonly TableNameHelper _tableNameHelper;
        private readonly object _builderLock = new object();
        private volatile bool _complete;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlCommandStringCache" /> class.
        /// </summary>
        /// <param name="tableNameHelper">The table name helper.</param>
        public PostgreSqlCommandStringCache(TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _tableNameHelper = tableNameHelper;
            _commandCache = new Dictionary<PostgreSqlCommandStringTypes, string>();
            _commandCacheRunTime = new ConcurrentDictionary<string, string>();
        }

        /// <summary>
        /// Gets the command for the indicated command type
        /// </summary>
        /// <param name="type">The command type.</param>
        /// <returns></returns>
        public string GetCommand(PostgreSqlCommandStringTypes type)
        {
            if (!_complete)
                BuildCommands();

            // ReSharper disable once InconsistentlySynchronizedField
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
            if (_complete) return;
            lock (_builderLock)
            {
                if (_complete)
                    return;

                _commandCache.Add(PostgreSqlCommandStringTypes.DeleteFromErrorTracking,
                    $"delete from {_tableNameHelper.ErrorTrackingName} where queueID = @queueID");

                _commandCache.Add(PostgreSqlCommandStringTypes.DeleteFromQueue,
                    $"delete from {_tableNameHelper.QueueName} where queueID = @queueID");

                _commandCache.Add(PostgreSqlCommandStringTypes.DeleteFromMetaData,
                    $"delete from {_tableNameHelper.MetaDataName} where queueID = @queueID");

                _commandCache.Add(PostgreSqlCommandStringTypes.DeleteFromStatus,
                    $"delete from {_tableNameHelper.StatusName} where queueID = @queueID");

                _commandCache.Add(PostgreSqlCommandStringTypes.SaveConfiguration,
                    $"insert into {_tableNameHelper.ConfigurationName} (Configuration) Values (@Configuration)");

                _commandCache.Add(PostgreSqlCommandStringTypes.UpdateStatusRecord,
                    $"update {_tableNameHelper.StatusName} set status = @status where queueID = @queueID");

                _commandCache.Add(PostgreSqlCommandStringTypes.ResetHeartbeat,
                    $"update {_tableNameHelper.MetaDataName} set status = @Status, heartbeat = null where queueID = @QueueID and status = @SourceStatus and HeartBeat = @HeartBeat");

                _commandCache.Add(PostgreSqlCommandStringTypes.SendHeartBeat,
                    $"Update {_tableNameHelper.MetaDataName} set HeartBeat = @date where status = @status and queueID = @queueID");

                _commandCache.Add(PostgreSqlCommandStringTypes.InsertMessageBody,
                    $"Insert into {_tableNameHelper.QueueName} (Body, Headers) VALUES (@Body, @Headers); SELECT lastval(); ");

                _commandCache.Add(PostgreSqlCommandStringTypes.UpdateErrorCount,
                    $"update {_tableNameHelper.ErrorTrackingName} set retrycount = retrycount + 1 where queueid = @queueid and ExceptionType = @ExceptionType");

                _commandCache.Add(PostgreSqlCommandStringTypes.InsertErrorCount,
                    $"Insert into {_tableNameHelper.ErrorTrackingName} (QueueID,ExceptionType, RetryCount) VALUES (@QueueID,@ExceptionType,1)");

                _commandCache.Add(PostgreSqlCommandStringTypes.GetHeartBeatExpiredMessageIds,
                    $"select queueid, heartbeat from {_tableNameHelper.MetaDataName} where status = @status and heartbeat is not null and heartbeat < @time FOR UPDATE SKIP LOCKED");

                _commandCache.Add(PostgreSqlCommandStringTypes.GetColumnNamesFromTable,
                    "select column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name = @TableName");

                _commandCache.Add(PostgreSqlCommandStringTypes.GetErrorRecordExists,
                    $"Select 1 from {_tableNameHelper.ErrorTrackingName} where queueid = @queueid and ExceptionType = @ExceptionType");

                _commandCache.Add(PostgreSqlCommandStringTypes.GetErrorRetryCount,
                    $"Select RetryCount from {_tableNameHelper.ErrorTrackingName} where queueid = @queueid and ExceptionType = @ExceptionType");

                _commandCache.Add(PostgreSqlCommandStringTypes.GetConfiguration,
                    $"select Configuration from {_tableNameHelper.ConfigurationName}");

                _commandCache.Add(PostgreSqlCommandStringTypes.GetTableExists,
                    "SELECT 1 FROM pg_catalog.pg_class c JOIN  pg_catalog.pg_namespace n ON n.oid = c.relnamespace WHERE n.nspname = 'public' AND  c.relname = @Table;");

                _commandCache.Add(PostgreSqlCommandStringTypes.GetUtcDate,
                   "select now() at time zone 'utc'");

                _commandCache.Add(PostgreSqlCommandStringTypes.GetPendingExcludeDelayCount,
                     $"Select count(queueid) from {_tableNameHelper.MetaDataName} where status = {Convert.ToInt32(QueueStatuses.Waiting)} AND (QueueProcessTime < @CurrentDate)");

                _commandCache.Add(PostgreSqlCommandStringTypes.GetPendingCount,
                     $"Select count(queueid) from {_tableNameHelper.StatusName} where status = {Convert.ToInt32(QueueStatuses.Waiting)} ");

                _commandCache.Add(PostgreSqlCommandStringTypes.GetWorkingCount,
                    $"Select count(queueid) from {_tableNameHelper.StatusName} where status = {Convert.ToInt32(QueueStatuses.Processing)} ");

                _commandCache.Add(PostgreSqlCommandStringTypes.GetErrorCount,
                    $"Select count(queueid) from {_tableNameHelper.StatusName} where status = {Convert.ToInt32(QueueStatuses.Error)} ");

                _commandCache.Add(PostgreSqlCommandStringTypes.GetPendingDelayCount,
                    $"Select count(queueid) from {_tableNameHelper.MetaDataName} where status = {Convert.ToInt32(QueueStatuses.Waiting)} AND (QueueProcessTime > @CurrentDate) ");

                _commandCache.Add(PostgreSqlCommandStringTypes.GetJobLastKnownEvent,
                    $"Select JobEventTime from {_tableNameHelper.JobTableName} where JobName = @JobName");

                _commandCache.Add(PostgreSqlCommandStringTypes.SetJobLastKnownEvent,
                   $"Insert into {_tableNameHelper.JobTableName} (JobName, JobEventTime, JobScheduledTime) values (@JobName, @JobEventTime, @JobScheduledTime) on conflict (JobName) do update set (JobEventTime, JobScheduledTime) = (@JobEventTime, @JobScheduledTime) where {_tableNameHelper.JobTableName}.JobName = @JobName");

                _commandCache.Add(PostgreSqlCommandStringTypes.DoesJobExist,
                    $"Select Status from {_tableNameHelper.StatusName} where JobName = @JobName");

                _commandCache.Add(PostgreSqlCommandStringTypes.GetJobId,
                   $"Select QueueID from {_tableNameHelper.StatusName} where JobName = @JobName");

                _commandCache.Add(PostgreSqlCommandStringTypes.GetJobLastScheduleTime,
                   $"Select JobScheduledTime from {_tableNameHelper.JobTableName} where JobName = @JobName");

                //always set this last
                _complete = true;
            }
        }
    }

    /// <summary>
    /// Command types
    /// </summary>
    public enum PostgreSqlCommandStringTypes
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
