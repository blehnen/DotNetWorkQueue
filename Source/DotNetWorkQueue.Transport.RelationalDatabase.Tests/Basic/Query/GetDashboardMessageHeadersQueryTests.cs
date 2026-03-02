using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    public class GetDashboardMessageHeadersQueryTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new GetDashboardMessageHeadersQuery("42");
            Assert.Equal("42", test.MessageId);
        }
    }
}
