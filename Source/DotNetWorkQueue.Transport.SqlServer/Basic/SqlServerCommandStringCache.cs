// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;

namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    /// <summary>
    /// Caches SQL command strings
    /// </summary>
    public class SqlServerCommandStringCache : CommandStringCache
    {
        private readonly ISqlSchema _schema;

        /// <summary>Initializes a new instance of the <see cref="SqlServerCommandStringCache"/> class.</summary>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="schema">The schema that the queue is using</param>
        public SqlServerCommandStringCache(ITableNameHelper tableNameHelper, ISqlSchema schema) : base(tableNameHelper)
        {
            _schema = schema;
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
                $"update {TableNameHelper.MetaDataName} with (updlock, readpast, rowlock) set status = @Status, heartbeat = null where queueID = @QueueID and status = @SourceStatus and HeartBeat = @HeartBeat");

            CommandCache.Add(CommandStringTypes.SendHeartBeat,
                $"declare @date as datetime set @date = GETUTCDATE() Update {TableNameHelper.MetaDataName} set HeartBeat = @date where status = @status and queueID = @queueID select @date");

            CommandCache.Add(CommandStringTypes.InsertMessageBody,
                $"Insert into {TableNameHelper.QueueName} (Body, Headers) VALUES (@Body, @Headers) select SCOPE_IDENTITY() ");

            CommandCache.Add(CommandStringTypes.UpdateErrorCount,
                $"update {TableNameHelper.ErrorTrackingName} set retrycount = retrycount + 1 where queueid = @queueid and ExceptionType = @ExceptionType");

            CommandCache.Add(CommandStringTypes.InsertErrorCount,
                $"Insert into {TableNameHelper.ErrorTrackingName} (QueueID,ExceptionType, RetryCount) VALUES (@QueueID,@ExceptionType,1)");

            CommandCache.Add(CommandStringTypes.GetHeartBeatExpiredMessageIds,
                $"select {TableNameHelper.MetaDataName}.queueid, heartbeat, headers from {TableNameHelper.MetaDataName} with (updlock, readpast, rowlock) inner join {TableNameHelper.QueueName} on {TableNameHelper.QueueName}.queueid = {TableNameHelper.MetaDataName}.queueid where status = @status and heartbeat is not null and (DATEDIFF(SECOND, heartbeat, GETUTCDATE()) > @time)");

            CommandCache.Add(CommandStringTypes.GetColumnNamesFromTable,
                "SELECT c.name FROM sys.columns c WHERE c.object_id = OBJECT_ID(@TableName)");

            CommandCache.Add(CommandStringTypes.GetErrorRecordExists,
                $"Select 1 from {TableNameHelper.ErrorTrackingName} where queueid = @queueid and ExceptionType = @ExceptionType");

            CommandCache.Add(CommandStringTypes.GetErrorRetryCount,
                $"Select RetryCount from {TableNameHelper.ErrorTrackingName} where queueid = @queueid and ExceptionType = @ExceptionType");

            CommandCache.Add(CommandStringTypes.GetMessageErrors,
                $"Select ExceptionType, RetryCount from {TableNameHelper.ErrorTrackingName} where queueid = @QueueID");

            CommandCache.Add(CommandStringTypes.GetConfiguration,
                $"select Configuration from {TableNameHelper.ConfigurationName}");

            CommandCache.Add(CommandStringTypes.GetTableExists,
                $"SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_CATALOG = @Database AND TABLE_NAME = @Table and TABLE_SCHEMA = '{_schema.Schema}'");

            CommandCache.Add(CommandStringTypes.GetUtcDate,
                "select getutcdate()");

            CommandCache.Add(CommandStringTypes.GetPendingExcludeDelayCount,
                $"Select count(queueid) from {TableNameHelper.MetaDataName} with (NOLOCK) where status = {Convert.ToInt32(QueueStatuses.Waiting)} AND (QueueProcessTime < getutcdate())");

            CommandCache.Add(CommandStringTypes.GetPendingCount,
                $"Select count(queueid) from {TableNameHelper.StatusName} with (NOLOCK) where status = {Convert.ToInt32(QueueStatuses.Waiting)} ");

            CommandCache.Add(CommandStringTypes.GetWorkingCount,
                $"Select count(queueid) from {TableNameHelper.StatusName} with (NOLOCK) where status = {Convert.ToInt32(QueueStatuses.Processing)} ");

            CommandCache.Add(CommandStringTypes.GetErrorCount,
                $"Select count(queueid) from {TableNameHelper.StatusName} with (NOLOCK) where status = {Convert.ToInt32(QueueStatuses.Error)} ");

            CommandCache.Add(CommandStringTypes.GetPendingDelayCount,
                $"Select count(queueid) from {TableNameHelper.MetaDataName} with (NOLOCK) where status = {Convert.ToInt32(QueueStatuses.Waiting)} AND (QueueProcessTime > getutcdate()) ");

            CommandCache.Add(CommandStringTypes.GetJobLastKnownEvent,
                $"Select JobEventTime from {TableNameHelper.JobTableName} where JobName = @JobName");

            CommandCache.Add(CommandStringTypes.SetJobLastKnownEvent,
                $"MERGE {TableNameHelper.JobTableName} USING(VALUES(@JobName, @JobEventTime, @JobScheduledTime)) AS updateJob(JobName, JobEventTime, JobScheduledTime) ON {TableNameHelper.JobTableName}.JobName = updateJob.JobName WHEN MATCHED THEN UPDATE SET JobEventTime = updateJob.JobEventTime, JobScheduledTime = updateJob.JobScheduledTime WHEN NOT MATCHED THEN INSERT(JobName, JobEventTime, JobScheduledTime) VALUES(JobName, JobEventTime, JobScheduledTime); ");

            CommandCache.Add(CommandStringTypes.DoesJobExist,
                $"Select Status from {TableNameHelper.StatusName} where JobName = @JobName");

            CommandCache.Add(CommandStringTypes.GetJobId,
                $"Select QueueID from {TableNameHelper.StatusName} where JobName = @JobName");

            CommandCache.Add(CommandStringTypes.GetJobLastScheduleTime,
                $"Select JobScheduledTime from {TableNameHelper.JobTableName} where JobName = @JobName");

            CommandCache.Add(CommandStringTypes.FindExpiredRecordsWithStatusToDelete,
                $"select queueid from {TableNameHelper.MetaDataName} with (updlock, readpast, rowlock) where status = {Convert.ToInt16(QueueStatuses.Waiting)} and GETUTCDate() > ExpirationTime");

            CommandCache.Add(CommandStringTypes.FindErrorRecordsToDelete,
                $"select queueid from {TableNameHelper.MetaDataErrorsName} with (updlock, readpast, rowlock) where @CurrentDate > lastexceptiondate");

            CommandCache.Add(CommandStringTypes.GetQueueCountStatus,
                $"select count_big(queueid) from {TableNameHelper.MetaDataName} where status = @status");

            CommandCache.Add(CommandStringTypes.GetQueueCountAll,
                $"select count_big(queueid) from {TableNameHelper.MetaDataName}");

            CommandCache.Add(CommandStringTypes.FindExpiredRecordsToDelete,
                $"select queueid from {TableNameHelper.MetaDataName} with (updlock, readpast, rowlock) where GETUTCDate() > ExpirationTime");

            CommandCache.Add(CommandStringTypes.DeleteTable,
                "IF OBJECT_ID('{0}', 'U') IS NOT NULL DROP TABLE {0};");

            CommandCache.Add(CommandStringTypes.GetHeader,
                $"select headers from {TableNameHelper.QueueName} WITH (NOLOCK) where queueid = @queueid ");
        }
    }
}
