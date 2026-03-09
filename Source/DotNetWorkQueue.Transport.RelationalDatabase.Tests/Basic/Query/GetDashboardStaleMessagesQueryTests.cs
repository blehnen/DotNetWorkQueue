using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    [TestClass]
    public class GetDashboardStaleMessagesQueryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = new GetDashboardStaleMessagesQuery(60, 0, 25);
            Assert.AreEqual(60, test.ThresholdSeconds);
            Assert.AreEqual(0, test.PageIndex);
            Assert.AreEqual(25, test.PageSize);
        }
    }
}
