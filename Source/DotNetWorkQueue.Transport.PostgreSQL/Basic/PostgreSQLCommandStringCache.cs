// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <inheritdoc />
    public class PostgreSqlCommandStringCache : CommandStringCache
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlCommandStringCache" /> class.
        /// </summary>
        /// <param name="tableNameHelper">The table name helper.</param>
        public PostgreSqlCommandStringCache(ITableNameHelper tableNameHelper) : base(tableNameHelper)
        {
        }

        /// <inheritdoc />
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
                $"Insert into {TableNameHelper.QueueName} (Body, Headers) VALUES (@Body, @Headers); SELECT lastval(); ");

            CommandCache.Add(CommandStringTypes.UpdateErrorCount,
                $"update {TableNameHelper.ErrorTrackingName} set retrycount = retrycount + 1 where queueid = @queueid and ExceptionType = @ExceptionType");

            CommandCache.Add(CommandStringTypes.InsertErrorCount,
                $"Insert into {TableNameHelper.ErrorTrackingName} (QueueID,ExceptionType, RetryCount) VALUES (@QueueID,@ExceptionType,1)");

            CommandCache.Add(CommandStringTypes.GetHeartBeatExpiredMessageIds,
                $"select {TableNameHelper.MetaDataName}.queueid, heartbeat, headers from {TableNameHelper.MetaDataName} inner join {TableNameHelper.QueueName} on {TableNameHelper.QueueName}.queueid = {TableNameHelper.MetaDataName}.queueid where status = @status and heartbeat is not null and heartbeat < @time FOR UPDATE SKIP LOCKED");

            CommandCache.Add(CommandStringTypes.GetColumnNamesFromTable,
                "select column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name = @TableName");

            CommandCache.Add(CommandStringTypes.GetErrorRecordExists,
                $"Select 1 from {TableNameHelper.ErrorTrackingName} where queueid = @queueid and ExceptionType = @ExceptionType");

            CommandCache.Add(CommandStringTypes.GetErrorRetryCount,
                $"Select RetryCount from {TableNameHelper.ErrorTrackingName} where queueid = @queueid and ExceptionType = @ExceptionType");

            CommandCache.Add(CommandStringTypes.GetMessageErrors,
                $"Select ExceptionType, RetryCount from {TableNameHelper.ErrorTrackingName} where queueid = @queueid");

            CommandCache.Add(CommandStringTypes.GetConfiguration,
                $"select Configuration from {TableNameHelper.ConfigurationName}");

            CommandCache.Add(CommandStringTypes.GetTableExists,
                "SELECT 1 FROM pg_catalog.pg_class c JOIN  pg_catalog.pg_namespace n ON n.oid = c.relnamespace WHERE n.nspname = 'public' AND  c.relname = @Table;");

            CommandCache.Add(CommandStringTypes.GetUtcDate,
                "select now() at time zone 'utc'");

            CommandCache.Add(CommandStringTypes.GetPendingExcludeDelayCount,
                $"Select count(queueid) from {TableNameHelper.MetaDataName} where status = {Convert.ToInt32(QueueStatuses.Waiting)} AND (QueueProcessTime < @CurrentDate)");

            CommandCache.Add(CommandStringTypes.GetPendingCount,
                $"Select count(queueid) from {TableNameHelper.StatusName} where status = {Convert.ToInt32(QueueStatuses.Waiting)} ");

            CommandCache.Add(CommandStringTypes.GetWorkingCount,
                $"Select count(queueid) from {TableNameHelper.StatusName} where status = {Convert.ToInt32(QueueStatuses.Processing)} ");

            CommandCache.Add(CommandStringTypes.GetErrorCount,
                $"Select count(queueid) from {TableNameHelper.StatusName} where status = {Convert.ToInt32(QueueStatuses.Error)} ");

            CommandCache.Add(CommandStringTypes.GetPendingDelayCount,
                $"Select count(queueid) from {TableNameHelper.MetaDataName} where status = {Convert.ToInt32(QueueStatuses.Waiting)} AND (QueueProcessTime > @CurrentDate) ");

            CommandCache.Add(CommandStringTypes.GetJobLastKnownEvent,
                $"Select JobEventTime from {TableNameHelper.JobTableName} where JobName = @JobName");

            CommandCache.Add(CommandStringTypes.SetJobLastKnownEvent,
                $"Insert into {TableNameHelper.JobTableName} (JobName, JobEventTime, JobScheduledTime) values (@JobName, @JobEventTime, @JobScheduledTime) on conflict (JobName) do update set (JobEventTime, JobScheduledTime) = (@JobEventTime, @JobScheduledTime) where {TableNameHelper.JobTableName}.JobName = @JobName");

            CommandCache.Add(CommandStringTypes.DoesJobExist,
                $"Select Status from {TableNameHelper.StatusName} where JobName = @JobName");

            CommandCache.Add(CommandStringTypes.GetJobId,
                $"Select QueueID from {TableNameHelper.StatusName} where JobName = @JobName");

            CommandCache.Add(CommandStringTypes.GetJobLastScheduleTime,
                $"Select JobScheduledTime from {TableNameHelper.JobTableName} where JobName = @JobName");

            CommandCache.Add(CommandStringTypes.FindExpiredRecordsWithStatusToDelete,
                $"select queueid from {TableNameHelper.MetaDataName} where status = {Convert.ToInt16(QueueStatuses.Waiting)} and @CurrentDate > ExpirationTime FOR UPDATE SKIP LOCKED");

            CommandCache.Add(CommandStringTypes.FindErrorRecordsToDelete,
                $"select queueid from {TableNameHelper.MetaDataErrorsName} where @CurrentDate > lastexceptiondate FOR UPDATE SKIP LOCKED");

            CommandCache.Add(CommandStringTypes.GetQueueCountStatus,
                $"select count(queueid) from {TableNameHelper.MetaDataName} where status = @status");

            CommandCache.Add(CommandStringTypes.GetQueueCountAll,
                $"select count(queueid) from {TableNameHelper.MetaDataName}");

            CommandCache.Add(CommandStringTypes.FindExpiredRecordsToDelete,
                $"select queueid from {TableNameHelper.MetaDataName} where @CurrentDate > ExpirationTime FOR UPDATE SKIP LOCKED");

            CommandCache.Add(CommandStringTypes.DeleteTable,
                "DROP TABLE IF EXISTS {0} CASCADE;");

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
                $"SELECT QueueID, QueuedDateTime, CorrelationID{{0}} FROM {TableNameHelper.MetaDataName} WHERE QueueID = @QueueID");

            CommandCache.Add(CommandStringTypes.GetDashboardStaleMessages,
                $"SELECT QueueID, QueuedDateTime, CorrelationID{{0}} FROM {TableNameHelper.MetaDataName} WHERE Status = {Convert.ToInt32(QueueStatuses.Processing)} AND HeartBeat IS NOT NULL AND HeartBeat < @ThresholdTicks ORDER BY HeartBeat ASC LIMIT @PageSize OFFSET @Offset");

            CommandCache.Add(CommandStringTypes.GetDashboardMessageBody,
                $"SELECT Body, Headers FROM {TableNameHelper.QueueName} WHERE QueueID = @QueueID");

            CommandCache.Add(CommandStringTypes.GetDashboardMessageHeaders,
                $"SELECT Headers FROM {TableNameHelper.QueueName} WHERE QueueID = @QueueID");

            // Dashboard write commands
            CommandCache.Add(CommandStringTypes.DashboardDeleteAllErrors_MetaDataErrors,
                $"DELETE FROM {TableNameHelper.MetaDataErrorsName} WHERE QueueID IN (SELECT QueueID FROM {TableNameHelper.MetaDataName} WHERE Status = {Convert.ToInt32(QueueStatuses.Error)})");

            CommandCache.Add(CommandStringTypes.DashboardDeleteAllErrors_ErrorTracking,
                $"DELETE FROM {TableNameHelper.ErrorTrackingName} WHERE QueueID IN (SELECT QueueID FROM {TableNameHelper.MetaDataName} WHERE Status = {Convert.ToInt32(QueueStatuses.Error)})");

            CommandCache.Add(CommandStringTypes.DashboardDeleteAllErrors_Queue,
                $"DELETE FROM {TableNameHelper.QueueName} WHERE QueueID IN (SELECT QueueID FROM {TableNameHelper.MetaDataName} WHERE Status = {Convert.ToInt32(QueueStatuses.Error)})");

            CommandCache.Add(CommandStringTypes.DashboardDeleteAllErrors_Status,
                $"DELETE FROM {TableNameHelper.StatusName} WHERE QueueID IN (SELECT QueueID FROM {TableNameHelper.MetaDataName} WHERE Status = {Convert.ToInt32(QueueStatuses.Error)})");

            CommandCache.Add(CommandStringTypes.DashboardDeleteAllErrors_MetaData,
                $"DELETE FROM {TableNameHelper.MetaDataName} WHERE Status = {Convert.ToInt32(QueueStatuses.Error)}");

            CommandCache.Add(CommandStringTypes.DashboardRequeueErrorMessage,
                $"UPDATE {TableNameHelper.MetaDataName} SET Status = {Convert.ToInt32(QueueStatuses.Waiting)}, HeartBeat = NULL WHERE QueueID = @QueueID AND Status = {Convert.ToInt32(QueueStatuses.Error)}");

            CommandCache.Add(CommandStringTypes.DashboardRequeueStatusTable,
                $"UPDATE {TableNameHelper.StatusName} SET Status = {Convert.ToInt32(QueueStatuses.Waiting)} WHERE QueueID = @QueueID");

            CommandCache.Add(CommandStringTypes.DashboardResetStaleMessage,
                $"UPDATE {TableNameHelper.MetaDataName} SET Status = {Convert.ToInt32(QueueStatuses.Waiting)}, HeartBeat = NULL WHERE QueueID = @QueueID AND Status = {Convert.ToInt32(QueueStatuses.Processing)}");

            CommandCache.Add(CommandStringTypes.DashboardResetStaleStatusTable,
                $"UPDATE {TableNameHelper.StatusName} SET Status = {Convert.ToInt32(QueueStatuses.Waiting)} WHERE QueueID = @QueueID");

            CommandCache.Add(CommandStringTypes.DashboardUpdateMessageBody,
                $"UPDATE {TableNameHelper.QueueName} SET Body = @Body, Headers = @Headers WHERE QueueID = @QueueID");
        }
    }
}
