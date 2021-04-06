using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    public class GetErrorRecordExistsQueryTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new GetErrorRecordExistsQuery<long>("test", 100);
            Assert.Equal("test", test.ExceptionType);
            Assert.Equal(100, test.QueueId);
        }
    }
}
