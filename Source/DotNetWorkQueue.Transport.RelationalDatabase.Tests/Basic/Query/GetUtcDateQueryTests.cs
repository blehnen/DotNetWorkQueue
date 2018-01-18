using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    public class GetUtcDateQueryTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new GetUtcDateQuery("test");
            Assert.Equal("test", test.ConnectionString);
        }
    }
}
