using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    public class GetDashboardMessagesQueryTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new GetDashboardMessagesQuery(0, 25, null);
            Assert.Equal(0, test.PageIndex);
            Assert.Equal(25, test.PageSize);
            Assert.Null(test.StatusFilter);
        }

        [Fact]
        public void Create_With_StatusFilter()
        {
            var test = new GetDashboardMessagesQuery(2, 50, 1);
            Assert.Equal(2, test.PageIndex);
            Assert.Equal(50, test.PageSize);
            Assert.Equal(1, test.StatusFilter);
        }
    }
}
