using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Queue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class GetHeaderDefaultTests
    {
        [TestMethod]
        public void GetHeaders_Test()
        {
            var header = new GetHeaderDefault();
            Assert.ThrowsExactly<DotNetWorkQueueException>(() => header.GetHeaders(null));
        }
    }
}