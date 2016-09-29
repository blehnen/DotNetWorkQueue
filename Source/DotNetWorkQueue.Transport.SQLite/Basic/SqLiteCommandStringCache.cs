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

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// Caches SQL command strings
    /// </summary>
    public class SqLiteCommandStringCache
    {
        private readonly Dictionary<SqLiteCommandStringTypes, string> _commandCache;
        private readonly ConcurrentDictionary<string, CommandString> _commandCacheRunTime;
        private readonly TableNameHelper _tableNameHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqLiteCommandStringCache" /> class.
        /// </summary>
        /// <param name="tableNameHelper">The table name helper.</param>
        public SqLiteCommandStringCache(TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _tableNameHelper = tableNameHelper;
            _commandCache = new Dictionary<SqLiteCommandStringTypes, string>();
            _commandCacheRunTime = new ConcurrentDictionary<string, CommandString>();

            BuildCommands();
        }

        /// <summary>
        /// Gets the command for the indicated command type
        /// </summary>
        /// <param name="type">The command type.</param>
        /// <returns></returns>
        public string GetCommand(SqLiteCommandStringTypes type)
        {
            return _commandCache[type];
        }

        /// <summary>
        /// Adds a new cached command string
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public CommandString Add(string key, CommandString value)
        {
            _commandCacheRunTime.TryAdd(key, value);
            return value;
        }

        /// <summary>
        /// Gets the specified command string.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public CommandString Get(string key)
        {
            CommandString value;
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
            _commandCache.Add(SqLiteCommandStringTypes.DeleteFromErrorTracking,
                $"delete from {_tableNameHelper.ErrorTrackingName} where queueID = @queueID");

            _commandCache.Add(SqLiteCommandStringTypes.DeleteFromQueue,
                $"delete from {_tableNameHelper.QueueName} where queueID = @queueID");

            _commandCache.Add(SqLiteCommandStringTypes.DeleteFromMetaData,
                $"delete from {_tableNameHelper.MetaDataName} where queueID = @queueID");

            _commandCache.Add(SqLiteCommandStringTypes.DeleteFromMetaDataErrors,
                $"delete from {_tableNameHelper.MetaDataErrorsName} where queueID = @queueID");

            _commandCache.Add(SqLiteCommandStringTypes.DeleteFromStatus,
                $"delete from {_tableNameHelper.StatusName} where queueID = @queueID");

            _commandCache.Add(SqLiteCommandStringTypes.SaveConfiguration,
                $"insert into {_tableNameHelper.ConfigurationName} (Configuration) Values (@Configuration)");

            _commandCache.Add(SqLiteCommandStringTypes.UpdateStatusRecord,
                $"update {_tableNameHelper.StatusName} set status = @status where queueID = @queueID");

            _commandCache.Add(SqLiteCommandStringTypes.ResetHeartbeat,
                $"update {_tableNameHelper.MetaDataName} set status = @Status, heartbeat = null where queueID = @QueueID and status = @SourceStatus and HeartBeat = @HeartBeat");

            _commandCache.Add(SqLiteCommandStringTypes.SendHeartBeat,
                $"Update {_tableNameHelper.MetaDataName} set HeartBeat = @date where status = @status and queueID = @queueID");

            _commandCache.Add(SqLiteCommandStringTypes.InsertMessageBody,
                $"Insert into {_tableNameHelper.QueueName} (Body, Headers) VALUES (@Body, @Headers); SELECT last_insert_rowid(); ");

            _commandCache.Add(SqLiteCommandStringTypes.UpdateErrorCount,
                $"update {_tableNameHelper.ErrorTrackingName} set retrycount = retrycount + 1 where queueid = @queueid and ExceptionType = @ExceptionType");

            _commandCache.Add(SqLiteCommandStringTypes.InsertErrorCount,
                $"Insert into {_tableNameHelper.ErrorTrackingName} (QueueID,ExceptionType, RetryCount) VALUES (@QueueID,@ExceptionType,1)");

            _commandCache.Add(SqLiteCommandStringTypes.GetHeartBeatExpiredMessageIds,
                $"select queueid, heartbeat from {_tableNameHelper.MetaDataName} where status = @status and heartbeat is not null and heartbeat < @time");

            _commandCache.Add(SqLiteCommandStringTypes.GetErrorRecordExists,
                $"Select 1 from {_tableNameHelper.ErrorTrackingName} where queueid = @queueid and ExceptionType = @ExceptionType");

            _commandCache.Add(SqLiteCommandStringTypes.GetErrorRetryCount,
                $"Select RetryCount from {_tableNameHelper.ErrorTrackingName} where queueid = @queueid and ExceptionType = @ExceptionType");

            _commandCache.Add(SqLiteCommandStringTypes.GetConfiguration,
                $"select Configuration from {_tableNameHelper.ConfigurationName}");

            _commandCache.Add(SqLiteCommandStringTypes.GetTableExists,
                "SELECT 1 FROM sqlite_master WHERE type='table' AND name=@Table;");

            _commandCache.Add(SqLiteCommandStringTypes.GetPendingExcludeDelayCount,
                $"Select count(queueid) from {_tableNameHelper.MetaDataName} where status = {Convert.ToInt32(QueueStatuses.Waiting)} AND (QueueProcessTime < @CurrentDateTime)");

            _commandCache.Add(SqLiteCommandStringTypes.GetPendingCount,
                $"Select count(queueid) from {_tableNameHelper.StatusName} where status = {Convert.ToInt32(QueueStatuses.Waiting)} ");

            _commandCache.Add(SqLiteCommandStringTypes.GetWorkingCount,
                $"Select count(queueid) from {_tableNameHelper.StatusName} where status = {Convert.ToInt32(QueueStatuses.Processing)} ");

            _commandCache.Add(SqLiteCommandStringTypes.GetErrorCount,
                $"Select count(queueid) from {_tableNameHelper.StatusName} where status = {Convert.ToInt32(QueueStatuses.Error)} ");

            _commandCache.Add(SqLiteCommandStringTypes.GetPendingDelayCount,
                $"Select count(queueid) from {_tableNameHelper.MetaDataName} where status = {Convert.ToInt32(QueueStatuses.Waiting)} AND (QueueProcessTime > @CurrentDateTime) ");

            _commandCache.Add(SqLiteCommandStringTypes.GetJobLastKnownEvent,
                $"Select JobEventTime from {_tableNameHelper.JobTableName} where JobName = @JobName");

            _commandCache.Add(SqLiteCommandStringTypes.SetJobLastKnownEvent,
                $"UPDATE {_tableNameHelper.JobTableName} SET JobEventTime = @JobEventTime, JobScheduledTime = @JobScheduledTime WHERE JobName=@JobName; INSERT OR IGNORE INTO {_tableNameHelper.JobTableName}(JobName, JobEventTime, JobScheduledTime) VALUES(@JobName, @JobEventTime, @JobScheduledTime);");

            _commandCache.Add(SqLiteCommandStringTypes.GetJobLastScheduleTime,
                $"Select JobScheduledTime from {_tableNameHelper.JobTableName} where JobName = @JobName");

            _commandCache.Add(SqLiteCommandStringTypes.DoesJobExist,
                $"Select Status from {_tableNameHelper.StatusName} where JobName = @JobName");

            _commandCache.Add(SqLiteCommandStringTypes.GetJobId,
                $"Select QueueID from {_tableNameHelper.StatusName} where JobName = @JobName");
        }
    }

    /// <summary>
    /// Contains the primary command and any secondary commands that should be executed after the primary command completes.
    /// </summary>
    public class CommandString
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandString"/> class.
        /// </summary>
        /// <param name="commandString">The command string.</param>
        /// <param name="commands">The commands.</param>
        public CommandString(string commandString, List<string> commands)
        {
            CommandText = commandString;
            AdditionalCommands = commands;
        }
        /// <summary>
        /// Gets the command text.
        /// </summary>
        /// <value>
        /// The command text.
        /// </value>
        public string CommandText { get; private set; }
        /// <summary>
        /// Gets the additional commands.
        /// </summary>
        /// <value>
        /// The additional commands.
        /// </value>
        public List<string> AdditionalCommands { get; private set; }
    }
    /// <summary>
    /// Command types
    /// </summary>
    public enum SqLiteCommandStringTypes
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
        GetJobLastScheduleTime,
        /// <summary>
        /// Deletes a record from the meta data error table
        /// </summary>
        DeleteFromMetaDataErrors
    }
}
