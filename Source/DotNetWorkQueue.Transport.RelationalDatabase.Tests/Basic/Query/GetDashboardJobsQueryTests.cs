using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    public class GetDashboardJobsQueryTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new GetDashboardJobsQuery();
            Assert.NotNull(test);
        }
    }
}
