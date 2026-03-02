using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    public class GetDashboardMessageCountQueryTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new GetDashboardMessageCountQuery(null);
            Assert.Null(test.StatusFilter);
        }

        [Fact]
        public void Create_With_StatusFilter()
        {
            var test = new GetDashboardMessageCountQuery(1);
            Assert.Equal(1, test.StatusFilter);
        }
    }
}
