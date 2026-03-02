using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    public class GetDashboardStaleMessagesQueryTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new GetDashboardStaleMessagesQuery(60, 0, 25);
            Assert.Equal(60, test.ThresholdSeconds);
            Assert.Equal(0, test.PageIndex);
            Assert.Equal(25, test.PageSize);
        }
    }
}
