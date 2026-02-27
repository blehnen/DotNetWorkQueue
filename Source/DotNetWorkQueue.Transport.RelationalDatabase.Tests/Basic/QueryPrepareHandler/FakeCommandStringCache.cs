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
        }
    }
}
