using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    public class GetDashboardErrorRetriesQueryTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new GetDashboardErrorRetriesQuery(99);
            Assert.Equal(99, test.QueueId);
        }
    }
}
