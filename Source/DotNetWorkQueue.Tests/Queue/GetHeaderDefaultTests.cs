using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Queue;
using Xunit;
namespace DotNetWorkQueue.Tests.Queue
{
    public class GetHeaderDefaultTests
    {
        [Fact()]
        public void GetHeaders_Test()
        {
            var header = new GetHeaderDefault();
            Assert.Throws<DotNetWorkQueueException>(() => header.GetHeaders(null));
        }
    }
}