using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    [TestClass]
    public class GetDashboardJobsQueryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = new GetDashboardJobsQuery();
            Assert.IsNotNull(test);
        }
    }
}
