using DotNetWorkQueue.Queue;
using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class ClearExpiredMessagesRpcMonitorNoOpTests
    {
        [Fact]
        public void Create()
        {
            var test = new ClearExpiredMessagesRpcMonitorNoOp();
            Assert.NotNull(test);
        }
    }
}
