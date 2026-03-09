using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    [TestClass]
    public class GetDashboardMessagesQueryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = new GetDashboardMessagesQuery(0, 25, null);
            Assert.AreEqual(0, test.PageIndex);
            Assert.AreEqual(25, test.PageSize);
            Assert.IsNull(test.StatusFilter);
        }

        [TestMethod]
        public void Create_With_StatusFilter()
        {
            var test = new GetDashboardMessagesQuery(2, 50, 1);
            Assert.AreEqual(2, test.PageIndex);
            Assert.AreEqual(50, test.PageSize);
            Assert.AreEqual(1, test.StatusFilter);
        }
    }
}
