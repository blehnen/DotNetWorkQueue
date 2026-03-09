using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    [TestClass]
    public class GetDashboardMessageCountQueryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = new GetDashboardMessageCountQuery(null);
            Assert.IsNull(test.StatusFilter);
        }

        [TestMethod]
        public void Create_With_StatusFilter()
        {
            var test = new GetDashboardMessageCountQuery(1);
            Assert.AreEqual(1, test.StatusFilter);
        }
    }
}
