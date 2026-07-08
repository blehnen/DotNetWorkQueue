using DotNetWorkQueue.Transport.Memory.Basic.QueryHandler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Tests.Basic.QueryHandler
{
    [TestClass]
    public class GetDashboardErrorMessageCountQueryHandlerAsyncTests
    {
        [TestMethod]
        public void Create_Default()
        {
            Assert.IsNotNull(new GetDashboardErrorMessageCountQueryHandlerAsync());
        }
    }
}
