using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    public class GetDashboardErrorMessagesQueryTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new GetDashboardErrorMessagesQuery(0, 25);
            Assert.Equal(0, test.PageIndex);
            Assert.Equal(25, test.PageSize);
        }
    }
}
