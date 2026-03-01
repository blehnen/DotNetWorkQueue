using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using NSubstitute;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryPrepareHandler
{
    internal class FakeCommandStringCache : CommandStringCache
    {
        public FakeCommandStringCache() : base(Substitute.For<ITableNameHelper>())
        {
        }

        protected override void BuildCommands()
        {
            CommandCache[CommandStringTypes.GetDashboardStatusCounts] = "SELECT status counts";
            CommandCache[CommandStringTypes.GetDashboardMessages] = "SELECT messages{0} ORDER BY QueuedDateTime DESC";
            CommandCache[CommandStringTypes.GetDashboardMessageCount] = "SELECT COUNT(*) FROM meta";
            CommandCache[CommandStringTypes.GetDashboardMessageDetail] = "SELECT detail{0} WHERE QueueID = @QueueId";
            CommandCache[CommandStringTypes.GetDashboardStaleMessages] = "SELECT stale{0}";
            CommandCache[CommandStringTypes.GetDashboardErrorMessages] = "SELECT errors";
            CommandCache[CommandStringTypes.GetDashboardErrorMessageCount] = "SELECT COUNT(*) FROM errors";
            CommandCache[CommandStringTypes.GetDashboardErrorRetries] = "SELECT retries";
            CommandCache[CommandStringTypes.GetDashboardConfiguration] = "SELECT config";
            CommandCache[CommandStringTypes.GetDashboardJobs] = "SELECT jobs";
            CommandCache[CommandStringTypes.GetDashboardMessageBody] = "SELECT Body, Headers FROM meta WHERE QueueID = @QueueId";
            CommandCache[CommandStringTypes.GetDashboardMessageHeaders] = "SELECT Headers FROM meta WHERE QueueID = @QueueId";
            CommandCache[CommandStringTypes.DashboardDeleteAllErrors_MetaDataErrors] = "DELETE FROM MetaDataErrors";
            CommandCache[CommandStringTypes.DashboardDeleteAllErrors_ErrorTracking] = "DELETE FROM ErrorTracking";
            CommandCache[CommandStringTypes.DashboardDeleteAllErrors_Queue] = "DELETE FROM Queue";
            CommandCache[CommandStringTypes.DashboardDeleteAllErrors_Status] = "DELETE FROM Status";
            CommandCache[CommandStringTypes.DashboardDeleteAllErrors_MetaData] = "DELETE FROM MetaData WHERE Status = 2";
            CommandCache[CommandStringTypes.DashboardRequeueErrorMessage] = "UPDATE MetaData SET Status = 0 WHERE QueueID = @QueueId AND Status = 2";
            CommandCache[CommandStringTypes.DashboardRequeueStatusTable] = "UPDATE Status SET Status = 0 WHERE QueueID = @QueueId";
            CommandCache[CommandStringTypes.DashboardResetStaleMessage] = "UPDATE MetaData SET Status = 0, HeartBeat = NULL WHERE QueueID = @QueueId AND Status = 1";
            CommandCache[CommandStringTypes.DashboardResetStaleStatusTable] = "UPDATE Status SET Status = 0 WHERE QueueID = @QueueId AND Status = 1";
            CommandCache[CommandStringTypes.DashboardUpdateMessageBody] = "UPDATE Queue SET Body = @Body, Headers = @Headers WHERE QueueID = @QueueId";
        }
    }
}
