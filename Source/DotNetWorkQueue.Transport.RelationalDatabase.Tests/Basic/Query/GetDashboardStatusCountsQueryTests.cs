using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    public class GetDashboardStatusCountsQueryTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new GetDashboardStatusCountsQuery();
            Assert.NotNull(test);
        }
    }
}
