using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    public class GetDashboardMessageDetailQueryTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new GetDashboardMessageDetailQuery(42);
            Assert.Equal(42, test.QueueId);
        }
    }
}
