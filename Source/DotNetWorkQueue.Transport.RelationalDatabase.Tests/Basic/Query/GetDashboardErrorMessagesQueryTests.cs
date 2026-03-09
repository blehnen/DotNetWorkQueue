using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    [TestClass]
    public class GetDashboardErrorMessagesQueryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = new GetDashboardErrorMessagesQuery(0, 25);
            Assert.AreEqual(0, test.PageIndex);
            Assert.AreEqual(25, test.PageSize);
        }
    }
}
