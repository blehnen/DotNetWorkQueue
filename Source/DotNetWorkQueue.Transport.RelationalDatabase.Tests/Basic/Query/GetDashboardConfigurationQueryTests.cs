using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    [TestClass]
    public class GetDashboardConfigurationQueryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = new GetDashboardConfigurationQuery();
            Assert.IsNotNull(test);
        }
    }
}
