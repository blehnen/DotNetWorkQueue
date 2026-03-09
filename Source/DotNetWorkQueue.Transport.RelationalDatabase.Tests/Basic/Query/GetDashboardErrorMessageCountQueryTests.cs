using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    [TestClass]
    public class GetDashboardErrorMessageCountQueryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = new GetDashboardErrorMessageCountQuery();
            Assert.IsNotNull(test);
        }
    }
}
