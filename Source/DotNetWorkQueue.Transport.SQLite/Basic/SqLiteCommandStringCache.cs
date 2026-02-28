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
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// Caches SQL command strings
    /// </summary>
    public class IDbCommandStringCache : CommandStringCache
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IDbCommandStringCache" /> class.
        /// </summary>
        /// <param name="tableNameHelper">The table name helper.</param>
        public IDbCommandStringCache(ITableNameHelper tableNameHelper) : base(tableNameHelper)
        {
        }

        /// <summary>
        /// Builds the commands.
        /// </summary>
        protected override void BuildCommands()
        {
            CommandCache.Add(CommandStringTypes.DeleteFromErrorTracking,
                $"delete from {TableNameHelper.ErrorTrackingName} where queueID = @QueueID");

            CommandCache.Add(CommandStringTypes.DeleteFromQueue,
                $"delete from {TableNameHelper.QueueName} where queueID = @QueueID");

            CommandCache.Add(CommandStringTypes.DeleteFromMetaData,
                $"delete from {TableNameHelper.MetaDataName} where queueID = @QueueID");

            CommandCache.Add(CommandStringTypes.DeleteFromMetaDataErrors,
                $"delete from {TableNameHelper.MetaDataErrorsName} where queueID = @QueueID");

            CommandCache.Add(CommandStringTypes.DeleteFromStatus,
                $"delete from {TableNameHelper.StatusName} where queueID = @QueueID");

            CommandCache.Add(CommandStringTypes.SaveConfiguration,
                $"insert into {TableNameHelper.ConfigurationName} (Configuration) Values (@Configuration)");

            CommandCache.Add(CommandStringTypes.UpdateStatusRecord,
                $"update {TableNameHelper.StatusName} set status = @Status where queueID = @QueueID");

            CommandCache.Add(CommandStringTypes.ResetHeartbeat,
                $"update {TableNameHelper.MetaDataName} set status = @Status, heartbeat = null where queueID = @QueueID and status = @SourceStatus and HeartBeat = @HeartBeat");

            CommandCache.Add(CommandStringTypes.SendHeartBeat,
                $"Update {TableNameHelper.MetaDataName} set HeartBeat = @date where status = @Status and queueID = @QueueID");

            CommandCache.Add(CommandStringTypes.InsertMessageBody,
                $"Insert into {TableNameHelper.QueueName} (Body, Headers) VALUES (@Body, @Headers); ");

            CommandCache.Add(CommandStringTypes.UpdateErrorCount,
                $"update {TableNameHelper.ErrorTrackingName} set retrycount = retrycount + 1 where queueid = @QueueID and ExceptionType = @ExceptionType");

            CommandCache.Add(CommandStringTypes.InsertErrorCount,
                $"Insert into {TableNameHelper.ErrorTrackingName} (QueueID,ExceptionType, RetryCount) VALUES (@QueueID,@ExceptionType,1)");

            CommandCache.Add(CommandStringTypes.GetHeartBeatExpiredMessageIds,
                $"select {TableNameHelper.MetaDataName}.queueid, heartbeat, headers from {TableNameHelper.MetaDataName} inner join {TableNameHelper.QueueName} on {TableNameHelper.QueueName}.queueid = {TableNameHelper.MetaDataName}.queueid where status = @Status and heartbeat is not null and heartbeat < @Time");

            CommandCache.Add(CommandStringTypes.GetErrorRecordExists,
                $"Select 1 from {TableNameHelper.ErrorTrackingName} where queueid = @QueueID and ExceptionType = @ExceptionType");

            CommandCache.Add(CommandStringTypes.GetErrorRetryCount,
                $"Select RetryCount from {TableNameHelper.ErrorTrackingName} where queueid = @QueueID and ExceptionType = @ExceptionType");

            CommandCache.Add(CommandStringTypes.GetMessageErrors,
                $"Select ExceptionType, RetryCount from {TableNameHelper.ErrorTrackingName} where queueid = @QueueID");

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

            CommandCache.Add(CommandStringTypes.FindExpiredRecordsToDelete,
                $"select queueid from {TableNameHelper.MetaDataName} where @CurrentDateTime > ExpirationTime");

            CommandCache.Add(CommandStringTypes.FindExpiredRecordsWithStatusToDelete,
                $"select queueid from {TableNameHelper.MetaDataName} where status = {Convert.ToInt16(QueueStatuses.Waiting)} and @CurrentDateTime > ExpirationTime");

            CommandCache.Add(CommandStringTypes.FindErrorRecordsToDelete,
                $"select queueid from {TableNameHelper.MetaDataErrorsName} where @CurrentDateTime > LastExceptionDate");

            CommandCache.Add(CommandStringTypes.GetQueueCountStatus,
                $"select count(queueid) from {TableNameHelper.MetaDataName} where status = @status");

            CommandCache.Add(CommandStringTypes.GetQueueCountAll,
                $"select count(queueid) from {TableNameHelper.MetaDataName}");

            CommandCache.Add(CommandStringTypes.GetColumnNamesFromTable,
                "PRAGMA table_info('{0}')");

            CommandCache.Add(CommandStringTypes.DeleteTable,
                "drop table {0}");

            CommandCache.Add(CommandStringTypes.GetHeader,
                $"select headers from {TableNameHelper.QueueName} where queueID = @queueID");

            // Dashboard queries
            CommandCache.Add(CommandStringTypes.GetDashboardStatusCounts,
                $"SELECT SUM(CASE WHEN status = {Convert.ToInt32(QueueStatuses.Waiting)} THEN 1 ELSE 0 END) AS Waiting, SUM(CASE WHEN status = {Convert.ToInt32(QueueStatuses.Processing)} THEN 1 ELSE 0 END) AS Processing, SUM(CASE WHEN status = {Convert.ToInt32(QueueStatuses.Error)} THEN 1 ELSE 0 END) AS Error, count(*) AS Total FROM {TableNameHelper.StatusName}");

            CommandCache.Add(CommandStringTypes.GetDashboardErrorMessageCount,
                $"SELECT count(*) FROM {TableNameHelper.MetaDataErrorsName}");

            CommandCache.Add(CommandStringTypes.GetDashboardErrorRetries,
                $"SELECT ErrorTrackingID, QueueID, ExceptionType, RetryCount FROM {TableNameHelper.ErrorTrackingName} WHERE QueueID = @QueueId ORDER BY ErrorTrackingID");

            CommandCache.Add(CommandStringTypes.GetDashboardConfiguration,
                $"SELECT Configuration FROM {TableNameHelper.ConfigurationName}");

            CommandCache.Add(CommandStringTypes.GetDashboardJobs,
                $"SELECT JobName, JobEventTime, JobScheduledTime FROM {TableNameHelper.JobTableName}");

            CommandCache.Add(CommandStringTypes.GetDashboardErrorMessages,
                $"SELECT ID, QueueID, LastException, LastExceptionDate FROM {TableNameHelper.MetaDataErrorsName} ORDER BY LastExceptionDate DESC LIMIT @PageSize OFFSET @Offset");



            CommandCache.Add(CommandStringTypes.GetDashboardMessages,
                $"SELECT QueueID, QueuedDateTime, CorrelationID{{0}} FROM {TableNameHelper.MetaDataName} ORDER BY QueueID DESC LIMIT @PageSize OFFSET @Offset");

            CommandCache.Add(CommandStringTypes.GetDashboardMessageCount,
                $"SELECT count(*) FROM {TableNameHelper.MetaDataName}");

            CommandCache.Add(CommandStringTypes.GetDashboardMessageDetail,
                $"SELECT QueueID, QueuedDateTime, CorrelationID{{0}} FROM {TableNameHelper.MetaDataName} WHERE QueueID = @QueueId");

            CommandCache.Add(CommandStringTypes.GetDashboardStaleMessages,
                $"SELECT QueueID, QueuedDateTime, CorrelationID{{0}} FROM {TableNameHelper.MetaDataName} WHERE Status = {Convert.ToInt32(QueueStatuses.Processing)} AND HeartBeat IS NOT NULL AND HeartBeat < @ThresholdTicks ORDER BY HeartBeat ASC LIMIT @PageSize OFFSET @Offset");

            CommandCache.Add(CommandStringTypes.GetDashboardMessageBody,
                $"SELECT Body, Headers FROM {TableNameHelper.QueueName} WHERE QueueID = @QueueId");

            CommandCache.Add(CommandStringTypes.GetDashboardMessageHeaders,
                $"SELECT Headers FROM {TableNameHelper.QueueName} WHERE QueueID = @QueueId");
        }
    }
}
