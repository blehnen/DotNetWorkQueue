using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    [TestClass]
    public class GetDashboardStatusCountsQueryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = new GetDashboardStatusCountsQuery();
            Assert.IsNotNull(test);
        }
    }
}
