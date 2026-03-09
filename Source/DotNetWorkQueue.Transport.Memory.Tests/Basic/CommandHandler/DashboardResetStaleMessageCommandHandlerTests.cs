using System;
using DotNetWorkQueue.Transport.Memory;
using DotNetWorkQueue.Transport.Memory.Basic.CommandHandler;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Tests.Basic.CommandHandler
{
    [TestClass]
    public class DashboardResetStaleMessageCommandHandlerTests
    {
        [TestMethod]
        public void Create_Default()
        {
            Assert.IsNotNull(new DashboardResetStaleMessageCommandHandler(Substitute.For<IDataStorage>()));
        }

        [TestMethod]
        public void Create_NullDataStorage_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new DashboardResetStaleMessageCommandHandler(null));
        }
    }
}
