using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    public class GetDashboardErrorMessageCountQueryTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new GetDashboardErrorMessageCountQuery();
            Assert.NotNull(test);
        }
    }
}
