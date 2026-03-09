using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    [TestClass]
    public class GetErrorRetryCountQueryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = new GetErrorRetryCountQuery<long>("test", 100);
            Assert.AreEqual("test", test.ExceptionType);
            Assert.AreEqual(100, test.QueueId);
        }
    }
}
