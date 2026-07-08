using DotNetWorkQueue.Transport.Memory.Basic.QueryHandler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Tests.Basic.QueryHandler
{
    [TestClass]
    public class GetDashboardErrorMessagesQueryHandlerAsyncTests
    {
        [TestMethod]
        public void Create_Default()
        {
            Assert.IsNotNull(new GetDashboardErrorMessagesQueryHandlerAsync());
        }
    }
}
