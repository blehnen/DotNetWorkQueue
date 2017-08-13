// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using System.Collections.Generic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// Caches SQL command strings
    /// </summary>
    public class SqLiteCommandStringCache: CommandStringCache
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqLiteCommandStringCache" /> class.
        /// </summary>
        /// <param name="tableNameHelper">The table name helper.</param>
        public SqLiteCommandStringCache(TableNameHelper tableNameHelper): base(tableNameHelper)
        {
        }

        /// <summary>
        /// Builds the commands.
        /// </summary>
        protected override void BuildCommands()
        {
            CommandCache.Add(CommandStringTypes.DeleteFromErrorTracking,
                $"delete from {TableNameHelper.ErrorTrackingName} where queueID = @queueID");

            CommandCache.Add(CommandStringTypes.DeleteFromQueue,
                $"delete from {TableNameHelper.QueueName} where queueID = @queueID");

            CommandCache.Add(CommandStringTypes.DeleteFromMetaData,
                $"delete from {TableNameHelper.MetaDataName} where queueID = @queueID");

            CommandCache.Add(CommandStringTypes.DeleteFromMetaDataErrors,
                $"delete from {TableNameHelper.MetaDataErrorsName} where queueID = @queueID");

            CommandCache.Add(CommandStringTypes.DeleteFromStatus,
                $"delete from {TableNameHelper.StatusName} where queueID = @queueID");

            CommandCache.Add(CommandStringTypes.SaveConfiguration,
                $"insert into {TableNameHelper.ConfigurationName} (Configuration) Values (@Configuration)");

            CommandCache.Add(CommandStringTypes.UpdateStatusRecord,
                $"update {TableNameHelper.StatusName} set status = @status where queueID = @queueID");

            CommandCache.Add(CommandStringTypes.ResetHeartbeat,
                $"update {TableNameHelper.MetaDataName} set status = @Status, heartbeat = null where queueID = @QueueID and status = @SourceStatus and HeartBeat = @HeartBeat");

            CommandCache.Add(CommandStringTypes.SendHeartBeat,
                $"Update {TableNameHelper.MetaDataName} set HeartBeat = @date where status = @status and queueID = @queueID");

            CommandCache.Add(CommandStringTypes.InsertMessageBody,
                $"Insert into {TableNameHelper.QueueName} (Body, Headers) VALUES (@Body, @Headers); SELECT last_insert_rowid(); ");

            CommandCache.Add(CommandStringTypes.UpdateErrorCount,
                $"update {TableNameHelper.ErrorTrackingName} set retrycount = retrycount + 1 where queueid = @queueid and ExceptionType = @ExceptionType");

            CommandCache.Add(CommandStringTypes.InsertErrorCount,
                $"Insert into {TableNameHelper.ErrorTrackingName} (QueueID,ExceptionType, RetryCount) VALUES (@QueueID,@ExceptionType,1)");

            CommandCache.Add(CommandStringTypes.GetHeartBeatExpiredMessageIds,
                $"select queueid, heartbeat from {TableNameHelper.MetaDataName} where status = @status and heartbeat is not null and heartbeat < @time");

            CommandCache.Add(CommandStringTypes.GetErrorRecordExists,
                $"Select 1 from {TableNameHelper.ErrorTrackingName} where queueid = @queueid and ExceptionType = @ExceptionType");

            CommandCache.Add(CommandStringTypes.GetErrorRetryCount,
                $"Select RetryCount from {TableNameHelper.ErrorTrackingName} where queueid = @queueid and ExceptionType = @ExceptionType");

            CommandCache.Add(CommandStringTypes.GetConfiguration,
                $"select Configuration from {TableNameHelper.ConfigurationName}");

            CommandCache.Add(CommandStringTypes.GetTableExists,
                "SELECT 1 FROM sqlite_master WHERE type='table' AND name=@Table;");

            CommandCache.Add(CommandStringTypes.GetPendingExcludeDelayCount,
                $"Select count(queueid) from {TableNameHelper.MetaDataName} where status = {Convert.ToInt32(QueueStatuses.Waiting)} AND (QueueProcessTime < @CurrentDateTime)");

            CommandCache.Add(CommandStringTypes.GetPendingCount,
                $"Select count(queueid) from {TableNameHelper.StatusName} where status = {Convert.ToInt32(QueueStatuses.Waiting)} ");

            CommandCache.Add(CommandStringTypes.GetWorkingCount,
                $"Select count(queueid) from {TableNameHelper.StatusName} where status = {Convert.ToInt32(QueueStatuses.Processing)} ");

            CommandCache.Add(CommandStringTypes.GetErrorCount,
                $"Select count(queueid) from {TableNameHelper.StatusName} where status = {Convert.ToInt32(QueueStatuses.Error)} ");

            CommandCache.Add(CommandStringTypes.GetPendingDelayCount,
                $"Select count(queueid) from {TableNameHelper.MetaDataName} where status = {Convert.ToInt32(QueueStatuses.Waiting)} AND (QueueProcessTime > @CurrentDateTime) ");

            CommandCache.Add(CommandStringTypes.GetJobLastKnownEvent,
                $"Select JobEventTime from {TableNameHelper.JobTableName} where JobName = @JobName");

            CommandCache.Add(CommandStringTypes.SetJobLastKnownEvent,
                $"UPDATE {TableNameHelper.JobTableName} SET JobEventTime = @JobEventTime, JobScheduledTime = @JobScheduledTime WHERE JobName=@JobName; INSERT OR IGNORE INTO {TableNameHelper.JobTableName}(JobName, JobEventTime, JobScheduledTime) VALUES(@JobName, @JobEventTime, @JobScheduledTime);");

            CommandCache.Add(CommandStringTypes.GetJobLastScheduleTime,
                $"Select JobScheduledTime from {TableNameHelper.JobTableName} where JobName = @JobName");

            CommandCache.Add(CommandStringTypes.DoesJobExist,
                $"Select Status from {TableNameHelper.StatusName} where JobName = @JobName");

            CommandCache.Add(CommandStringTypes.GetJobId,
                $"Select QueueID from {TableNameHelper.StatusName} where JobName = @JobName");
        }
    }
}
