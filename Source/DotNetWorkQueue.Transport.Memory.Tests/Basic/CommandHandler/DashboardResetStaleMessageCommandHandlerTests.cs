using DotNetWorkQueue.Transport.Memory.Basic.CommandHandler;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Tests.Basic.CommandHandler
{
    [TestClass]
    public class DashboardResetStaleMessageCommandHandlerTests
    {
        [TestMethod]
        public void Create_Default()
        {
            Assert.IsNotNull(new DashboardResetStaleMessageCommandHandler());
        }
    }
}
